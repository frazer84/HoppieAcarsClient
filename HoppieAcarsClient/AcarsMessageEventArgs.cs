using System;

namespace HoppieAcarsClient
{
    public class AcarsMessageEventArgs : EventArgs
    {
        public AcarsMessage[] Messages { get; private set; }

        public AcarsMessageEventArgs(AcarsMessage[] messages)
        {
            Messages = messages;
        }
    }
}
