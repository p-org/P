using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Pc.TypeChecker;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.States;

namespace Microsoft.Pc.Backend.Prt
{
    public static class PrtTranslationUtils
    {
        private static readonly Dictionary<Type, string> DeclNameParts = new Dictionary<Type, string>
        {
            {typeof(EnumElem), "ENUMELEM"},
            {typeof(Function), "FUNCTION"},
            {typeof(Implementation), "IMPL"},
            {typeof(Interface), "I"},
            {typeof(Machine), "MACHINE"},
            {typeof(NamedEventSet), "EVENTSET"},
            {typeof(NamedModule), "MODULE"},
            {typeof(PEnum), "ENUM"},
            {typeof(PEvent), "EVENT"},
            {typeof(RefinementTest), "REFINEMENT_TEST"},
            {typeof(SafetyTest), "SAFETY_TEST"},
            {typeof(State), "STATE"},
            {typeof(StateGroup), "STATEGROUP"},
            {typeof(TypeDef), "TYPEDEF"},
            {typeof(Variable), "VAR"}
        };

        public static IList<T> ToOrderedListByPermutation<T>(IEnumerable<T> enumerable, Func<T, int> perm)
        {
            var items = enumerable.ToList();
            IList<T> inOrder = new T[items.Count];
            foreach (T item in items)
            {
                inOrder[perm(item)] = item;
            }

            return inOrder;
        }

        public static IEnumerable<Function> AllMethods(Scope scope)
        {
            // TODO: There are like six copies of this function.
            foreach (Function function in scope.Functions)
            {
                yield return function;
            }

            foreach (Machine machine in scope.Machines)
            {
                // includes anonymous functions
                foreach (Function method in machine.Methods)
                {
                    yield return method;
                }
            }
        }

        public static StateActionResults BuildActionSets(CompilationContext context, State state)
        {
            var defersSet = new NamedEventSet(state.Name + "_DEFERS", state.SourceLocation);
            var transSet = new NamedEventSet(state.Name + "_TRANS", state.SourceLocation);
            var dosSet = new NamedEventSet(state.Name + "_DOS", state.SourceLocation);

            var dos = new List<(PEvent, Function)>();
            var trans = new List<(PEvent, int, string)>();

            foreach (var eventActionPair in state.AllEventHandlers)
            {
                PEvent pEvent = eventActionPair.Key;
                switch (eventActionPair.Value)
                {
                    case EventDefer _:
                        defersSet.AddEvent(pEvent);
                        break;
                    case EventDoAction eventDoAction:
                        dosSet.AddEvent(pEvent);
                        dos.Add((pEvent, eventDoAction.Target));
                        break;
                    case EventGotoState eventGotoState:
                        transSet.AddEvent(pEvent);
                        string transFunName = eventGotoState.TransitionFunction == null
                            ? "_P_NO_OP"
                            : GetPrtNameForDecl(context, eventGotoState.TransitionFunction);
                        trans.Add((pEvent, context.GetNumberForState(eventGotoState.Target), "&" + transFunName));
                        break;
                    case EventIgnore _:
                        dosSet.AddEvent(pEvent);
                        dos.Add((pEvent, null));
                        break;
                    case EventPushState eventPushState:
                        transSet.AddEvent(pEvent);
                        trans.Add((pEvent, context.GetNumberForState(eventPushState.Target), "NULL"));
                        break;
                }
            }

            return new StateActionResults(defersSet, transSet, dosSet, dos, trans);
        }

        public static object[] ParsePrintMessage(string message)
        {
            var parts = new List<object>();
            var sb = new StringBuilder();
            for (var i = 0; i < message.Length; i++)
            {
                if (message[i] == '{')
                {
                    if (i + 1 == message.Length)
                    {
                        throw new ArgumentException("unmatched opening brace", nameof(message));
                    }

                    if (message[i + 1] == '{')
                    {
                        i++;
                        sb.Append(message[i]);
                    }
                    else if (char.IsDigit(message[i + 1]))
                    {
                        parts.Add(sb.ToString());
                        sb.Clear();

                        var position = 0;
                        while (++i < message.Length && '0' <= message[i] && message[i] <= '9')
                        {
                            position = 10 * position + (message[i] - '0');
                        }

                        if (i == message.Length || message[i] != '}')
                        {
                            throw new ArgumentException("unmatched opening brace in position expression", nameof(message));
                        }

                        parts.Add(position);
                    }
                    else
                    {
                        throw new ArgumentException("opening brace not followed by digits", nameof(message));
                    }
                }
                else if (message[i] == '}')
                {
                    if (i + 1 == message.Length || message[i + 1] != '}')
                    {
                        throw new ArgumentException("unmatched closing brace", nameof(message));
                    }

                    sb.Append(message[i]);
                    i++;
                }
                else
                {
                    sb.Append(message[i]);
                }
            }

            parts.Add(sb.ToString());
            return parts.ToArray();
        }

        public static string GetPrtNameForDecl(CompilationContext context, IPDecl decl)
        {
            if (decl == null)
            {
                throw new ArgumentNullException(nameof(decl));
            }

            var computedPrefix = "";
            switch (decl)
            {
                case EnumElem enumElem:
                    computedPrefix = $"{enumElem.ParentEnum.Name}_";
                    break;
                case PEvent pEvent:
                    if (pEvent.IsNullEvent)
                    {
                        return "_P_EVENT_NULL_STRUCT";
                    }

                    if (pEvent.IsHaltEvent)
                    {
                        return "_P_EVENT_HALT_STRUCT";
                    }

                    break;
                case Implementation impl:
                    return $"P_GEND_IMPL_{impl.Name}";
            }

            if (DeclNameParts.TryGetValue(decl.GetType(), out string prefix))
            {
                prefix += "_";
            }
            else
            {
                prefix = "";
            }

            return context.Names.GetNameForNode(decl, prefix + computedPrefix);
        }

        public class StateActionResults
        {
            public StateActionResults(NamedEventSet defersSet, NamedEventSet transSet, NamedEventSet dosSet, List<(PEvent, Function)> dos,
                                      List<(PEvent, int, string)> trans)
            {
                DefersSet = defersSet;
                TransSet = transSet;
                DosSet = dosSet;
                Dos = dos;
                Trans = trans;
            }

            public NamedEventSet DefersSet { get; }
            public NamedEventSet TransSet { get; }
            public NamedEventSet DosSet { get; }

            public List<(PEvent, Function)> Dos { get; }
            public List<(PEvent, int, string)> Trans { get; }
        }
    }
}
