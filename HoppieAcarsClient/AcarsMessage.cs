using static HoppieAcarsClient.AcarsClient;

namespace HoppieAcarsClient
{
    /// <summary>
    /// Class containing a single ACARS message
    /// </summary>
    public class AcarsMessage
    {
        /// <summary>
        /// Callsign from which the message was recieved
        /// </summary>
        public string From;

        /// <summary>
        /// Callsign to which the message was addressed
        /// </summary>
        public string To;

        /// <summary>
        /// Type of ACARS message
        /// </summary>
        public MessageType Type;

        /// <summary>
        /// Data contained in the message
        /// </summary>
        public string Data;

        public AcarsMessage(
            string from,
            string to,
            MessageType type,
            string data)
        {
            From = from;
            To = to;
            Type = type;
            Data = data;
        }

        public override string ToString()
        {
            return string.Format("From: {0}, To: {1}, Type: {2}, Data: {3}", From, To, Type.ToString(), Data);
        }
    }
}
