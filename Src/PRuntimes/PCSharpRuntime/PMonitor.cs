using System.Collections.Generic;
using System.Linq;
using PChecker;
using PChecker.Actors;
using PChecker.Actors.Events;
using PChecker.Specifications;
using PChecker.Specifications.Monitors;

namespace Plang.CSharpRuntime
{
    public class PMonitor : Monitor
    {
        public static List<string> observes = new List<string>();

        public object gotoPayload;

        public void TryRaiseEvent(Event ev, object payload = null)
        {
            Assert(!(ev is DefaultEvent), "Monitor cannot raise a null event");
            var oneArgConstructor = ev.GetType().GetConstructors().First(x => x.GetParameters().Length > 0);
            var @event = (Event)oneArgConstructor.Invoke(new[] { payload });
            RaiseEvent(@event);
        }

        public void TryGotoState<T>(object payload = null) where T : State
        {
            gotoPayload = payload;
            RaiseGotoStateEvent<T>();
        }

        public void TryAssert(bool predicate)
        {
            Assert(predicate);
        }

        public void TryAssert(bool predicate, string s, params object[] args)
        {
            Assert(predicate, s, args);
        }
        
        public void LogLine(string message)
        {
            Logger.WriteLine($"<PrintLog> {message}");
        }

        public void Log(string message)
        {
            Logger.Write($"{message}");
        }
    }
}