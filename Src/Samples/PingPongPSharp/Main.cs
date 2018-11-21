using Microsoft.PSharp;
using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using PrtSharp;
using PrtSharp.Values;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 162, 219, 414
namespace Main
{
    public static partial class GlobalFunctions { }
    internal partial class E : PEvent<PrtInt>
    {
        static E() { AssertVal = 1; AssumeVal = -1; }
        public E() : base() { }
        public E(PrtInt payload) : base(payload) { }
        public override IPrtValue Clone() { return new E(); }
    }
    internal partial class EI1 : PEvent<PrtInt>
    {
        static EI1() { AssertVal = -1; AssumeVal = -1; }
        public EI1() : base() { }
        public EI1(PrtInt payload) : base(payload) { }
        public override IPrtValue Clone() { return new EI1(); }
    }
    internal partial class EI2 : PEvent<PrtInt>
    {
        static EI2() { AssertVal = -1; AssumeVal = -1; }
        public EI2() : base() { }
        public EI2(PrtInt payload) : base(payload) { }
        public override IPrtValue Clone() { return new EI2(); }
    }
    internal partial class EI3 : PEvent<PrtInt>
    {
        static EI3() { AssertVal = -1; AssumeVal = -1; }
        public EI3() : base() { }
        public EI3(PrtInt payload) : base(payload) { }
        public override IPrtValue Clone() { return new EI3(); }
    }
    internal partial class EI4 : PEvent<PrtInt>
    {
        static EI4() { AssertVal = -1; AssumeVal = -1; }
        public EI4() : base() { }
        public EI4(PrtInt payload) : base(payload) { }
        public override IPrtValue Clone() { return new EI4(); }
    }
    internal partial class EI5 : PEvent<PrtInt>
    {
        static EI5() { AssertVal = -1; AssumeVal = -1; }
        public EI5() : base() { }
        public EI5(PrtInt payload) : base(payload) { }
        public override IPrtValue Clone() { return new EI5(); }
    }
    internal partial class EI6 : PEvent<PrtInt>
    {
        static EI6() { AssertVal = -1; AssumeVal = -1; }
        public EI6() : base() { }
        public EI6(PrtInt payload) : base(payload) { }
        public override IPrtValue Clone() { return new EI6(); }
    }
    internal partial class E1 : PEvent<IPrtValue>
    {
        static E1() { AssertVal = 1; AssumeVal = -1; }
        public E1() : base() { }
        public E1(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new E1(); }
    }
    internal partial class E2 : PEvent<IPrtValue>
    {
        static E2() { AssertVal = 1; AssumeVal = -1; }
        public E2() : base() { }
        public E2(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new E2(); }
    }
    internal partial class ET1 : PEvent<PrtNamedTuple>
    {
        static ET1() { AssertVal = -1; AssumeVal = -1; }
        public ET1() : base() { }
        public ET1(PrtNamedTuple payload) : base(payload) { }
        public override IPrtValue Clone() { return new ET1(); }
    }
    internal partial class ET2 : PEvent<PrtNamedTuple>
    {
        static ET2() { AssertVal = -1; AssumeVal = -1; }
        public ET2() : base() { }
        public ET2(PrtNamedTuple payload) : base(payload) { }
        public override IPrtValue Clone() { return new ET2(); }
    }
    internal partial class ESEQ1 : PEvent<PrtSeq>
    {
        static ESEQ1() { AssertVal = -1; AssumeVal = -1; }
        public ESEQ1() : base() { }
        public ESEQ1(PrtSeq payload) : base(payload) { }
        public override IPrtValue Clone() { return new ESEQ1(); }
    }
    internal partial class ESEQ2 : PEvent<PrtSeq>
    {
        static ESEQ2() { AssertVal = -1; AssumeVal = -1; }
        public ESEQ2() : base() { }
        public ESEQ2(PrtSeq payload) : base(payload) { }
        public override IPrtValue Clone() { return new ESEQ2(); }
    }
    internal partial class EMAP1 : PEvent<PrtMap>
    {
        static EMAP1() { AssertVal = -1; AssumeVal = -1; }
        public EMAP1() : base() { }
        public EMAP1(PrtMap payload) : base(payload) { }
        public override IPrtValue Clone() { return new EMAP1(); }
    }
    internal partial class EMAP11 : PEvent<PrtMap>
    {
        static EMAP11() { AssertVal = -1; AssumeVal = -1; }
        public EMAP11() : base() { }
        public EMAP11(PrtMap payload) : base(payload) { }
        public override IPrtValue Clone() { return new EMAP11(); }
    }
    internal partial class EMAP2 : PEvent<PrtMap>
    {
        static EMAP2() { AssertVal = -1; AssumeVal = -1; }
        public EMAP2() : base() { }
        public EMAP2(PrtMap payload) : base(payload) { }
        public override IPrtValue Clone() { return new EMAP2(); }
    }
    internal partial class EMAP3 : PEvent<PrtMap>
    {
        static EMAP3() { AssertVal = -1; AssumeVal = -1; }
        public EMAP3() : base() { }
        public EMAP3(PrtMap payload) : base(payload) { }
        public override IPrtValue Clone() { return new EMAP3(); }
    }
    internal partial class Main : PMachine
    {
        private PrtNamedTuple t = (new PrtNamedTuple(new string[] { "a", "b" }, new PrtSeq(), new PrtMap()));
        private PrtNamedTuple t1 = (new PrtNamedTuple(new string[] { "a", "b" }, new PrtSeq(), new PrtMap()));
        private PrtNamedTuple ts = (new PrtNamedTuple(new string[] { "a", "b" }, ((PrtInt)0), ((PrtInt)0)));
        private PrtNamedTuple ts1 = (new PrtNamedTuple(new string[] { "a", "b" }, ((PrtInt)0), ((PrtBool)false)));
        private PrtTuple tt = (new PrtTuple(((PrtInt)0), ((PrtInt)0)));
        private PrtTuple tbool = (new PrtTuple(((PrtBool)false), ((PrtBool)false)));
        private PrtTuple te = (new PrtTuple(((PrtInt)0), null));
        private PrtBool b = ((PrtBool)false);
        private PrtInt y = ((PrtInt)0);
        private PrtInt tmp = ((PrtInt)0);
        private PrtInt tmp1 = ((PrtInt)0);
        private IEventWithPayload ev = null;
        private IPrtValue a = null;
        private PrtNamedTuple tmp2 = (new PrtNamedTuple(new string[] { "a", "b" }, new PrtSeq(), new PrtMap()));
        private PrtMap tmp3 = new PrtMap();
        private PrtSeq s = new PrtSeq();
        private PrtSeq s1 = new PrtSeq();
        private PrtSeq s2 = new PrtSeq();
        private PrtSeq s3 = new PrtSeq();
        private PrtSeq s4 = new PrtSeq();
        private PrtSeq s8 = new PrtSeq();
        private PrtSeq s5 = new PrtSeq();
        private PrtSeq s6 = new PrtSeq();
        private PrtSeq s7 = new PrtSeq();
        private PrtSeq s9 = new PrtSeq();
        private PrtSeq s10 = new PrtSeq();
        private PrtSeq s11 = new PrtSeq();
        private PrtSeq s12 = new PrtSeq();
        private PrtInt i = ((PrtInt)0);
        private PMachineValue mac = null;
        private PrtMap m1 = new PrtMap();
        private PrtMap m4 = new PrtMap();
        private PrtMap m3 = new PrtMap();
        private PrtMap m5 = new PrtMap();
        private PrtMap m6 = new PrtMap();
        private PrtMap m2 = new PrtMap();
        private PrtMap m7 = new PrtMap();
        private PrtMap m8 = new PrtMap();
        private PrtMap m9 = new PrtMap();
        public class ConstructorEvent : PEvent<IPrtValue> { public ConstructorEvent(IPrtValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Main()
        {
            this.sends.Add(nameof(E));
            this.sends.Add(nameof(E1));
            this.sends.Add(nameof(E2));
            this.sends.Add(nameof(EI1));
            this.sends.Add(nameof(EI2));
            this.sends.Add(nameof(EI3));
            this.sends.Add(nameof(EI4));
            this.sends.Add(nameof(EI5));
            this.sends.Add(nameof(EI6));
            this.sends.Add(nameof(EMAP1));
            this.sends.Add(nameof(EMAP11));
            this.sends.Add(nameof(EMAP2));
            this.sends.Add(nameof(EMAP3));
            this.sends.Add(nameof(ESEQ1));
            this.sends.Add(nameof(ESEQ2));
            this.sends.Add(nameof(ET1));
            this.sends.Add(nameof(ET2));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(E));
            this.receives.Add(nameof(E1));
            this.receives.Add(nameof(E2));
            this.receives.Add(nameof(EI1));
            this.receives.Add(nameof(EI2));
            this.receives.Add(nameof(EI3));
            this.receives.Add(nameof(EI4));
            this.receives.Add(nameof(EI5));
            this.receives.Add(nameof(EI6));
            this.receives.Add(nameof(EMAP1));
            this.receives.Add(nameof(EMAP11));
            this.receives.Add(nameof(EMAP2));
            this.receives.Add(nameof(EMAP3));
            this.receives.Add(nameof(ESEQ1));
            this.receives.Add(nameof(ESEQ2));
            this.receives.Add(nameof(ET1));
            this.receives.Add(nameof(ET2));
            this.receives.Add(nameof(PHalt));
            this.creates.Add(nameof(I_XYZ));
        }

