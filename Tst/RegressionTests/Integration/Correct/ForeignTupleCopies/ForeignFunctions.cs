using System.Linq;
using PChecker.Runtime.StateMachines;
using PChecker.Runtime.Values;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        public static PTuple MakeTwo(StateMachine machine)
        {
            // The buffer is reused for the second tuple.
            var buffer = new IPValue[] { (PInt)1, (PInt)2 };
            var first = new PTuple(buffer);
            buffer[0] = (PInt)3;
            buffer[1] = (PInt)4;
            var second = new PTuple(buffer);
            return new PTuple(first, second);
        }

        public static PTuple MakeNamed(StateMachine machine)
        {
            var names = new[] { "a", "b" };
            var buffer = new IPValue[] { (PInt)1, (PInt)2 };
            var first = new PNamedTuple(names, buffer);
            buffer[0] = (PInt)3;
            buffer[1] = (PInt)4;
            var second = new PNamedTuple(names, buffer);
            return new PTuple(first, second);
        }

        public static PString Describe(PNamedTuple t, StateMachine machine)
        {
            var parts = new string[t.fieldValues.Count];
            for (var i = 0; i < t.fieldNames.Count; i++)
            {
                var value = t.fieldValues[t.fieldNames.IndexOf(t.fieldNames[i])];
                parts[i] = t.fieldNames[i] + "=" + (value is PSeq seq ? string.Join(",", seq) : value.ToString());
            }

            return string.Join(":", parts.Where(p => p != null));
        }

        public static void Change(PNamedTuple t, StateMachine machine)
        {
            ((PSeq)t["a"]).Add((PInt)5);
            t["b"] = (PInt)6;
        }
    }
}
