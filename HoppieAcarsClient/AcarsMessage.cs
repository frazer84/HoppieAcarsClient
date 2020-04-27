using static HoppieAcarsClient.AcarsClient;

namespace HoppieAcarsClient
{
    public class AcarsMessage
    {
        public string From;
        public string To;
        public MessageType Type;
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
