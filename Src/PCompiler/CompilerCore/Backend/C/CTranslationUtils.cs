using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plang.Compiler.Backend.C
{
    public static class CTranslationUtils
    {
        public static IList<T> ToOrderedListByPermutation<T>(IEnumerable<T> enumerable, Func<T, int> perm)
        {
            List<T> items = enumerable.ToList();
            IList<T> inOrder = new T[items.Count];
            foreach (T item in items)
            {
                inOrder[perm(item)] = item;
            }

            return inOrder;
        }

        public static StateActionResults BuildActionSets(CompilationContext context, State state)
        {
            NamedEventSet defersSet = new NamedEventSet(state.Name + "_DEFERS", state.SourceLocation);
            NamedEventSet transSet = new NamedEventSet(state.Name + "_TRANS", state.SourceLocation);
            NamedEventSet dosSet = new NamedEventSet(state.Name + "_DOS", state.SourceLocation);

            List<(PEvent, Function)> dos = new List<(PEvent, Function)>();
            List<(PEvent, int, string)> trans = new List<(PEvent, int, string)>();

            foreach (KeyValuePair<PEvent, TypeChecker.AST.IStateAction> eventActionPair in state.AllEventHandlers)
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
                            : context.Names.GetNameForDecl(eventGotoState.TransitionFunction);
                        trans.Add((pEvent, context.GetDeclNumber(eventGotoState.Target), "&" + transFunName));
                        break;

                    case EventIgnore _:
                        dosSet.AddEvent(pEvent);
                        dos.Add((pEvent, null));
                        break;

                    case EventPushState eventPushState:
                        transSet.AddEvent(pEvent);
                        trans.Add((pEvent, context.GetDeclNumber(eventPushState.Target), "NULL"));
                        break;
                }
            }

            return new StateActionResults(defersSet, transSet, dosSet, dos, trans);
        }

        public static object[] ParsePrintMessage(string message)
        {
            List<object> parts = new List<object>();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < message.Length; i++)
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

                        int position = 0;
                        while (++i < message.Length && '0' <= message[i] && message[i] <= '9')
                        {
                            position = 10 * position + (message[i] - '0');
                        }

                        if (i == message.Length || message[i] != '}')
                        {
                            throw new ArgumentException("unmatched opening brace in position expression",
                                nameof(message));
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

        public class StateActionResults
        {
            public StateActionResults(NamedEventSet defersSet, NamedEventSet transSet, NamedEventSet dosSet,
                List<(PEvent, Function)> dos,
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