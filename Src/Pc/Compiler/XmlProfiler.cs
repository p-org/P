using System;
using System.Diagnostics;
using System.Xml.Linq;

namespace Microsoft.Pc
{
    public class XmlProfiler : IProfiler
    {
        XDocument data;
        XElement current;

        public XmlProfiler()
        {
            data = new XDocument(new XElement("data"));
        }

        public XDocument Data { get { return this.data; } }

        public IDisposable Start(string operation, string message)
        {
            XElement e = new XElement("operation", new XAttribute("name", operation), new XAttribute("description", message));
            if (current == null)
            {
                data.Root.Add(e);
            }
            else
            {
                current.Add(e);
            }
            current = e;
            return new XmlProfileWatcher(this, e, operation, message);
        }

        private void Finish(XElement e, DateTime timestamp, TimeSpan elapsed)
        {
            e.Add(new XAttribute("timestsamp", timestamp));
            e.Add(new XAttribute("elapsed", elapsed));
            if (current == e)
            {
                current = current.Parent;
            }
        }

        class XmlProfileWatcher : IDisposable
        {
            XmlProfiler owner;
            XElement e;
            Stopwatch watch = new Stopwatch();
            string operation;
            string message;

            public XmlProfileWatcher(XmlProfiler owner, XElement e, string operation, string message)
            {
                this.e = e;
                this.owner = owner;
                this.operation = operation;
                this.message = message;
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