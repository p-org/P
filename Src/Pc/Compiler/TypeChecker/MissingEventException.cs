using System;

namespace Microsoft.Pc.TypeChecker {
    public class MissingEventException : Exception
    {
        public EventSet EventSet { get; }
        public string EventName { get; }

        public MissingEventException(EventSet eventSet, string eventName)
        {
            EventSet = eventSet;
            EventName = eventName;
        }
    }
}