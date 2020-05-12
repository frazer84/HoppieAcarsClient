using System;

namespace HoppieAcarsClient
{
    public class AcarsMessageEventArgs : EventArgs
    {
        public AcarsMessage[] NewMessages { get; private set; }

        public AcarsMessage[] MessageHistory { get; private set; }

        public AcarsMessageEventArgs(AcarsMessage[] newMessages, AcarsMessage[] history)
        {
            NewMessages = newMessages;
            MessageHistory = history;
        }
    }
}
