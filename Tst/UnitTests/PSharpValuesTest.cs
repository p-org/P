using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PrtSharp;
using PrtSharp.Values;

namespace UnitTests
{
    [TestFixture]
    public class PSharpValuesTest
    {
        [Test]
        public void TestPrtBoolOverloading()
        {
            PrtBool boolT = true;
            PrtBool boolF = false;
            Assert.AreEqual(boolT, boolF || boolT);
            Assert.AreEqual(boolT, boolT || boolF);
            Assert.AreEqual(boolT, !boolF);
            Assert.AreEqual(boolF, boolT && boolF);
            Assert.AreEqual(boolT, boolT && boolT);
        }

        [Test]
        public void TestPrtFloatComparisions()
        {
            PrtBool boolT = true;
            PrtBool boolF = false;
            Assert.AreEqual(boolT, PrtValues.Box(1.0) < PrtValues.Box(2.0));
            Assert.AreEqual(boolF, PrtValues.Box(1.0) < PrtValues.Box(1.0));
            Assert.AreEqual(boolT, PrtValues.Box(1.0) <= PrtValues.Box(1.0));
            Assert.AreEqual(boolT, PrtValues.Box(1.0) <= PrtValues.Box(2.0));
            Assert.AreEqual(boolT, PrtValues.Box(1.0) == PrtValues.Box(1.0));
            Assert.AreEqual(boolT, PrtValues.Box(1.0) != PrtValues.Box(2.0));
            Assert.AreEqual(boolT, PrtValues.Box(1.0) >= PrtValues.Box(1.0));
            Assert.AreEqual(boolT, PrtValues.Box(1.0) >= PrtValues.Box(0.0));
            Assert.AreEqual(boolT, PrtValues.Box(1.0) > PrtValues.Box(0.0));
        }

        [Test]
        public void TestPrtFloatOverloading()
        {
            PrtFloat float1 = 1.0;
            PrtFloat float2 = 2.0;
            PrtFloat float3 = 3.0;
            Assert.AreEqual(float3, float1 + float2);
            Assert.AreEqual(float1, float3 - float2);
            Assert.AreEqual(float2, float1 * float2);
            Assert.AreEqual(PrtValues.Box(1.5), float3 / float2);
            Assert.AreEqual(PrtValues.Box(6.0), float1 + float2 + float3);
            Assert.AreEqual(PrtValues.Box(6.0), float1 * float2 * float3);
            Assert.AreEqual(float1, float3 / (float1 + float2));
            Assert.AreEqual(-float1, float2 - float3);
            Assert.AreEqual(+float1, float1);
            Assert.AreEqual(float1, +float1);
        }

        [Test]
        public void TestPrtHashCodesAreValueDependent()
        {
            var list = new PrtSeq<PrtInt> {1};
            int hashCode1 = list.GetHashCode();
            list.Add(2);
            IPrtValue clonedList = list.Clone();
            int hashCode2 = list.GetHashCode();
            int hashCodeCloned2 = clonedList.GetHashCode();
            list.RemoveAt(1);
            int hashCode3 = list.GetHashCode();
            Assert.AreEqual(hashCode1, hashCode3);
            Assert.AreEqual(hashCode2, hashCodeCloned2);
            Assert.AreNotEqual(hashCode1, hashCode2);
            Assert.AreNotEqual(hashCode1, hashCodeCloned2);
        }

        [Test]
        public void TestPrtIntComparisions()
        {
            PrtBool boolT = true;
            PrtBool boolF = false;
            Assert.AreEqual(boolT, PrtValues.Box(1) < PrtValues.Box(2));
            Assert.AreEqual(boolF, PrtValues.Box(1) < PrtValues.Box(1));
            Assert.AreEqual(boolT, PrtValues.Box(1) <= PrtValues.Box(1));
            Assert.AreEqual(boolT, PrtValues.Box(1) <= PrtValues.Box(2));
            Assert.AreEqual(boolT, PrtValues.Box(1) == PrtValues.Box(1));
            Assert.AreEqual(boolT, PrtValues.Box(1) != PrtValues.Box(2));
            Assert.AreEqual(boolT, PrtValues.Box(1) >= PrtValues.Box(1));
            Assert.AreEqual(boolT, PrtValues.Box(1) >= PrtValues.Box(0));
            Assert.AreEqual(boolT, PrtValues.Box(1) > PrtValues.Box(0));
        }

        [Test]
        public void TestPrtIntOverloading()
        {
            PrtInt int1 = 1;
            PrtInt int2 = 2;
            PrtInt int3 = 3;
            Assert.AreEqual(int3, int1 + int2);
            Assert.AreEqual(int1, int3 - int2);
            Assert.AreEqual(int2, int1 * int2);
            Assert.AreEqual(int1, int3 / int2);
            Assert.AreEqual(PrtValues.Box(6), int1 + int2 + int3);
            Assert.AreEqual(PrtValues.Box(6), int1 * int2 * int3);
            Assert.AreEqual(int1, int3 / (int1 + int2));
            Assert.AreEqual(-int1, int2 - int3);
            Assert.AreEqual(+int1, int1);
            Assert.AreEqual(int1, +int1);
        }

        [Test]
        public void TestPrtSequenceEquality()
        {
            // var t : (a:int, b:float);
            // t as (b:int, a:float)

            var list1 = new PrtSeq<PrtSeq<PrtInt>>();
            var list2 = new PrtSeq<PrtSeq<PrtInt>>();

            var subList = new PrtSeq<PrtInt> {1, 2, 3};
            var subList2 = (PrtSeq<PrtInt>) subList.Clone();

            list1.Add(subList);
            list2.Add(subList2);

            Assert.AreEqual(list1, list2);
            Assert.True(Equals(list1, list2));

            list1[0].Add(4);

            Assert.AreNotEqual(list1, list2);
            Assert.False(Equals(list1, list2));
        }

