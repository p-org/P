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
    public static partial class GlobalFunctions_Main { }
    internal class E : PEvent<IPrtValue>
    {
        static E() { AssertVal = 1; AssumeVal = -1; }
        public E() : base() { }
        public E(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new E(); }
    }
    internal class E1 : PEvent<IPrtValue>
    {
        static E1() { AssertVal = 1; AssumeVal = -1; }
        public E1() : base() { }
        public E1(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new E1(); }
    }
    internal class E2 : PEvent<IPrtValue>
    {
        static E2() { AssertVal = 1; AssumeVal = -1; }
        public E2() : base() { }
        public E2(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new E2(); }
    }
    internal class E3 : PEvent<IPrtValue>
    {
        static E3() { AssertVal = 1; AssumeVal = -1; }
        public E3() : base() { }
        public E3(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new E3(); }
    }
    internal class Main : PMachine
    {
        private tuple t = (new tuple(new PrtSeq<PrtInt>(), new PrtMap<PrtInt, PrtSeq<PrtInt>>()));
        private tuple t1 = (new tuple(new PrtSeq<PrtInt>(), new PrtMap<PrtInt, PrtSeq<PrtInt>>()));
        private tuple_1 ts = (new tuple_1(((PrtInt)0), ((PrtInt)0)));
        private PrtTuple<PrtInt, PrtInt> tt = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
        private PrtTuple<PrtBool, PrtBool> tbool = (new PrtTuple<PrtBool, PrtBool>(((PrtBool)false), ((PrtBool)false)));
        private PrtTuple<PrtInt, IEventWithPayload> te = (new PrtTuple<PrtInt, IEventWithPayload>(((PrtInt)0), null));
        private PrtBool b = ((PrtBool)false);
        private PrtInt y = ((PrtInt)0);
        private PrtInt tmp = ((PrtInt)0);
        private PrtInt tmp1 = ((PrtInt)0);
        private IEventWithPayload ev = null;
        private IPrtValue a = null;
        private tuple_2 tmp2 = (new tuple_2(new PrtSeq<IPrtValue>(), new PrtMap<PrtInt, PrtSeq<IPrtValue>>()));
        private PrtMap<PrtInt, PrtSeq<PrtInt>> tmp3 = new PrtMap<PrtInt, PrtSeq<PrtInt>>();
        private PrtSeq<PrtInt> s = new PrtSeq<PrtInt>();
        private PrtSeq<IPrtValue> s1 = new PrtSeq<IPrtValue>();
        private PrtSeq<PrtInt> s2 = new PrtSeq<PrtInt>();
        private PrtSeq<PrtSeq<IPrtValue>> s3 = new PrtSeq<PrtSeq<IPrtValue>>();
        private PrtSeq<PrtTuple<PrtInt, PrtInt>> s4 = new PrtSeq<PrtTuple<PrtInt, PrtInt>>();
        private PrtSeq<PrtTuple<PrtInt, PrtInt>> s8 = new PrtSeq<PrtTuple<PrtInt, PrtInt>>();
        private PrtSeq<PrtBool> s5 = new PrtSeq<PrtBool>();
        private PrtSeq<PrtMap<PrtInt, IPrtValue>> s6 = new PrtSeq<PrtMap<PrtInt, IPrtValue>>();
        private PrtSeq<PrtInt> s7 = new PrtSeq<PrtInt>();
        private PrtSeq<IEventWithPayload> s9 = new PrtSeq<IEventWithPayload>();
        private PrtSeq<IPrtValue> s10 = new PrtSeq<IPrtValue>();
        private PrtSeq<PrtInt> s11 = new PrtSeq<PrtInt>();
        private PrtSeq<PrtBool> s12 = new PrtSeq<PrtBool>();
        private PrtInt i = ((PrtInt)0);
        private PMachineValue mac = null;
        private PrtMap<PrtInt, PrtInt> m1 = new PrtMap<PrtInt, PrtInt>();
        private PrtMap<PrtInt, PrtInt> m4 = new PrtMap<PrtInt, PrtInt>();
        private PrtMap<PrtInt, PrtBool> m3 = new PrtMap<PrtInt, PrtBool>();
        private PrtMap<PrtInt, IPrtValue> m5 = new PrtMap<PrtInt, IPrtValue>();
        private PrtMap<PrtInt, IPrtValue> m6 = new PrtMap<PrtInt, IPrtValue>();
        private PrtMap<PrtInt, PrtMap<PrtInt, IPrtValue>> m2 = new PrtMap<PrtInt, PrtMap<PrtInt, IPrtValue>>();
        private PrtMap<PrtBool, PrtSeq<tuple_1>> m7 = new PrtMap<PrtBool, PrtSeq<tuple_1>>();
        private PrtMap<PrtInt, IEventWithPayload> m8 = new PrtMap<PrtInt, IEventWithPayload>();
        private PrtMap<PrtInt, IPrtValue> m9 = new PrtMap<PrtInt, IPrtValue>();
        public class ConstructorEvent : PEvent<IPrtValue> { public ConstructorEvent(IPrtValue val) : base(val) { } }

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
        }

        public void Anon()
        {
            Main currentMachine = this;
            PrtBool TMP_tmp0 = ((PrtBool)false);
            PrtBool TMP_tmp1 = ((PrtBool)false);
            PrtBool TMP_tmp2 = ((PrtBool)false);
            PrtBool TMP_tmp3 = ((PrtBool)false);
            PrtBool TMP_tmp4 = ((PrtBool)false);
            PrtBool TMP_tmp5 = ((PrtBool)false);
            PrtBool TMP_tmp6 = ((PrtBool)false);
            PrtBool TMP_tmp7 = ((PrtBool)false);
            PrtBool TMP_tmp8 = ((PrtBool)false);
            PrtBool TMP_tmp9 = ((PrtBool)false);
            PrtBool TMP_tmp10 = ((PrtBool)false);
            PrtBool TMP_tmp11 = ((PrtBool)false);
            PrtBool TMP_tmp12 = ((PrtBool)false);
            PrtBool TMP_tmp13 = ((PrtBool)false);
            PrtBool TMP_tmp14 = ((PrtBool)false);
            PrtBool TMP_tmp15 = ((PrtBool)false);
            PrtInt TMP_tmp16 = ((PrtInt)0);
            PrtInt TMP_tmp17 = ((PrtInt)0);
            PrtInt TMP_tmp18 = ((PrtInt)0);
            PrtInt TMP_tmp19 = ((PrtInt)0);
            PrtInt TMP_tmp20 = ((PrtInt)0);
            PrtBool TMP_tmp21 = ((PrtBool)false);
            PrtInt TMP_tmp22 = ((PrtInt)0);
            PrtInt TMP_tmp23 = ((PrtInt)0);
            PrtInt TMP_tmp24 = ((PrtInt)0);
            PrtBool TMP_tmp25 = ((PrtBool)false);
            PrtInt TMP_tmp26 = ((PrtInt)0);
            PrtInt TMP_tmp27 = ((PrtInt)0);
            PrtInt TMP_tmp28 = ((PrtInt)0);
            PrtInt TMP_tmp29 = ((PrtInt)0);
            PrtBool TMP_tmp30 = ((PrtBool)false);
            PrtInt TMP_tmp31 = ((PrtInt)0);
            PrtInt TMP_tmp32 = ((PrtInt)0);
            PrtInt TMP_tmp33 = ((PrtInt)0);
            PrtInt TMP_tmp34 = ((PrtInt)0);
            PrtBool TMP_tmp35 = ((PrtBool)false);
            PrtInt TMP_tmp36 = ((PrtInt)0);
            PrtInt TMP_tmp37 = ((PrtInt)0);
            PrtInt TMP_tmp38 = ((PrtInt)0);
            PrtBool TMP_tmp39 = ((PrtBool)false);
            PrtInt TMP_tmp40 = ((PrtInt)0);
            PrtInt TMP_tmp41 = ((PrtInt)0);
            PrtBool TMP_tmp42 = ((PrtBool)false);
            PrtInt TMP_tmp43 = ((PrtInt)0);
            PrtBool TMP_tmp44 = ((PrtBool)false);
            PrtInt TMP_tmp45 = ((PrtInt)0);
            PrtInt TMP_tmp46 = ((PrtInt)0);
            PrtBool TMP_tmp47 = ((PrtBool)false);
            PrtBool TMP_tmp48 = ((PrtBool)false);
            PrtBool TMP_tmp49 = ((PrtBool)false);
            PrtBool TMP_tmp50 = ((PrtBool)false);
            PrtBool TMP_tmp51 = ((PrtBool)false);
            PrtBool TMP_tmp52 = ((PrtBool)false);
            PrtBool TMP_tmp53 = ((PrtBool)false);
            PrtBool TMP_tmp54 = ((PrtBool)false);
            PrtBool TMP_tmp55 = ((PrtBool)false);
            PrtBool TMP_tmp56 = ((PrtBool)false);
            PrtBool TMP_tmp57 = ((PrtBool)false);
            PrtBool TMP_tmp58 = ((PrtBool)false);
            PrtBool TMP_tmp59 = ((PrtBool)false);
            PrtBool TMP_tmp60 = ((PrtBool)false);
            PrtInt TMP_tmp61 = ((PrtInt)0);
            PrtInt TMP_tmp62 = ((PrtInt)0);
            PrtInt TMP_tmp63 = ((PrtInt)0);
            PrtInt TMP_tmp64 = ((PrtInt)0);
            PrtInt TMP_tmp65 = ((PrtInt)0);
            PrtBool TMP_tmp66 = ((PrtBool)false);
            PrtBool TMP_tmp67 = ((PrtBool)false);
            PrtSeq<PrtInt> TMP_tmp68 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp69 = ((PrtInt)0);
            PrtBool TMP_tmp70 = ((PrtBool)false);
            PrtInt TMP_tmp71 = ((PrtInt)0);
            PrtInt TMP_tmp72 = ((PrtInt)0);
            PrtInt TMP_tmp73 = ((PrtInt)0);
            PrtInt TMP_tmp74 = ((PrtInt)0);
            PrtInt TMP_tmp75 = ((PrtInt)0);
            PrtBool TMP_tmp76 = ((PrtBool)false);
            PrtInt TMP_tmp77 = ((PrtInt)0);
            PrtInt TMP_tmp78 = ((PrtInt)0);
            PrtBool TMP_tmp79 = ((PrtBool)false);
            PrtInt TMP_tmp80 = ((PrtInt)0);
            PrtInt TMP_tmp81 = ((PrtInt)0);
            PrtBool TMP_tmp82 = ((PrtBool)false);
            PrtInt TMP_tmp83 = ((PrtInt)0);
            PrtInt TMP_tmp84 = ((PrtInt)0);
            PrtBool TMP_tmp85 = ((PrtBool)false);
            PrtInt TMP_tmp86 = ((PrtInt)0);
            PrtInt TMP_tmp87 = ((PrtInt)0);
            PrtBool TMP_tmp88 = ((PrtBool)false);
            PrtBool TMP_tmp89 = ((PrtBool)false);
            PrtBool TMP_tmp90 = ((PrtBool)false);
            PrtBool TMP_tmp91 = ((PrtBool)false);
            PrtBool TMP_tmp92 = ((PrtBool)false);
            PrtInt TMP_tmp93 = ((PrtInt)0);
            PrtBool TMP_tmp94 = ((PrtBool)false);
            PrtInt TMP_tmp95 = ((PrtInt)0);
            PrtSeq<PrtInt> TMP_tmp96 = new PrtSeq<PrtInt>();
            PrtMap<PrtInt, PrtSeq<PrtInt>> TMP_tmp97 = new PrtMap<PrtInt, PrtSeq<PrtInt>>();
            tuple TMP_tmp98 = (new tuple(new PrtSeq<PrtInt>(), new PrtMap<PrtInt, PrtSeq<PrtInt>>()));
            PrtInt TMP_tmp99 = ((PrtInt)0);
            PrtSeq<PrtInt> TMP_tmp100 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp101 = ((PrtInt)0);
            PrtBool TMP_tmp102 = ((PrtBool)false);
            PrtInt TMP_tmp103 = ((PrtInt)0);
            PrtSeq<PrtInt> TMP_tmp104 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp105 = ((PrtInt)0);
            PrtBool TMP_tmp106 = ((PrtBool)false);
            PrtInt TMP_tmp107 = ((PrtInt)0);
            PrtSeq<PrtInt> TMP_tmp108 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp109 = ((PrtInt)0);
            PrtBool TMP_tmp110 = ((PrtBool)false);
            PrtSeq<PrtInt> TMP_tmp111 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp112 = ((PrtInt)0);
            PrtBool TMP_tmp113 = ((PrtBool)false);
            PrtInt TMP_tmp114 = ((PrtInt)0);
            PrtBool TMP_tmp115 = ((PrtBool)false);
            PrtSeq<PrtInt> TMP_tmp116 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp117 = ((PrtInt)0);
            PrtBool TMP_tmp118 = ((PrtBool)false);
            PrtMap<PrtInt, PrtSeq<PrtInt>> TMP_tmp119 = new PrtMap<PrtInt, PrtSeq<PrtInt>>();
            PrtSeq<PrtInt> TMP_tmp120 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp121 = ((PrtInt)0);
            PrtBool TMP_tmp122 = ((PrtBool)false);
            PrtInt TMP_tmp123 = ((PrtInt)0);
            PrtInt TMP_tmp124 = ((PrtInt)0);
            PrtBool TMP_tmp125 = ((PrtBool)false);
            PrtInt TMP_tmp126 = ((PrtInt)0);
            PrtBool TMP_tmp127 = ((PrtBool)false);
            PrtBool TMP_tmp128 = ((PrtBool)false);
            PrtSeq<PrtInt> TMP_tmp129 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp130 = ((PrtInt)0);
            PrtBool TMP_tmp131 = ((PrtBool)false);
            PrtMap<PrtInt, PrtSeq<PrtInt>> TMP_tmp132 = new PrtMap<PrtInt, PrtSeq<PrtInt>>();
            PrtSeq<PrtInt> TMP_tmp133 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp134 = ((PrtInt)0);
            PrtBool TMP_tmp135 = ((PrtBool)false);
            PrtSeq<PrtInt> TMP_tmp136 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp137 = ((PrtInt)0);
            PrtBool TMP_tmp138 = ((PrtBool)false);
            PrtMap<PrtInt, PrtSeq<PrtInt>> TMP_tmp139 = new PrtMap<PrtInt, PrtSeq<PrtInt>>();
            PrtSeq<PrtInt> TMP_tmp140 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp141 = ((PrtInt)0);
            PrtBool TMP_tmp142 = ((PrtBool)false);
            PrtInt TMP_tmp143 = ((PrtInt)0);
            PrtInt TMP_tmp144 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp145 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp146 = ((PrtInt)0);
            PrtInt TMP_tmp147 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp148 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp149 = ((PrtInt)0);
            PrtInt TMP_tmp150 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp151 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp152 = ((PrtInt)0);
            PrtInt TMP_tmp153 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp154 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp155 = ((PrtInt)0);
            PrtInt TMP_tmp156 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp157 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp158 = ((PrtInt)0);
            PrtInt TMP_tmp159 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp160 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtTuple<PrtInt, PrtInt> TMP_tmp161 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp162 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp163 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp164 = ((PrtInt)0);
            PrtInt TMP_tmp165 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp166 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp167 = ((PrtInt)0);
            PrtInt TMP_tmp168 = ((PrtInt)0);
            PrtBool TMP_tmp169 = ((PrtBool)false);
            PrtTuple<PrtInt, PrtInt> TMP_tmp170 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp171 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp172 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp173 = ((PrtInt)0);
            PrtInt TMP_tmp174 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp175 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp176 = ((PrtInt)0);
            PrtInt TMP_tmp177 = ((PrtInt)0);
            PrtBool TMP_tmp178 = ((PrtBool)false);
            IEventWithPayload TMP_tmp179 = null;
            tbool.Item1 = ((PrtBool)true);
            TMP_tmp0 = tbool.Item1;
            TMP_tmp1 = !(TMP_tmp0);
            tbool.Item2 = TMP_tmp1;
            TMP_tmp2 = tbool.Item2;
            TMP_tmp3 = (TMP_tmp2) == (((PrtBool)false));
            currentMachine.Assert(TMP_tmp3, "");
            TMP_tmp4 = tbool.Item1;
            TMP_tmp5 = tbool.Item2;
            TMP_tmp6 = (TMP_tmp5) == (((PrtBool)false));
            TMP_tmp7 = (TMP_tmp4) && (TMP_tmp6);
            currentMachine.Assert(TMP_tmp7, "");
            TMP_tmp8 = tbool.Item1;
            TMP_tmp9 = tbool.Item2;
            TMP_tmp10 = (TMP_tmp9) == (((PrtBool)true));
            TMP_tmp11 = (TMP_tmp8) || (TMP_tmp10);
            currentMachine.Assert(TMP_tmp11, "");
            TMP_tmp12 = tbool.Item1;
            TMP_tmp13 = tbool.Item2;
            TMP_tmp14 = (TMP_tmp12) != (TMP_tmp13);
            currentMachine.Assert(TMP_tmp14, "");
            ev = new E(null);
            TMP_tmp15 = (ev) != (null);
            currentMachine.Assert(TMP_tmp15, "");
            tt.Item1 = ((PrtInt)1);
            TMP_tmp16 = tt.Item1;
            TMP_tmp17 = -(TMP_tmp16);
            tt.Item2 = TMP_tmp17;
            TMP_tmp18 = tt.Item1;
            TMP_tmp19 = tt.Item2;
            TMP_tmp20 = (TMP_tmp18) + (TMP_tmp19);
            TMP_tmp21 = (TMP_tmp20) == (((PrtInt)0));
            currentMachine.Assert(TMP_tmp21, "");
            TMP_tmp22 = tt.Item1;
            TMP_tmp23 = tt.Item2;
            TMP_tmp24 = (TMP_tmp22) - (TMP_tmp23);
            TMP_tmp25 = (TMP_tmp24) == (((PrtInt)2));
            currentMachine.Assert(TMP_tmp25, "");
            TMP_tmp26 = tt.Item1;
            TMP_tmp27 = tt.Item2;
            TMP_tmp28 = (TMP_tmp26) * (TMP_tmp27);
            TMP_tmp29 = -(((PrtInt)1));
            TMP_tmp30 = (TMP_tmp28) == (TMP_tmp29);
            currentMachine.Assert(TMP_tmp30, "");
            TMP_tmp31 = tt.Item1;
            TMP_tmp32 = tt.Item2;
            TMP_tmp33 = (TMP_tmp31) / (TMP_tmp32);
            TMP_tmp34 = -(((PrtInt)1));
            TMP_tmp35 = (TMP_tmp33) == (TMP_tmp34);
            currentMachine.Assert(TMP_tmp35, "");
            TMP_tmp36 = tt.Item1;
            TMP_tmp37 = (TMP_tmp36) * (((PrtInt)5));
            TMP_tmp38 = (TMP_tmp37) / (((PrtInt)2));
            TMP_tmp39 = (TMP_tmp38) == (((PrtInt)2));
            currentMachine.Assert(TMP_tmp39, "");
            TMP_tmp40 = tt.Item1;
            TMP_tmp41 = tt.Item2;
            TMP_tmp42 = (TMP_tmp40) != (TMP_tmp41);
            currentMachine.Assert(TMP_tmp42, "");
            a = null;
            TMP_tmp43 = tt.Item2;
            TMP_tmp44 = (TMP_tmp43) != (a);
            currentMachine.Assert(TMP_tmp44, "");
            TMP_tmp45 = -(((PrtInt)1));
            a = (IPrtValue)TMP_tmp45;
            TMP_tmp46 = tt.Item2;
            TMP_tmp47 = (TMP_tmp46) == (a);
            currentMachine.Assert(TMP_tmp47, "");
            TMP_tmp48 = (a) != (ev);
            currentMachine.Assert(TMP_tmp48, "");
            a = null;
            TMP_tmp49 = (mac) == (a);
            currentMachine.Assert(TMP_tmp49, "");
            a = ((PrtInt)1);
            TMP_tmp50 = tbool.Item1;
            TMP_tmp51 = !(TMP_tmp50);
            a = (IPrtValue)TMP_tmp51;
            TMP_tmp52 = tbool.Item2;
            TMP_tmp53 = (a) == (TMP_tmp52);
            currentMachine.Assert(TMP_tmp53, "");
            a = null;
            TMP_tmp54 = (a) != (((PrtInt)1));
            currentMachine.Assert(TMP_tmp54, "");
            a = ((PrtInt)1);
            TMP_tmp55 = (a) == (((PrtInt)1));
            currentMachine.Assert(TMP_tmp55, "");
            TMP_tmp56 = ((PrtBool)true);
            s12.Insert(((PrtInt)0), TMP_tmp56);
            TMP_tmp57 = (s12)[((PrtInt)0)];
            TMP_tmp58 = !(TMP_tmp57);
            s12.Insert(((PrtInt)1), TMP_tmp58);
            TMP_tmp59 = (s12)[((PrtInt)1)];
            TMP_tmp60 = (TMP_tmp59) == (((PrtBool)false));
            currentMachine.Assert(TMP_tmp60, "");
            TMP_tmp61 = ((PrtInt)1);
            s2.Insert(((PrtInt)0), TMP_tmp61);
            TMP_tmp62 = (s2)[((PrtInt)0)];
            TMP_tmp63 = -(TMP_tmp62);
            s2.Insert(((PrtInt)1), TMP_tmp63);
            TMP_tmp64 = (s2)[((PrtInt)1)];
            TMP_tmp65 = -(((PrtInt)1));
            TMP_tmp66 = (TMP_tmp64) == (TMP_tmp65);
            currentMachine.Assert(TMP_tmp66, "");
            (m1)[((PrtInt)0)] = ((PrtInt)1);
            TMP_tmp67 = ((PrtBool)(m1).ContainsKey(((PrtInt)0)));
            currentMachine.Assert(TMP_tmp67, "");
            TMP_tmp68 = (m1).CloneKeys();
            TMP_tmp69 = (TMP_tmp68)[((PrtInt)0)];
            i = TMP_tmp69;
            TMP_tmp70 = (i) == (((PrtInt)0));
            currentMachine.Assert(TMP_tmp70, "");
            (m1)[((PrtInt)1)] = ((PrtInt)3);
            TMP_tmp71 = (m1)[((PrtInt)1)];
            TMP_tmp72 = -(TMP_tmp71);
            (m1)[((PrtInt)2)] = TMP_tmp72;
            TMP_tmp73 = (m1)[((PrtInt)1)];
            TMP_tmp74 = (m1)[((PrtInt)2)];
            TMP_tmp75 = (TMP_tmp73) + (TMP_tmp74);
            TMP_tmp76 = (TMP_tmp75) == (((PrtInt)0));
            currentMachine.Assert(TMP_tmp76, "");
            TMP_tmp77 = (m1)[((PrtInt)0)];
            TMP_tmp78 = (m1)[((PrtInt)1)];
            TMP_tmp79 = (TMP_tmp77) < (TMP_tmp78);
            currentMachine.Assert(TMP_tmp79, "");
            (m1)[((PrtInt)0)] = ((PrtInt)3);
            TMP_tmp80 = (m1)[((PrtInt)0)];
            TMP_tmp81 = (m1)[((PrtInt)1)];
            TMP_tmp82 = (TMP_tmp80) <= (TMP_tmp81);
            currentMachine.Assert(TMP_tmp82, "");
            TMP_tmp83 = (m1)[((PrtInt)1)];
            TMP_tmp84 = (m1)[((PrtInt)2)];
            TMP_tmp85 = (TMP_tmp83) > (TMP_tmp84);
            currentMachine.Assert(TMP_tmp85, "");
            (m1)[((PrtInt)4)] = ((PrtInt)3);
            TMP_tmp86 = (m1)[((PrtInt)1)];
            TMP_tmp87 = (m1)[((PrtInt)4)];
            TMP_tmp88 = (TMP_tmp86) >= (TMP_tmp87);
            currentMachine.Assert(TMP_tmp88, "");
            (m3)[((PrtInt)0)] = ((PrtBool)true);
            (m3)[((PrtInt)2)] = ((PrtBool)false);
            TMP_tmp89 = (m3)[((PrtInt)0)];
            TMP_tmp90 = (m3)[((PrtInt)2)];
            TMP_tmp91 = !(TMP_tmp90);
            TMP_tmp92 = (TMP_tmp89) == (TMP_tmp91);
            currentMachine.Assert(TMP_tmp92, "");
            TMP_tmp93 = ((PrtInt)(m3).Count);
            TMP_tmp94 = (TMP_tmp93) == (((PrtInt)2));
            currentMachine.Assert(TMP_tmp94, "");
            TMP_tmp95 = ((PrtInt)1);
            s.Insert(((PrtInt)0), TMP_tmp95);
            (tmp3)[((PrtInt)0)] = ((PrtSeq<PrtInt>)((IPrtValue)s)?.Clone());
            TMP_tmp96 = ((PrtSeq<PrtInt>)((IPrtValue)s)?.Clone());
            TMP_tmp97 = ((PrtMap<PrtInt, PrtSeq<PrtInt>>)((IPrtValue)tmp3)?.Clone());
            TMP_tmp98 = (new tuple(TMP_tmp96, TMP_tmp97));
            t = TMP_tmp98;
            TMP_tmp99 = ((PrtInt)2);
            (t).a.Insert(((PrtInt)0), TMP_tmp99);
            TMP_tmp100 = (t).a;
            TMP_tmp101 = (TMP_tmp100)[((PrtInt)0)];
            TMP_tmp102 = (TMP_tmp101) == (((PrtInt)2));
            currentMachine.Assert(TMP_tmp102, "");
            TMP_tmp103 = ((PrtInt)2);
            (t).a.Insert(((PrtInt)1), TMP_tmp103);
            TMP_tmp104 = (t).a;
            TMP_tmp105 = (TMP_tmp104)[((PrtInt)1)];
            TMP_tmp106 = (TMP_tmp105) == (((PrtInt)2));
            currentMachine.Assert(TMP_tmp106, "");
            TMP_tmp107 = ((PrtInt)3);
            (t).a.Insert(((PrtInt)0), TMP_tmp107);
            TMP_tmp108 = (t).a;
            TMP_tmp109 = (TMP_tmp108)[((PrtInt)0)];
            TMP_tmp110 = (TMP_tmp109) == (((PrtInt)3));
            currentMachine.Assert(TMP_tmp110, "");
            TMP_tmp111 = (t).a;
            TMP_tmp112 = (TMP_tmp111)[((PrtInt)1)];
            TMP_tmp113 = (TMP_tmp112) == (((PrtInt)2));
            currentMachine.Assert(TMP_tmp113, "");
            TMP_tmp114 = (s)[((PrtInt)0)];
            TMP_tmp115 = (TMP_tmp114) == (((PrtInt)1));
            currentMachine.Assert(TMP_tmp115, "");
            TMP_tmp116 = (tmp3)[((PrtInt)0)];
            TMP_tmp117 = (TMP_tmp116)[((PrtInt)0)];
            TMP_tmp118 = (TMP_tmp117) == (((PrtInt)1));
            currentMachine.Assert(TMP_tmp118, "");
            TMP_tmp119 = (t).b;
            TMP_tmp120 = (TMP_tmp119)[((PrtInt)0)];
            TMP_tmp121 = (TMP_tmp120)[((PrtInt)0)];
            TMP_tmp122 = (TMP_tmp121) == (((PrtInt)1));
            currentMachine.Assert(TMP_tmp122, "");
            TMP_tmp123 = ((PrtInt)2);
            s.Insert(((PrtInt)0), TMP_tmp123);
            TMP_tmp124 = (s)[((PrtInt)0)];
            TMP_tmp125 = (TMP_tmp124) == (((PrtInt)2));
            TMP_tmp126 = (s)[((PrtInt)1)];
            TMP_tmp127 = (TMP_tmp126) == (((PrtInt)1));
            TMP_tmp128 = (TMP_tmp125) && (TMP_tmp127);
            currentMachine.Assert(TMP_tmp128, "");
            TMP_tmp129 = (tmp3)[((PrtInt)0)];
            TMP_tmp130 = (TMP_tmp129)[((PrtInt)0)];
            TMP_tmp131 = (TMP_tmp130) == (((PrtInt)1));
            currentMachine.Assert(TMP_tmp131, "");
            TMP_tmp132 = (t).b;
            TMP_tmp133 = (TMP_tmp132)[((PrtInt)0)];
            TMP_tmp134 = (TMP_tmp133)[((PrtInt)0)];
            TMP_tmp135 = (TMP_tmp134) == (((PrtInt)1));
            currentMachine.Assert(TMP_tmp135, "");
            (tmp3)[((PrtInt)0)] = ((PrtSeq<PrtInt>)((IPrtValue)s)?.Clone());
            TMP_tmp136 = (tmp3)[((PrtInt)0)];
            TMP_tmp137 = (TMP_tmp136)[((PrtInt)0)];
            TMP_tmp138 = (TMP_tmp137) == (((PrtInt)2));
            currentMachine.Assert(TMP_tmp138, "");
            TMP_tmp139 = (t).b;
            TMP_tmp140 = (TMP_tmp139)[((PrtInt)0)];
            TMP_tmp141 = (TMP_tmp140)[((PrtInt)0)];
            TMP_tmp142 = (TMP_tmp141) == (((PrtInt)1));
            currentMachine.Assert(TMP_tmp142, "");
            TMP_tmp143 = ((PrtInt)0);
            TMP_tmp144 = ((PrtInt)0);
            TMP_tmp145 = new PrtTuple<PrtInt, PrtInt>((PrtInt)TMP_tmp143, (PrtInt)TMP_tmp144);
            s4.Insert(((PrtInt)0), TMP_tmp145);
            TMP_tmp146 = ((PrtInt)1);
            TMP_tmp147 = ((PrtInt)1);
            TMP_tmp148 = new PrtTuple<PrtInt, PrtInt>((PrtInt)TMP_tmp146, (PrtInt)TMP_tmp147);
            s4.Insert(((PrtInt)1), TMP_tmp148);
            TMP_tmp149 = ((PrtInt)2);
            TMP_tmp150 = ((PrtInt)2);
            TMP_tmp151 = new PrtTuple<PrtInt, PrtInt>((PrtInt)TMP_tmp149, (PrtInt)TMP_tmp150);
            s4.Insert(((PrtInt)2), TMP_tmp151);
            TMP_tmp152 = ((PrtInt)1);
            TMP_tmp153 = ((PrtInt)2);
            TMP_tmp154 = new PrtTuple<PrtInt, PrtInt>((PrtInt)TMP_tmp152, (PrtInt)TMP_tmp153);
            s8.Insert(((PrtInt)0), TMP_tmp154);
            TMP_tmp155 = ((PrtInt)2);
            TMP_tmp156 = ((PrtInt)3);
            TMP_tmp157 = new PrtTuple<PrtInt, PrtInt>((PrtInt)TMP_tmp155, (PrtInt)TMP_tmp156);
            s8.Insert(((PrtInt)1), TMP_tmp157);
            TMP_tmp158 = ((PrtInt)3);
            TMP_tmp159 = ((PrtInt)4);
            TMP_tmp160 = new PrtTuple<PrtInt, PrtInt>((PrtInt)TMP_tmp158, (PrtInt)TMP_tmp159);
            s8.Insert(((PrtInt)2), TMP_tmp160);
            TMP_tmp161 = (s4)[((PrtInt)0)];
            TMP_tmp162 = TMP_tmp161.Item1;
            TMP_tmp163 = (s8)[((PrtInt)1)];
            TMP_tmp164 = TMP_tmp163.Item2;
            TMP_tmp165 = (TMP_tmp162) + (TMP_tmp164);
            TMP_tmp166 = (s8)[((PrtInt)2)];
            TMP_tmp167 = TMP_tmp166.Item1;
            TMP_tmp168 = (TMP_tmp165) - (TMP_tmp167);
            TMP_tmp169 = (TMP_tmp168) == (((PrtInt)0));
            currentMachine.Assert(TMP_tmp169, "");
            TMP_tmp170 = (s4)[((PrtInt)1)];
            TMP_tmp171 = TMP_tmp170.Item1;
            TMP_tmp172 = (s8)[((PrtInt)1)];
            TMP_tmp173 = TMP_tmp172.Item2;
            TMP_tmp174 = (TMP_tmp171) * (TMP_tmp173);
            TMP_tmp175 = (s8)[((PrtInt)2)];
            TMP_tmp176 = TMP_tmp175.Item1;
            TMP_tmp177 = (TMP_tmp174) / (TMP_tmp176);
            TMP_tmp178 = (TMP_tmp177) == (((PrtInt)1));
            currentMachine.Assert(TMP_tmp178, "");
            TMP_tmp179 = new PHalt(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp179);
            throw new PUnreachableCodeException();
        }
        public PrtInt foo()
        {
            Main currentMachine = this;
            return ((PrtInt)1);
        }
        public tuple GetT()
        {
            Main currentMachine = this;
            return ((tuple)((IPrtValue)t)?.Clone());
        }
        public PrtInt IncY()
        {
            Main currentMachine = this;
            PrtInt TMP_tmp0_1 = ((PrtInt)0);
            TMP_tmp0_1 = (y) + (((PrtInt)1));
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
    internal class XYZ : PMachine
    {
        private PrtSeq<PrtInt> ss = new PrtSeq<PrtInt>();
        public class ConstructorEvent : PEvent<PrtSeq<PrtInt>> { public ConstructorEvent(PrtSeq<PrtInt> val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PrtSeq<PrtInt>)value); }
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
            PrtSeq<PrtInt> payload = ((PEvent<PrtSeq<PrtInt>>)currentMachine.ReceivedEvent).PayloadT;
            PrtInt TMP_tmp0_2 = ((PrtInt)0);
            PrtBool TMP_tmp1_1 = ((PrtBool)false);
            ss = ((PrtSeq<PrtInt>)((IPrtValue)payload)?.Clone());
            TMP_tmp0_2 = (ss)[((PrtInt)0)];
            TMP_tmp1_1 = (TMP_tmp0_2) == (((PrtInt)3));
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

    public class tuple : PrtTuple<PrtSeq<PrtInt>, PrtMap<PrtInt, PrtSeq<PrtInt>>>
    {
        public tuple(PrtSeq<PrtInt> a, PrtMap<PrtInt, PrtSeq<PrtInt>> b) : base(a, b) { }
        public tuple(IReadOnlyPrtTuple<PrtSeq<PrtInt>, PrtMap<PrtInt, PrtSeq<PrtInt>>> other) : base(other) { }
        public PrtSeq<PrtInt> a { get => Item1; set => Item1 = value; }
        public PrtMap<PrtInt, PrtSeq<PrtInt>> b { get => Item2; set => Item2 = value; }
        public override IPrtValue Clone() { return new tuple(a, b); }
    }
    public class tuple_1 : PrtTuple<PrtInt, PrtInt>
    {
        public tuple_1(PrtInt a, PrtInt b) : base(a, b) { }
        public tuple_1(IReadOnlyPrtTuple<PrtInt, PrtInt> other) : base(other) { }
        public PrtInt a { get => Item1; set => Item1 = value; }
        public PrtInt b { get => Item2; set => Item2 = value; }
        public override IPrtValue Clone() { return new tuple_1(a, b); }
    }
    public class tuple_2 : PrtTuple<PrtSeq<IPrtValue>, PrtMap<PrtInt, PrtSeq<IPrtValue>>>
    {
        public tuple_2(PrtSeq<IPrtValue> a, PrtMap<PrtInt, PrtSeq<IPrtValue>> b) : base(a, b) { }
        public tuple_2(IReadOnlyPrtTuple<PrtSeq<IPrtValue>, PrtMap<PrtInt, PrtSeq<IPrtValue>>> other) : base(other) { }
        public PrtSeq<IPrtValue> a { get => Item1; set => Item1 = value; }
        public PrtMap<PrtInt, PrtSeq<IPrtValue>> b { get => Item2; set => Item2 = value; }
        public override IPrtValue Clone() { return new tuple_2(a, b); }
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
