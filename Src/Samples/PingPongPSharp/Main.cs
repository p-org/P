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
        private PrtSeq v1 = new PrtSeq();
        private PrtSeq v3 = new PrtSeq();
        private PrtSeq v2 = new PrtSeq();
        private PrtNamedTuple t = (new PrtNamedTuple(new string[] { "a", "b" }, new PrtSeq(), new PrtMap()));
        private PrtNamedTuple t1 = (new PrtNamedTuple(new string[] { "x", "y" }, ((PrtInt)0), ((PrtInt)0)));
        private PrtMap t0 = new PrtMap();
        private PrtMap t2 = new PrtMap();
        private PrtNamedTuple t3 = (new PrtNamedTuple(new string[] { "x", "y" }, new PrtSeq(), new PrtMap()));
        private PrtSeq s1 = new PrtSeq();
        private PrtSeq s4 = new PrtSeq();
        private PrtSeq s2 = new PrtSeq();
        private PrtSeq s3 = new PrtSeq();
        private PrtMap m2 = new PrtMap();
        private PrtSeq s6 = new PrtSeq();
        private PrtInt i = ((PrtInt)0);
        private PMachineValue mac = null;
        private PrtInt day = ((PrtInt)0);
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
            PrtInt TMP_tmp1 = ((PrtInt)0);
            PrtBool TMP_tmp2 = ((PrtBool)false);
            PrtInt TMP_tmp3 = ((PrtInt)0);
            PrtInt TMP_tmp4 = ((PrtInt)0);
            PrtBool TMP_tmp5 = ((PrtBool)false);
            PrtInt TMP_tmp6 = ((PrtInt)0);
            PrtBool TMP_tmp7 = ((PrtBool)false);
            PrtInt TMP_tmp8 = ((PrtInt)0);
            PrtBool TMP_tmp9 = ((PrtBool)false);
            PrtNamedTuple TMP_tmp10 = (new PrtNamedTuple(new string[] { "x", "y" }, ((PrtInt)0), ((PrtInt)0)));
            PrtBool TMP_tmp11 = ((PrtBool)false);
            PrtSeq TMP_tmp12 = new PrtSeq();
            PrtBool TMP_tmp13 = ((PrtBool)false);
            PrtInt TMP_tmp14 = ((PrtInt)0);
            PrtBool TMP_tmp15 = ((PrtBool)false);
            PrtInt TMP_tmp16 = ((PrtInt)0);
            PrtInt TMP_tmp17 = ((PrtInt)0);
            PrtNamedTuple TMP_tmp18 = (new PrtNamedTuple(new string[] { "x", "y" }, ((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp19 = ((PrtInt)0);
            PrtBool TMP_tmp20 = ((PrtBool)false);
            PrtInt TMP_tmp21 = ((PrtInt)0);
            PrtBool TMP_tmp22 = ((PrtBool)false);
            PrtBool TMP_tmp23 = ((PrtBool)false);
            PrtNamedTuple TMP_tmp24 = (new PrtNamedTuple(new string[] { "x", "y" }, ((PrtInt)0), ((PrtInt)0)));
            PrtSeq TMP_tmp25 = new PrtSeq();
            PrtInt TMP_tmp26 = ((PrtInt)0);
            PrtInt TMP_tmp27 = ((PrtInt)0);
            PrtInt TMP_tmp28 = ((PrtInt)0);
            PrtBool TMP_tmp29 = ((PrtBool)false);
            PrtInt TMP_tmp30 = ((PrtInt)0);
            PrtBool TMP_tmp31 = ((PrtBool)false);
            PrtInt TMP_tmp32 = ((PrtInt)0);
            PrtBool TMP_tmp33 = ((PrtBool)false);
            PrtBool TMP_tmp34 = ((PrtBool)false);
            PrtInt TMP_tmp35 = ((PrtInt)0);
            PrtInt TMP_tmp36 = ((PrtInt)0);
            PrtSeq TMP_tmp37 = new PrtSeq();
            PrtInt TMP_tmp38 = ((PrtInt)0);
            PrtInt TMP_tmp39 = ((PrtInt)0);
            PrtInt TMP_tmp40 = ((PrtInt)0);
            PrtSeq TMP_tmp41 = new PrtSeq();
            PrtInt TMP_tmp42 = ((PrtInt)0);
            PrtInt TMP_tmp43 = ((PrtInt)0);
            PrtSeq TMP_tmp44 = new PrtSeq();
            PMachineValue TMP_tmp45 = null;
            PrtSeq TMP_tmp46 = new PrtSeq();
            PrtInt TMP_tmp47 = ((PrtInt)0);
            PrtInt TMP_tmp48 = ((PrtInt)0);
            PrtInt TMP_tmp49 = ((PrtInt)0);
            PrtBool TMP_tmp50 = ((PrtBool)false);
            PrtInt TMP_tmp51 = ((PrtInt)0);
            PrtBool TMP_tmp52 = ((PrtBool)false);
            PrtBool TMP_tmp53 = ((PrtBool)false);
            PrtSeq TMP_tmp54 = new PrtSeq();
            PrtInt TMP_tmp55 = ((PrtInt)0);
            PrtBool TMP_tmp56 = ((PrtBool)false);
            PrtSeq TMP_tmp57 = new PrtSeq();
            PrtInt TMP_tmp58 = ((PrtInt)0);
            PrtBool TMP_tmp59 = ((PrtBool)false);
            PrtBool TMP_tmp60 = ((PrtBool)false);
            PrtBool TMP_tmp61 = ((PrtBool)false);
            PrtSeq TMP_tmp62 = new PrtSeq();
            PrtInt TMP_tmp63 = ((PrtInt)0);
            PrtBool TMP_tmp64 = ((PrtBool)false);
            PrtSeq TMP_tmp65 = new PrtSeq();
            PrtSeq TMP_tmp66 = new PrtSeq();
            PrtInt TMP_tmp67 = ((PrtInt)0);
            PrtBool TMP_tmp68 = ((PrtBool)false);
            PrtInt TMP_tmp69 = ((PrtInt)0);
            PrtBool TMP_tmp70 = ((PrtBool)false);
            PrtInt TMP_tmp71 = ((PrtInt)0);
            PrtBool TMP_tmp72 = ((PrtBool)false);
            PrtSeq TMP_tmp73 = new PrtSeq();
            PrtInt TMP_tmp74 = ((PrtInt)0);
            PrtInt TMP_tmp75 = ((PrtInt)0);
            PrtInt TMP_tmp76 = ((PrtInt)0);
            PrtBool TMP_tmp77 = ((PrtBool)false);
            PrtSeq TMP_tmp78 = new PrtSeq();
            PrtInt TMP_tmp79 = ((PrtInt)0);
            PrtBool TMP_tmp80 = ((PrtBool)false);
            PrtSeq TMP_tmp81 = new PrtSeq();
            PrtInt TMP_tmp82 = ((PrtInt)0);
            PrtBool TMP_tmp83 = ((PrtBool)false);
            PrtBool TMP_tmp84 = ((PrtBool)false);
            PrtInt TMP_tmp85 = ((PrtInt)0);
            PrtBool TMP_tmp86 = ((PrtBool)false);
            PrtSeq TMP_tmp87 = new PrtSeq();
            PrtInt TMP_tmp88 = ((PrtInt)0);
            PrtInt TMP_tmp89 = ((PrtInt)0);
            PrtSeq TMP_tmp90 = new PrtSeq();
            PrtInt TMP_tmp91 = ((PrtInt)0);
            PrtInt TMP_tmp92 = ((PrtInt)0);
            PrtSeq TMP_tmp93 = new PrtSeq();
            PrtMap TMP_tmp94 = new PrtMap();
            PrtSeq TMP_tmp95 = new PrtSeq();
            PrtInt TMP_tmp96 = ((PrtInt)0);
            PrtInt TMP_tmp97 = ((PrtInt)0);
            PrtSeq TMP_tmp98 = new PrtSeq();
            PrtInt TMP_tmp99 = ((PrtInt)0);
            PrtInt TMP_tmp100 = ((PrtInt)0);
            PrtMap TMP_tmp101 = new PrtMap();
            PrtInt TMP_tmp102 = ((PrtInt)0);
            PrtBool TMP_tmp103 = ((PrtBool)false);
            PrtSeq TMP_tmp104 = new PrtSeq();
            PrtInt TMP_tmp105 = ((PrtInt)0);
            PrtBool TMP_tmp106 = ((PrtBool)false);
            PrtSeq TMP_tmp107 = new PrtSeq();
            PrtInt TMP_tmp108 = ((PrtInt)0);
            PrtBool TMP_tmp109 = ((PrtBool)false);
            PrtBool TMP_tmp110 = ((PrtBool)false);
            PrtSeq TMP_tmp111 = new PrtSeq();
            PrtInt TMP_tmp112 = ((PrtInt)0);
            PrtBool TMP_tmp113 = ((PrtBool)false);
            PrtSeq TMP_tmp114 = new PrtSeq();
            PrtInt TMP_tmp115 = ((PrtInt)0);
            PrtBool TMP_tmp116 = ((PrtBool)false);
            PrtBool TMP_tmp117 = ((PrtBool)false);
            PrtNamedTuple TMP_tmp118 = (new PrtNamedTuple(new string[] { "x", "y" }, ((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp119 = ((PrtInt)0);
            PrtInt TMP_tmp120 = ((PrtInt)0);
            PrtNamedTuple TMP_tmp121 = (new PrtNamedTuple(new string[] { "x", "y" }, ((PrtInt)0), ((PrtInt)0)));
            PrtSeq TMP_tmp122 = new PrtSeq();
            PrtNamedTuple TMP_tmp123 = (new PrtNamedTuple(new string[] { "x", "y" }, ((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp124 = ((PrtInt)0);
            PrtBool TMP_tmp125 = ((PrtBool)false);
            PrtInt TMP_tmp126 = ((PrtInt)0);
            PrtBool TMP_tmp127 = ((PrtBool)false);
            PrtBool TMP_tmp128 = ((PrtBool)false);
            PrtInt TMP_tmp129 = ((PrtInt)0);
            PrtBool TMP_tmp130 = ((PrtBool)false);
            PrtBool TMP_tmp131 = ((PrtBool)false);
            PrtInt TMP_tmp132 = ((PrtInt)0);
            PrtBool TMP_tmp133 = ((PrtBool)false);
            PrtBool TMP_tmp134 = ((PrtBool)false);
            PrtSeq TMP_tmp135 = new PrtSeq();
            PrtNamedTuple TMP_tmp136 = (new PrtNamedTuple(new string[] { "x", "y" }, ((PrtInt)0), ((PrtInt)0)));
            PrtSeq TMP_tmp137 = new PrtSeq();
            PrtMap TMP_tmp138 = new PrtMap();
            PrtNamedTuple TMP_tmp139 = (new PrtNamedTuple(new string[] { "x", "y" }, new PrtSeq(), new PrtMap()));
            PrtSeq TMP_tmp140 = new PrtSeq();
            PrtNamedTuple TMP_tmp141 = (new PrtNamedTuple(new string[] { "x", "y" }, ((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp142 = ((PrtInt)0);
            PrtBool TMP_tmp143 = ((PrtBool)false);
            PrtMap TMP_tmp144 = new PrtMap();
            PrtInt TMP_tmp145 = ((PrtInt)0);
            PrtBool TMP_tmp146 = ((PrtBool)false);
            PrtSeq TMP_tmp147 = new PrtSeq();
            PrtInt TMP_tmp148 = ((PrtInt)0);
            PrtBool TMP_tmp149 = ((PrtBool)false);
            PrtMap TMP_tmp150 = new PrtMap();
            PrtInt TMP_tmp151 = ((PrtInt)0);
            PrtBool TMP_tmp152 = ((PrtBool)false);
            PrtBool TMP_tmp153 = ((PrtBool)false);
            PrtInt TMP_tmp154 = ((PrtInt)0);
            PrtInt TMP_tmp155 = ((PrtInt)0);
            TMP_tmp0 = (PrtInt)(((PrtNamedTuple)t1)["x"]);
            TMP_tmp1 = (PrtInt)(((PrtInt)0));
            TMP_tmp2 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp0), PrtValues.Box((long)TMP_tmp1))));
            currentMachine.Assert(TMP_tmp2, "");
            TMP_tmp3 = (PrtInt)(((PrtNamedTuple)t1)["y"]);
            TMP_tmp4 = (PrtInt)(((PrtInt)0));
            TMP_tmp5 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp3), PrtValues.Box((long)TMP_tmp4))));
            currentMachine.Assert(TMP_tmp5, "");
            TMP_tmp6 = (PrtInt)(((PrtNamedTuple)t1)["x"]);
            TMP_tmp7 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp6), PrtValues.Box((long)(PrtEnum[foo0])))));
            currentMachine.Assert(TMP_tmp7, "");
            TMP_tmp8 = (PrtInt)(((PrtNamedTuple)t1)["y"]);
            TMP_tmp9 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp8), PrtValues.Box((long)(PrtEnum[bar0])))));
            currentMachine.Assert(TMP_tmp9, "");
            TMP_tmp10 = (PrtNamedTuple)((new PrtNamedTuple(new string[] { "x", "y" }, ((PrtInt)0), ((PrtInt)0))));
            TMP_tmp11 = (PrtBool)((PrtValues.SafeEquals(t1, TMP_tmp10)));
            currentMachine.Assert(TMP_tmp11, "");
            TMP_tmp12 = (PrtSeq)(new PrtSeq());
            TMP_tmp13 = (PrtBool)((PrtValues.SafeEquals(v1, TMP_tmp12)));
            currentMachine.Assert(TMP_tmp13, "");
            TMP_tmp14 = (PrtInt)(((PrtInt)(v1).Count));
            TMP_tmp15 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp14, ((PrtInt)0))));
            currentMachine.Assert(TMP_tmp15, "");
            TMP_tmp16 = (PrtInt)((PrtEnum[foo1]));
            TMP_tmp17 = (PrtInt)((PrtEnum[bar2]));
            TMP_tmp18 = (PrtNamedTuple)((new PrtNamedTuple(new string[] { "x", "y" }, TMP_tmp16, TMP_tmp17)));
            t1 = TMP_tmp18;
            TMP_tmp19 = (PrtInt)(((PrtNamedTuple)t1)["x"]);
            TMP_tmp20 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp19), PrtValues.Box((long)(PrtEnum[foo1])))));
            TMP_tmp21 = (PrtInt)(((PrtNamedTuple)t1)["y"]);
            TMP_tmp22 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp21), PrtValues.Box((long)(PrtEnum[bar2])))));
            TMP_tmp23 = (PrtBool)((TMP_tmp20) && (TMP_tmp22));
            currentMachine.Assert(TMP_tmp23, "");
            ((PrtNamedTuple)t1)["x"] = (PrtInt)((PrtEnum[foo4]));
            TMP_tmp24 = (PrtNamedTuple)(baz());
            t1 = TMP_tmp24;
            TMP_tmp25 = (PrtSeq)(new PrtSeq());
            v1 = TMP_tmp25;
            TMP_tmp26 = (PrtInt)((PrtEnum[foo1]));
            ((PrtSeq)v1).Insert(((PrtInt)0), TMP_tmp26);
            TMP_tmp27 = (PrtInt)((PrtEnum[foo2]));
            ((PrtSeq)v1).Insert(((PrtInt)0), TMP_tmp27);
            TMP_tmp28 = (PrtInt)(((PrtSeq)v1)[((PrtInt)0)]);
            TMP_tmp29 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp28), PrtValues.Box((long)(PrtEnum[foo2])))));
            currentMachine.Assert(TMP_tmp29, "");
            TMP_tmp30 = (PrtInt)(((PrtSeq)v1)[((PrtInt)0)]);
            TMP_tmp31 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp30), PrtValues.Box((long)(PrtEnum[foo2])))));
            TMP_tmp32 = (PrtInt)(((PrtSeq)v1)[((PrtInt)1)]);
            TMP_tmp33 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp32), PrtValues.Box((long)(PrtEnum[foo1])))));
            TMP_tmp34 = (PrtBool)((TMP_tmp31) && (TMP_tmp33));
            currentMachine.Assert(TMP_tmp34, "");
            v3 = (PrtSeq)(((PrtSeq)((IPrtValue)v1)?.Clone()));
            ((PrtSeq)v1).RemoveAt(((PrtInt)1));
            TMP_tmp35 = (PrtInt)((PrtEnum[foo3]));
            ((PrtSeq)v1).Insert(((PrtInt)0), TMP_tmp35);
            TMP_tmp36 = (PrtInt)((PrtEnum[foo4]));
            ((PrtSeq)v1).Insert(((PrtInt)0), TMP_tmp36);
            ((PrtSeq)v1).RemoveAt(((PrtInt)1));
            TMP_tmp37 = (PrtSeq)(new PrtSeq());
            v1 = TMP_tmp37;
            TMP_tmp38 = (PrtInt)((PrtEnum[foo1]));
            ((PrtSeq)v1).Insert(((PrtInt)0), TMP_tmp38);
            ((PrtSeq)v1)[((PrtInt)0)] = (PrtInt)((PrtEnum[foo2]));
            TMP_tmp39 = (PrtInt)(foo());
            TMP_tmp40 = (PrtInt)((TMP_tmp39) - (((PrtInt)1)));
            ((PrtSeq)v1).RemoveAt(TMP_tmp40);
            TMP_tmp41 = (PrtSeq)(new PrtSeq());
            v1 = TMP_tmp41;
            TMP_tmp42 = (PrtInt)((PrtEnum[foo1]));
            ((PrtSeq)v1).Insert(((PrtInt)0), TMP_tmp42);
            TMP_tmp43 = (PrtInt)((PrtEnum[foo3]));
            ((PrtSeq)v1).Insert(((PrtInt)0), TMP_tmp43);
            TMP_tmp44 = (PrtSeq)(((PrtSeq)((IPrtValue)v1)?.Clone()));
            TMP_tmp45 = (PMachineValue)(currentMachine.CreateInterface<I_XYZ>(currentMachine, TMP_tmp44));
            mac = (PMachineValue)TMP_tmp45;
            TMP_tmp46 = (PrtSeq)(new PrtSeq());
            v1 = TMP_tmp46;
            TMP_tmp47 = (PrtInt)((PrtEnum[foo1]));
            ((PrtSeq)v1).Insert(((PrtInt)0), TMP_tmp47);
            TMP_tmp48 = (PrtInt)((PrtEnum[foo2]));
            ((PrtSeq)v1).Insert(((PrtInt)1), TMP_tmp48);
            TMP_tmp49 = (PrtInt)(((PrtSeq)v1)[((PrtInt)0)]);
            TMP_tmp50 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp49), PrtValues.Box((long)(PrtEnum[foo1])))));
            TMP_tmp51 = (PrtInt)(((PrtSeq)v1)[((PrtInt)1)]);
            TMP_tmp52 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp51), PrtValues.Box((long)(PrtEnum[foo2])))));
            TMP_tmp53 = (PrtBool)((TMP_tmp50) && (TMP_tmp52));
            currentMachine.Assert(TMP_tmp53, "");
            ((PrtMap)t2)[((PrtInt)0)] = (PrtSeq)(((PrtSeq)((IPrtValue)v1)?.Clone()));
            TMP_tmp54 = (PrtSeq)(((PrtMap)t2)[((PrtInt)0)]);
            TMP_tmp55 = (PrtInt)(((PrtSeq)TMP_tmp54)[((PrtInt)0)]);
            TMP_tmp56 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp55), PrtValues.Box((long)(PrtEnum[foo1])))));
            TMP_tmp57 = (PrtSeq)(((PrtMap)t2)[((PrtInt)0)]);
            TMP_tmp58 = (PrtInt)(((PrtSeq)TMP_tmp57)[((PrtInt)1)]);
            TMP_tmp59 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp58), PrtValues.Box((long)(PrtEnum[foo2])))));
            TMP_tmp60 = (PrtBool)((TMP_tmp56) && (TMP_tmp59));
            currentMachine.Assert(TMP_tmp60, "");
            TMP_tmp61 = (PrtBool)(((PrtBool)(t2).ContainsKey(((PrtInt)0))));
            currentMachine.Assert(TMP_tmp61, "");
            TMP_tmp62 = (PrtSeq)((t2).CloneKeys());
            TMP_tmp63 = (PrtInt)(((PrtSeq)TMP_tmp62)[((PrtInt)0)]);
            i = TMP_tmp63;
            TMP_tmp64 = (PrtBool)((PrtValues.SafeEquals(i, ((PrtInt)0))));
            currentMachine.Assert(TMP_tmp64, "");
            TMP_tmp65 = (PrtSeq)((t2).CloneValues());
            TMP_tmp66 = (PrtSeq)(((PrtSeq)TMP_tmp65)[((PrtInt)0)]);
            TMP_tmp67 = (PrtInt)(((PrtSeq)TMP_tmp66)[((PrtInt)0)]);
            TMP_tmp68 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp67), PrtValues.Box((long)(PrtEnum[foo1])))));
            currentMachine.Assert(TMP_tmp68, "");
            TMP_tmp69 = (PrtInt)(((PrtInt)(t2).Count));
            TMP_tmp70 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp69, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp70, "");
            ((PrtMap)t2).Remove(((PrtInt)0));
            TMP_tmp71 = (PrtInt)(((PrtInt)(t2).Count));
            TMP_tmp72 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp71, ((PrtInt)0))));
            currentMachine.Assert(TMP_tmp72, "");
            TMP_tmp73 = (PrtSeq)(new PrtSeq());
            v3 = TMP_tmp73;
            TMP_tmp74 = (PrtInt)((PrtEnum[foo3]));
            ((PrtSeq)v3).Insert(((PrtInt)0), TMP_tmp74);
            TMP_tmp75 = (PrtInt)((PrtEnum[foo4]));
            ((PrtSeq)v3).Insert(((PrtInt)1), TMP_tmp75);
            ((PrtMap)t2)[((PrtInt)0)] = (PrtSeq)(((PrtSeq)((IPrtValue)v1)?.Clone()));
            ((PrtMap)t2)[((PrtInt)1)] = (PrtSeq)(((PrtSeq)((IPrtValue)v3)?.Clone()));
            TMP_tmp76 = (PrtInt)(((PrtInt)(t2).Count));
            TMP_tmp77 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp76, ((PrtInt)2))));
            currentMachine.Assert(TMP_tmp77, "");
            TMP_tmp78 = (PrtSeq)(((PrtMap)t2)[((PrtInt)1)]);
            TMP_tmp79 = (PrtInt)(((PrtSeq)TMP_tmp78)[((PrtInt)0)]);
            TMP_tmp80 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp79), PrtValues.Box((long)(PrtEnum[foo3])))));
            TMP_tmp81 = (PrtSeq)(((PrtMap)t2)[((PrtInt)1)]);
            TMP_tmp82 = (PrtInt)(((PrtSeq)TMP_tmp81)[((PrtInt)1)]);
            TMP_tmp83 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp82), PrtValues.Box((long)(PrtEnum[foo4])))));
            TMP_tmp84 = (PrtBool)((TMP_tmp80) && (TMP_tmp83));
            currentMachine.Assert(TMP_tmp84, "");
            ((PrtMap)t2).Remove(((PrtInt)1));
            TMP_tmp85 = (PrtInt)(((PrtInt)(t2).Count));
            TMP_tmp86 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp85, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp86, "");
            TMP_tmp87 = (PrtSeq)(new PrtSeq());
            v1 = TMP_tmp87;
            TMP_tmp88 = (PrtInt)((PrtEnum[foo1]));
            ((PrtSeq)v1).Insert(((PrtInt)0), TMP_tmp88);
            TMP_tmp89 = (PrtInt)((PrtEnum[foo2]));
            ((PrtSeq)v1).Insert(((PrtInt)1), TMP_tmp89);
            ((PrtMap)t2)[((PrtInt)0)] = (PrtSeq)(((PrtSeq)((IPrtValue)v1)?.Clone()));
            TMP_tmp90 = (PrtSeq)(new PrtSeq());
            v3 = TMP_tmp90;
            TMP_tmp91 = (PrtInt)((PrtEnum[foo3]));
            ((PrtSeq)v3).Insert(((PrtInt)0), TMP_tmp91);
            TMP_tmp92 = (PrtInt)((PrtEnum[foo4]));
            ((PrtSeq)v3).Insert(((PrtInt)1), TMP_tmp92);
            ((PrtMap)t2)[((PrtInt)1)] = (PrtSeq)(((PrtSeq)((IPrtValue)v3)?.Clone()));
            TMP_tmp93 = (PrtSeq)(new PrtSeq());
            s6 = TMP_tmp93;
            TMP_tmp94 = (PrtMap)(((PrtMap)((IPrtValue)t2)?.Clone()));
            ((PrtSeq)s6).Insert(((PrtInt)0), TMP_tmp94);
            TMP_tmp95 = (PrtSeq)(new PrtSeq());
            v1 = TMP_tmp95;
            TMP_tmp96 = (PrtInt)((PrtEnum[foo1]));
            ((PrtSeq)v1).Insert(((PrtInt)0), TMP_tmp96);
            TMP_tmp97 = (PrtInt)((PrtEnum[foo2]));
            ((PrtSeq)v1).Insert(((PrtInt)1), TMP_tmp97);
            TMP_tmp98 = (PrtSeq)(new PrtSeq());
            v3 = TMP_tmp98;
            TMP_tmp99 = (PrtInt)((PrtEnum[foo3]));
            ((PrtSeq)v3).Insert(((PrtInt)0), TMP_tmp99);
            TMP_tmp100 = (PrtInt)((PrtEnum.Get("foo4")));
            ((PrtSeq)v3).Insert(((PrtInt)1), TMP_tmp100);
            TMP_tmp101 = (PrtMap)(new PrtMap());
            t2 = TMP_tmp101;
            ((PrtMap)t2)[((PrtInt)0)] = (PrtSeq)(((PrtSeq)((IPrtValue)v1)?.Clone()));
            ((PrtMap)t2)[((PrtInt)1)] = (PrtSeq)(((PrtSeq)((IPrtValue)v3)?.Clone()));
            TMP_tmp102 = (PrtInt)(((PrtInt)(t2).Count));
            TMP_tmp103 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp102, ((PrtInt)2))));
            currentMachine.Assert(TMP_tmp103, "");
            TMP_tmp104 = (PrtSeq)(((PrtMap)t2)[((PrtInt)0)]);
            TMP_tmp105 = (PrtInt)(((PrtSeq)TMP_tmp104)[((PrtInt)0)]);
            TMP_tmp106 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp105), PrtValues.Box((long)(PrtEnum[foo1])))));
            TMP_tmp107 = (PrtSeq)(((PrtMap)t2)[((PrtInt)0)]);
            TMP_tmp108 = (PrtInt)(((PrtSeq)TMP_tmp107)[((PrtInt)1)]);
            TMP_tmp109 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp108), PrtValues.Box((long)(PrtEnum[foo2])))));
            TMP_tmp110 = (PrtBool)((TMP_tmp106) && (TMP_tmp109));
            currentMachine.Assert(TMP_tmp110, "");
            TMP_tmp111 = (PrtSeq)(((PrtMap)t2)[((PrtInt)1)]);
            TMP_tmp112 = (PrtInt)(((PrtSeq)TMP_tmp111)[((PrtInt)0)]);
            TMP_tmp113 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp112), PrtValues.Box((long)(PrtEnum[foo3])))));
            TMP_tmp114 = (PrtSeq)(((PrtMap)t2)[((PrtInt)1)]);
            TMP_tmp115 = (PrtInt)(((PrtSeq)TMP_tmp114)[((PrtInt)1)]);
            TMP_tmp116 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp115), PrtValues.Box((long)(PrtEnum[foo4])))));
            TMP_tmp117 = (PrtBool)((TMP_tmp113) && (TMP_tmp116));
            currentMachine.Assert(TMP_tmp117, "");
            TMP_tmp118 = (PrtNamedTuple)((new PrtNamedTuple(new string[] { "x", "y" }, ((PrtInt)0), ((PrtInt)0))));
            t1 = TMP_tmp118;
            TMP_tmp119 = (PrtInt)((PrtEnum[foo3]));
            TMP_tmp120 = (PrtInt)((PrtEnum[bar0]));
            TMP_tmp121 = (PrtNamedTuple)((new PrtNamedTuple(new string[] { "x", "y" }, TMP_tmp119, TMP_tmp120)));
            t1 = TMP_tmp121;
            TMP_tmp122 = (PrtSeq)(new PrtSeq());
            v2 = TMP_tmp122;
            TMP_tmp123 = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)t1)?.Clone()));
            ((PrtSeq)v2).Insert(((PrtInt)0), TMP_tmp123);
            ((PrtMap)t0)[((PrtInt)0)] = (PrtInt)((PrtEnum[foo0]));
            ((PrtMap)t0)[((PrtInt)1)] = (PrtInt)((PrtEnum[foo1]));
            ((PrtMap)t0)[((PrtInt)2)] = (PrtInt)((PrtEnum[foo2]));
            ((PrtMap)t0)[((PrtInt)3)] = (PrtInt)((PrtEnum[foo3]));
            TMP_tmp124 = (PrtInt)(((PrtMap)t0)[((PrtInt)0)]);
            TMP_tmp125 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp124), PrtValues.Box((long)(PrtEnum[foo0])))));
            TMP_tmp126 = (PrtInt)(((PrtMap)t0)[((PrtInt)1)]);
            TMP_tmp127 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp126), PrtValues.Box((long)(PrtEnum[foo1])))));
            TMP_tmp128 = (PrtBool)((TMP_tmp125) && (TMP_tmp127));
            TMP_tmp129 = (PrtInt)(((PrtMap)t0)[((PrtInt)2)]);
            TMP_tmp130 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp129), PrtValues.Box((long)(PrtEnum[foo2])))));
            TMP_tmp131 = (PrtBool)((TMP_tmp128) && (TMP_tmp130));
            TMP_tmp132 = (PrtInt)(((PrtMap)t0)[((PrtInt)3)]);
            TMP_tmp133 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp132), PrtValues.Box((long)(PrtEnum[foo3])))));
            TMP_tmp134 = (PrtBool)((TMP_tmp131) && (TMP_tmp133));
            currentMachine.Assert(TMP_tmp134, "");
            TMP_tmp135 = (PrtSeq)(new PrtSeq());
            v2 = TMP_tmp135;
            TMP_tmp136 = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)t1)?.Clone()));
            ((PrtSeq)v2).Insert(((PrtInt)0), TMP_tmp136);
            TMP_tmp137 = (PrtSeq)(((PrtSeq)((IPrtValue)v2)?.Clone()));
            TMP_tmp138 = (PrtMap)(((PrtMap)((IPrtValue)t0)?.Clone()));
            TMP_tmp139 = (PrtNamedTuple)((new PrtNamedTuple(new string[] { "x", "y" }, TMP_tmp137, TMP_tmp138)));
            t3 = TMP_tmp139;
            TMP_tmp140 = (PrtSeq)(((PrtNamedTuple)t3)["x"]);
            TMP_tmp141 = (PrtNamedTuple)(((PrtSeq)TMP_tmp140)[((PrtInt)0)]);
            TMP_tmp142 = (PrtInt)(((PrtNamedTuple)TMP_tmp141)["x"]);
            TMP_tmp143 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp142), PrtValues.Box((long)(PrtEnum[foo3])))));
            currentMachine.Assert(TMP_tmp143, "");
            TMP_tmp144 = (PrtMap)(((PrtNamedTuple)t3)["y"]);
            TMP_tmp145 = (PrtInt)(((PrtMap)TMP_tmp144)[((PrtInt)0)]);
            TMP_tmp146 = (PrtBool)((PrtValues.SafeEquals(PrtValues.Box((long)TMP_tmp145), PrtValues.Box((long)(PrtEnum[foo0])))));
            currentMachine.Assert(TMP_tmp146, "");
            TMP_tmp147 = (PrtSeq)(((PrtNamedTuple)t3)["x"]);
            TMP_tmp148 = (PrtInt)(((PrtInt)(TMP_tmp147).Count));
            TMP_tmp149 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp148, ((PrtInt)1))));
            TMP_tmp150 = (PrtMap)(((PrtNamedTuple)t3)["y"]);
            TMP_tmp151 = (PrtInt)(((PrtInt)(TMP_tmp150).Count));
            TMP_tmp152 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp151, ((PrtInt)4))));
            TMP_tmp153 = (PrtBool)((TMP_tmp149) && (TMP_tmp152));
            currentMachine.Assert(TMP_tmp153, "");
            day = (PrtInt)((PrtEnum.Get["mon"]));
            TMP_tmp154 = (PrtInt)(((PrtInt)97));
            TMP_tmp155 = (PrtInt)(((PrtInt)((IPrtValue)day)?.Clone()));
            XYZfun_param(TMP_tmp154, TMP_tmp155);
        }
        public PrtInt foo()
        {
            Main currentMachine = this;
            return ((PrtInt)1);
        }
        public PrtNamedTuple baz()
        {
            Main currentMachine = this;
            PrtInt TMP_tmp0_1 = ((PrtInt)0);
            PrtInt TMP_tmp1_1 = ((PrtInt)0);
            PrtNamedTuple TMP_tmp2_1 = (new PrtNamedTuple(new string[] { "x", "y" }, ((PrtInt)0), ((PrtInt)0)));
            TMP_tmp0_1 = (PrtInt)(((PrtInt)0));
            TMP_tmp1_1 = (PrtInt)(((PrtInt)0));
            TMP_tmp2_1 = (PrtNamedTuple)((new PrtNamedTuple(new string[] { "x", "y" }, TMP_tmp0_1, TMP_tmp1_1)));
            return ((PrtNamedTuple)((IPrtValue)TMP_tmp2_1)?.Clone());
        }
        public void XYZfun_param(PrtInt x, PrtInt day_1)
        {
            Main currentMachine = this;
            PrtInt TMP_tmp0_2 = ((PrtInt)0);
            PrtBool TMP_tmp1_2 = ((PrtBool)false);
            TMP_tmp0_2 = (PrtInt)((day_1));
            TMP_tmp1_2 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp0_2, ((PrtInt)0))));
            if (TMP_tmp1_2)
            {
            }
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
            ss = (PrtSeq)(((PrtSeq)((IPrtValue)payload)?.Clone()));
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
            PModule.linkMap.Clear();
            PModule.linkMap[nameof(I_Main)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_XYZ), nameof(I_XYZ));
            PModule.linkMap[nameof(I_XYZ)] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Clear();
            PModule.interfaceDefinitionMap.Add(nameof(I_Main), typeof(Main));
            PModule.interfaceDefinitionMap.Add(nameof(I_XYZ), typeof(XYZ));
        }

        public static void InitializeMonitorObserves()
        {
            PModule.monitorObserves.Clear();
        }

        public static void InitializeMonitorMap(PSharpRuntime runtime)
        {
            PModule.monitorMap.Clear();
        }


        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.SetLogger(new PLogger());
            PModule.runtime = runtime;
            PHelper.InitializeInterfaces();
            PHelper.InitializeEnums();
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
            PInterfaces.Clear();
            PInterfaces.AddInterface(nameof(I_Main), nameof(E), nameof(E1), nameof(E2), nameof(E3), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_XYZ), nameof(E), nameof(E1), nameof(E2), nameof(E3), nameof(PHalt));
        }
    }

    public partial class PHelper
    {
        public static void InitializeEnums()
        {
            PrtEnum.Clear();
            PrtEnum.AddEnumElements(new[] { "foo0", "foo1", "foo2", "foo3", "foo4" }, new[] { 0, 1, 2, 3, 4 });
            PrtEnum.AddEnumElements(new[] { "bar0", "bar1", "bar2", "bar3" }, new[] { 0, 1, 2, 3 });
            PrtEnum.AddEnumElements(new[] { "sun", "mon", "tue", "wed", "thur", "fri", "sat" }, new[] { 0, 1, 2, 3, 4, 5, 6 });
        }
    }

}
#pragma warning restore 162, 219, 414
