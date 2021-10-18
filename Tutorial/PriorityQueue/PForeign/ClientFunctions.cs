using System.Collections;
using Plang.CSharpRuntime.Values;

namespace PImplementation
{
    /*
     * Foreign functions declared in a class must be implemented inside the Class corresponding to the P machine
     * In this case the `Client` machine class
     */
    partial class Client
    {
        public static tPriorityQueue AddIntToQueue(tPriorityQueue queue, IPrtValue elem, PrtInt priority)
        {
            queue.Add(new ElementWithPriority(elem, priority));
            return queue;
        }
    }
}