        public void Anon()
        {
            Main currentMachine = this;
            IPrtValue TMP_tmp0 = null;
            PrtInt TMP_tmp1 = ((PrtInt)0);
            PrtBool TMP_tmp2 = ((PrtBool)false);
            IPrtValue TMP_tmp3 = null;
            PrtBool TMP_tmp4 = ((PrtBool)false);
            PrtBool TMP_tmp5 = ((PrtBool)false);
            IPrtValue TMP_tmp6 = null;
            PrtBool TMP_tmp7 = ((PrtBool)false);
            IEventWithPayload TMP_tmp8 = null;
            PrtBool TMP_tmp9 = ((PrtBool)false);
            IEventWithPayload TMP_tmp10 = null;
            PrtBool TMP_tmp11 = ((PrtBool)false);
            IPrtValue TMP_tmp12 = null;
            PrtBool TMP_tmp13 = ((PrtBool)false);
            PMachineValue TMP_tmp14 = null;
            PrtBool TMP_tmp15 = ((PrtBool)false);
            PMachineValue TMP_tmp16 = null;
            PrtBool TMP_tmp17 = ((PrtBool)false);
            PMachineValue TMP_tmp18 = null;
            PMachineValue TMP_tmp19 = null;
            PrtBool TMP_tmp20 = ((PrtBool)false);
            IPrtValue TMP_tmp21 = null;
            PrtBool TMP_tmp22 = ((PrtBool)false);
            IPrtValue TMP_tmp23 = null;
            PrtInt TMP_tmp24 = ((PrtInt)0);
            PrtInt TMP_tmp25 = ((PrtInt)0);
            PrtBool TMP_tmp26 = ((PrtBool)false);
            IPrtValue TMP_tmp27 = null;
            PrtBool TMP_tmp28 = ((PrtBool)false);
            IPrtValue TMP_tmp29 = null;
            PrtBool TMP_tmp30 = ((PrtBool)false);
            PMachineValue TMP_tmp31 = null;
            IEventWithPayload TMP_tmp32 = null;
            PrtInt TMP_tmp33 = ((PrtInt)0);
            PMachineValue TMP_tmp34 = null;
            IEventWithPayload TMP_tmp35 = null;
            PrtInt TMP_tmp36 = ((PrtInt)0);
            PMachineValue TMP_tmp37 = null;
            IEventWithPayload TMP_tmp38 = null;
            PrtNamedTuple TMP_tmp39 = (new PrtNamedTuple(new string[] { "a", "b" }, ((PrtInt)0), ((PrtBool)false)));
            PMachineValue TMP_tmp40 = null;
            IEventWithPayload TMP_tmp41 = null;
            PrtInt TMP_tmp42 = ((PrtInt)0);
            PrtBool TMP_tmp43 = ((PrtBool)false);
            PrtNamedTuple TMP_tmp44 = (new PrtNamedTuple(new string[] { "a", "b" }, ((PrtInt)0), ((PrtBool)false)));
            PrtSeq TMP_tmp45 = new PrtSeq();
            PrtInt TMP_tmp46 = ((PrtInt)0);
            PrtInt TMP_tmp47 = ((PrtInt)0);
            PMachineValue TMP_tmp48 = null;
            IEventWithPayload TMP_tmp49 = null;
            PrtSeq TMP_tmp50 = new PrtSeq();
            PMachineValue TMP_tmp51 = null;
            IEventWithPayload TMP_tmp52 = null;
            PrtSeq TMP_tmp53 = new PrtSeq();
            PrtMap TMP_tmp54 = new PrtMap();
            PMachineValue TMP_tmp55 = null;
            IEventWithPayload TMP_tmp56 = null;
            PrtMap TMP_tmp57 = new PrtMap();
            PMachineValue TMP_tmp58 = null;
            IEventWithPayload TMP_tmp59 = null;
            PrtMap TMP_tmp60 = new PrtMap();
            PrtMap TMP_tmp61 = new PrtMap();
            PMachineValue TMP_tmp62 = null;
            IEventWithPayload TMP_tmp63 = null;
            PrtMap TMP_tmp64 = new PrtMap();
            PMachineValue TMP_tmp65 = null;
            IEventWithPayload TMP_tmp66 = null;
            PrtMap TMP_tmp67 = new PrtMap();
            IEventWithPayload TMP_tmp68 = null;
            TMP_tmp0 = (IPrtValue)(null);
            a = TMP_tmp0;
            a = (IPrtValue)(((PrtInt)1));
            TMP_tmp1 = (PrtInt)(((PrtInt)a));
            y = TMP_tmp1;
            TMP_tmp2 = (PrtBool)((PrtValues.SafeEquals(y, a)));
            currentMachine.Assert(TMP_tmp2, "");
            TMP_tmp3 = (IPrtValue)(null);
            a = TMP_tmp3;
            a = (IPrtValue)(((PrtBool)true));
            TMP_tmp4 = (PrtBool)(((PrtBool)a));
            b = TMP_tmp4;
            TMP_tmp5 = (PrtBool)((PrtValues.SafeEquals(b, a)));
            currentMachine.Assert(TMP_tmp5, "");
            TMP_tmp6 = (IPrtValue)(null);
            a = TMP_tmp6;
            TMP_tmp7 = (PrtBool)((PrtValues.SafeEquals(a, null)));
            currentMachine.Assert(TMP_tmp7, "");
            TMP_tmp8 = (IEventWithPayload)(((IEventWithPayload)a));
            ev = TMP_tmp8;
            TMP_tmp9 = (PrtBool)((PrtValues.SafeEquals(a, ev)));
            currentMachine.Assert(TMP_tmp9, "");
            a = (IPrtValue)(new E(((PrtInt)0)));
            TMP_tmp10 = (IEventWithPayload)(((IEventWithPayload)a));
            ev = TMP_tmp10;
            TMP_tmp11 = (PrtBool)((PrtValues.SafeEquals(ev, new E(((PrtInt)0)))));
            currentMachine.Assert(TMP_tmp11, "");
            TMP_tmp12 = (IPrtValue)(null);
            a = TMP_tmp12;
            TMP_tmp13 = (PrtBool)((PrtValues.SafeEquals(a, null)));
            currentMachine.Assert(TMP_tmp13, "");
            TMP_tmp14 = (PMachineValue)(null);
            mac = TMP_tmp14;
            TMP_tmp15 = (PrtBool)((PrtValues.SafeEquals(mac, null)));
            currentMachine.Assert(TMP_tmp15, "");
            TMP_tmp16 = (PMachineValue)(((PMachineValue)a));
            mac = TMP_tmp16;
            TMP_tmp17 = (PrtBool)((PrtValues.SafeEquals(mac, a)));
            currentMachine.Assert(TMP_tmp17, "");
            TMP_tmp18 = (PMachineValue)(currentMachine.CreateInterface<I_XYZ>(currentMachine));
            a = (IPrtValue)TMP_tmp18;
            TMP_tmp19 = (PMachineValue)(((PMachineValue)a));
            mac = TMP_tmp19;
            TMP_tmp20 = (PrtBool)((PrtValues.SafeEquals(mac, a)));
            currentMachine.Assert(TMP_tmp20, "");
            TMP_tmp21 = (IPrtValue)(null);
            a = TMP_tmp21;
            ((PrtMap)m1)[((PrtInt)0)] = (PrtInt)(((PrtInt)1));
            ((PrtMap)m1)[((PrtInt)1)] = (PrtInt)(((PrtInt)2));
            a = (IPrtValue)(((PrtMap)((IPrtValue)m1)?.Clone()));
            TMP_tmp22 = (PrtBool)((PrtValues.SafeEquals(a, m1)));
            currentMachine.Assert(TMP_tmp22, "");
            TMP_tmp23 = (IPrtValue)(null);
            a = TMP_tmp23;
            TMP_tmp24 = (PrtInt)(((PrtInt)1));
            ((PrtSeq)s).Insert(((PrtInt)0), TMP_tmp24);
            TMP_tmp25 = (PrtInt)(((PrtInt)2));
            ((PrtSeq)s).Insert(((PrtInt)1), TMP_tmp25);
            a = (IPrtValue)(((PrtSeq)((IPrtValue)s)?.Clone()));
            TMP_tmp26 = (PrtBool)((PrtValues.SafeEquals(a, s)));
            currentMachine.Assert(TMP_tmp26, "");
            TMP_tmp27 = (IPrtValue)(null);
            a = TMP_tmp27;
            ((PrtNamedTuple)ts)["a"] = (PrtInt)(((PrtInt)1));
            ((PrtNamedTuple)ts)["b"] = (PrtInt)(((PrtInt)2));
            a = (IPrtValue)(((PrtNamedTuple)((IPrtValue)ts)?.Clone()));
            TMP_tmp28 = (PrtBool)((PrtValues.SafeEquals(a, ts)));
            currentMachine.Assert(TMP_tmp28, "");
            TMP_tmp29 = (IPrtValue)(null);
            a = TMP_tmp29;
            ((PrtTuple)tt)[0] = (PrtInt)(((PrtInt)1));
            ((PrtTuple)tt)[1] = (PrtInt)(((PrtInt)2));
            a = (IPrtValue)(((PrtTuple)((IPrtValue)tt)?.Clone()));
            TMP_tmp30 = (PrtBool)((PrtValues.SafeEquals(a, tt)));
            currentMachine.Assert(TMP_tmp30, "");
            y = (PrtInt)(((PrtInt)1));
            TMP_tmp31 = (PMachineValue)(((PMachineValue)((IPrtValue)mac)?.Clone()));
            TMP_tmp32 = (IEventWithPayload)(new EI1(((PrtInt)0)));
            TMP_tmp33 = (PrtInt)(((PrtInt)((IPrtValue)y)?.Clone()));
            currentMachine.SendEvent(currentMachine, TMP_tmp31, (Event)TMP_tmp32, TMP_tmp33);
            a = (IPrtValue)(((PrtInt)1));
            TMP_tmp34 = (PMachineValue)(((PMachineValue)((IPrtValue)mac)?.Clone()));
            TMP_tmp35 = (IEventWithPayload)(new EI6(((PrtInt)0)));
            TMP_tmp36 = (PrtInt)(((PrtInt)a));
            currentMachine.SendEvent(currentMachine, TMP_tmp34, (Event)TMP_tmp35, TMP_tmp36);
            ((PrtNamedTuple)ts1)["a"] = (PrtInt)(((PrtInt)1));
            ((PrtNamedTuple)ts1)["b"] = (PrtBool)(((PrtBool)true));
            TMP_tmp37 = (PMachineValue)(((PMachineValue)((IPrtValue)mac)?.Clone()));
            TMP_tmp38 = (IEventWithPayload)(new ET1((new PrtNamedTuple(new string[] { "a", "b" }, ((PrtInt)0), ((PrtBool)false)))));
            TMP_tmp39 = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)ts1)?.Clone()));
            currentMachine.SendEvent(currentMachine, TMP_tmp37, (Event)TMP_tmp38, TMP_tmp39);
            TMP_tmp40 = (PMachineValue)(((PMachineValue)((IPrtValue)mac)?.Clone()));
            TMP_tmp41 = (IEventWithPayload)(new ET2((new PrtNamedTuple(new string[] { "a", "b" }, ((PrtInt)0), ((PrtBool)false)))));
            TMP_tmp42 = (PrtInt)(((PrtInt)2));
            TMP_tmp43 = (PrtBool)(((PrtBool)false));
            TMP_tmp44 = (PrtNamedTuple)((new PrtNamedTuple(new string[] { "a", "b" }, TMP_tmp42, TMP_tmp43)));
            currentMachine.SendEvent(currentMachine, TMP_tmp40, (Event)TMP_tmp41, TMP_tmp44);
            TMP_tmp45 = (PrtSeq)(new PrtSeq());
            s = TMP_tmp45;
            TMP_tmp46 = (PrtInt)(((PrtInt)1));
            ((PrtSeq)s).Insert(((PrtInt)0), TMP_tmp46);
            TMP_tmp47 = (PrtInt)(((PrtInt)2));
            ((PrtSeq)s).Insert(((PrtInt)1), TMP_tmp47);
            TMP_tmp48 = (PMachineValue)(((PMachineValue)((IPrtValue)mac)?.Clone()));
            TMP_tmp49 = (IEventWithPayload)(new ESEQ1(new PrtSeq()));
            TMP_tmp50 = (PrtSeq)(((PrtSeq)((IPrtValue)s)?.Clone()));
            currentMachine.SendEvent(currentMachine, TMP_tmp48, (Event)TMP_tmp49, TMP_tmp50);
            s1 = (PrtSeq)(new PrtSeq(((PrtSeq)((IPrtValue)s)?.Clone()).Cast<IPrtValue>()));
            TMP_tmp51 = (PMachineValue)(((PMachineValue)((IPrtValue)mac)?.Clone()));
            TMP_tmp52 = (IEventWithPayload)(new ESEQ2(new PrtSeq()));
            TMP_tmp53 = (PrtSeq)(((PrtSeq)s1));
            currentMachine.SendEvent(currentMachine, TMP_tmp51, (Event)TMP_tmp52, TMP_tmp53);
            TMP_tmp54 = (PrtMap)(new PrtMap());
            m1 = TMP_tmp54;
            TMP_tmp55 = (PMachineValue)(((PMachineValue)((IPrtValue)mac)?.Clone()));
            TMP_tmp56 = (IEventWithPayload)(new EMAP1(new PrtMap()));
            TMP_tmp57 = (PrtMap)(((PrtMap)((IPrtValue)m1)?.Clone()));
            currentMachine.SendEvent(currentMachine, TMP_tmp55, (Event)TMP_tmp56, TMP_tmp57);
            ((PrtMap)m1)[((PrtInt)0)] = (PrtInt)(((PrtInt)1));
            ((PrtMap)m1)[((PrtInt)3)] = (PrtInt)(((PrtInt)3));
            TMP_tmp58 = (PMachineValue)(((PMachineValue)((IPrtValue)mac)?.Clone()));
            TMP_tmp59 = (IEventWithPayload)(new EMAP11(new PrtMap()));
            TMP_tmp60 = (PrtMap)(((PrtMap)((IPrtValue)m1)?.Clone()));
            currentMachine.SendEvent(currentMachine, TMP_tmp58, (Event)TMP_tmp59, TMP_tmp60);
            TMP_tmp61 = (PrtMap)(new PrtMap());
            m9 = TMP_tmp61;
            TMP_tmp62 = (PMachineValue)(((PMachineValue)((IPrtValue)mac)?.Clone()));
            TMP_tmp63 = (IEventWithPayload)(new EMAP2(new PrtMap()));
            TMP_tmp64 = (PrtMap)(((PrtMap)m9));
            currentMachine.SendEvent(currentMachine, TMP_tmp62, (Event)TMP_tmp63, TMP_tmp64);
            m9 = (PrtMap)(new PrtMap(((PrtMap)((IPrtValue)m1)?.Clone())));
            TMP_tmp65 = (PMachineValue)(((PMachineValue)((IPrtValue)mac)?.Clone()));
            TMP_tmp66 = (IEventWithPayload)(new EMAP3(new PrtMap()));
            TMP_tmp67 = (PrtMap)(((PrtMap)m9));
            currentMachine.SendEvent(currentMachine, TMP_tmp65, (Event)TMP_tmp66, TMP_tmp67);
            TMP_tmp68 = (IEventWithPayload)(new PHalt(null));
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp68);
            throw new PUnreachableCodeException();
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(S))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon))]
        class S : MachineState
        {
        }
    }
    internal partial class XYZ : PMachine
    {
        private PrtSeq ss = new PrtSeq();
        private PrtInt yt = ((PrtInt)0);
        private PrtNamedTuple tts1 = (new PrtNamedTuple(new string[] { "a", "b" }, ((PrtInt)0), ((PrtBool)false)));
        private PrtNamedTuple tts = (new PrtNamedTuple(new string[] { "a", "b" }, ((PrtInt)0), ((PrtInt)0)));
        private IPrtValue ta = null;
        private PrtSeq s_1 = new PrtSeq();
        private PrtSeq s1_1 = new PrtSeq();
        private PrtMap mi = new PrtMap();
        private PrtMap ma = new PrtMap();
        public class ConstructorEvent : PEvent<IPrtValue> { public ConstructorEvent(IPrtValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public XYZ()
        {
            this.sends.Add(nameof(E));
            this.sends.Add(nameof(E1));
            this.sends.Add(nameof(E2));
            this.sends.Add(nameof(EI1));
            this.sends.Add(nameof(EI2));
            this.sends.Add(nameof(EI3));
            this.sends.Add(nameof(EI4));
            this.sends.Add(nameof(EI5));
            this.sends.Add(nameof(EI6));
            this.sends.Add(nameof(EMAP1));
            this.sends.Add(nameof(EMAP11));
            this.sends.Add(nameof(EMAP2));
            this.sends.Add(nameof(EMAP3));
            this.sends.Add(nameof(ESEQ1));
            this.sends.Add(nameof(ESEQ2));
            this.sends.Add(nameof(ET1));
            this.sends.Add(nameof(ET2));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(E));
            this.receives.Add(nameof(E1));
            this.receives.Add(nameof(E2));
            this.receives.Add(nameof(EI1));
            this.receives.Add(nameof(EI2));
            this.receives.Add(nameof(EI3));
            this.receives.Add(nameof(EI4));
            this.receives.Add(nameof(EI5));
            this.receives.Add(nameof(EI6));
            this.receives.Add(nameof(EMAP1));
            this.receives.Add(nameof(EMAP11));
            this.receives.Add(nameof(EMAP2));
            this.receives.Add(nameof(EMAP3));
            this.receives.Add(nameof(ESEQ1));
            this.receives.Add(nameof(ESEQ2));
            this.receives.Add(nameof(ET1));
            this.receives.Add(nameof(ET2));
            this.receives.Add(nameof(PHalt));
        }

        public void Anon_1()
        {
            XYZ currentMachine = this;
        }
        public void Anon_2()
        {
            XYZ currentMachine = this;
            IPrtValue payload = this.gotoPayload == null ? ((PEvent<IPrtValue>)currentMachine.ReceivedEvent).PayloadT : (IPrtValue)this.gotoPayload;
            this.gotoPayload = null;
            PrtBool TMP_tmp0_1 = ((PrtBool)false);
            ta = (IPrtValue)(((IPrtValue)((IPrtValue)payload)?.Clone()));
            TMP_tmp0_1 = (PrtBool)((PrtValues.SafeEquals(ta, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp0_1, "");
            currentMachine.PopState();
            throw new PUnreachableCodeException();
        }
        public void Anon_3()
        {
            XYZ currentMachine = this;
            PrtInt payload_1 = this.gotoPayload == null ? ((PEvent<PrtInt>)currentMachine.ReceivedEvent).PayloadT : (PrtInt)this.gotoPayload;
            this.gotoPayload = null;
            PrtInt TMP_tmp0_2 = ((PrtInt)0);
            PrtBool TMP_tmp1_1 = ((PrtBool)false);
            PrtBool TMP_tmp2_1 = ((PrtBool)false);
            IPrtValue TMP_tmp3_1 = null;
            PrtBool TMP_tmp4_1 = ((PrtBool)false);
            TMP_tmp0_2 = (PrtInt)(((PrtInt)payload_1));
            yt = TMP_tmp0_2;
            TMP_tmp1_1 = (PrtBool)((PrtValues.SafeEquals(yt, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp1_1, "");
            yt = (PrtInt)(((PrtInt)((IPrtValue)payload_1)?.Clone()));
            TMP_tmp2_1 = (PrtBool)((PrtValues.SafeEquals(yt, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp2_1, "");
            TMP_tmp3_1 = (IPrtValue)(((IPrtValue)payload_1));
            ta = TMP_tmp3_1;
            TMP_tmp4_1 = (PrtBool)((PrtValues.SafeEquals(yt, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp4_1, "");
            currentMachine.PopState();
            throw new PUnreachableCodeException();
        }
        public void Anon_4()
        {
            XYZ currentMachine = this;
            PrtNamedTuple payload_2 = this.gotoPayload == null ? ((PEvent<PrtNamedTuple>)currentMachine.ReceivedEvent).PayloadT : (PrtNamedTuple)this.gotoPayload;
            this.gotoPayload = null;
            PrtInt TMP_tmp0_3 = ((PrtInt)0);
            PrtBool TMP_tmp1_2 = ((PrtBool)false);
            PrtBool TMP_tmp2_2 = ((PrtBool)false);
            PrtBool TMP_tmp3_2 = ((PrtBool)false);
            PrtBool TMP_tmp4_2 = ((PrtBool)false);
            PrtInt TMP_tmp5_1 = ((PrtInt)0);
            PrtBool TMP_tmp6_1 = ((PrtBool)false);
            PrtBool TMP_tmp7_1 = ((PrtBool)false);
            PrtBool TMP_tmp8_1 = ((PrtBool)false);
            PrtBool TMP_tmp9_1 = ((PrtBool)false);
            tts1 = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)payload_2)?.Clone()));
            TMP_tmp0_3 = (PrtInt)(((PrtNamedTuple)tts1)["a"]);
            TMP_tmp1_2 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp0_3, ((PrtInt)1))));
            TMP_tmp2_2 = (PrtBool)(((PrtNamedTuple)tts1)["b"]);
            TMP_tmp3_2 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp2_2, ((PrtBool)true))));
            TMP_tmp4_2 = (PrtBool)((TMP_tmp1_2) && (TMP_tmp3_2));
            currentMachine.Assert(TMP_tmp4_2, "");
            tts1 = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)payload_2)?.Clone()));
            TMP_tmp5_1 = (PrtInt)(((PrtNamedTuple)tts1)["a"]);
            TMP_tmp6_1 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp5_1, ((PrtInt)1))));
            TMP_tmp7_1 = (PrtBool)(((PrtNamedTuple)tts1)["b"]);
            TMP_tmp8_1 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp7_1, ((PrtBool)true))));
            TMP_tmp9_1 = (PrtBool)((TMP_tmp6_1) && (TMP_tmp8_1));
            currentMachine.Assert(TMP_tmp9_1, "");
            currentMachine.PopState();
            throw new PUnreachableCodeException();
        }
        public void Anon_5()
        {
            XYZ currentMachine = this;
            PrtNamedTuple payload_3 = this.gotoPayload == null ? ((PEvent<PrtNamedTuple>)currentMachine.ReceivedEvent).PayloadT : (PrtNamedTuple)this.gotoPayload;
            this.gotoPayload = null;
            PrtInt TMP_tmp0_4 = ((PrtInt)0);
            PrtBool TMP_tmp1_3 = ((PrtBool)false);
            PrtBool TMP_tmp2_3 = ((PrtBool)false);
            PrtBool TMP_tmp3_3 = ((PrtBool)false);
            PrtBool TMP_tmp4_3 = ((PrtBool)false);
            tts1 = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)payload_3)?.Clone()));
            TMP_tmp0_4 = (PrtInt)(((PrtNamedTuple)tts1)["a"]);
            TMP_tmp1_3 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp0_4, ((PrtInt)2))));
            TMP_tmp2_3 = (PrtBool)(((PrtNamedTuple)tts1)["b"]);
            TMP_tmp3_3 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp2_3, ((PrtBool)false))));
            TMP_tmp4_3 = (PrtBool)((TMP_tmp1_3) && (TMP_tmp3_3));
            currentMachine.Assert(TMP_tmp4_3, "");
            currentMachine.PopState();
            throw new PUnreachableCodeException();
        }
        public void Anon_6()
        {
            XYZ currentMachine = this;
            PrtSeq payload_4 = this.gotoPayload == null ? ((PEvent<PrtSeq>)currentMachine.ReceivedEvent).PayloadT : (PrtSeq)this.gotoPayload;
            this.gotoPayload = null;
            PrtInt TMP_tmp0_5 = ((PrtInt)0);
            PrtBool TMP_tmp1_4 = ((PrtBool)false);
            PrtInt TMP_tmp2_4 = ((PrtInt)0);
            PrtBool TMP_tmp3_4 = ((PrtBool)false);
            IPrtValue TMP_tmp4_4 = null;
            PrtBool TMP_tmp5_2 = ((PrtBool)false);
            IPrtValue TMP_tmp6_2 = null;
            PrtBool TMP_tmp7_2 = ((PrtBool)false);
            PrtSeq TMP_tmp8_2 = new PrtSeq();
            IPrtValue TMP_tmp9_2 = null;
            PrtBool TMP_tmp10_1 = ((PrtBool)false);
            s_1 = (PrtSeq)(((PrtSeq)((IPrtValue)payload_4)?.Clone()));
            TMP_tmp0_5 = (PrtInt)(((PrtSeq)s_1)[((PrtInt)0)]);
            TMP_tmp1_4 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp0_5, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp1_4, "");
            s_1 = (PrtSeq)(((PrtSeq)((IPrtValue)payload_4)?.Clone()));
            TMP_tmp2_4 = (PrtInt)(((PrtSeq)s_1)[((PrtInt)0)]);
            TMP_tmp3_4 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp2_4, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp3_4, "");
            s1_1 = (PrtSeq)(new PrtSeq(((PrtSeq)((IPrtValue)payload_4)?.Clone()).Cast<IPrtValue>()));
            TMP_tmp4_4 = (IPrtValue)(((PrtSeq)s1_1)[((PrtInt)0)]);
            TMP_tmp5_2 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp4_4, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp5_2, "");
            s1_1 = (PrtSeq)(new PrtSeq(((PrtSeq)((IPrtValue)payload_4)?.Clone()).Cast<IPrtValue>()));
            TMP_tmp6_2 = (IPrtValue)(((PrtSeq)s1_1)[((PrtInt)0)]);
            TMP_tmp7_2 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp6_2, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp7_2, "");
            TMP_tmp8_2 = (PrtSeq)(((PrtSeq)payload_4));
            s1_1 = TMP_tmp8_2;
            TMP_tmp9_2 = (IPrtValue)(((PrtSeq)s1_1)[((PrtInt)0)]);
            TMP_tmp10_1 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp9_2, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp10_1, "");
            currentMachine.PopState();
            throw new PUnreachableCodeException();
        }
        public void Anon_7()
        {
            XYZ currentMachine = this;
            PrtSeq payload_5 = this.gotoPayload == null ? ((PEvent<PrtSeq>)currentMachine.ReceivedEvent).PayloadT : (PrtSeq)this.gotoPayload;
            this.gotoPayload = null;
            PrtInt TMP_tmp0_6 = ((PrtInt)0);
            PrtBool TMP_tmp1_5 = ((PrtBool)false);
            PrtInt TMP_tmp2_5 = ((PrtInt)0);
            PrtBool TMP_tmp3_5 = ((PrtBool)false);
            IPrtValue TMP_tmp4_5 = null;
            PrtBool TMP_tmp5_3 = ((PrtBool)false);
            IPrtValue TMP_tmp6_3 = null;
            PrtBool TMP_tmp7_3 = ((PrtBool)false);
            PrtSeq TMP_tmp8_3 = new PrtSeq();
            IPrtValue TMP_tmp9_3 = null;
            PrtBool TMP_tmp10_2 = ((PrtBool)false);
            s_1 = (PrtSeq)(((PrtSeq)((IPrtValue)payload_5)?.Clone()));
            TMP_tmp0_6 = (PrtInt)(((PrtSeq)s_1)[((PrtInt)0)]);
            TMP_tmp1_5 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp0_6, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp1_5, "");
            s_1 = (PrtSeq)(((PrtSeq)((IPrtValue)payload_5)?.Clone()));
            TMP_tmp2_5 = (PrtInt)(((PrtSeq)s_1)[((PrtInt)0)]);
            TMP_tmp3_5 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp2_5, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp3_5, "");
            s1_1 = (PrtSeq)(new PrtSeq(((PrtSeq)((IPrtValue)payload_5)?.Clone()).Cast<IPrtValue>()));
            TMP_tmp4_5 = (IPrtValue)(((PrtSeq)s1_1)[((PrtInt)0)]);
            TMP_tmp5_3 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp4_5, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp5_3, "");
            s1_1 = (PrtSeq)(new PrtSeq(((PrtSeq)((IPrtValue)payload_5)?.Clone()).Cast<IPrtValue>()));
            TMP_tmp6_3 = (IPrtValue)(((PrtSeq)s1_1)[((PrtInt)0)]);
            TMP_tmp7_3 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp6_3, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp7_3, "");
            TMP_tmp8_3 = (PrtSeq)(((PrtSeq)payload_5));
            s1_1 = TMP_tmp8_3;
            TMP_tmp9_3 = (IPrtValue)(((PrtSeq)s1_1)[((PrtInt)0)]);
            TMP_tmp10_2 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp9_3, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp10_2, "");
            currentMachine.PopState();
            throw new PUnreachableCodeException();
        }
        public void Anon_8()
        {
            XYZ currentMachine = this;
            PrtMap payload_6 = this.gotoPayload == null ? ((PEvent<PrtMap>)currentMachine.ReceivedEvent).PayloadT : (PrtMap)this.gotoPayload;
            this.gotoPayload = null;
            PrtInt TMP_tmp0_7 = ((PrtInt)0);
            PrtBool TMP_tmp1_6 = ((PrtBool)false);
            PrtInt TMP_tmp2_6 = ((PrtInt)0);
            PrtBool TMP_tmp3_6 = ((PrtBool)false);
            PrtBool TMP_tmp4_6 = ((PrtBool)false);
            PrtMap TMP_tmp5_4 = new PrtMap();
            PrtMap TMP_tmp6_4 = new PrtMap();
            PrtMap TMP_tmp7_4 = new PrtMap();
            PrtMap TMP_tmp8_4 = new PrtMap();
            mi = (PrtMap)(((PrtMap)((IPrtValue)payload_6)?.Clone()));
            ((PrtMap)mi)[((PrtInt)0)] = (PrtInt)(((PrtInt)0));
            ((PrtMap)mi)[((PrtInt)3)] = (PrtInt)(((PrtInt)3));
            TMP_tmp0_7 = (PrtInt)(((PrtMap)mi)[((PrtInt)0)]);
            TMP_tmp1_6 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp0_7, ((PrtInt)0))));
            TMP_tmp2_6 = (PrtInt)(((PrtMap)mi)[((PrtInt)3)]);
            TMP_tmp3_6 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp2_6, ((PrtInt)3))));
            TMP_tmp4_6 = (PrtBool)((TMP_tmp1_6) && (TMP_tmp3_6));
            currentMachine.Assert(TMP_tmp4_6, "");
            TMP_tmp5_4 = (PrtMap)(new PrtMap());
            mi = TMP_tmp5_4;
            mi = (PrtMap)(((PrtMap)((IPrtValue)payload_6)?.Clone()));
            ma = (PrtMap)(new PrtMap(((PrtMap)((IPrtValue)payload_6)?.Clone())));
            TMP_tmp6_4 = (PrtMap)(new PrtMap());
            ma = TMP_tmp6_4;
            ma = (PrtMap)(new PrtMap(((PrtMap)((IPrtValue)payload_6)?.Clone())));
            TMP_tmp7_4 = (PrtMap)(new PrtMap());
            ma = TMP_tmp7_4;
            TMP_tmp8_4 = (PrtMap)(((PrtMap)payload_6));
            ma = TMP_tmp8_4;
            currentMachine.PopState();
            throw new PUnreachableCodeException();
        }
        public void Anon_9()
        {
            XYZ currentMachine = this;
            PrtMap payload_7 = this.gotoPayload == null ? ((PEvent<PrtMap>)currentMachine.ReceivedEvent).PayloadT : (PrtMap)this.gotoPayload;
            this.gotoPayload = null;
            PrtMap TMP_tmp0_8 = new PrtMap();
            PrtInt TMP_tmp1_7 = ((PrtInt)0);
            PrtBool TMP_tmp2_7 = ((PrtBool)false);
            PrtInt TMP_tmp3_7 = ((PrtInt)0);
            PrtBool TMP_tmp4_7 = ((PrtBool)false);
            PrtBool TMP_tmp5_5 = ((PrtBool)false);
            PrtMap TMP_tmp6_5 = new PrtMap();
            PrtInt TMP_tmp7_5 = ((PrtInt)0);
            PrtBool TMP_tmp8_5 = ((PrtBool)false);
            PrtInt TMP_tmp9_4 = ((PrtInt)0);
            PrtBool TMP_tmp10_3 = ((PrtBool)false);
            PrtBool TMP_tmp11_1 = ((PrtBool)false);
            IPrtValue TMP_tmp12_1 = null;
            PrtBool TMP_tmp13_1 = ((PrtBool)false);
            IPrtValue TMP_tmp14_1 = null;
            PrtBool TMP_tmp15_1 = ((PrtBool)false);
            PrtBool TMP_tmp16_1 = ((PrtBool)false);
            PrtMap TMP_tmp17_1 = new PrtMap();
            IPrtValue TMP_tmp18_1 = null;
            PrtBool TMP_tmp19_1 = ((PrtBool)false);
            IPrtValue TMP_tmp20_1 = null;
            PrtBool TMP_tmp21_1 = ((PrtBool)false);
            PrtBool TMP_tmp22_1 = ((PrtBool)false);
            PrtMap TMP_tmp23_1 = new PrtMap();
            PrtMap TMP_tmp24_1 = new PrtMap();
            IPrtValue TMP_tmp25_1 = null;
            PrtBool TMP_tmp26_1 = ((PrtBool)false);
            IPrtValue TMP_tmp27_1 = null;
            PrtBool TMP_tmp28_1 = ((PrtBool)false);
            PrtBool TMP_tmp29_1 = ((PrtBool)false);
            TMP_tmp0_8 = (PrtMap)(new PrtMap());
            mi = TMP_tmp0_8;
            mi = (PrtMap)(((PrtMap)((IPrtValue)payload_7)?.Clone()));
            TMP_tmp1_7 = (PrtInt)(((PrtMap)mi)[((PrtInt)0)]);
            TMP_tmp2_7 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp1_7, ((PrtInt)1))));
            TMP_tmp3_7 = (PrtInt)(((PrtMap)mi)[((PrtInt)3)]);
            TMP_tmp4_7 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp3_7, ((PrtInt)3))));
            TMP_tmp5_5 = (PrtBool)((TMP_tmp2_7) && (TMP_tmp4_7));
            currentMachine.Assert(TMP_tmp5_5, "");
            TMP_tmp6_5 = (PrtMap)(new PrtMap());
            mi = TMP_tmp6_5;
            mi = (PrtMap)(((PrtMap)((IPrtValue)payload_7)?.Clone()));
            TMP_tmp7_5 = (PrtInt)(((PrtMap)mi)[((PrtInt)0)]);
            TMP_tmp8_5 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp7_5, ((PrtInt)1))));
            TMP_tmp9_4 = (PrtInt)(((PrtMap)mi)[((PrtInt)3)]);
            TMP_tmp10_3 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp9_4, ((PrtInt)3))));
            TMP_tmp11_1 = (PrtBool)((TMP_tmp8_5) && (TMP_tmp10_3));
            currentMachine.Assert(TMP_tmp11_1, "");
            ma = (PrtMap)(new PrtMap(((PrtMap)((IPrtValue)payload_7)?.Clone())));
            TMP_tmp12_1 = (IPrtValue)(((PrtMap)ma)[((PrtInt)0)]);
            TMP_tmp13_1 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp12_1, ((PrtInt)1))));
            TMP_tmp14_1 = (IPrtValue)(((PrtMap)ma)[((PrtInt)3)]);
            TMP_tmp15_1 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp14_1, ((PrtInt)3))));
            TMP_tmp16_1 = (PrtBool)((TMP_tmp13_1) && (TMP_tmp15_1));
            currentMachine.Assert(TMP_tmp16_1, "");
            TMP_tmp17_1 = (PrtMap)(new PrtMap());
            ma = TMP_tmp17_1;
            ma = (PrtMap)(new PrtMap(((PrtMap)((IPrtValue)payload_7)?.Clone())));
            TMP_tmp18_1 = (IPrtValue)(((PrtMap)ma)[((PrtInt)0)]);
            TMP_tmp19_1 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp18_1, ((PrtInt)1))));
            TMP_tmp20_1 = (IPrtValue)(((PrtMap)ma)[((PrtInt)3)]);
            TMP_tmp21_1 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp20_1, ((PrtInt)3))));
            TMP_tmp22_1 = (PrtBool)((TMP_tmp19_1) && (TMP_tmp21_1));
            currentMachine.Assert(TMP_tmp22_1, "");
            TMP_tmp23_1 = (PrtMap)(new PrtMap());
            ma = TMP_tmp23_1;
            TMP_tmp24_1 = (PrtMap)(((PrtMap)payload_7));
            ma = TMP_tmp24_1;
            TMP_tmp25_1 = (IPrtValue)(((PrtMap)ma)[((PrtInt)0)]);
            TMP_tmp26_1 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp25_1, ((PrtInt)1))));
            TMP_tmp27_1 = (IPrtValue)(((PrtMap)ma)[((PrtInt)3)]);
            TMP_tmp28_1 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp27_1, ((PrtInt)3))));
            TMP_tmp29_1 = (PrtBool)((TMP_tmp26_1) && (TMP_tmp28_1));
            currentMachine.Assert(TMP_tmp29_1, "");
            currentMachine.PopState();
            throw new PUnreachableCodeException();
        }
        public void Anon_10()
        {
            XYZ currentMachine = this;
            PrtMap payload_8 = this.gotoPayload == null ? ((PEvent<PrtMap>)currentMachine.ReceivedEvent).PayloadT : (PrtMap)this.gotoPayload;
            this.gotoPayload = null;
            PrtMap TMP_tmp0_9 = new PrtMap();
            PrtMap TMP_tmp1_8 = new PrtMap();
            PrtMap TMP_tmp2_8 = new PrtMap();
            PrtMap TMP_tmp3_8 = new PrtMap();
            mi = (PrtMap)(((PrtMap)((IPrtValue)payload_8)?.Clone()));
            TMP_tmp0_9 = (PrtMap)(new PrtMap());
            mi = TMP_tmp0_9;
            mi = (PrtMap)(((PrtMap)((IPrtValue)payload_8)?.Clone()));
            ma = (PrtMap)(new PrtMap(((PrtMap)((IPrtValue)payload_8)?.Clone())));
            TMP_tmp1_8 = (PrtMap)(new PrtMap());
            ma = TMP_tmp1_8;
            ma = (PrtMap)(new PrtMap(((PrtMap)((IPrtValue)payload_8)?.Clone())));
            TMP_tmp2_8 = (PrtMap)(new PrtMap());
            ma = TMP_tmp2_8;
            TMP_tmp3_8 = (PrtMap)(((PrtMap)payload_8));
            ma = TMP_tmp3_8;
            currentMachine.PopState();
            throw new PUnreachableCodeException();
        }
        public void Anon_11()
        {
            XYZ currentMachine = this;
            PrtMap payload_9 = this.gotoPayload == null ? ((PEvent<PrtMap>)currentMachine.ReceivedEvent).PayloadT : (PrtMap)this.gotoPayload;
            this.gotoPayload = null;
            PrtInt TMP_tmp0_10 = ((PrtInt)0);
            PrtBool TMP_tmp1_9 = ((PrtBool)false);
            PrtInt TMP_tmp2_9 = ((PrtInt)0);
            PrtBool TMP_tmp3_9 = ((PrtBool)false);
            PrtBool TMP_tmp4_8 = ((PrtBool)false);
            PrtMap TMP_tmp5_6 = new PrtMap();
            PrtInt TMP_tmp6_6 = ((PrtInt)0);
            PrtBool TMP_tmp7_6 = ((PrtBool)false);
            PrtInt TMP_tmp8_6 = ((PrtInt)0);
            PrtBool TMP_tmp9_5 = ((PrtBool)false);
            PrtBool TMP_tmp10_4 = ((PrtBool)false);
            IPrtValue TMP_tmp11_2 = null;
            PrtBool TMP_tmp12_2 = ((PrtBool)false);
            IPrtValue TMP_tmp13_2 = null;
            PrtBool TMP_tmp14_2 = ((PrtBool)false);
            PrtBool TMP_tmp15_2 = ((PrtBool)false);
            PrtMap TMP_tmp16_2 = new PrtMap();
            IPrtValue TMP_tmp17_2 = null;
            PrtBool TMP_tmp18_2 = ((PrtBool)false);
            IPrtValue TMP_tmp19_2 = null;
            PrtBool TMP_tmp20_2 = ((PrtBool)false);
            PrtBool TMP_tmp21_2 = ((PrtBool)false);
            PrtMap TMP_tmp22_2 = new PrtMap();
            PrtMap TMP_tmp23_2 = new PrtMap();
            IPrtValue TMP_tmp24_2 = null;
            PrtBool TMP_tmp25_2 = ((PrtBool)false);
            IPrtValue TMP_tmp26_2 = null;
            PrtBool TMP_tmp27_2 = ((PrtBool)false);
            PrtBool TMP_tmp28_2 = ((PrtBool)false);
            mi = (PrtMap)(((PrtMap)((IPrtValue)payload_9)?.Clone()));
            TMP_tmp0_10 = (PrtInt)(((PrtMap)mi)[((PrtInt)0)]);
            TMP_tmp1_9 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp0_10, ((PrtInt)1))));
            TMP_tmp2_9 = (PrtInt)(((PrtMap)mi)[((PrtInt)3)]);
            TMP_tmp3_9 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp2_9, ((PrtInt)3))));
            TMP_tmp4_8 = (PrtBool)((TMP_tmp1_9) && (TMP_tmp3_9));
            currentMachine.Assert(TMP_tmp4_8, "");
            TMP_tmp5_6 = (PrtMap)(new PrtMap());
            mi = TMP_tmp5_6;
            mi = (PrtMap)(((PrtMap)((IPrtValue)payload_9)?.Clone()));
            TMP_tmp6_6 = (PrtInt)(((PrtMap)mi)[((PrtInt)0)]);
            TMP_tmp7_6 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp6_6, ((PrtInt)1))));
            TMP_tmp8_6 = (PrtInt)(((PrtMap)mi)[((PrtInt)3)]);
            TMP_tmp9_5 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp8_6, ((PrtInt)3))));
            TMP_tmp10_4 = (PrtBool)((TMP_tmp7_6) && (TMP_tmp9_5));
            currentMachine.Assert(TMP_tmp10_4, "");
            ma = (PrtMap)(new PrtMap(((PrtMap)((IPrtValue)payload_9)?.Clone())));
            TMP_tmp11_2 = (IPrtValue)(((PrtMap)ma)[((PrtInt)0)]);
            TMP_tmp12_2 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp11_2, ((PrtInt)1))));
            TMP_tmp13_2 = (IPrtValue)(((PrtMap)ma)[((PrtInt)3)]);
            TMP_tmp14_2 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp13_2, ((PrtInt)3))));
            TMP_tmp15_2 = (PrtBool)((TMP_tmp12_2) && (TMP_tmp14_2));
            currentMachine.Assert(TMP_tmp15_2, "");
            TMP_tmp16_2 = (PrtMap)(new PrtMap());
            ma = TMP_tmp16_2;
            ma = (PrtMap)(new PrtMap(((PrtMap)((IPrtValue)payload_9)?.Clone())));
            TMP_tmp17_2 = (IPrtValue)(((PrtMap)ma)[((PrtInt)0)]);
            TMP_tmp18_2 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp17_2, ((PrtInt)1))));
            TMP_tmp19_2 = (IPrtValue)(((PrtMap)ma)[((PrtInt)3)]);
            TMP_tmp20_2 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp19_2, ((PrtInt)3))));
            TMP_tmp21_2 = (PrtBool)((TMP_tmp18_2) && (TMP_tmp20_2));
            currentMachine.Assert(TMP_tmp21_2, "");
            TMP_tmp22_2 = (PrtMap)(new PrtMap());
            ma = TMP_tmp22_2;
            TMP_tmp23_2 = (PrtMap)(((PrtMap)payload_9));
            ma = TMP_tmp23_2;
            TMP_tmp24_2 = (IPrtValue)(((PrtMap)ma)[((PrtInt)0)]);
            TMP_tmp25_2 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp24_2, ((PrtInt)1))));
            TMP_tmp26_2 = (IPrtValue)(((PrtMap)ma)[((PrtInt)3)]);
            TMP_tmp27_2 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp26_2, ((PrtInt)3))));
            TMP_tmp28_2 = (PrtBool)((TMP_tmp25_2) && (TMP_tmp27_2));
            currentMachine.Assert(TMP_tmp28_2, "");
            currentMachine.PopState();
            throw new PUnreachableCodeException();
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(init))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_1))]
        [OnEventPushState(typeof(EI1), typeof(XYZEI1))]
        [OnEventPushState(typeof(EI6), typeof(XYZEI6))]
        [OnEventPushState(typeof(ET1), typeof(XYZET1))]
        [OnEventPushState(typeof(ET2), typeof(XYZET2))]
        [OnEventPushState(typeof(ESEQ1), typeof(XYZESEQ1))]
        [OnEventPushState(typeof(ESEQ2), typeof(XYZESEQ2))]
        [OnEventPushState(typeof(EMAP1), typeof(XYZEMAP1))]
        [OnEventPushState(typeof(EMAP11), typeof(XYZEMAP11))]
        [OnEventPushState(typeof(EMAP2), typeof(XYZEMAP2))]
        [OnEventPushState(typeof(EMAP3), typeof(XYZEMAP3))]
        class init : MachineState
        {
        }
        [OnEntry(nameof(Anon_2))]
        class XYZEI1 : MachineState
        {
        }
        [OnEntry(nameof(Anon_3))]
        class XYZEI6 : MachineState
        {
        }
        [OnEntry(nameof(Anon_4))]
        class XYZET1 : MachineState
        {
        }
        [OnEntry(nameof(Anon_5))]
        class XYZET2 : MachineState
        {
        }
        [OnEntry(nameof(Anon_6))]
        class XYZESEQ1 : MachineState
        {
        }
        [OnEntry(nameof(Anon_7))]
        class XYZESEQ2 : MachineState
        {
        }
        [OnEntry(nameof(Anon_8))]
        class XYZEMAP1 : MachineState
        {
        }
        [OnEntry(nameof(Anon_9))]
        class XYZEMAP11 : MachineState
        {
        }
        [OnEntry(nameof(Anon_10))]
        class XYZEMAP2 : MachineState
        {
        }
        [OnEntry(nameof(Anon_11))]
        class XYZEMAP3 : MachineState
        {
        }
    }
    public class DefaultImpl
    {
        public static void InitializeLinkMap()
        {
            PModule.linkMap[nameof(I_Main)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_XYZ), nameof(I_XYZ));
            PModule.linkMap[nameof(I_XYZ)] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Add(nameof(I_Main), typeof(Main));
            PModule.interfaceDefinitionMap.Add(nameof(I_XYZ), typeof(XYZ));
        }

        public static void InitializeMonitorObserves()
        {
        }

        public static void InitializeMonitorMap(PSharpRuntime runtime)
        {
        }


        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.SetLogger(new PLogger());
            PModule.runtime = runtime;
            PHelper.InitializeInterfaces();
            InitializeLinkMap();
            InitializeInterfaceDefMap();
            InitializeMonitorMap(runtime);
            InitializeMonitorObserves();
            runtime.CreateMachine(typeof(_GodMachine), new _GodMachine.Config(typeof(Main)));
        }
    }
    public class I_Main : PMachineValue
    {
        public I_Main(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_XYZ : PMachineValue
    {
        public I_XYZ(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.AddInterface(nameof(I_Main), nameof(E), nameof(E1), nameof(E2), nameof(EI1), nameof(EI2), nameof(EI3), nameof(EI4), nameof(EI5), nameof(EI6), nameof(EMAP1), nameof(EMAP11), nameof(EMAP2), nameof(EMAP3), nameof(ESEQ1), nameof(ESEQ2), nameof(ET1), nameof(ET2), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_XYZ), nameof(E), nameof(E1), nameof(E2), nameof(EI1), nameof(EI2), nameof(EI3), nameof(EI4), nameof(EI5), nameof(EI6), nameof(EMAP1), nameof(EMAP11), nameof(EMAP2), nameof(EMAP3), nameof(ESEQ1), nameof(ESEQ2), nameof(ET1), nameof(ET2), nameof(PHalt));
        }
    }

}
#pragma warning restore 162, 219, 414




namespace Main
{
    public class _TestRegression
    {
        public static void Main(string[] args)
        {
            // Optional: increases verbosity level to see the P# runtime log.
            var configuration = Configuration.Create().WithVerbosityEnabled(2);

            // Creates a new P# runtime instance, and passes an optional configuration.
            var runtime = PSharpRuntime.Create(configuration);

            // Executes the P# program.
            DefaultImpl.Execute(runtime);

            // The P# runtime executes asynchronously, so we wait
            // to not terminate the process.
            Console.WriteLine("Press Enter to terminate...");
            Console.ReadLine();
        }
    }
}
