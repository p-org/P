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
    internal partial class E : PEvent<IPrtValue>
    {
        static E() { AssertVal = 1; AssumeVal = -1; }
        public E() : base() { }
        public E(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new E(); }
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
    internal partial class E3 : PEvent<IPrtValue>
    {
        static E3() { AssertVal = 1; AssumeVal = -1; }
        public E3() : base() { }
        public E3(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new E3(); }
    }
    internal partial class Main : PMachine
    {
        private tuple t = (new tuple(new PrtSeq<PrtInt>(), new PrtMap<PrtInt, PrtSeq<PrtInt>>()));
        private tuple t1 = (new tuple(new PrtSeq<PrtInt>(), new PrtMap<PrtInt, PrtSeq<PrtInt>>()));
        private tuple_1 ts = (new tuple_1(((PrtInt)0), ((PrtInt)0)));
        private PrtTuple<PrtInt, PrtInt> tt = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
        private PrtTuple<PrtInt, IEventWithPayload> te = (new PrtTuple<PrtInt, IEventWithPayload>(((PrtInt)0), null));
        private PrtInt y = ((PrtInt)0);
        private PrtBool b = ((PrtBool)false);
        private IEventWithPayload e = null;
        private IPrtValue a = null;
        private PrtInt tmp = ((PrtInt)0);
        private PrtInt tmp1 = ((PrtInt)0);
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
        private PrtSeq<PrtInt> s12 = new PrtSeq<PrtInt>();
        private PrtSeq<PrtInt> tmp4 = new PrtSeq<PrtInt>();
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
        private PrtMap<PrtInt, IPrtValue> m10 = new PrtMap<PrtInt, IPrtValue>();
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
            this.creates.Add(nameof(I_XYZ));
        }

        public void Anon()
        {
            Main currentMachine = this;
            PrtBool TMP_tmp0 = ((PrtBool)false);
            PrtInt TMP_tmp1 = ((PrtInt)0);
            PrtBool TMP_tmp2 = ((PrtBool)false);
            PrtBool TMP_tmp3 = ((PrtBool)false);
            PrtBool TMP_tmp4 = ((PrtBool)false);
            PrtBool TMP_tmp5 = ((PrtBool)false);
            PrtBool TMP_tmp6 = ((PrtBool)false);
            IEventWithPayload TMP_tmp7 = null;
            PrtBool TMP_tmp8 = ((PrtBool)false);
            PMachineValue TMP_tmp9 = null;
            PrtBool TMP_tmp10 = ((PrtBool)false);
            IPrtValue TMP_tmp11 = null;
            IPrtValue TMP_tmp12 = null;
            PrtBool TMP_tmp13 = ((PrtBool)false);
            PrtMap<PrtInt, IPrtValue> TMP_tmp14 = new PrtMap<PrtInt, IPrtValue>();
            IPrtValue TMP_tmp15 = null;
            PrtBool TMP_tmp16 = ((PrtBool)false);
            PMachineValue TMP_tmp17 = null;
            PrtBool TMP_tmp18 = ((PrtBool)false);
            PrtInt TMP_tmp19 = ((PrtInt)0);
            PrtInt TMP_tmp20 = ((PrtInt)0);
            PrtInt TMP_tmp21 = ((PrtInt)0);
            PrtBool TMP_tmp22 = ((PrtBool)false);
            PrtInt TMP_tmp23 = ((PrtInt)0);
            PrtBool TMP_tmp24 = ((PrtBool)false);
            PrtBool TMP_tmp25 = ((PrtBool)false);
            PrtInt TMP_tmp26 = ((PrtInt)0);
            PrtInt TMP_tmp27 = ((PrtInt)0);
            tuple_1 TMP_tmp28 = (new tuple_1(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp29 = ((PrtInt)0);
            PrtBool TMP_tmp30 = ((PrtBool)false);
            PrtInt TMP_tmp31 = ((PrtInt)0);
            PrtBool TMP_tmp32 = ((PrtBool)false);
            PrtBool TMP_tmp33 = ((PrtBool)false);
            tuple_1 TMP_tmp34 = (new tuple_1(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp35 = ((PrtInt)0);
            PrtBool TMP_tmp36 = ((PrtBool)false);
            PrtInt TMP_tmp37 = ((PrtInt)0);
            PrtBool TMP_tmp38 = ((PrtBool)false);
            PrtBool TMP_tmp39 = ((PrtBool)false);
            PrtInt TMP_tmp40 = ((PrtInt)0);
            PrtInt TMP_tmp41 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp42 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp43 = ((PrtInt)0);
            PrtBool TMP_tmp44 = ((PrtBool)false);
            PrtInt TMP_tmp45 = ((PrtInt)0);
            PrtBool TMP_tmp46 = ((PrtBool)false);
            PrtBool TMP_tmp47 = ((PrtBool)false);
            PrtTuple<PrtInt, PrtInt> TMP_tmp48 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp49 = ((PrtInt)0);
            PrtBool TMP_tmp50 = ((PrtBool)false);
            PrtInt TMP_tmp51 = ((PrtInt)0);
            PrtBool TMP_tmp52 = ((PrtBool)false);
            PrtBool TMP_tmp53 = ((PrtBool)false);
            PrtInt TMP_tmp54 = ((PrtInt)0);
            IEventWithPayload TMP_tmp55 = null;
            PrtTuple<PrtInt, IEventWithPayload> TMP_tmp56 = (new PrtTuple<PrtInt, IEventWithPayload>(((PrtInt)0), null));
            PrtInt TMP_tmp57 = ((PrtInt)0);
            IEventWithPayload TMP_tmp58 = null;
            PrtTuple<PrtInt, IEventWithPayload> TMP_tmp59 = (new PrtTuple<PrtInt, IEventWithPayload>(((PrtInt)0), null));
            IEventWithPayload TMP_tmp60 = null;
            PrtBool TMP_tmp61 = ((PrtBool)false);
            PrtInt TMP_tmp62 = ((PrtInt)0);
            PrtInt TMP_tmp63 = ((PrtInt)0);
            PrtInt TMP_tmp64 = ((PrtInt)0);
            PrtInt TMP_tmp65 = ((PrtInt)0);
            PrtInt TMP_tmp66 = ((PrtInt)0);
            PrtBool TMP_tmp67 = ((PrtBool)false);
            PrtInt TMP_tmp68 = ((PrtInt)0);
            PrtBool TMP_tmp69 = ((PrtBool)false);
            PrtInt TMP_tmp70 = ((PrtInt)0);
            PrtInt TMP_tmp71 = ((PrtInt)0);
            PrtBool TMP_tmp72 = ((PrtBool)false);
            PrtInt TMP_tmp73 = ((PrtInt)0);
            PrtBool TMP_tmp74 = ((PrtBool)false);
            PrtInt TMP_tmp75 = ((PrtInt)0);
            PrtBool TMP_tmp76 = ((PrtBool)false);
            IEventWithPayload TMP_tmp77 = null;
            IEventWithPayload TMP_tmp78 = null;
            IEventWithPayload TMP_tmp79 = null;
            IEventWithPayload TMP_tmp80 = null;
            PrtInt TMP_tmp81 = ((PrtInt)0);
            PrtBool TMP_tmp82 = ((PrtBool)false);
            IEventWithPayload TMP_tmp83 = null;
            PrtInt TMP_tmp84 = ((PrtInt)0);
            PrtInt TMP_tmp85 = ((PrtInt)0);
            PrtSeq<PrtInt> TMP_tmp86 = new PrtSeq<PrtInt>();
            PMachineValue TMP_tmp87 = null;
            PrtBool TMP_tmp88 = ((PrtBool)false);
            PrtSeq<PrtInt> TMP_tmp89 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp90 = ((PrtInt)0);
            PrtBool TMP_tmp91 = ((PrtBool)false);
            PrtSeq<PrtInt> TMP_tmp92 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp93 = ((PrtInt)0);
            PrtBool TMP_tmp94 = ((PrtBool)false);
            PrtInt TMP_tmp95 = ((PrtInt)0);
            PrtBool TMP_tmp96 = ((PrtBool)false);
            PrtInt TMP_tmp97 = ((PrtInt)0);
            PrtBool TMP_tmp98 = ((PrtBool)false);
            PrtInt TMP_tmp99 = ((PrtInt)0);
            PrtBool TMP_tmp100 = ((PrtBool)false);
            PrtSeq<PrtInt> TMP_tmp101 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp102 = ((PrtInt)0);
            PrtBool TMP_tmp103 = ((PrtBool)false);
            PrtInt TMP_tmp104 = ((PrtInt)0);
            PrtBool TMP_tmp105 = ((PrtBool)false);
            PrtInt TMP_tmp106 = ((PrtInt)0);
            PrtBool TMP_tmp107 = ((PrtBool)false);
            PrtSeq<PrtInt> TMP_tmp108 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp109 = ((PrtInt)0);
            PrtBool TMP_tmp110 = ((PrtBool)false);
            PrtInt TMP_tmp111 = ((PrtInt)0);
            PrtBool TMP_tmp112 = ((PrtBool)false);
            IEventWithPayload TMP_tmp113 = null;
            PrtBool TMP_tmp114 = ((PrtBool)false);
            PrtBool TMP_tmp115 = ((PrtBool)false);
            PrtSeq<PrtBool> TMP_tmp116 = new PrtSeq<PrtBool>();
            PrtSeq<IPrtValue> TMP_tmp117 = new PrtSeq<IPrtValue>();
            PrtSeq<IPrtValue> TMP_tmp118 = new PrtSeq<IPrtValue>();
            PrtBool TMP_tmp119 = ((PrtBool)false);
            PrtSeq<IPrtValue> TMP_tmp120 = new PrtSeq<IPrtValue>();
            IPrtValue TMP_tmp121 = null;
            PrtBool TMP_tmp122 = ((PrtBool)false);
            PrtSeq<IPrtValue> TMP_tmp123 = new PrtSeq<IPrtValue>();
            IPrtValue TMP_tmp124 = null;
            PrtBool TMP_tmp125 = ((PrtBool)false);
            PrtSeq<IPrtValue> TMP_tmp126 = new PrtSeq<IPrtValue>();
            PrtInt TMP_tmp127 = ((PrtInt)0);
            PrtBool TMP_tmp128 = ((PrtBool)false);
            PrtInt TMP_tmp129 = ((PrtInt)0);
            PrtBool TMP_tmp130 = ((PrtBool)false);
            PrtInt TMP_tmp131 = ((PrtInt)0);
            PrtBool TMP_tmp132 = ((PrtBool)false);
            PrtBool TMP_tmp133 = ((PrtBool)false);
            PrtSeq<PrtBool> TMP_tmp134 = new PrtSeq<PrtBool>();
            PrtSeq<IPrtValue> TMP_tmp135 = new PrtSeq<IPrtValue>();
            PrtSeq<IPrtValue> TMP_tmp136 = new PrtSeq<IPrtValue>();
            PrtBool TMP_tmp137 = ((PrtBool)false);
            PrtSeq<IPrtValue> TMP_tmp138 = new PrtSeq<IPrtValue>();
            IPrtValue TMP_tmp139 = null;
            PrtBool TMP_tmp140 = ((PrtBool)false);
            PrtSeq<PrtSeq<IPrtValue>> TMP_tmp141 = new PrtSeq<PrtSeq<IPrtValue>>();
            PrtSeq<PrtSeq<IPrtValue>> TMP_tmp142 = new PrtSeq<PrtSeq<IPrtValue>>();
            PrtBool TMP_tmp143 = ((PrtBool)false);
            PrtSeq<IPrtValue> TMP_tmp144 = new PrtSeq<IPrtValue>();
            PrtSeq<IPrtValue> TMP_tmp145 = new PrtSeq<IPrtValue>();
            PrtSeq<IPrtValue> TMP_tmp146 = new PrtSeq<IPrtValue>();
            PrtSeq<IPrtValue> TMP_tmp147 = new PrtSeq<IPrtValue>();
            PrtBool TMP_tmp148 = ((PrtBool)false);
            PrtSeq<IPrtValue> TMP_tmp149 = new PrtSeq<IPrtValue>();
            PrtBool TMP_tmp150 = ((PrtBool)false);
            PrtMap<PrtInt, IPrtValue> TMP_tmp151 = new PrtMap<PrtInt, IPrtValue>();
            PrtInt TMP_tmp152 = ((PrtInt)0);
            PrtInt TMP_tmp153 = ((PrtInt)0);
            PrtMap<PrtInt, IPrtValue> TMP_tmp154 = new PrtMap<PrtInt, IPrtValue>();
            PrtMap<PrtInt, IPrtValue> TMP_tmp155 = new PrtMap<PrtInt, IPrtValue>();
            IPrtValue TMP_tmp156 = null;
            PrtBool TMP_tmp157 = ((PrtBool)false);
            PrtMap<PrtInt, IPrtValue> TMP_tmp158 = new PrtMap<PrtInt, IPrtValue>();
            IPrtValue TMP_tmp159 = null;
            PrtBool TMP_tmp160 = ((PrtBool)false);
            PrtMap<PrtInt, IPrtValue> TMP_tmp161 = new PrtMap<PrtInt, IPrtValue>();
            IPrtValue TMP_tmp162 = null;
            PrtBool TMP_tmp163 = ((PrtBool)false);
            PrtMap<PrtInt, IPrtValue> TMP_tmp164 = new PrtMap<PrtInt, IPrtValue>();
            IPrtValue TMP_tmp165 = null;
            PrtBool TMP_tmp166 = ((PrtBool)false);
            PrtMap<PrtInt, IPrtValue> TMP_tmp167 = new PrtMap<PrtInt, IPrtValue>();
            IPrtValue TMP_tmp168 = null;
            PrtSeq<PrtInt> TMP_tmp169 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp170 = ((PrtInt)0);
            PrtBool TMP_tmp171 = ((PrtBool)false);
            PrtMap<PrtInt, IPrtValue> TMP_tmp172 = new PrtMap<PrtInt, IPrtValue>();
            IPrtValue TMP_tmp173 = null;
            PrtSeq<PrtInt> TMP_tmp174 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp175 = ((PrtInt)0);
            PrtBool TMP_tmp176 = ((PrtBool)false);
            PrtInt TMP_tmp177 = ((PrtInt)0);
            PrtInt TMP_tmp178 = ((PrtInt)0);
            PrtBool TMP_tmp179 = ((PrtBool)false);
            PrtSeq<PrtInt> TMP_tmp180 = new PrtSeq<PrtInt>();
            PrtMap<PrtInt, PrtSeq<PrtInt>> TMP_tmp181 = new PrtMap<PrtInt, PrtSeq<PrtInt>>();
            tuple TMP_tmp182 = (new tuple(new PrtSeq<PrtInt>(), new PrtMap<PrtInt, PrtSeq<PrtInt>>()));
            PrtMap<PrtInt, PrtSeq<PrtInt>> TMP_tmp183 = new PrtMap<PrtInt, PrtSeq<PrtInt>>();
            PrtSeq<PrtInt> TMP_tmp184 = new PrtSeq<PrtInt>();
            PrtBool TMP_tmp185 = ((PrtBool)false);
            PrtMap<PrtInt, PrtSeq<PrtInt>> TMP_tmp186 = new PrtMap<PrtInt, PrtSeq<PrtInt>>();
            PrtSeq<PrtInt> TMP_tmp187 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp188 = ((PrtInt)0);
            PrtBool TMP_tmp189 = ((PrtBool)false);
            PrtInt TMP_tmp190 = ((PrtInt)0);
            PrtSeq<PrtInt> TMP_tmp191 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp192 = ((PrtInt)0);
            PrtBool TMP_tmp193 = ((PrtBool)false);
            PrtInt TMP_tmp194 = ((PrtInt)0);
            PrtSeq<PrtInt> TMP_tmp195 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp196 = ((PrtInt)0);
            PrtBool TMP_tmp197 = ((PrtBool)false);
            PrtInt TMP_tmp198 = ((PrtInt)0);
            PrtSeq<PrtInt> TMP_tmp199 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp200 = ((PrtInt)0);
            PrtBool TMP_tmp201 = ((PrtBool)false);
            PrtSeq<PrtInt> TMP_tmp202 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp203 = ((PrtInt)0);
            PrtBool TMP_tmp204 = ((PrtBool)false);
            PrtSeq<PrtInt> TMP_tmp205 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp206 = ((PrtInt)0);
            PrtBool TMP_tmp207 = ((PrtBool)false);
            PrtInt TMP_tmp208 = ((PrtInt)0);
            PrtInt TMP_tmp209 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp210 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp211 = ((PrtInt)0);
            PrtInt TMP_tmp212 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp213 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp214 = ((PrtInt)0);
            PrtInt TMP_tmp215 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp216 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp217 = ((PrtInt)0);
            PrtBool TMP_tmp218 = ((PrtBool)false);
            PrtTuple<PrtInt, PrtInt> TMP_tmp219 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp220 = ((PrtInt)0);
            PrtBool TMP_tmp221 = ((PrtBool)false);
            PrtInt TMP_tmp222 = ((PrtInt)0);
            PrtInt TMP_tmp223 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp224 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp225 = ((PrtInt)0);
            PrtInt TMP_tmp226 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp227 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp228 = ((PrtInt)0);
            PrtInt TMP_tmp229 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp230 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp231 = ((PrtInt)0);
            PrtBool TMP_tmp232 = ((PrtBool)false);
            PrtTuple<PrtInt, PrtInt> TMP_tmp233 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtInt TMP_tmp234 = ((PrtInt)0);
            PrtInt TMP_tmp235 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp236 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            PrtBool TMP_tmp237 = ((PrtBool)false);
            PrtSeq<PrtInt> TMP_tmp238 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp239 = ((PrtInt)0);
            PrtInt TMP_tmp240 = ((PrtInt)0);
            PrtBool TMP_tmp241 = ((PrtBool)false);
            PrtInt TMP_tmp242 = ((PrtInt)0);
            PrtSeq<PrtInt> TMP_tmp243 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp244 = ((PrtInt)0);
            PrtInt TMP_tmp245 = ((PrtInt)0);
            PrtBool TMP_tmp246 = ((PrtBool)false);
            PrtInt TMP_tmp247 = ((PrtInt)0);
            tuple TMP_tmp248 = (new tuple(new PrtSeq<PrtInt>(), new PrtMap<PrtInt, PrtSeq<PrtInt>>()));
            PrtSeq<IPrtValue> TMP_tmp249 = new PrtSeq<IPrtValue>();
            PrtInt TMP_tmp250 = ((PrtInt)0);
            IPrtValue TMP_tmp251 = null;
            PrtBool TMP_tmp252 = ((PrtBool)false);
            PrtInt TMP_tmp253 = ((PrtInt)0);
            PrtSeq<IPrtValue> TMP_tmp254 = new PrtSeq<IPrtValue>();
            PrtInt TMP_tmp255 = ((PrtInt)0);
            IPrtValue TMP_tmp256 = null;
            PrtBool TMP_tmp257 = ((PrtBool)false);
            PrtBool TMP_tmp258 = ((PrtBool)false);
            PrtInt TMP_tmp259 = ((PrtInt)0);
            PrtBool TMP_tmp260 = ((PrtBool)false);
            PrtInt TMP_tmp261 = ((PrtInt)0);
            PrtSeq<PrtInt> TMP_tmp262 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp263 = ((PrtInt)0);
            PrtBool TMP_tmp264 = ((PrtBool)false);
            PrtInt TMP_tmp265 = ((PrtInt)0);
            PrtSeq<PrtInt> TMP_tmp266 = new PrtSeq<PrtInt>();
            PrtInt TMP_tmp267 = ((PrtInt)0);
            PrtBool TMP_tmp268 = ((PrtBool)false);
            PrtInt TMP_tmp269 = ((PrtInt)0);
            PrtBool TMP_tmp270 = ((PrtBool)false);
            PrtMap<PrtInt, IPrtValue> TMP_tmp271 = new PrtMap<PrtInt, IPrtValue>();
            PrtBool TMP_tmp272 = ((PrtBool)false);
            PrtMap<PrtInt, IPrtValue> TMP_tmp273 = new PrtMap<PrtInt, IPrtValue>();
            PrtBool TMP_tmp274 = ((PrtBool)false);
            PrtBool TMP_tmp275 = ((PrtBool)false);
            PrtBool TMP_tmp276 = ((PrtBool)false);
            PrtBool TMP_tmp277 = ((PrtBool)false);
            PrtMap<PrtInt, IPrtValue> TMP_tmp278 = new PrtMap<PrtInt, IPrtValue>();
            IPrtValue TMP_tmp279 = null;
            PrtMap<PrtInt, IPrtValue> TMP_tmp280 = new PrtMap<PrtInt, IPrtValue>();
            IPrtValue TMP_tmp281 = null;
            PrtBool TMP_tmp282 = ((PrtBool)false);
            PrtMap<PrtInt, IPrtValue> TMP_tmp283 = new PrtMap<PrtInt, IPrtValue>();
            IPrtValue TMP_tmp284 = null;
            PrtMap<PrtInt, IPrtValue> TMP_tmp285 = new PrtMap<PrtInt, IPrtValue>();
            IPrtValue TMP_tmp286 = null;
            PrtBool TMP_tmp287 = ((PrtBool)false);
            PrtInt TMP_tmp288 = ((PrtInt)0);
            IEventWithPayload TMP_tmp289 = null;
            y = ((PrtInt)2);
            TMP_tmp0 = (PrtValues.SafeEquals(y, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp0, "");
            TMP_tmp1 = ((PrtInt)0);
            y = TMP_tmp1;
            TMP_tmp2 = (PrtValues.SafeEquals(y, ((PrtInt)0)));
            currentMachine.Assert(TMP_tmp2, "");
            b = ((PrtBool)true);
            TMP_tmp3 = (PrtValues.SafeEquals(b, ((PrtBool)true)));
            currentMachine.Assert(TMP_tmp3, "");
            TMP_tmp4 = ((PrtBool)false);
            b = TMP_tmp4;
            TMP_tmp5 = (PrtValues.SafeEquals(b, ((PrtBool)false)));
            currentMachine.Assert(TMP_tmp5, "");
            e = new E(null);
            TMP_tmp6 = (PrtValues.SafeEquals(e, new E(null)));
            currentMachine.Assert(TMP_tmp6, "");
            TMP_tmp7 = null;
            e = TMP_tmp7;
            TMP_tmp8 = (PrtValues.SafeEquals(e, null));
            currentMachine.Assert(TMP_tmp8, "");
            mac = new PMachineValue(currentMachine.self);
            TMP_tmp9 = null;
            mac = TMP_tmp9;
            TMP_tmp10 = (PrtValues.SafeEquals(mac, null));
            currentMachine.Assert(TMP_tmp10, "");
            a = ((PrtBool)true);
            TMP_tmp11 = null;
            a = TMP_tmp11;
            (m5)[((PrtInt)1)] = ((PrtBool)true);
            TMP_tmp12 = (m5)[((PrtInt)1)];
            TMP_tmp13 = (PrtValues.SafeEquals(TMP_tmp12, ((PrtBool)true)));
            currentMachine.Assert(TMP_tmp13, "");
            TMP_tmp14 = new PrtMap<PrtInt, IPrtValue>();
            m5 = TMP_tmp14;
            (m5)[((PrtInt)1)] = new E(null);
            TMP_tmp15 = (m5)[((PrtInt)1)];
            TMP_tmp16 = (PrtValues.SafeEquals(TMP_tmp15, new E(null)));
            currentMachine.Assert(TMP_tmp16, "");
            TMP_tmp17 = ((PMachineValue)null);
            mac = TMP_tmp17;
            TMP_tmp18 = (PrtValues.SafeEquals(mac, null));
            currentMachine.Assert(TMP_tmp18, "");
            TMP_tmp19 = (ts).b;
            TMP_tmp20 = (TMP_tmp19) + (((PrtInt)1));
            (ts).a = TMP_tmp20;
            TMP_tmp21 = (ts).a;
            TMP_tmp22 = (PrtValues.SafeEquals(TMP_tmp21, ((PrtInt)1)));
            TMP_tmp23 = (ts).b;
            TMP_tmp24 = (PrtValues.SafeEquals(TMP_tmp23, ((PrtInt)0)));
            TMP_tmp25 = (TMP_tmp22) && (TMP_tmp24);
            currentMachine.Assert(TMP_tmp25, "");
            TMP_tmp26 = ((PrtInt)1);
            TMP_tmp27 = ((PrtInt)2);
            TMP_tmp28 = (new tuple_1(TMP_tmp26, TMP_tmp27));
            ts = TMP_tmp28;
            TMP_tmp29 = (ts).a;
            TMP_tmp30 = (PrtValues.SafeEquals(TMP_tmp29, ((PrtInt)1)));
            TMP_tmp31 = (ts).b;
            TMP_tmp32 = (PrtValues.SafeEquals(TMP_tmp31, ((PrtInt)2)));
            TMP_tmp33 = (TMP_tmp30) && (TMP_tmp32);
            currentMachine.Assert(TMP_tmp33, "");
            TMP_tmp34 = (new tuple_1(((PrtInt)0), ((PrtInt)0)));
            ts = TMP_tmp34;
            TMP_tmp35 = (ts).a;
            TMP_tmp36 = (PrtValues.SafeEquals(TMP_tmp35, ((PrtInt)0)));
            TMP_tmp37 = (ts).b;
            TMP_tmp38 = (PrtValues.SafeEquals(TMP_tmp37, ((PrtInt)0)));
            TMP_tmp39 = (TMP_tmp36) && (TMP_tmp38);
            currentMachine.Assert(TMP_tmp39, "");
            TMP_tmp40 = ((PrtInt)1);
            TMP_tmp41 = ((PrtInt)2);
            TMP_tmp42 = new PrtTuple<PrtInt, PrtInt>((PrtInt)TMP_tmp40, (PrtInt)TMP_tmp41);
            tt = TMP_tmp42;
            TMP_tmp43 = tt.Item1;
            TMP_tmp44 = (PrtValues.SafeEquals(TMP_tmp43, ((PrtInt)1)));
            TMP_tmp45 = tt.Item2;
            TMP_tmp46 = (PrtValues.SafeEquals(TMP_tmp45, ((PrtInt)2)));
            TMP_tmp47 = (TMP_tmp44) && (TMP_tmp46);
            currentMachine.Assert(TMP_tmp47, "");
            TMP_tmp48 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            tt = TMP_tmp48;
            TMP_tmp49 = tt.Item1;
            TMP_tmp50 = (PrtValues.SafeEquals(TMP_tmp49, ((PrtInt)0)));
            TMP_tmp51 = tt.Item2;
            TMP_tmp52 = (PrtValues.SafeEquals(TMP_tmp51, ((PrtInt)0)));
            TMP_tmp53 = (TMP_tmp50) && (TMP_tmp52);
            currentMachine.Assert(TMP_tmp53, "");
            TMP_tmp54 = ((PrtInt)2);
            TMP_tmp55 = new E2(null);
            TMP_tmp56 = new PrtTuple<PrtInt, IEventWithPayload>((PrtInt)TMP_tmp54, (IEventWithPayload)TMP_tmp55);
            te = TMP_tmp56;
            TMP_tmp57 = ((PrtInt)3);
            TMP_tmp58 = ((IEventWithPayload)null);
            TMP_tmp59 = new PrtTuple<PrtInt, IEventWithPayload>((PrtInt)TMP_tmp57, (IEventWithPayload)TMP_tmp58);
            te = TMP_tmp59;
            TMP_tmp60 = te.Item2;
            TMP_tmp61 = (PrtValues.SafeEquals(TMP_tmp60, null));
            currentMachine.Assert(TMP_tmp61, "");
            TMP_tmp62 = ((PrtInt)1);
            s.Insert(((PrtInt)0), TMP_tmp62);
            TMP_tmp63 = ((PrtInt)2);
            s.Insert(((PrtInt)1), TMP_tmp63);
            s1 = new PrtSeq<IPrtValue>(((PrtSeq<PrtInt>)((IPrtValue)s)?.Clone()).Cast<IPrtValue>());
            s.RemoveAt(((PrtInt)1));
            TMP_tmp64 = ((PrtInt)5);
            s.Insert(((PrtInt)0), TMP_tmp64);
            TMP_tmp65 = ((PrtInt)6);
            s.Insert(((PrtInt)0), TMP_tmp65);
            TMP_tmp66 = (s)[((PrtInt)1)];
            TMP_tmp67 = (PrtValues.SafeEquals(TMP_tmp66, ((PrtInt)5)));
            currentMachine.Assert(TMP_tmp67, "");
            s.RemoveAt(((PrtInt)1));
            TMP_tmp68 = ((PrtInt)(s).Count);
            TMP_tmp69 = (PrtValues.SafeEquals(TMP_tmp68, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp69, "");
            TMP_tmp70 = ((PrtInt)1);
            s.Insert(((PrtInt)0), TMP_tmp70);
            TMP_tmp71 = (s)[((PrtInt)0)];
            TMP_tmp72 = (PrtValues.SafeEquals(TMP_tmp71, ((PrtInt)1)));
            currentMachine.Assert(TMP_tmp72, "");
            (s)[((PrtInt)0)] = ((PrtInt)2);
            TMP_tmp73 = (s)[((PrtInt)0)];
            TMP_tmp74 = (PrtValues.SafeEquals(TMP_tmp73, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp74, "");
            i = ((PrtInt)0);
            TMP_tmp75 = (s)[i];
            TMP_tmp76 = (PrtValues.SafeEquals(TMP_tmp75, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp76, "");
            TMP_tmp77 = new E(null);
            s9.Insert(((PrtInt)0), TMP_tmp77);
            TMP_tmp78 = new E1(null);
            s9.Insert(((PrtInt)1), TMP_tmp78);
            TMP_tmp79 = new E2(null);
            s9.Insert(((PrtInt)2), TMP_tmp79);
            TMP_tmp80 = ((IEventWithPayload)null);
            s9.Insert(((PrtInt)3), TMP_tmp80);
            TMP_tmp81 = ((PrtInt)(s9).Count);
            TMP_tmp82 = (PrtValues.SafeEquals(TMP_tmp81, ((PrtInt)4)));
            currentMachine.Assert(TMP_tmp82, "");
            TMP_tmp83 = new E(null);
            s10.Insert(((PrtInt)0), TMP_tmp83);
            TMP_tmp84 = ((PrtInt)1);
            s2.Insert(((PrtInt)0), TMP_tmp84);
            TMP_tmp85 = ((PrtInt)3);
            s2.Insert(((PrtInt)0), TMP_tmp85);
            TMP_tmp86 = ((PrtSeq<PrtInt>)((IPrtValue)s2)?.Clone());
            TMP_tmp87 = currentMachine.CreateInterface<I_XYZ>(currentMachine, TMP_tmp86);
            mac = (PMachineValue)TMP_tmp87;
            (m1)[((PrtInt)0)] = ((PrtInt)1);
            TMP_tmp88 = ((PrtBool)(m1).ContainsKey(((PrtInt)0)));
            currentMachine.Assert(TMP_tmp88, "");
            TMP_tmp89 = (m1).CloneKeys();
            TMP_tmp90 = (TMP_tmp89)[((PrtInt)0)];
            i = TMP_tmp90;
            TMP_tmp91 = (PrtValues.SafeEquals(i, ((PrtInt)0)));
            currentMachine.Assert(TMP_tmp91, "");
            TMP_tmp92 = (m1).CloneValues();
            TMP_tmp93 = (TMP_tmp92)[((PrtInt)0)];
            TMP_tmp94 = (PrtValues.SafeEquals(TMP_tmp93, ((PrtInt)1)));
            currentMachine.Assert(TMP_tmp94, "");
            TMP_tmp95 = (m1)[((PrtInt)0)];
            TMP_tmp96 = (PrtValues.SafeEquals(TMP_tmp95, ((PrtInt)1)));
            currentMachine.Assert(TMP_tmp96, "");
            (m1)[((PrtInt)0)] = ((PrtInt)2);
            TMP_tmp97 = (m1)[((PrtInt)0)];
            TMP_tmp98 = (PrtValues.SafeEquals(TMP_tmp97, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp98, "");
            m1.Remove(((PrtInt)0));
            TMP_tmp99 = ((PrtInt)(m1).Count);
            TMP_tmp100 = (PrtValues.SafeEquals(TMP_tmp99, ((PrtInt)0)));
            currentMachine.Assert(TMP_tmp100, "");
            TMP_tmp101 = (m1).CloneValues();
            TMP_tmp102 = ((PrtInt)(TMP_tmp101).Count);
            TMP_tmp103 = (PrtValues.SafeEquals(TMP_tmp102, ((PrtInt)0)));
            currentMachine.Assert(TMP_tmp103, "");
            (m1)[((PrtInt)0)] = ((PrtInt)2);
            i = ((PrtInt)0);
            TMP_tmp104 = (m1)[i];
            TMP_tmp105 = (PrtValues.SafeEquals(TMP_tmp104, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp105, "");
            (m1)[((PrtInt)1)] = ((PrtInt)3);
            TMP_tmp106 = ((PrtInt)(m1).Count);
            TMP_tmp107 = (PrtValues.SafeEquals(TMP_tmp106, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp107, "");
            TMP_tmp108 = (m1).CloneValues();
            TMP_tmp109 = ((PrtInt)(TMP_tmp108).Count);
            TMP_tmp110 = (PrtValues.SafeEquals(TMP_tmp109, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp110, "");
            (m3)[((PrtInt)0)] = ((PrtBool)true);
            (m3)[((PrtInt)2)] = ((PrtBool)false);
            TMP_tmp111 = ((PrtInt)(m3).Count);
            TMP_tmp112 = (PrtValues.SafeEquals(TMP_tmp111, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp112, "");
            (m8)[((PrtInt)0)] = new E(null);
            (m8)[((PrtInt)1)] = new E1(null);
            TMP_tmp113 = ((IEventWithPayload)null);
            (m8)[((PrtInt)2)] = TMP_tmp113;
            (m9)[((PrtInt)0)] = new E(null);
            (m9)[((PrtInt)1)] = null;
            TMP_tmp114 = ((PrtBool)true);
            s5.Insert(((PrtInt)0), TMP_tmp114);
            TMP_tmp115 = ((PrtBool)false);
            s5.Insert(((PrtInt)1), TMP_tmp115);
            TMP_tmp116 = ((PrtSeq<PrtBool>)((IPrtValue)s5)?.Clone());
            s3.Insert(((PrtInt)0), (PrtSeq<IPrtValue>)TMP_tmp116);
            TMP_tmp117 = ((PrtSeq<IPrtValue>)((IPrtValue)s1)?.Clone());
            s3.Insert(((PrtInt)1), TMP_tmp117);
            TMP_tmp118 = (s3)[((PrtInt)0)];
            TMP_tmp119 = (PrtValues.SafeEquals(TMP_tmp118, s5));
            currentMachine.Assert(TMP_tmp119, "");
            TMP_tmp120 = (s3)[((PrtInt)0)];
            TMP_tmp121 = (TMP_tmp120)[((PrtInt)1)];
            TMP_tmp122 = (PrtValues.SafeEquals(TMP_tmp121, ((PrtBool)false)));
            currentMachine.Assert(TMP_tmp122, "");
            TMP_tmp123 = (s3)[((PrtInt)1)];
            TMP_tmp124 = (TMP_tmp123)[((PrtInt)0)];
            TMP_tmp125 = (PrtValues.SafeEquals(TMP_tmp124, ((PrtInt)1)));
            currentMachine.Assert(TMP_tmp125, "");
            TMP_tmp126 = (s3)[((PrtInt)0)];
            TMP_tmp127 = ((PrtInt)(TMP_tmp126).Count);
            TMP_tmp128 = (PrtValues.SafeEquals(TMP_tmp127, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp128, "");
            s3.RemoveAt(((PrtInt)1));
            s3.RemoveAt(((PrtInt)0));
            TMP_tmp129 = ((PrtInt)(s3).Count);
            TMP_tmp130 = (PrtValues.SafeEquals(TMP_tmp129, ((PrtInt)0)));
            currentMachine.Assert(TMP_tmp130, "");
            TMP_tmp131 = ((PrtInt)(s5).Count);
            TMP_tmp132 = (PrtValues.SafeEquals(TMP_tmp131, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp132, "");
            TMP_tmp133 = ((PrtBool)true);
            s1.Insert(((PrtInt)0), TMP_tmp133);
            TMP_tmp134 = ((PrtSeq<PrtBool>)((IPrtValue)s5)?.Clone());
            s3.Insert(((PrtInt)0), TMP_tmp134);
            TMP_tmp135 = ((PrtSeq<IPrtValue>)((IPrtValue)s1)?.Clone());
            s3.Insert(((PrtInt)1), TMP_tmp135);
            TMP_tmp136 = (s3)[((PrtInt)0)];
            TMP_tmp137 = (PrtValues.SafeEquals(TMP_tmp136, s5));
            currentMachine.Assert(TMP_tmp137, "");
            TMP_tmp138 = (s3)[((PrtInt)1)];
            TMP_tmp139 = (TMP_tmp138)[((PrtInt)0)];
            TMP_tmp140 = (PrtValues.SafeEquals(TMP_tmp139, ((PrtBool)true)));
            currentMachine.Assert(TMP_tmp140, "");
            TMP_tmp141 = new PrtSeq<PrtSeq<IPrtValue>>();
            s3 = TMP_tmp141;
            TMP_tmp142 = new PrtSeq<PrtSeq<IPrtValue>>();
            TMP_tmp143 = (PrtValues.SafeEquals(s3, TMP_tmp142));
            currentMachine.Assert(TMP_tmp143, "");
            TMP_tmp144 = new PrtSeq<IPrtValue>();
            s1 = TMP_tmp144;
            TMP_tmp145 = ((PrtSeq<IPrtValue>)((IPrtValue)s1)?.Clone());
            s3.Insert(((PrtInt)0), TMP_tmp145);
            TMP_tmp146 = (s3)[((PrtInt)0)];
            TMP_tmp147 = new PrtSeq<IPrtValue>();
            TMP_tmp148 = (PrtValues.SafeEquals(TMP_tmp146, TMP_tmp147));
            currentMachine.Assert(TMP_tmp148, "");
            TMP_tmp149 = (s3)[((PrtInt)0)];
            TMP_tmp150 = (PrtValues.SafeEquals(TMP_tmp149, s1));
            currentMachine.Assert(TMP_tmp150, "");
            (m9)[((PrtInt)0)] = new E(null);
            (m9)[((PrtInt)1)] = null;
            TMP_tmp151 = ((PrtMap<PrtInt, IPrtValue>)((IPrtValue)m9)?.Clone());
            s6.Insert(((PrtInt)0), TMP_tmp151);
            TMP_tmp152 = ((PrtInt)1);
            s12.Insert(((PrtInt)0), TMP_tmp152);
            TMP_tmp153 = ((PrtInt)2);
            s12.Insert(((PrtInt)1), TMP_tmp153);
            (m10)[((PrtInt)1)] = ((PrtInt)100);
            (m10)[((PrtInt)5)] = ((PrtBool)true);
            (m10)[((PrtInt)10)] = ((PrtSeq<PrtInt>)((IPrtValue)s12)?.Clone());
            TMP_tmp154 = ((PrtMap<PrtInt, IPrtValue>)((IPrtValue)m10)?.Clone());
            s6.Insert(((PrtInt)1), TMP_tmp154);
            TMP_tmp155 = (s6)[((PrtInt)0)];
            TMP_tmp156 = (TMP_tmp155)[((PrtInt)0)];
            TMP_tmp157 = (PrtValues.SafeEquals(TMP_tmp156, new E(null)));
            currentMachine.Assert(TMP_tmp157, "");
            TMP_tmp158 = (s6)[((PrtInt)0)];
            TMP_tmp159 = (TMP_tmp158)[((PrtInt)1)];
            TMP_tmp160 = (PrtValues.SafeEquals(TMP_tmp159, null));
            currentMachine.Assert(TMP_tmp160, "");
            TMP_tmp161 = (s6)[((PrtInt)1)];
            TMP_tmp162 = (TMP_tmp161)[((PrtInt)5)];
            TMP_tmp163 = (PrtValues.SafeEquals(TMP_tmp162, ((PrtBool)true)));
            currentMachine.Assert(TMP_tmp163, "");
            TMP_tmp164 = (s6)[((PrtInt)1)];
            TMP_tmp165 = (TMP_tmp164)[((PrtInt)10)];
            TMP_tmp166 = (PrtValues.SafeEquals(TMP_tmp165, s12));
            currentMachine.Assert(TMP_tmp166, "");
            TMP_tmp167 = (s6)[((PrtInt)1)];
            TMP_tmp168 = (TMP_tmp167)[((PrtInt)10)];
            TMP_tmp169 = ((PrtSeq<PrtInt>)TMP_tmp168);
            tmp4 = TMP_tmp169;
            TMP_tmp170 = (tmp4)[((PrtInt)0)];
            TMP_tmp171 = (PrtValues.SafeEquals(TMP_tmp170, ((PrtInt)1)));
            currentMachine.Assert(TMP_tmp171, "");
            TMP_tmp172 = (s6)[((PrtInt)1)];
            TMP_tmp173 = (TMP_tmp172)[((PrtInt)10)];
            TMP_tmp174 = ((PrtSeq<PrtInt>)TMP_tmp173);
            TMP_tmp175 = (TMP_tmp174)[((PrtInt)0)];
            TMP_tmp176 = (PrtValues.SafeEquals(TMP_tmp175, ((PrtInt)1)));
            currentMachine.Assert(TMP_tmp176, "");
            TMP_tmp177 = ((PrtInt)1);
            s.Insert(((PrtInt)0), TMP_tmp177);
            TMP_tmp178 = ((PrtInt)(s).Count);
            TMP_tmp179 = (PrtValues.SafeEquals(TMP_tmp178, ((PrtInt)4)));
            currentMachine.Assert(TMP_tmp179, "");
            (tmp3)[((PrtInt)0)] = ((PrtSeq<PrtInt>)((IPrtValue)s)?.Clone());
            TMP_tmp180 = ((PrtSeq<PrtInt>)((IPrtValue)s)?.Clone());
            TMP_tmp181 = ((PrtMap<PrtInt, PrtSeq<PrtInt>>)((IPrtValue)tmp3)?.Clone());
            TMP_tmp182 = (new tuple(TMP_tmp180, TMP_tmp181));
            t = TMP_tmp182;
            TMP_tmp183 = (t).b;
            TMP_tmp184 = (TMP_tmp183)[((PrtInt)0)];
            TMP_tmp185 = (PrtValues.SafeEquals(TMP_tmp184, s));
            currentMachine.Assert(TMP_tmp185, "");
            TMP_tmp186 = (t).b;
            TMP_tmp187 = (TMP_tmp186)[((PrtInt)0)];
            TMP_tmp188 = (TMP_tmp187)[((PrtInt)0)];
            TMP_tmp189 = (PrtValues.SafeEquals(TMP_tmp188, ((PrtInt)1)));
            currentMachine.Assert(TMP_tmp189, "");
            TMP_tmp190 = ((PrtInt)2);
            (t).a.Insert(((PrtInt)0), TMP_tmp190);
            TMP_tmp191 = (t).a;
            TMP_tmp192 = (TMP_tmp191)[((PrtInt)0)];
            TMP_tmp193 = (PrtValues.SafeEquals(TMP_tmp192, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp193, "");
            TMP_tmp194 = ((PrtInt)2);
            (t).a.Insert(((PrtInt)1), TMP_tmp194);
            TMP_tmp195 = (t).a;
            TMP_tmp196 = (TMP_tmp195)[((PrtInt)1)];
            TMP_tmp197 = (PrtValues.SafeEquals(TMP_tmp196, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp197, "");
            TMP_tmp198 = ((PrtInt)3);
            (t).a.Insert(((PrtInt)0), TMP_tmp198);
            TMP_tmp199 = (t).a;
            TMP_tmp200 = (TMP_tmp199)[((PrtInt)0)];
            TMP_tmp201 = (PrtValues.SafeEquals(TMP_tmp200, ((PrtInt)3)));
            currentMachine.Assert(TMP_tmp201, "");
            TMP_tmp202 = (t).a;
            TMP_tmp203 = (TMP_tmp202)[((PrtInt)1)];
            TMP_tmp204 = (PrtValues.SafeEquals(TMP_tmp203, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp204, "");
            TMP_tmp205 = (t).a;
            TMP_tmp206 = ((PrtInt)(TMP_tmp205).Count);
            TMP_tmp207 = (PrtValues.SafeEquals(TMP_tmp206, ((PrtInt)7)));
            currentMachine.Assert(TMP_tmp207, "");
            TMP_tmp208 = ((PrtInt)0);
            TMP_tmp209 = ((PrtInt)0);
            TMP_tmp210 = new PrtTuple<PrtInt, PrtInt>((PrtInt)TMP_tmp208, (PrtInt)TMP_tmp209);
            s4.Insert(((PrtInt)0), TMP_tmp210);
            TMP_tmp211 = ((PrtInt)1);
            TMP_tmp212 = ((PrtInt)1);
            TMP_tmp213 = new PrtTuple<PrtInt, PrtInt>((PrtInt)TMP_tmp211, (PrtInt)TMP_tmp212);
            s4.Insert(((PrtInt)1), TMP_tmp213);
            TMP_tmp214 = ((PrtInt)2);
            TMP_tmp215 = ((PrtInt)2);
            TMP_tmp216 = new PrtTuple<PrtInt, PrtInt>((PrtInt)TMP_tmp214, (PrtInt)TMP_tmp215);
            s4.Insert(((PrtInt)2), TMP_tmp216);
            TMP_tmp217 = ((PrtInt)(s4).Count);
            TMP_tmp218 = (PrtValues.SafeEquals(TMP_tmp217, ((PrtInt)3)));
            currentMachine.Assert(TMP_tmp218, "");
            TMP_tmp219 = (s4)[((PrtInt)2)];
            TMP_tmp220 = TMP_tmp219.Item1;
            TMP_tmp221 = (PrtValues.SafeEquals(TMP_tmp220, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp221, "");
            TMP_tmp222 = ((PrtInt)1);
            TMP_tmp223 = ((PrtInt)1);
            TMP_tmp224 = new PrtTuple<PrtInt, PrtInt>((PrtInt)TMP_tmp222, (PrtInt)TMP_tmp223);
            s8.Insert(((PrtInt)0), TMP_tmp224);
            TMP_tmp225 = ((PrtInt)2);
            TMP_tmp226 = ((PrtInt)2);
            TMP_tmp227 = new PrtTuple<PrtInt, PrtInt>((PrtInt)TMP_tmp225, (PrtInt)TMP_tmp226);
            s8.Insert(((PrtInt)1), TMP_tmp227);
            TMP_tmp228 = ((PrtInt)3);
            TMP_tmp229 = ((PrtInt)3);
            TMP_tmp230 = new PrtTuple<PrtInt, PrtInt>((PrtInt)TMP_tmp228, (PrtInt)TMP_tmp229);
            s8.Insert(((PrtInt)2), TMP_tmp230);
            TMP_tmp231 = ((PrtInt)(s8).Count);
            TMP_tmp232 = (PrtValues.SafeEquals(TMP_tmp231, ((PrtInt)3)));
            currentMachine.Assert(TMP_tmp232, "");
            TMP_tmp233 = (s8)[((PrtInt)2)];
            TMP_tmp234 = ((PrtInt)3);
            TMP_tmp235 = ((PrtInt)3);
            TMP_tmp236 = new PrtTuple<PrtInt, PrtInt>((PrtInt)TMP_tmp234, (PrtInt)TMP_tmp235);
            TMP_tmp237 = (PrtValues.SafeEquals(TMP_tmp233, TMP_tmp236));
            currentMachine.Assert(TMP_tmp237, "");
            TMP_tmp238 = (t).a;
            TMP_tmp239 = foo();
            TMP_tmp240 = (TMP_tmp238)[TMP_tmp239];
            TMP_tmp241 = (PrtValues.SafeEquals(TMP_tmp240, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp241, "");
            TMP_tmp242 = foo();
            ((t).a)[TMP_tmp242] = ((PrtInt)300);
            TMP_tmp243 = (t).a;
            TMP_tmp244 = foo();
            TMP_tmp245 = (TMP_tmp243)[TMP_tmp244];
            TMP_tmp246 = (PrtValues.SafeEquals(TMP_tmp245, ((PrtInt)300)));
            currentMachine.Assert(TMP_tmp246, "");
            TMP_tmp247 = foo();
            tmp = TMP_tmp247;
            TMP_tmp248 = GetT();
            tmp2 = (tuple_2)TMP_tmp248;
            TMP_tmp249 = (tmp2).a;
            TMP_tmp250 = foo();
            TMP_tmp251 = (TMP_tmp249)[TMP_tmp250];
            TMP_tmp252 = (PrtValues.SafeEquals(TMP_tmp251, ((PrtInt)300)));
            currentMachine.Assert(TMP_tmp252, "");
            TMP_tmp253 = foo();
            ((tmp2).a)[TMP_tmp253] = ((PrtInt)100);
            TMP_tmp254 = (tmp2).a;
            TMP_tmp255 = foo();
            TMP_tmp256 = (TMP_tmp254)[TMP_tmp255];
            TMP_tmp257 = (PrtValues.SafeEquals(TMP_tmp256, ((PrtInt)100)));
            currentMachine.Assert(TMP_tmp257, "");
            TMP_tmp258 = (!PrtValues.SafeEquals(tmp2, t));
            currentMachine.Assert(TMP_tmp258, "");
            TMP_tmp259 = IncY();
            tmp1 = TMP_tmp259;
            TMP_tmp260 = (PrtValues.SafeEquals(tmp1, y));
            currentMachine.Assert(TMP_tmp260, "");
            TMP_tmp261 = foo();
            ((t).a)[TMP_tmp261] = ((PrtInt)((IPrtValue)tmp1)?.Clone());
            TMP_tmp262 = (t).a;
            TMP_tmp263 = (TMP_tmp262)[((PrtInt)1)];
            TMP_tmp264 = (PrtValues.SafeEquals(TMP_tmp263, ((PrtInt)1)));
            currentMachine.Assert(TMP_tmp264, "");
            TMP_tmp265 = (tmp1) + (((PrtInt)1));
            ((t).a)[tmp] = TMP_tmp265;
            TMP_tmp266 = (t).a;
            TMP_tmp267 = (TMP_tmp266)[((PrtInt)1)];
            TMP_tmp268 = (PrtValues.SafeEquals(TMP_tmp267, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp268, "");
            TMP_tmp269 = IncY();
            y = TMP_tmp269;
            TMP_tmp270 = (PrtValues.SafeEquals(y, ((PrtInt)2)));
            currentMachine.Assert(TMP_tmp270, "");
            (m10)[((PrtInt)1)] = ((PrtInt)100);
            (m10)[((PrtInt)2)] = ((PrtInt)200);
            (m10)[((PrtInt)3)] = ((PrtInt)300);
            (m10)[((PrtInt)4)] = ((PrtInt)400);
            (m10)[((PrtInt)5)] = ((PrtInt)500);
            (m2)[((PrtInt)0)] = ((PrtMap<PrtInt, IPrtValue>)((IPrtValue)m10)?.Clone());
            TMP_tmp271 = (m2)[((PrtInt)0)];
            TMP_tmp272 = (PrtValues.SafeEquals(TMP_tmp271, m10));
            currentMachine.Assert(TMP_tmp272, "");
            (m10)[((PrtInt)3)] = ((PrtInt)333);
            (m2)[((PrtInt)1)] = ((PrtMap<PrtInt, IPrtValue>)((IPrtValue)m10)?.Clone());
            TMP_tmp273 = (m2)[((PrtInt)1)];
            TMP_tmp274 = (PrtValues.SafeEquals(TMP_tmp273, m10));
            currentMachine.Assert(TMP_tmp274, "");
            i = ((PrtInt)1);
            TMP_tmp275 = (i) < (((PrtInt)5));
            TMP_tmp276 = ((PrtBool)((IPrtValue)TMP_tmp275)?.Clone());
            while (TMP_tmp276)
            {
                TMP_tmp277 = (!PrtValues.SafeEquals(i, ((PrtInt)3)));
                if (TMP_tmp277)
                {
                    TMP_tmp278 = (m2)[((PrtInt)0)];
                    TMP_tmp279 = (TMP_tmp278)[i];
                    TMP_tmp280 = (m2)[((PrtInt)1)];
                    TMP_tmp281 = (TMP_tmp280)[i];
                    TMP_tmp282 = (PrtValues.SafeEquals(TMP_tmp279, TMP_tmp281));
                    currentMachine.Assert(TMP_tmp282, "");
                }
                else
                {
                    TMP_tmp283 = (m2)[((PrtInt)0)];
                    TMP_tmp284 = (TMP_tmp283)[i];
                    TMP_tmp285 = (m2)[((PrtInt)1)];
                    TMP_tmp286 = (TMP_tmp285)[i];
                    TMP_tmp287 = (!PrtValues.SafeEquals(TMP_tmp284, TMP_tmp286));
                    currentMachine.Assert(TMP_tmp287, "");
                }
                TMP_tmp288 = (i) + (((PrtInt)1));
                i = TMP_tmp288;
                TMP_tmp275 = (i) < (((PrtInt)5));
                TMP_tmp276 = ((PrtBool)((IPrtValue)TMP_tmp275)?.Clone());
            }
            TMP_tmp289 = new PHalt(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp289);
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
    internal partial class XYZ : PMachine
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
            PrtSeq<PrtInt> payload = this.gotoPayload == null ? ((PEvent<PrtSeq<PrtInt>>)currentMachine.ReceivedEvent).PayloadT : (PrtSeq<PrtInt>)this.gotoPayload;
            this.gotoPayload = null;
            PrtInt TMP_tmp0_2 = ((PrtInt)0);
            PrtBool TMP_tmp1_1 = ((PrtBool)false);
            ss = ((PrtSeq<PrtInt>)((IPrtValue)payload)?.Clone());
            TMP_tmp0_2 = (ss)[((PrtInt)0)];
            TMP_tmp1_1 = (PrtValues.SafeEquals(TMP_tmp0_2, ((PrtInt)3)));
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