        [Test]
        public void TestStableHashes()
        {
            // ints
            for (int i = 0; i < 100; i++)
            {
                PrtInt i1 = i;
                PrtInt i2 = i;
                Assert.AreEqual(i1.GetHashCode(), i2.GetHashCode());
            }

            // bools
            Assert.AreEqual(PrtValues.Box(true).GetHashCode(), ((PrtBool)true).GetHashCode());
            Assert.AreEqual(PrtValues.Box(false).GetHashCode(), ((PrtBool)false).GetHashCode());

            // floats
            for (int i = 0; i < 100; i++)
            {
                PrtFloat f1 = 3.1415 * i;
                PrtFloat f2 = 3.1415 * i;
                Assert.AreEqual(f1.GetHashCode(), f2.GetHashCode());
            }
        }

        [Test]
        public void TestPrtMapBasic()
        {
            // Build two identical maps in different orders
            var map1 = new PrtMap<PrtInt, PrtBool>();
            map1.Add(2, true);
            map1.Add(3, true);
            map1.Add(4, false);
            map1.Add(5, true);
            map1.Add(6, false);

            var map2 = new PrtMap<PrtInt, PrtBool>();
            map2[6] = false;
            map2[5] = true;
            map2[3] = true;
            map2[2] = true;
            map2[4] = false;

            // Test basic equality
            Assert.AreEqual(map1, map2);
            Assert.AreEqual(map1.GetHashCode(), map2.GetHashCode());

            // Map equality considers keys and values; map hashes are over only keys
            map2[6] = true;
            Assert.AreNotEqual(map1, map2);
            Assert.AreEqual(map1.GetHashCode(), map2.GetHashCode());
        }

        [Test]
        public void TestFreezingValues()
        {
            var list = new PrtSeq<PrtInt> {PrtValues.Box(1), PrtValues.Box(2), PrtValues.Box(3)};

            var map = new PrtMap<PrtSeq<PrtInt>, PrtFloat> {{list, PrtValues.Box(3.14)}};
            Assert.AreEqual(PrtValues.Box(3.14), map[list]);

            Assert.Throws<PFrozenMutationException>(() => list[0] = PrtValues.Box(4));
            Assert.Throws<PFrozenMutationException>(() => list.Add(PrtValues.Box(4)));

            map[list] = 2.71;
            Assert.AreEqual(PrtValues.Box(2.71), map[list]);
        }

        [Test]
        public void TestDeeplyFrozenValues()
        {
            // Create a list of int-lists and clone it
            var nestedList = new PrtSeq<PrtSeq<PrtInt>>
            {
                new PrtSeq<PrtInt> {1, 2},
                new PrtSeq<PrtInt> {2, 4},
                new PrtSeq<PrtInt> {3, 6}
            };
            var listBackup = (PrtSeq<PrtSeq<PrtInt>>) nestedList.Clone();

            // Use one of the clones as a map key, which freezes it to prevent
            // hash code changes under the map's nose
            var weirdMap = new PrtMap<PrtSeq<PrtSeq<PrtInt>>, PrtBool>();
            weirdMap.Add(nestedList, true);

            // Ensure that attempts to mutate any part of the key-list are denied
            Assert.Throws<PFrozenMutationException>(() =>
                nestedList[0] = new PrtSeq<PrtInt> {2, 1});

            Assert.Throws<PFrozenMutationException>(() =>
                nestedList[0][0] = 0);

            // Ensure that those attempts truly had no effect
            var keyList = weirdMap.Keys.First();
            Assert.AreEqual(listBackup, keyList);

            // Ensure that the other clone was not affected
            listBackup[0][0] = 0;
            Assert.AreNotEqual(listBackup, keyList);
        }

        [Test]
        public void TestExplicitCastIsBox()
        {
            Assert.AreEqual(typeof(PrtInt), ((PrtInt)3).GetType());
            Assert.AreEqual(typeof(PrtFloat), ((PrtFloat)3.14).GetType());
            Assert.AreEqual(typeof(PrtBool), ((PrtBool)true).GetType());
        }

        [Test]
        public void TestIfStatementTranslation()
        {
            PrtBool unconditional = true;
            if (unconditional)
            {
                Assert.Pass();
            }
            Assert.Fail();
        }

        [Test]
        public void TestFib()
        {
            PrtInt a = 1;
            PrtInt b = 1;
            for (int i = 0; i < 47; i++)
            {
                var c = b;
                b = a + b;
                a = c;
            }
            Assert.AreEqual(7778742049, (long)b);
        }

        [Test]
        public void TestFibOpt()
        {
            long a = 1;
            long b = 1;
            for (int i = 0; i < 47; i++)
            {
                var c = b;
                b = a + b;
                a = c;
            }
            Assert.AreEqual(7778742049, b);
        }

        [Test]
        public void TestFloatOptimization()
        {
            PrtFloat init = 5.0;
            for (int i = 0; i < 100; i++)
            {
                var f = init * init + 2 * init + 1;
                var df = 2 * init + 2;
                init = init - f / df;
            }
            
            Assert.True(Math.Abs(init + 1) < 1e-8f);
        }

        [Test]
        public void TestPMachineValue()
        {
            var pm1 = new I_Main(null, new List<string>());
            var pm2 = new I_Main(null, new List<string>());
            Assert.AreEqual(pm1, pm2);
        }

        [Test]
        public void TestSeqToEnum()
        {
            var seq = new PrtSeq<PrtInt>{1,2,3,4};
            var genericSeq = new PrtSeq<IPrtValue>(seq.Cast<IPrtValue>());
        }
    }
}