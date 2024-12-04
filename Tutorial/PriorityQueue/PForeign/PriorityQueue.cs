using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Linq.Expressions;
using PChecker.Runtime;
using PChecker.Runtime.Values;
using PChecker.Runtime.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace PImplementation
{
    /* tPriorityQueue type implementation:
     * (1) An implementation of a foreign type must have the same name as the declaration in the P program.
     * (2) It must be implemnented in the `PImplementation` namespace and must extend the `IPrtValue` interface.
     * (3) Implementing the `IPrtValue` interface basically requires the programmer to provide two functions in the foreign
     *  type, `Equals and Clone`.
     Apart from that the implementation of the foreign type can be arbitrary sequential C# code.

     If you want to pretty print the foreign type value, you can also override the ToString() function in their implementation.
     */
    public class tPriorityQueue : IPValue
    {
        // naive implementation of the priority queue as a list of elements with priority
        private List<ElementWithPriority> elements = new List<ElementWithPriority>();

        // add the element into the list
        public void Add(ElementWithPriority elem)
        {
            elements.Add(elem);
        }

        // delete all elements irrespective of the priority
        public void Delete(ElementWithPriority elem)
        {
            elements = elements.Where(el => !el.Element.Equals(elem.Element)).ToList();
        }

        // priority remove
        public IPValue PriorityRemove()
        {
            var removeElem = elements.OrderBy(el => el.Priority).First();
            elements.Remove(removeElem);
            return removeElem.Element;
        }

        public bool Equals(IPValue other)
        {
            if (other is tPriorityQueue || other != null)
            {
                return this.elements.SequenceEqual(((tPriorityQueue)other).elements);
            }
            else
            {
                return false;
            }
        }

        public IPValue Clone()
        {
            var cloned = new tPriorityQueue();
            foreach (var elem in elements)
            {
                cloned.Add(elem);
            }
            return cloned;
        }

        public int Size()
        {
            return elements.Count;
        }

        public IPValue GetElementAt(int index)
        {
            return elements.ElementAt(index).Element;
        }
    }

    public class ElementWithPriority
    {
        public IPValue Element { get; }
        public int Priority { get;  }

        public ElementWithPriority(IPValue elem, int priority)
        {
            Element = elem;
            Priority = priority;
        }
    }
}