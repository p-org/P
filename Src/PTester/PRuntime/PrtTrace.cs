using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P.Runtime
{
    /// <summary>
    /// Visible actions of a trace
    /// </summary>
    [Serializable]
    public abstract class VisibleAction
    {

    }

    [Serializable]
    public class SendAction : VisibleAction
    {
        Tuple<string, int> target;
        string ev;
        string payload;

        public SendAction(Tuple<string, int> t, string e, PrtValue v)
        {
            target = new Tuple<string, int>(t.Item1, t.Item2);
            ev = e;
            payload = v.ToString();
        }
        public override bool Equals(object obj)
        {
            var sendAct = obj as SendAction;
            if (sendAct == null)
            {
                return false;
            }
            else
            {
                return sendAct.target.Item1.Equals(this.target.Item1) &&
                    sendAct.target.Item2.Equals(this.target.Item2) &&
                    sendAct.ev.Equals(this.ev) &&
                    sendAct.payload.Equals(this.payload);
            }
        }

        public override int GetHashCode()
        {
            return target.GetHashCode() ^ ev.GetHashCode() ^ payload.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[({0}, {1}), {2}, {3}]", target.Item1, target.Item2, ev.ToString(), payload.ToString());
        }
    }

    [Serializable]
    public class CreateAction : VisibleAction
    {
        string interfaceName;
        string constructorArg;
        public CreateAction(string i, string arg)
        {
            interfaceName = i;
            constructorArg = arg;
        }

        public override bool Equals(object obj)
        {
            var createAct = obj as CreateAction;
            if (createAct == null)
            {
                return false;
            }
            else
            {
                return this.interfaceName == createAct.interfaceName && this.constructorArg == createAct.constructorArg;
            }
        }

        public override int GetHashCode()
        {
            return interfaceName.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}]", interfaceName, constructorArg);
        }
    }
    /// <summary>
    /// Sequence of visible actions
    /// </summary>
    [Serializable]
    public class VisibleTrace
    {
        List<VisibleAction> trace;

        public VisibleTrace()
        {
            trace = new List<VisibleAction>();
        }

        public List<VisibleAction> Trace {
            get { return trace; }
            set { trace = value; }
        }

        public override bool Equals(object obj)
        {
            var vtrace = obj as VisibleTrace;
            if (vtrace == null)
            {
                return false;
            }
            else
            {
                var trace2 = vtrace.trace;
                if (trace.Count != trace2.Count)
                    return false;

                for (int i = 0; i < trace2.Count(); i++)
                {
                    if (!trace[i].Equals(trace2[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override int GetHashCode()
        {
            return trace.GetHashCode();
        }

        public override string ToString()
        {
            string ret = "------------\n";
            foreach (var action in trace)
            {
                ret = ret + action.ToString() + "\n";
            }
            ret = ret + "------------\n";
            return ret;
        }

        public void AddAction(string interfaceName, string arg)
        {
            trace.Add(new CreateAction(interfaceName, arg));
        }

        public void AddAction(Tuple<string, int> t, PrtEventValue e, PrtValue v)
        {
            trace.Add(new SendAction(t, e.evt.name, v));
        }
    }
}
