using System;
using System.Collections.Generic;

namespace PChecker.Actors.Logging
{
    internal sealed class JsonWriter
    {
        private readonly List<Dictionary<string, string>> _writer;
        private Dictionary<string, string> _step;

        public JsonWriter()
        {
            _writer = new List<Dictionary<string, string>>();
            _step = new Dictionary<string, string>();
        }

        private void AddToSteps()
        {
            _writer.Add(_step);
            _step = new Dictionary<string, string>();
        }

        private void AddToStep(string attr, string value)
        {
            _step.Add(attr, value);
        }

        public void AddStep() => AddToSteps();


        public void AddAttribute(string attr, string value)
        {
            AddToStep(attr, value);
        }

        public void AddAssertionFailure(string error)
        {
            AddToStep("AssertionFailure", error);
        }

        public void AddElement(string element)
        {
            AddToStep("element", element);
        }

        public void AddId(string id)
        {
            AddToStep("id", id);
        }

        public void AddEvent(string eventName)
        {
            AddToStep("event", eventName);
        }

        public List<Dictionary<string, string>> ToJson() => _writer;
    }
}