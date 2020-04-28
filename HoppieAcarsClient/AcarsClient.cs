using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HoppieAcarsClient
{
    public class AcarsClient : IDisposable
    {
        private const string HOPPIE_URL_CONNECT = "https://www.hoppie.nl/acars/system/connect.html";
        private string callsign;
        private string logonSecret = null;
        private int messageCounter = 0;

        HttpClient httpClient;
        Thread messagePollThread;
        private Regex messageRegex = new Regex(@"\{(\S*)\s(\S*)\s\{(\/\S*\/|TELEX\s)([^\}]*)\}\}");

        /// <summary>
        /// Event is triggered when automatic polling of new messages gets at least one message
        /// </summary>
        public event EventHandler<AcarsMessageEventArgs> MessageRecieved;

        /// <summary>
        /// List of all messages recieved since client instance was created
        /// </summary>
        public List<AcarsMessage> MessageHistory
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callsign">The callsign to be used then sending and recieving messages</param>
        /// <param name="logonSecret">The hopplie secret token you get when registering on www.hoppie.nl</param>
        /// <param name="pollForMessages">Controls if the acars client should start listening to new messaged automatically, triggering the MessageRecieved event</param>
        /// <param name="httpClient">ONLY FOR UNIT TEST USE</param>
        public AcarsClient(string callsign, string logonSecret, bool pollForMessages = true, HttpClient httpClient = null)
        {
            if (logonSecret == null || logonSecret.Length < 6)
                throw new ArgumentException("Invalid logon secret.");
            if (callsign == null || callsign.Length < 3)
                throw new ArgumentException("Invalid callsign.");

            this.callsign = callsign;
            this.logonSecret = logonSecret;

            if (httpClient != null)
            {
                // Unit test is mocking the http client
                this.httpClient = httpClient;
            }
            else
            { 
                // Normal http client implementation (non-mocked)
                this.httpClient = new HttpClient();
                this.httpClient.Timeout = TimeSpan.FromSeconds(5);
            }

            messagePollThread = new Thread(MessagePollRunner);
            if(pollForMessages)
                messagePollThread.Start();
        }

        #region Enums
        public enum MessageType
        {
            Progress,
            CPDLC,
            Telex,
            Ping,
            PositionRequest,
            Position,
            DataRequest,
            Poll,
            Peek
        }

        private readonly Dictionary<MessageType, string> messageTypeStrings = new Dictionary<MessageType, string>
        {
            { MessageType.CPDLC, "cpdlc" },
            { MessageType.DataRequest, "datareq" },
            { MessageType.Peek, "peek" },
            { MessageType.Ping, "ping" },
            { MessageType.Poll, "poll" },
            { MessageType.Position, "position" },
            { MessageType.PositionRequest, "posreq" },
            { MessageType.Progress, "progress" },
            { MessageType.Telex, "telex" }
        };

        public enum CPDLCMessage
        {
            REQUEST_LOGON
        }
        #endregion

        #region Message polling thread
        private async void MessagePollRunner()
        {
            while (true)
            {
                try
                {
                    AcarsMessage[] messages = await pollMessages();
                    if(messages != null && messages.Length > 0)
                    {
                        EventHandler<AcarsMessageEventArgs> handler = MessageRecieved;
                        handler?.Invoke(this, new AcarsMessageEventArgs(messages));
                    }
                    Thread.Sleep(5000);
                }
                catch(ThreadAbortException)
                {
                    return;
                }
            }
        }

        private async Task<AcarsMessage[]> pollMessages()
        {
            return await GetPendingMessages(callsign).ConfigureAwait(false);
        }
        #endregion

        public async Task<AcarsMessage[]> GetPendingMessages()
        {
            return await GetPendingMessages(callsign).ConfigureAwait(false);
        }

        public async Task<AcarsMessage[]> GetPendingMessages(string callsign)
        {
            List<AcarsMessage> messages = new List<AcarsMessage>();
            try
            {
                string response = await sendMessageToHoppie(
                    logonSecret,
                    callsign,
                    "SERVER",
                    MessageType.Poll,
                    ""
                ).ConfigureAwait(false);

                MatchCollection matches = messageRegex.Matches(response);
                foreach (Match match in matches)
                {
                    MessageType messageType = messageTypeStrings.FirstOrDefault(x => x.Value == match.Groups[2].Value).Key;
                    string data = match.Groups[3].Value;
                    if (messageType == MessageType.CPDLC && match.Groups.Count > 3)
                        data += match.Groups[4].Value;
                    if(messageType == MessageType.Telex && match.Groups.Count > 3)
                        data = match.Groups[4].Value;
                    AcarsMessage acarsMessage = new AcarsMessage(match.Groups[1].Value, callsign, messageType, data);
                    messages.Add(acarsMessage);
                }
                MessageHistory.AddRange(messages);
                return messages.ToArray();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Sends a request to the Hoppie ACARS server to return all callsigns that is online on the ACARS network right now
        /// </summary>
        /// <returns>Array of strings representing the callsigns that is currently online on the ACARS network</returns>
        public async Task<string[]> GetAllCallsignsOnline()
        {
            try
            {
                string response = await sendMessageToHoppie(
                    logonSecret,
                    "TEST",
                    "TEST",
                    MessageType.Ping,
                    "ALL-CALLSIGNS"
                ).ConfigureAwait(false);

                return response.Trim('{', '}', ' ').Split(' ');

            }
            catch(Exception e)
            {
                throw e;
            }
        }

        public async Task<string> SendCPDLC(
            string fromCallsign,
            string toCallsign,
            CPDLCMessage message)
        {
            try
            {
                string data = getCpdlcData(message);
                if (data != null)
                {
                    string response = await sendMessageToHoppie(
                        logonSecret,
                        fromCallsign,
                        toCallsign,
                        MessageType.CPDLC,
                        data
                    ).ConfigureAwait(false);

                    return response;
                }
                else
                {
                    throw new Exception("Could not create valid CPDLC data based on input parameters");
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string getCpdlcData(
            CPDLCMessage message)
        {
            string messageString = null;
            switch (message)
            {
                case CPDLCMessage.REQUEST_LOGON:
                    messageString = "REQUEST LOGON";
                    break;
            }

            if (messageString != null)
            {

                string data = "/data2/0/";
                if (messageCounter > 0)
                    data += messageCounter;
                data += "/Y/";
                data += messageString;
                messageCounter++;

                return data;
            }
            throw new ArgumentException("Invalid CPDLC message type");
        }

        public async Task<string> SendTelex(
            string fromCallsign,
            string toCallsign,
            string messageText)
        {
            try
            {
                string response = await sendMessageToHoppie(
                    logonSecret,
                    fromCallsign,
                    toCallsign,
                    MessageType.Telex,
                    messageText
                ).ConfigureAwait(false);

                return response;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task<string> SendDataRequest(
            string fromCallsign,
            string toCallsign)
        {
            try
            {
                string response = await sendMessageToHoppie(
                    logonSecret,
                    fromCallsign,
                    toCallsign,
                    MessageType.DataRequest,
                    ""
                ).ConfigureAwait(false);

                return response;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private async Task<string> sendMessageToHoppie(
            string logonSecret,
            string fromCallsign,
            string toCallsign,
            MessageType messageType,
            string packetData)
        {
            try
            {
                string response = await httpClient.GetStringAsync(
                    getHoppieUrl(
                        logonSecret,
                        fromCallsign,
                        toCallsign,
                        messageType,
                        packetData
                    )
                ).ConfigureAwait(false);

                if (response.StartsWith("ok"))
                {
                    // Valid response
                    if(response.Contains("{"))
                        return response.Substring(3);
                    return response;
                }
                else if (response.StartsWith("error"))
                {
                    string errorMessage = response.Substring(6).Trim('{', '}', ' ');
                    throw new Exception("Hoppie server error: " + errorMessage);
                }
                else
                {
                    throw new Exception("Got unknown response from Hoppie Server: " + response);
                }
            }
            catch (HttpRequestException e)
            {
                throw new Exception("Error communicating with Hoppie server over HTTP: " + e.Message, e);
            }
        }

        private string getHoppieUrl(
            string logonSecret,
            string fromCallsign,
            string toCallsign,
            MessageType messageType,
            string packetData)
        {
            NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryString.Add("logon", logonSecret);
            queryString.Add("from", fromCallsign);
            queryString.Add("to", toCallsign);
            queryString.Add("type", messageTypeStrings[messageType]);
            queryString.Add("packet", packetData);
            return HOPPIE_URL_CONNECT + "?" + queryString.ToString();
        }

        /*private string getMessageTypeString(MessageType type)
        {
            switch(type)
            {
                case MessageType.CPDLC:
                    return "cpdlc";
                case MessageType.DataRequest:
                    return "datareq";
                case MessageType.Peek:
                    return "peek";
                case MessageType.Ping:
                    return "ping";
                case MessageType.Poll:
                    return "poll";
                case MessageType.Position:
                    return "position";
                case MessageType.PositionRequest:
                    return "posreq";
                case MessageType.Progress:
                    return "progress";
                case MessageType.Telex:
                    return "telex";
            }
            throw new NotSupportedException("MessageType " + type.ToString() + " not supported.");
        }*/

        public void Dispose()
        {
            if(httpClient != null)
                httpClient.Dispose();
            if(messagePollThread != null)
                messagePollThread.Abort();
        }
    }
}
