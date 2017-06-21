using System;
using System.Diagnostics;
using System.Xml.Linq;

namespace Microsoft.Pc
{
    public class XmlProfiler : IProfiler
    {
        private XElement current;

        public XmlProfiler()
        {
            Data = new XDocument(new XElement("data"));
        }

        public XDocument Data { get; }

        public IDisposable Start(string operation, string message)
        {
            var e = new XElement("operation", new XAttribute("name", operation), new XAttribute("description", message));
            if (current == null)
            {
                Data.Root.Add(e);
            }
            else
            {
                current.Add(e);
            }
            current = e;
            return new XmlProfileWatcher(this, e);
        }

        private void Finish(XContainer e, DateTime timestamp, TimeSpan elapsed)
        {
            e.Add(new XAttribute("timestsamp", timestamp));
            e.Add(new XAttribute("elapsed", elapsed));
            if (current == e)
            {
                current = current.Parent;
            }
        }

        private class XmlProfileWatcher : IDisposable
        {
            private readonly XElement e;
            private readonly XmlProfiler owner;
            private readonly Stopwatch watch = new Stopwatch();

            public XmlProfileWatcher(XmlProfiler owner, XElement e)
            {
                this.e = e;
                this.owner = owner;
                watch.Start();
            }

            public void Dispose()
            {
                watch.Stop();
                owner.Finish(e, DateTime.Now, watch.Elapsed);
            }
        }
    }
}