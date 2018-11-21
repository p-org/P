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
    internal partial class E : PEvent
    {
        static E() { AssertVal = 1; AssumeVal = -1; }
        public E() : base() { }
        public E(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new E(); }
    }
    internal partial class E1 : PEvent
    {
        static E1() { AssertVal = 1; AssumeVal = -1; }
        public E1() : base() { }
        public E1(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new E1(); }
    }
    internal partial class E2 : PEvent
    {
        static E2() { AssertVal = 1; AssumeVal = -1; }
        public E2() : base() { }
        public E2(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new E2(); }
    }
    internal partial class E3 : PEvent
    {
        static E3() { AssertVal = 1; AssumeVal = -1; }
        public E3() : base() { }
        public E3(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new E3(); }
    }
    internal partial class Main : PMachine
    {
        private PrtNamedTuple t = (new PrtNamedTuple(new string[] { "a", "b" }, new PrtSeq(), new PrtMap()));
        private PrtNamedTuple t1 = (new PrtNamedTuple(new string[] { "a", "b" }, new PrtSeq(), new PrtMap()));
        private PrtNamedTuple ts = (new PrtNamedTuple(new string[] { "a", "b" }, ((PrtInt)0), ((PrtInt)0)));
        private PrtTuple tt = (new PrtTuple(((PrtInt)0), ((PrtInt)0)));
        private PrtTuple te = (new PrtTuple(((PrtInt)0), (PEvent)PrtValues.NullEvent));
        private PrtInt y = ((PrtInt)0);
        private PrtBool b = ((PrtBool)false);
        private PEvent e = (PEvent)PrtValues.NullEvent;
        private IPrtValue a = (PMachineValue)PrtValues.Null;
        private PrtInt tmp = ((PrtInt)0);
        private PrtInt tmp1 = ((PrtInt)0);
        private PrtNamedTuple tmp2 = (new PrtNamedTuple(new string[] { "a", "b" }, new PrtSeq(), new PrtMap()));
        private PrtMap tmp3 = new PrtMap();
        private PrtSeq s = new PrtSeq();
        private PrtSeq s1 = new PrtSeq();
        private PrtSeq s2 = new PrtSeq();
        private PrtSeq s3 = new PrtSeq();
        private PrtSeq s33 = new PrtSeq();
        private PrtSeq s4 = new PrtSeq();
        private PrtSeq s8 = new PrtSeq();
        private PrtSeq s5 = new PrtSeq();
        private PrtSeq s6 = new PrtSeq();
        private PrtSeq s7 = new PrtSeq();
        private PrtSeq s9 = new PrtSeq();
        private PrtSeq s10 = new PrtSeq();
        private PrtSeq s11 = new PrtSeq();
        private PrtSeq s12 = new PrtSeq();
        private PrtSeq tmp4 = new PrtSeq();
        private PrtInt i = ((PrtInt)0);
        private PMachineValue mac = (PMachineValue)PrtValues.Null;
        private PrtMap m1 = new PrtMap();
        private PrtMap m4 = new PrtMap();
        private PrtMap m3 = new PrtMap();
        private PrtMap m5 = new PrtMap();
        private PrtMap m6 = new PrtMap();
        private PrtMap m2 = new PrtMap();
        private PrtMap m7 = new PrtMap();
        private PrtMap m8 = new PrtMap();
        private PrtMap m9 = new PrtMap();
        private PrtMap m10 = new PrtMap();
        private PrtMap m11 = new PrtMap();
        public class ConstructorEvent : PEvent { public ConstructorEvent(IPrtValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Main()
        {
            this.sends.Add(nameof(E));
            this.sends.Add(nameof(E1));
            this.sends.Add(nameof(E2));
            this.sends.Add(nameof(E3));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(E));
            this.receives.Add(nameof(E1));
            this.receives.Add(nameof(E2));
            this.receives.Add(nameof(E3));
            this.receives.Add(nameof(PHalt));
            this.creates.Add(nameof(I_XYZ));
        }

        public void Anon()
        {
            Main currentMachine = this;
            PrtInt TMP_tmp0 = ((PrtInt)0);
            PrtBool TMP_tmp1 = ((PrtBool)false);
            PEvent TMP_tmp2 = (PEvent)PrtValues.NullEvent;
            PMachineValue TMP_tmp3 = (PMachineValue)PrtValues.Null;
            IPrtValue TMP_tmp4 = (PMachineValue)PrtValues.Null;
            PrtMap TMP_tmp5 = new PrtMap();
            PrtInt TMP_tmp6 = ((PrtInt)0);
            PrtInt TMP_tmp7 = ((PrtInt)0);
            PrtInt TMP_tmp8 = ((PrtInt)0);
            PrtNamedTuple TMP_tmp9 = (new PrtNamedTuple(new string[] { "a", "b" }, ((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp10 = ((PrtInt)0);
            PrtInt TMP_tmp11 = ((PrtInt)0);
            PrtInt TMP_tmp12 = ((PrtInt)0);
            PrtTuple TMP_tmp13 = (new PrtTuple(((PrtInt)0), ((PrtInt)0)));
            PrtTuple TMP_tmp14 = (new PrtTuple(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp15 = ((PrtInt)0);
            PEvent TMP_tmp16 = (PEvent)PrtValues.NullEvent;
            PrtTuple TMP_tmp17 = (new PrtTuple(((PrtInt)0), (PEvent)PrtValues.NullEvent));
            PrtInt TMP_tmp18 = ((PrtInt)0);
            PEvent TMP_tmp19 = (PEvent)PrtValues.NullEvent;
            PrtTuple TMP_tmp20 = (new PrtTuple(((PrtInt)0), (PEvent)PrtValues.NullEvent));
            PrtInt TMP_tmp21 = ((PrtInt)0);
            PEvent TMP_tmp22 = (PEvent)PrtValues.NullEvent;
            PrtTuple TMP_tmp23 = (new PrtTuple(((PrtInt)0), (PEvent)PrtValues.NullEvent));
            PrtInt TMP_tmp24 = ((PrtInt)0);
            PrtInt TMP_tmp25 = ((PrtInt)0);
            PrtInt TMP_tmp26 = ((PrtInt)0);
            PrtInt TMP_tmp27 = ((PrtInt)0);
            PrtSeq TMP_tmp28 = new PrtSeq();
            PrtInt TMP_tmp29 = ((PrtInt)0);
            PrtInt TMP_tmp30 = ((PrtInt)0);
            PrtInt TMP_tmp31 = ((PrtInt)0);
            PrtInt TMP_tmp32 = ((PrtInt)0);
            PrtInt TMP_tmp33 = ((PrtInt)0);
            PrtInt TMP_tmp34 = ((PrtInt)0);
            PrtInt TMP_tmp35 = ((PrtInt)0);
            PEvent TMP_tmp36 = (PEvent)PrtValues.NullEvent;
            PEvent TMP_tmp37 = (PEvent)PrtValues.NullEvent;
            PEvent TMP_tmp38 = (PEvent)PrtValues.NullEvent;
            PEvent TMP_tmp39 = (PEvent)PrtValues.NullEvent;
            PrtInt TMP_tmp40 = ((PrtInt)0);
            PrtBool TMP_tmp41 = ((PrtBool)false);
            PEvent TMP_tmp42 = (PEvent)PrtValues.NullEvent;
            PrtInt TMP_tmp43 = ((PrtInt)0);
            PrtInt TMP_tmp44 = ((PrtInt)0);
            PrtSeq TMP_tmp45 = new PrtSeq();
            PMachineValue TMP_tmp46 = (PMachineValue)PrtValues.Null;
            PrtBool TMP_tmp47 = ((PrtBool)false);
            PrtSeq TMP_tmp48 = new PrtSeq();
            PrtInt TMP_tmp49 = ((PrtInt)0);
            PrtBool TMP_tmp50 = ((PrtBool)false);
            PrtSeq TMP_tmp51 = new PrtSeq();
            PrtInt TMP_tmp52 = ((PrtInt)0);
            PrtBool TMP_tmp53 = ((PrtBool)false);
            PrtInt TMP_tmp54 = ((PrtInt)0);
            PrtBool TMP_tmp55 = ((PrtBool)false);
            PrtInt TMP_tmp56 = ((PrtInt)0);
            PrtBool TMP_tmp57 = ((PrtBool)false);
            PrtInt TMP_tmp58 = ((PrtInt)0);
            PrtBool TMP_tmp59 = ((PrtBool)false);
            PEvent TMP_tmp60 = (PEvent)PrtValues.NullEvent;
            PrtSeq TMP_tmp61 = new PrtSeq();
            PrtSeq TMP_tmp62 = new PrtSeq();
            PrtSeq TMP_tmp63 = new PrtSeq();
            PrtBool TMP_tmp64 = ((PrtBool)false);
            PrtSeq TMP_tmp65 = new PrtSeq();
            IPrtValue TMP_tmp66 = (PMachineValue)PrtValues.Null;
            PrtBool TMP_tmp67 = ((PrtBool)false);
            PrtInt TMP_tmp68 = ((PrtInt)0);
            PrtBool TMP_tmp69 = ((PrtBool)false);
            IPrtValue TMP_tmp70 = (PMachineValue)PrtValues.Null;
            PrtBool TMP_tmp71 = ((PrtBool)false);
            PrtSeq TMP_tmp72 = new PrtSeq();
            PrtSeq TMP_tmp73 = new PrtSeq();
            PrtSeq TMP_tmp74 = new PrtSeq();
            PrtBool TMP_tmp75 = ((PrtBool)false);
            PrtSeq TMP_tmp76 = new PrtSeq();
            IPrtValue TMP_tmp77 = (PMachineValue)PrtValues.Null;
            PrtBool TMP_tmp78 = ((PrtBool)false);
            PrtSeq TMP_tmp79 = new PrtSeq();
            PrtMap TMP_tmp80 = new PrtMap();
            PrtInt TMP_tmp81 = ((PrtInt)0);
            PrtInt TMP_tmp82 = ((PrtInt)0);
            PrtMap TMP_tmp83 = new PrtMap();
            PrtMap TMP_tmp84 = new PrtMap();
            IPrtValue TMP_tmp85 = (PMachineValue)PrtValues.Null;
            PrtBool TMP_tmp86 = ((PrtBool)false);
            PrtMap TMP_tmp87 = new PrtMap();
            IPrtValue TMP_tmp88 = (PMachineValue)PrtValues.Null;
            PrtBool TMP_tmp89 = ((PrtBool)false);
            PrtMap TMP_tmp90 = new PrtMap();
            IPrtValue TMP_tmp91 = (PMachineValue)PrtValues.Null;
            PrtBool TMP_tmp92 = ((PrtBool)false);
            PrtMap TMP_tmp93 = new PrtMap();
            IPrtValue TMP_tmp94 = (PMachineValue)PrtValues.Null;
            PrtBool TMP_tmp95 = ((PrtBool)false);
            PrtMap TMP_tmp96 = new PrtMap();
            IPrtValue TMP_tmp97 = (PMachineValue)PrtValues.Null;
            PrtSeq TMP_tmp98 = new PrtSeq();
            PrtInt TMP_tmp99 = ((PrtInt)0);
            PrtBool TMP_tmp100 = ((PrtBool)false);
            PrtMap TMP_tmp101 = new PrtMap();
            IPrtValue TMP_tmp102 = (PMachineValue)PrtValues.Null;
            PrtSeq TMP_tmp103 = new PrtSeq();
            PrtInt TMP_tmp104 = ((PrtInt)0);
            PrtBool TMP_tmp105 = ((PrtBool)false);
            PrtInt TMP_tmp106 = ((PrtInt)0);
            PrtSeq TMP_tmp107 = new PrtSeq();
            PrtMap TMP_tmp108 = new PrtMap();
            PrtNamedTuple TMP_tmp109 = (new PrtNamedTuple(new string[] { "a", "b" }, new PrtSeq(), new PrtMap()));
            PrtInt TMP_tmp110 = ((PrtInt)0);
            PrtSeq TMP_tmp111 = new PrtSeq();
            PrtInt TMP_tmp112 = ((PrtInt)0);
            PrtBool TMP_tmp113 = ((PrtBool)false);
            PrtInt TMP_tmp114 = ((PrtInt)0);
            PrtSeq TMP_tmp115 = new PrtSeq();
            PrtInt TMP_tmp116 = ((PrtInt)0);
            PrtBool TMP_tmp117 = ((PrtBool)false);
            PrtInt TMP_tmp118 = ((PrtInt)0);
            PrtSeq TMP_tmp119 = new PrtSeq();
            PrtInt TMP_tmp120 = ((PrtInt)0);
            PrtBool TMP_tmp121 = ((PrtBool)false);
            PrtSeq TMP_tmp122 = new PrtSeq();
            PrtInt TMP_tmp123 = ((PrtInt)0);
            PrtBool TMP_tmp124 = ((PrtBool)false);
            PrtInt TMP_tmp125 = ((PrtInt)0);
            PrtInt TMP_tmp126 = ((PrtInt)0);
            PrtTuple TMP_tmp127 = (new PrtTuple(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp128 = ((PrtInt)0);
            PrtInt TMP_tmp129 = ((PrtInt)0);
            PrtTuple TMP_tmp130 = (new PrtTuple(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp131 = ((PrtInt)0);
            PrtInt TMP_tmp132 = ((PrtInt)0);
            PrtTuple TMP_tmp133 = (new PrtTuple(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp134 = ((PrtInt)0);
            PrtInt TMP_tmp135 = ((PrtInt)0);
            PrtTuple TMP_tmp136 = (new PrtTuple(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp137 = ((PrtInt)0);
            PrtInt TMP_tmp138 = ((PrtInt)0);
            PrtTuple TMP_tmp139 = (new PrtTuple(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp140 = ((PrtInt)0);
            PrtInt TMP_tmp141 = ((PrtInt)0);
            PrtTuple TMP_tmp142 = (new PrtTuple(((PrtInt)0), ((PrtInt)0)));
            PrtMap TMP_tmp143 = new PrtMap();
            PrtMap TMP_tmp144 = new PrtMap();
            PrtInt TMP_tmp145 = ((PrtInt)0);
            PrtInt TMP_tmp146 = ((PrtInt)0);
            PrtNamedTuple TMP_tmp147 = (new PrtNamedTuple(new string[] { "a", "b" }, new PrtSeq(), new PrtMap()));
            PrtBool TMP_tmp148 = ((PrtBool)false);
            PrtInt TMP_tmp149 = ((PrtInt)0);
            PrtBool TMP_tmp150 = ((PrtBool)false);
            PrtInt TMP_tmp151 = ((PrtInt)0);
            PrtInt TMP_tmp152 = ((PrtInt)0);
            PrtBool TMP_tmp153 = ((PrtBool)false);
            PrtInt TMP_tmp154 = ((PrtInt)0);
            PrtInt TMP_tmp155 = ((PrtInt)0);
            PrtBool TMP_tmp156 = ((PrtBool)false);
            PEvent TMP_tmp157 = (PEvent)PrtValues.NullEvent;
            y = (PrtInt)(((PrtInt)2));
            TMP_tmp0 = (PrtInt)(((PrtInt)0));
            y = TMP_tmp0;
            b = (PrtBool)(((PrtBool)true));
            TMP_tmp1 = (PrtBool)(((PrtBool)false));
            b = TMP_tmp1;
            e = (PEvent)(new E((PMachineValue)PrtValues.Null));
            TMP_tmp2 = (PEvent)((PEvent)PrtValues.NullEvent);
            e = TMP_tmp2;
            mac = (PMachineValue)(new PMachineValue(currentMachine.self));
            TMP_tmp3 = (PMachineValue)((PMachineValue)PrtValues.Null);
            mac = TMP_tmp3;
            a = (IPrtValue)(((PrtBool)true));
            TMP_tmp4 = (IPrtValue)((PMachineValue)PrtValues.Null);
            a = TMP_tmp4;
            ((PrtMap)m5)[((PrtInt)1)] = (IPrtValue)(((PrtBool)true));
            TMP_tmp5 = (PrtMap)(new PrtMap());
            m5 = TMP_tmp5;
            ((PrtMap)m5)[((PrtInt)1)] = (IPrtValue)(new E((PMachineValue)PrtValues.Null));
            ((PrtNamedTuple)ts)["b"] = (PrtInt)(((PrtInt)1));
            TMP_tmp6 = (PrtInt)(((PrtNamedTuple)ts)["b"]);
            TMP_tmp7 = (PrtInt)(foo());
            TMP_tmp8 = (PrtInt)((TMP_tmp6) + (TMP_tmp7));
            ((PrtNamedTuple)ts)["a"] = TMP_tmp8;
            TMP_tmp9 = (PrtNamedTuple)((new PrtNamedTuple(new string[] { "a", "b" }, ((PrtInt)0), ((PrtInt)0))));
            ts = TMP_tmp9;
            TMP_tmp10 = (PrtInt)(((PrtInt)1));
            TMP_tmp11 = (PrtInt)(foo());
            TMP_tmp12 = (PrtInt)((TMP_tmp11) + (((PrtInt)1)));
            TMP_tmp13 = (PrtTuple)(new PrtTuple(TMP_tmp10, TMP_tmp12));
            tt = TMP_tmp13;
            TMP_tmp14 = (PrtTuple)((new PrtTuple(((PrtInt)0), ((PrtInt)0))));
            tt = TMP_tmp14;
            TMP_tmp15 = (PrtInt)(((PrtInt)2));
            TMP_tmp16 = (PEvent)(new E2((PMachineValue)PrtValues.Null));
            TMP_tmp17 = (PrtTuple)(new PrtTuple(TMP_tmp15, TMP_tmp16));
            te = TMP_tmp17;
            TMP_tmp18 = (PrtInt)(((PrtInt)3));
            TMP_tmp19 = (PEvent)(bar());
            TMP_tmp20 = (PrtTuple)(new PrtTuple(TMP_tmp18, TMP_tmp19));
            te = TMP_tmp20;
            TMP_tmp21 = (PrtInt)(((PrtInt)4));
            TMP_tmp22 = (PEvent)(((PEvent)PrtValues.Null));
            TMP_tmp23 = (PrtTuple)(new PrtTuple(TMP_tmp21, TMP_tmp22));
            te = TMP_tmp23;
            TMP_tmp24 = (PrtInt)(((PrtInt)1));
            ((PrtSeq)s).Insert(((PrtInt)0), TMP_tmp24);
            TMP_tmp25 = (PrtInt)(((PrtInt)2));
            ((PrtSeq)s).Insert(((PrtInt)1), TMP_tmp25);
            s1 = (PrtSeq)(new PrtSeq(((PrtSeq)((IPrtValue)s)?.Clone()).Cast<IPrtValue>()));
            ((PrtSeq)s).RemoveAt(((PrtInt)1));
            TMP_tmp26 = (PrtInt)(((PrtInt)5));
            ((PrtSeq)s).Insert(((PrtInt)0), TMP_tmp26);
            TMP_tmp27 = (PrtInt)(((PrtInt)6));
            ((PrtSeq)s).Insert(((PrtInt)0), TMP_tmp27);
            ((PrtSeq)s).RemoveAt(((PrtInt)1));
            TMP_tmp28 = (PrtSeq)(new PrtSeq());
            s = TMP_tmp28;
            TMP_tmp29 = (PrtInt)(((PrtInt)1));
            ((PrtSeq)s).Insert(((PrtInt)0), TMP_tmp29);
            ((PrtSeq)s)[((PrtInt)0)] = (PrtInt)(((PrtInt)2));
            i = (PrtInt)(((PrtInt)0));
            TMP_tmp30 = (PrtInt)(foo());
            TMP_tmp31 = (PrtInt)(foo());
            TMP_tmp32 = (PrtInt)((((PrtInt)2)) * (TMP_tmp31));
            TMP_tmp33 = (PrtInt)((TMP_tmp30) + (TMP_tmp32));
            ((PrtSeq)s).Insert(((PrtInt)0), TMP_tmp33);
            TMP_tmp34 = (PrtInt)(foo());
            TMP_tmp35 = (PrtInt)((TMP_tmp34) - (((PrtInt)1)));
            ((PrtSeq)s).RemoveAt(TMP_tmp35);
            TMP_tmp36 = (PEvent)(new E((PMachineValue)PrtValues.Null));
            ((PrtSeq)s9).Insert(((PrtInt)0), TMP_tmp36);
            TMP_tmp37 = (PEvent)(new E1((PMachineValue)PrtValues.Null));
            ((PrtSeq)s9).Insert(((PrtInt)1), TMP_tmp37);
            TMP_tmp38 = (PEvent)(new E2((PMachineValue)PrtValues.Null));
            ((PrtSeq)s9).Insert(((PrtInt)2), TMP_tmp38);
            TMP_tmp39 = (PEvent)(((PEvent)PrtValues.Null));
            ((PrtSeq)s9).Insert(((PrtInt)3), TMP_tmp39);
            TMP_tmp40 = (PrtInt)(((PrtInt)(s9).Count));
            TMP_tmp41 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp40, ((PrtInt)4))));
            currentMachine.Assert(TMP_tmp41, "");
            TMP_tmp42 = (PEvent)(new E((PMachineValue)PrtValues.Null));
            ((PrtSeq)s10).Insert(((PrtInt)0), TMP_tmp42);
            TMP_tmp43 = (PrtInt)(((PrtInt)1));
            ((PrtSeq)s2).Insert(((PrtInt)0), TMP_tmp43);
            TMP_tmp44 = (PrtInt)(((PrtInt)3));
            ((PrtSeq)s2).Insert(((PrtInt)0), TMP_tmp44);
            TMP_tmp45 = (PrtSeq)(((PrtSeq)((IPrtValue)s2)?.Clone()));
            TMP_tmp46 = (PMachineValue)(currentMachine.CreateInterface<I_XYZ>(currentMachine, TMP_tmp45));
            mac = (PMachineValue)TMP_tmp46;
            ((PrtMap)m1)[((PrtInt)0)] = (PrtInt)(((PrtInt)1));
            TMP_tmp47 = (PrtBool)(((PrtBool)(m1).ContainsKey(((PrtInt)0))));
            currentMachine.Assert(TMP_tmp47, "");
            TMP_tmp48 = (PrtSeq)((m1).CloneKeys());
            TMP_tmp49 = (PrtInt)(((PrtSeq)TMP_tmp48)[((PrtInt)0)]);
            i = TMP_tmp49;
            TMP_tmp50 = (PrtBool)((PrtValues.SafeEquals(i, ((PrtInt)0))));
            currentMachine.Assert(TMP_tmp50, "");
            TMP_tmp51 = (PrtSeq)((m1).CloneValues());
            TMP_tmp52 = (PrtInt)(((PrtSeq)TMP_tmp51)[((PrtInt)0)]);
            TMP_tmp53 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp52, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp53, "");
            TMP_tmp54 = (PrtInt)(((PrtMap)m1)[((PrtInt)0)]);
            TMP_tmp55 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp54, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp55, "");
            ((PrtMap)m1)[((PrtInt)0)] = (PrtInt)(((PrtInt)2));
            ((PrtMap)m1).Remove(((PrtInt)0));
            TMP_tmp56 = (PrtInt)(((PrtInt)(m1).Count));
            TMP_tmp57 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp56, ((PrtInt)0))));
            currentMachine.Assert(TMP_tmp57, "");
            ((PrtMap)m1)[((PrtInt)0)] = (PrtInt)(((PrtInt)2));
            i = (PrtInt)(((PrtInt)0));
            TMP_tmp58 = (PrtInt)(((PrtMap)m1)[i]);
            TMP_tmp59 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp58, ((PrtInt)2))));
            currentMachine.Assert(TMP_tmp59, "");
            ((PrtMap)m1)[((PrtInt)1)] = (PrtInt)(((PrtInt)3));
            ((PrtMap)m3)[((PrtInt)0)] = (PrtBool)(((PrtBool)true));
            ((PrtMap)m3)[((PrtInt)2)] = (PrtBool)(((PrtBool)false));
            ((PrtMap)m8)[((PrtInt)0)] = (PEvent)(new E((PMachineValue)PrtValues.Null));
            ((PrtMap)m8)[((PrtInt)1)] = (PEvent)(new E1((PMachineValue)PrtValues.Null));
            TMP_tmp60 = (PEvent)(((PEvent)PrtValues.Null));
            ((PrtMap)m8)[((PrtInt)2)] = TMP_tmp60;
            ((PrtMap)m9)[((PrtInt)0)] = (IPrtValue)(new E((PMachineValue)PrtValues.Null));
            ((PrtMap)m9)[((PrtInt)1)] = (IPrtValue)(PrtValues.Null);
            TMP_tmp61 = (PrtSeq)(((PrtSeq)((IPrtValue)s5)?.Clone()));
            ((PrtSeq)s3).Insert(((PrtInt)0), TMP_tmp61);
            TMP_tmp62 = (PrtSeq)(((PrtSeq)((IPrtValue)s1)?.Clone()));
            ((PrtSeq)s3).Insert(((PrtInt)1), TMP_tmp62);
            TMP_tmp63 = (PrtSeq)(((PrtSeq)s3)[((PrtInt)0)]);
            TMP_tmp64 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp63, s5)));
            currentMachine.Assert(TMP_tmp64, "");
            TMP_tmp65 = (PrtSeq)(((PrtSeq)s3)[((PrtInt)1)]);
            TMP_tmp66 = (IPrtValue)(((PrtSeq)TMP_tmp65)[((PrtInt)0)]);
            TMP_tmp67 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp66, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp67, "");
            ((PrtSeq)s3).RemoveAt(((PrtInt)1));
            ((PrtSeq)s3).RemoveAt(((PrtInt)0));
            TMP_tmp68 = (PrtInt)(((PrtInt)(s3).Count));
            TMP_tmp69 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp68, ((PrtInt)0))));
            currentMachine.Assert(TMP_tmp69, "");
            TMP_tmp70 = (IPrtValue)(PrtValues.Null);
            ((PrtSeq)s1).Insert(((PrtInt)0), TMP_tmp70);
            TMP_tmp71 = (PrtBool)(((PrtBool)true));
            ((PrtSeq)s1).Insert(((PrtInt)0), TMP_tmp71);
            TMP_tmp72 = (PrtSeq)(((PrtSeq)((IPrtValue)s5)?.Clone()));
            ((PrtSeq)s3).Insert(((PrtInt)0), TMP_tmp72);
            TMP_tmp73 = (PrtSeq)(((PrtSeq)((IPrtValue)s1)?.Clone()));
            ((PrtSeq)s3).Insert(((PrtInt)1), TMP_tmp73);
            TMP_tmp74 = (PrtSeq)(((PrtSeq)s3)[((PrtInt)0)]);
            TMP_tmp75 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp74, s5)));
            currentMachine.Assert(TMP_tmp75, "");
            TMP_tmp76 = (PrtSeq)(((PrtSeq)s3)[((PrtInt)1)]);
            TMP_tmp77 = (IPrtValue)(((PrtSeq)TMP_tmp76)[((PrtInt)0)]);
            TMP_tmp78 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp77, ((PrtBool)true))));
            currentMachine.Assert(TMP_tmp78, "");
            TMP_tmp79 = (PrtSeq)(new PrtSeq());
            s3 = TMP_tmp79;
            ((PrtMap)m9)[((PrtInt)0)] = (IPrtValue)(new E((PMachineValue)PrtValues.Null));
            ((PrtMap)m9)[((PrtInt)1)] = (IPrtValue)(PrtValues.Null);
            TMP_tmp80 = (PrtMap)(((PrtMap)((IPrtValue)m9)?.Clone()));
            ((PrtSeq)s6).Insert(((PrtInt)0), TMP_tmp80);
            TMP_tmp81 = (PrtInt)(((PrtInt)1));
            ((PrtSeq)s12).Insert(((PrtInt)0), TMP_tmp81);
            TMP_tmp82 = (PrtInt)(((PrtInt)2));
            ((PrtSeq)s12).Insert(((PrtInt)1), TMP_tmp82);
            ((PrtMap)m10)[((PrtInt)1)] = (IPrtValue)(((PrtInt)100));
            ((PrtMap)m10)[((PrtInt)5)] = (IPrtValue)(((PrtBool)true));
            ((PrtMap)m10)[((PrtInt)10)] = (IPrtValue)(((PrtSeq)((IPrtValue)s12)?.Clone()));
            TMP_tmp83 = (PrtMap)(((PrtMap)((IPrtValue)m10)?.Clone()));
            ((PrtSeq)s6).Insert(((PrtInt)1), TMP_tmp83);
            TMP_tmp84 = (PrtMap)(((PrtSeq)s6)[((PrtInt)0)]);
            TMP_tmp85 = (IPrtValue)(((PrtMap)TMP_tmp84)[((PrtInt)0)]);
            TMP_tmp86 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp85, new E((PMachineValue)PrtValues.Null))));
            currentMachine.Assert(TMP_tmp86, "");
            TMP_tmp87 = (PrtMap)(((PrtSeq)s6)[((PrtInt)0)]);
            TMP_tmp88 = (IPrtValue)(((PrtMap)TMP_tmp87)[((PrtInt)1)]);
            TMP_tmp89 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp88, PrtValues.Null)));
            currentMachine.Assert(TMP_tmp89, "");
            TMP_tmp90 = (PrtMap)(((PrtSeq)s6)[((PrtInt)1)]);
            TMP_tmp91 = (IPrtValue)(((PrtMap)TMP_tmp90)[((PrtInt)5)]);
            TMP_tmp92 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp91, ((PrtBool)true))));
            currentMachine.Assert(TMP_tmp92, "");
            TMP_tmp93 = (PrtMap)(((PrtSeq)s6)[((PrtInt)1)]);
            TMP_tmp94 = (IPrtValue)(((PrtMap)TMP_tmp93)[((PrtInt)10)]);
            TMP_tmp95 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp94, s12)));
            currentMachine.Assert(TMP_tmp95, "");
            TMP_tmp96 = (PrtMap)(((PrtSeq)s6)[((PrtInt)1)]);
            TMP_tmp97 = (IPrtValue)(((PrtMap)TMP_tmp96)[((PrtInt)10)]);
            TMP_tmp98 = (PrtSeq)(((PrtSeq)TMP_tmp97));
            tmp4 = TMP_tmp98;
            TMP_tmp99 = (PrtInt)(((PrtSeq)tmp4)[((PrtInt)0)]);
            TMP_tmp100 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp99, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp100, "");
            TMP_tmp101 = (PrtMap)(((PrtSeq)s6)[((PrtInt)1)]);
            TMP_tmp102 = (IPrtValue)(((PrtMap)TMP_tmp101)[((PrtInt)10)]);
            TMP_tmp103 = (PrtSeq)(((PrtSeq)TMP_tmp102));
            TMP_tmp104 = (PrtInt)(((PrtSeq)TMP_tmp103)[((PrtInt)0)]);
            TMP_tmp105 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp104, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp105, "");
            TMP_tmp106 = (PrtInt)(((PrtInt)1));
            ((PrtSeq)s).Insert(((PrtInt)0), TMP_tmp106);
            ((PrtMap)tmp3)[((PrtInt)0)] = (PrtSeq)(((PrtSeq)((IPrtValue)s)?.Clone()));
            TMP_tmp107 = (PrtSeq)(((PrtSeq)((IPrtValue)s)?.Clone()));
            TMP_tmp108 = (PrtMap)(((PrtMap)((IPrtValue)tmp3)?.Clone()));
            TMP_tmp109 = (PrtNamedTuple)((new PrtNamedTuple(new string[] { "a", "b" }, TMP_tmp107, TMP_tmp108)));
            t = TMP_tmp109;
            TMP_tmp110 = (PrtInt)(((PrtInt)2));
            ((PrtSeq)((PrtNamedTuple)t)["a"]).Insert(((PrtInt)0), TMP_tmp110);
            TMP_tmp111 = (PrtSeq)(((PrtNamedTuple)t)["a"]);
            TMP_tmp112 = (PrtInt)(((PrtSeq)TMP_tmp111)[((PrtInt)0)]);
            TMP_tmp113 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp112, ((PrtInt)2))));
            currentMachine.Assert(TMP_tmp113, "");
            TMP_tmp114 = (PrtInt)(((PrtInt)2));
            ((PrtSeq)((PrtNamedTuple)t)["a"]).Insert(((PrtInt)1), TMP_tmp114);
            TMP_tmp115 = (PrtSeq)(((PrtNamedTuple)t)["a"]);
            TMP_tmp116 = (PrtInt)(((PrtSeq)TMP_tmp115)[((PrtInt)1)]);
            TMP_tmp117 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp116, ((PrtInt)2))));
            currentMachine.Assert(TMP_tmp117, "");
            TMP_tmp118 = (PrtInt)(((PrtInt)3));
            ((PrtSeq)((PrtNamedTuple)t)["a"]).Insert(((PrtInt)0), TMP_tmp118);
            TMP_tmp119 = (PrtSeq)(((PrtNamedTuple)t)["a"]);
            TMP_tmp120 = (PrtInt)(((PrtSeq)TMP_tmp119)[((PrtInt)0)]);
            TMP_tmp121 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp120, ((PrtInt)3))));
            currentMachine.Assert(TMP_tmp121, "");
            TMP_tmp122 = (PrtSeq)(((PrtNamedTuple)t)["a"]);
            TMP_tmp123 = (PrtInt)(((PrtSeq)TMP_tmp122)[((PrtInt)1)]);
            TMP_tmp124 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp123, ((PrtInt)2))));
            currentMachine.Assert(TMP_tmp124, "");
            TMP_tmp125 = (PrtInt)(((PrtInt)0));
            TMP_tmp126 = (PrtInt)(((PrtInt)0));
            TMP_tmp127 = (PrtTuple)(new PrtTuple(TMP_tmp125, TMP_tmp126));
            ((PrtSeq)s4).Insert(((PrtInt)0), TMP_tmp127);
            TMP_tmp128 = (PrtInt)(((PrtInt)1));
            TMP_tmp129 = (PrtInt)(((PrtInt)1));
            TMP_tmp130 = (PrtTuple)(new PrtTuple(TMP_tmp128, TMP_tmp129));
            ((PrtSeq)s4).Insert(((PrtInt)1), TMP_tmp130);
            TMP_tmp131 = (PrtInt)(((PrtInt)2));
            TMP_tmp132 = (PrtInt)(((PrtInt)2));
            TMP_tmp133 = (PrtTuple)(new PrtTuple(TMP_tmp131, TMP_tmp132));
            ((PrtSeq)s4).Insert(((PrtInt)2), TMP_tmp133);
            TMP_tmp134 = (PrtInt)(((PrtInt)1));
            TMP_tmp135 = (PrtInt)(((PrtInt)1));
            TMP_tmp136 = (PrtTuple)(new PrtTuple(TMP_tmp134, TMP_tmp135));
            ((PrtSeq)s8).Insert(((PrtInt)0), TMP_tmp136);
            TMP_tmp137 = (PrtInt)(((PrtInt)2));
            TMP_tmp138 = (PrtInt)(((PrtInt)2));
            TMP_tmp139 = (PrtTuple)(new PrtTuple(TMP_tmp137, TMP_tmp138));
            ((PrtSeq)s8).Insert(((PrtInt)1), TMP_tmp139);
            TMP_tmp140 = (PrtInt)(((PrtInt)3));
            TMP_tmp141 = (PrtInt)(((PrtInt)3));
            TMP_tmp142 = (PrtTuple)(new PrtTuple(TMP_tmp140, TMP_tmp141));
            ((PrtSeq)s8).Insert(((PrtInt)2), TMP_tmp142);
            TMP_tmp143 = (PrtMap)(new PrtMap());
            m5 = TMP_tmp143;
            ((PrtMap)m5)[((PrtInt)1)] = (IPrtValue)(((PrtBool)true));
            ((PrtMap)m5)[((PrtInt)2)] = (IPrtValue)(new E((PMachineValue)PrtValues.Null));
            ((PrtMap)m5)[((PrtInt)5)] = (IPrtValue)(((PrtInt)5));
            TMP_tmp144 = (PrtMap)(new PrtMap());
            m6 = TMP_tmp144;
            ((PrtMap)m6)[((PrtInt)0)] = (IPrtValue)(((PrtInt)0));
            ((PrtMap)m6)[((PrtInt)2)] = (IPrtValue)(((PrtInt)2));
            ((PrtMap)m6)[((PrtInt)4)] = (IPrtValue)(((PrtInt)4));
            ((PrtMap)m6)[((PrtInt)6)] = (IPrtValue)(new E((PMachineValue)PrtValues.Null));
            ((PrtMap)m2)[((PrtInt)1)] = (PrtMap)(((PrtMap)((IPrtValue)m5)?.Clone()));
            ((PrtMap)m2)[((PrtInt)2)] = (PrtMap)(((PrtMap)((IPrtValue)m6)?.Clone()));
            TMP_tmp145 = (PrtInt)(foo());
            ((PrtSeq)((PrtNamedTuple)t)["a"])[TMP_tmp145] = (PrtInt)(((PrtInt)2));
            TMP_tmp146 = (PrtInt)(foo());
            tmp = TMP_tmp146;
            TMP_tmp147 = (PrtNamedTuple)(GetT());
            tmp2 = (PrtNamedTuple)TMP_tmp147;
            TMP_tmp148 = (PrtBool)((PrtValues.SafeEquals(tmp2, t)));
            currentMachine.Assert(TMP_tmp148, "");
            TMP_tmp149 = (PrtInt)(foo());
            ((PrtSeq)((PrtNamedTuple)tmp2)["a"])[TMP_tmp149] = (IPrtValue)(((PrtInt)1));
            TMP_tmp150 = (PrtBool)((!PrtValues.SafeEquals(tmp2, t)));
            currentMachine.Assert(TMP_tmp150, "");
            ((PrtMap)m11)[((PrtInt)1)] = (IPrtValue)(PrtValues.Null);
            ((PrtMap)m11)[PrtValues.Null] = (IPrtValue)(((PrtInt)1));
            TMP_tmp151 = (PrtInt)(((PrtInt)0));
            y = TMP_tmp151;
            TMP_tmp152 = (PrtInt)(IncY());
            tmp1 = TMP_tmp152;
            TMP_tmp153 = (PrtBool)((PrtValues.SafeEquals(tmp1, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp153, "");
            TMP_tmp154 = (PrtInt)(foo());
            ((PrtSeq)((PrtNamedTuple)t)["a"])[TMP_tmp154] = (PrtInt)(((PrtInt)((IPrtValue)tmp1)?.Clone()));
            ((PrtSeq)((PrtNamedTuple)t)["a"])[tmp] = (PrtInt)(((PrtInt)((IPrtValue)tmp1)?.Clone()));
            TMP_tmp155 = (PrtInt)(IncY());
            y = TMP_tmp155;
            TMP_tmp156 = (PrtBool)((PrtValues.SafeEquals(y, ((PrtInt)2))));
            currentMachine.Assert(TMP_tmp156, "");
            TMP_tmp157 = (PEvent)(new PHalt((PMachineValue)PrtValues.Null));
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp157);
            throw new PUnreachableCodeException();
        }
        public PrtInt foo()
        {
            Main currentMachine = this;
            return ((PrtInt)1);
        }
        public PEvent bar()
        {
            Main currentMachine = this;
            return new E((PMachineValue)PrtValues.Null);
        }
        public PrtNamedTuple GetT()
        {
            Main currentMachine = this;
            return ((PrtNamedTuple)((IPrtValue)t)?.Clone());
        }
        public PrtInt IncY()
        {
            Main currentMachine = this;
            PrtInt TMP_tmp0_1 = ((PrtInt)0);
            TMP_tmp0_1 = (PrtInt)((y) + (((PrtInt)1)));
            y = TMP_tmp0_1;
            return ((PrtInt)((IPrtValue)y)?.Clone());
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
        public class ConstructorEvent : PEvent { public ConstructorEvent(PrtSeq val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PrtSeq)value); }
        public XYZ()
        {
            this.sends.Add(nameof(E));
            this.sends.Add(nameof(E1));
            this.sends.Add(nameof(E2));
            this.sends.Add(nameof(E3));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(E));
            this.receives.Add(nameof(E1));
            this.receives.Add(nameof(E2));
            this.receives.Add(nameof(E3));
            this.receives.Add(nameof(PHalt));
        }

        public void Anon_1()
        {
            XYZ currentMachine = this;
            PrtSeq payload = (PrtSeq)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtInt TMP_tmp0_2 = ((PrtInt)0);
            PrtBool TMP_tmp1_1 = ((PrtBool)false);
            ss = (PrtSeq)(((PrtSeq)((IPrtValue)payload)?.Clone()));
            TMP_tmp0_2 = (PrtInt)(((PrtSeq)ss)[((PrtInt)0)]);
            TMP_tmp1_1 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp0_2, ((PrtInt)3))));
            currentMachine.Assert(TMP_tmp1_1, "");
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(init))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_1))]
        class init : MachineState
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
            PInterfaces.AddInterface(nameof(I_Main), nameof(E), nameof(E1), nameof(E2), nameof(E3), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_XYZ), nameof(E), nameof(E1), nameof(E2), nameof(E3), nameof(PHalt));
        }
    }

}
#pragma warning restore 162, 219, 414
