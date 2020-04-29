using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static HoppieAcarsClient.AcarsClient;

namespace HoppieAcarsClient
{
    public class CpdlcAcarsMessage : AcarsMessage
    {
        public ResponseAttribute ResponseType { get; private set; }
        public CPDLCUplinkMessageType UplinkMessageType { get; private set; }
        public int MessageCount { get; private set; }
        public string Message { get; private set; }

        /// <summary>
        /// Use to create a new CPDLC Message object
        /// </summary>
        /// <param name="recievedAt">When the message was recieved by the client (not when it was created or sent from the sender station)</param>
        /// <param name="from">Callsign from which the message was recieved</param>
        /// <param name="to">Callsign to which the message was addressed</param>
        /// <param name="responseType">Expected response type</param>
        /// <param name="messageCount">Message counter, increases for every new message sent</param>
        /// <param name="message">The ATC instruction</param>
        public CpdlcAcarsMessage(
            string from,
            string to,
            ResponseAttribute responseType,
            int messageCount,
            CPDLCUplinkMessageType messageType) :
                base(
                    DateTime.Now,
                    from,
                    to,
                    MessageType.CPDLC,
                    GetDataString(messageCount, responseType, messageType))
        {
            ResponseType = responseType;
            MessageCount = messageCount;
            UplinkMessageType = messageType;
            Message = getAtcMessage(messageType);
        }

        /// <summary>
        /// Use to create a new CPDLC Message object
        /// </summary>
        /// <param name="recievedAt">When the message was recieved by the client (not when it was created or sent from the sender station)</param>
        /// <param name="from">Callsign from which the message was recieved</param>
        /// <param name="to">Callsign to which the message was addressed</param>
        /// <param name="responseType">Expected response type</param>
        /// <param name="messageCount">Message counter, increases for every new message sent</param>
        /// <param name="message">The ATC instruction</param>
        public CpdlcAcarsMessage(
            DateTime recievedAt,
            string from,
            string to,
            ResponseAttribute responseType,
            int messageCount,
            string message) : 
                base(
                    recievedAt, 
                    from, 
                    to, 
                    MessageType.CPDLC, 
                    GetDataString(messageCount, responseType, message))
        {
            ResponseType = responseType;
            UplinkMessageType = CPDLCUplinkMessageType.NONE;
            MessageCount = messageCount;
            Message = message;
        }

        /// <summary>
        /// Use to create a new CPDLC Message object
        /// </summary>
        /// <param name="recievedAt">When the message was recieved by the client (not when it was created or sent from the sender station)</param>
        /// <param name="from">Callsign from which the message was recieved</param>
        /// <param name="to">Callsign to which the message was addressed</param>
        /// <param name="responseType">Expected response type</param>
        /// <param name="messageCount">Message counter, increases for every new message sent</param>
        /// <param name="message">The ATC instruction</param>
        public CpdlcAcarsMessage(
            DateTime recievedAt,
            string from,
            string to,
            string rawDataMessage) :
                base(
                    recievedAt,
                    from,
                    to,
                    MessageType.CPDLC,
                    rawDataMessage)
        {
            ResponseType = getResponseType(rawDataMessage);
            UplinkMessageType = CPDLCUplinkMessageType.NONE;
            MessageCount = getMessageCount(rawDataMessage);
            Message = getAtcMessage(rawDataMessage);
        }

        private static string GetDataString(
            int messageCount,
            ResponseAttribute responseType,
            CPDLCUplinkMessageType messageType)
        {            
            return GetDataString(messageCount, responseType, getAtcMessage(messageType));
        }

        private static string GetDataString(
            int messageCount,
            ResponseAttribute responseType,
            string message)
        {
            // Example: "/data2/17//WU/CROSS @SPL@ AT @5000"
            string dataString = String.Format("/data2/{0}//{1}/{2}", (messageCount > 0 ? messageCount.ToString() : ""), responseAttributeStrings[responseType], message);

            return dataString;
        }

        private static readonly Regex cpdlcDataRegex = new Regex(@"\/data2\/(\d*)\/(.*?)\/(\w{1,2})\/(.*)");

        private static ResponseAttribute getResponseType(string rawMessage)
        {
            Match match = cpdlcDataRegex.Match(rawMessage);
            ResponseAttribute responseType = responseAttributeStrings.FirstOrDefault(x => x.Value == match.Groups[3].Value).Key;
            return responseType;
        }

        private static int getMessageCount(string rawMessage)
        {
            Match match = cpdlcDataRegex.Match(rawMessage);
            return int.Parse(match.Groups[1].Value);
        }

        private static string getAtcMessage(string rawMessage)
        {
            Match match = cpdlcDataRegex.Match(rawMessage);
            return match.Groups[4].Value;
        }

        private static string getAtcMessage(CPDLCUplinkMessageType messageType)
        {
            return messageType.ToString().Replace('_', ' '); //TODO: Add support for more types
        }

        private static readonly Dictionary<ResponseAttribute, string> responseAttributeStrings = new Dictionary<ResponseAttribute, string>
        {
            { ResponseAttribute.UPLINK_WILCO_UNABLE, "WU" },
            { ResponseAttribute.UPLINK_AFFIRM_NEGATIVE, "AN" },
            { ResponseAttribute.UPLINK_ROGER, "R" },
            { ResponseAttribute.UPLINK_NOT_ENABLED, "NE" },
            { ResponseAttribute.DOWNLINK_RESPONSE_REQUIRED, "Y" },
            { ResponseAttribute.DOWNLINK_RESPONSE_NOT_REQUIRED, "N" }
        };

        public string RawDataMessage
        {
            get
            {
                return GetDataString(MessageCount, ResponseType, UplinkMessageType);
            }
        }

        public enum CPDLCUplinkMessageType
        {
            NONE = 0,
            REQUEST_LOGON,

            WILCO,
            UNABLE,
            AFFIRM,
            NEGATIVE,
            ROGER
        }

        public enum ResponseAttribute
        {
            UPLINK_WILCO_UNABLE = 1,
            UPLINK_AFFIRM_NEGATIVE = 2,
            UPLINK_ROGER = 3,
            UPLINK_NOT_ENABLED = 4,

            DOWNLINK_RESPONSE_REQUIRED = 10,
            DOWNLINK_RESPONSE_NOT_REQUIRED = 11
        }
    }
}
