using System;
using static HoppieAcarsClient.AcarsClient;

namespace HoppieAcarsClient
{
    /// <summary>
    /// Class containing a single ACARS message
    /// </summary>
    public class AcarsMessage
    {
        public Guid Id { get; private set; }
        /// <summary>
        /// When the message was recieved by the client (not when it was created or sent from the sender station)
        /// </summary>
        public DateTime RecievedAt { get; private set; }

        /// <summary>
        /// Callsign from which the message was recieved
        /// </summary>
        public string From { get; private set; }

        /// <summary>
        /// Callsign to which the message was addressed
        /// </summary>
        public string To { get; private set; }

        /// <summary>
        /// Type of ACARS message
        /// </summary>
        public MessageType Type { get; private set; }

        /// <summary>
        /// Data contained in the message
        /// </summary>
        public string Data { get; private set; }

        /// <summary>
        /// Use to create a generic ACARS message
        /// </summary>
        /// <param name="recievedAt">When the message was recieved by the client (not when it was created or sent from the sender station)</param>
        /// <param name="from">Callsign from which the message was recieved</param>
        /// <param name="to">Callsign to which the message was addressed</param>
        /// <param name="type">Type of ACARS message</param>
        /// <param name="data">Data contained in the message</param>
        public AcarsMessage(
            DateTime recievedAt,
            string from,
            string to,
            MessageType type,
            string data)
        {
            RecievedAt = recievedAt;
            From = from;
            To = to;
            Type = type;
            Data = data;
            Id = Guid.NewGuid();
        }

        public override string ToString()
        {
            return string.Format("From: {0}, To: {1}, Type: {2}, Data: {3}", From, To, Type.ToString(), Data);
        }
    }
}
