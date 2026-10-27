using NUnit.Framework;
using PChecker.Runtime.Events;
using PChecker.Runtime.Values;

namespace UnitTests
{
    /// <summary>
    /// The parameterless event constructor leaves the payload null, as on master. Only the
    /// constructor that generated code uses gives the event the default payload of its type,
    /// built when first read.
    /// </summary>
    [TestFixture]
    public class EventPayloadTest
    {
        private class eSeq : Event
        {
            public int DefaultsCreated;

            public eSeq() { }
            public eSeq(PSeq payload) : base(payload) { }
            public eSeq(LazyDefaultPayload tag) : base(tag) { }
            public override IPValue Clone() => new eSeq();

            protected override IPValue CreateDefaultPayload()
            {
                DefaultsCreated++;
                return new PSeq();
            }
        }

        [Test]
        public void ParameterlessConstructorLeavesPayloadNull()
        {
            var ev = new eSeq();
            Assert.IsNull(ev.Payload);
            Assert.AreEqual(0, ev.DefaultsCreated);
            Assert.IsNull(((Event)new eSeq(new PSeq()).Clone()).Payload);
        }

        [Test]
        public void LazyDefaultPayloadIsBuiltOnceWhenRead()
        {
            var ev = new eSeq(default(LazyDefaultPayload));
            Assert.AreEqual(0, ev.DefaultsCreated);
            var payload = ev.Payload;
            Assert.IsInstanceOf<PSeq>(payload);
            Assert.AreSame(payload, ev.Payload);
            Assert.AreEqual(1, ev.DefaultsCreated);
        }

        [Test]
        public void AssignedPayloadReplacesLazyDefault()
        {
            var ev = new eSeq(default(LazyDefaultPayload));
            var assigned = new PSeq();
            ev.Payload = assigned;
            Assert.AreSame(assigned, ev.Payload);
            Assert.AreEqual(0, ev.DefaultsCreated);

            ev = new eSeq(default(LazyDefaultPayload)) { Payload = null };
            Assert.IsNull(ev.Payload);
            Assert.AreEqual(0, ev.DefaultsCreated);
        }
    }
}
