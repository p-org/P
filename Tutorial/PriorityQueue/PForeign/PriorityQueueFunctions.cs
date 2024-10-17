/*****************************************************
 * We will use this example of the PriorityQueue to explain the implementation details for Foreign Types and Functions Interfaces
 ******************************************************/

using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using PChecker.Runtime;
using PChecker.Runtime.Values;
using PChecker.Runtime.Exceptions;
using PChecker.Runtime.StateMachines;
using System.Threading;
using System.Threading.Tasks;

// Note: All the foreign code (foreign types or functions) for any P project is always implemnented under the `PImplementation` global namespace.
namespace PImplementation
{
  // Note: Global Foreign functions, i.e., foreign functions declared at the top-level and not inside a machine in P, are all implemented inside the
  // GlobalFunctions class. We can use `partial` keyword in C# to extend the classes declared in the Generated Code.
  // Also, all global foreign functions must be declared static!
  public static partial class GlobalFunctions
  {
      public static tPriorityQueue CreatePriorityQueue(StateMachine machine)
      {
          // use these log statements if you want logs in the foreign code to show up in the error trace.
          machine.LogLine("Creating Priority Queue!");
          return new tPriorityQueue();
      }
      public static tPriorityQueue AddElement(tPriorityQueue queue, IPValue elem, PInt priority, StateMachine machine)
      {
          queue.Add(new ElementWithPriority(elem, priority));
          machine.LogLine("Adding Element in the Priority Queue!");
          return queue;
      }

      public static PNamedTuple RemoveElement(tPriorityQueue queue, StateMachine machine)
      {
          var element = queue.PriorityRemove();
          var retVal = new PNamedTuple(new string[] { "element", "queue" }, new IPValue[] { element, queue });
          return retVal;
      }

      public static PInt CountElement(tPriorityQueue queue, StateMachine machine)
      {
          return queue.Size();
      }

      /*
       * Modeling Nondeterminism or Randomness in Foreign Functions
       */
      public static IPValue ChooseElement(tPriorityQueue queue, StateMachine machine)
      {
          // one can write a nondeterministic foreign function using the machine.*Random functions.
          // all machine.TryRandom*() functions are controlled by the P checker during exploration.
          var index = (PInt)machine.TryRandom((PInt)queue.Size());
          machine.LogLine("Choosing element at location: " + index);
          return queue.GetElementAt(index);
      }
  }
}