using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using System.Collections.Generic;
using System.Linq;

namespace Plang.CSharpRuntime
{
    public class PMonitor : Monitor
    {
        public static List<string> observes = new List<string>();

        public object gotoPayload;

        public void TryRaiseEvent(Event ev, object payload = null)
        {
            Assert(!(ev is DefaultEvent), "Monitor cannot raise a null event");
            System.Reflection.ConstructorInfo oneArgConstructor = ev.GetType().GetConstructors().First(x => x.GetParameters().Length > 0);
            Event @event = (Event)oneArgConstructor.Invoke(new[] { payload });
            base.RaiseEvent(@event);
        }

        public void TryGotoState<T>(object payload = null) where T : State
        {
            gotoPayload = payload;
            base.RaiseGotoStateEvent<T>();
        }

        public void TryAssert(bool predicate)
        {
            base.Assert(predicate);
        }

        public void TryAssert(bool predicate, string s, params object[] args)
        {
            base.Assert(predicate, s, args);
        }
    }
}