using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc;
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
            PrtBool boolT = PrtValues.Box(true);
            PrtBool boolF = PrtValues.Box(false);
            Assert.AreEqual(boolT, boolF || boolT);
            Assert.AreEqual(boolT, boolT || boolF);
            Assert.AreEqual(boolT, !boolF);
            Assert.AreEqual(boolF, boolT && boolF);
            Assert.AreEqual(boolT, boolT && boolT);
        }

        [Test]
        public void TestPrtFloatComparisions()
        {
            PrtBool boolT = PrtValues.Box(true);
            PrtBool boolF = PrtValues.Box(false);
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
            PrtFloat float1 = PrtValues.Box(1.0);
            PrtFloat float2 = PrtValues.Box(2.0);
            PrtFloat float3 = PrtValues.Box(3.0);
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
            var list = new PrtSeq<PrtInt> {PrtValues.Box(1)};
            int hashCode1 = list.GetHashCode();
            list.Add(PrtValues.Box(2));
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
            PrtBool boolT = PrtValues.Box(true);
            PrtBool boolF = PrtValues.Box(false);
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
            PrtInt int1 = PrtValues.Box(1);
            PrtInt int2 = PrtValues.Box(2);
            PrtInt int3 = PrtValues.Box(3);
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

            var subList = new PrtSeq<PrtInt> {PrtValues.Box(1), PrtValues.Box(2), PrtValues.Box(3)};
            var subList2 = (PrtSeq<PrtInt>) subList.Clone();

            list1.Add(subList);
            list2.Add(subList2);

            Assert.AreEqual(list1, list2);
            Assert.True(Equals(list1, list2));

            list1[0].Add(PrtValues.Box(4));

            Assert.AreNotEqual(list1, list2);
            Assert.False(Equals(list1, list2));
        }

        [Test]
        public void TestStableHashes()
        {
            // ints
            for (int i = 0; i < 100; i++)
            {
                var i1 = PrtValues.Box(i);
                var i2 = PrtValues.Box(i);
                Assert.AreEqual(i1.GetHashCode(), i2.GetHashCode());
            }

            // bools
            Assert.AreEqual(PrtValues.Box(true).GetHashCode(), PrtBool.PrtTrue.GetHashCode());
            Assert.AreEqual(PrtValues.Box(false).GetHashCode(), PrtBool.PrtFalse.GetHashCode());

            // floats
            for (int i = 0; i < 100; i++)
            {
                var f1 = PrtValues.Box(3.1415 * i);
                var f2 = PrtValues.Box(3.1415 * i);
                Assert.AreEqual(f1.GetHashCode(), f2.GetHashCode());
            }
        }

        [Test]
        public void TestPrtMapBasic()
        {
            // Build two identical maps in different orders
            var map1 = new PrtMap<PrtInt, PrtBool>();
            map1.Add(PrtValues.Box(2), PrtValues.Box(true));
            map1.Add(PrtValues.Box(3), PrtValues.Box(true));
            map1.Add(PrtValues.Box(4), PrtValues.Box(false));
            map1.Add(PrtValues.Box(5), PrtValues.Box(true));
            map1.Add(PrtValues.Box(6), PrtValues.Box(false));

            var map2 = new PrtMap<PrtInt, PrtBool>();
            map2[PrtValues.Box(6)] = PrtValues.Box(false);
            map2[PrtValues.Box(5)] = PrtValues.Box(true);
            map2[PrtValues.Box(3)] = PrtValues.Box(true);
            map2[PrtValues.Box(2)] = PrtValues.Box(true);
            map2[PrtValues.Box(4)] = PrtValues.Box(false);

            // Test basic equality
            Assert.AreEqual(map1, map2);
            Assert.AreEqual(map1.GetHashCode(), map2.GetHashCode());

            // Map equality considers keys and values; map hashes are over only keys
            map2[PrtValues.Box(6)] = PrtBool.PrtTrue;
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

            map[list] = PrtValues.Box(2.71);
            Assert.AreEqual(PrtValues.Box(2.71), map[list]);
        }

        [Test]
        public void TestDeeplyFrozenValues()
        {
            // Create a list of int-lists and clone it
            var nestedList = new PrtSeq<PrtSeq<PrtInt>>
            {
                new PrtSeq<PrtInt> {PrtValues.Box(1), PrtValues.Box(2)},
                new PrtSeq<PrtInt> {PrtValues.Box(2), PrtValues.Box(4)},
                new PrtSeq<PrtInt> {PrtValues.Box(3), PrtValues.Box(6)}
            };
            var listBackup = (PrtSeq<PrtSeq<PrtInt>>) nestedList.Clone();

            // Use one of the clones as a map key, which freezes it to prevent
            // hash code changes under the map's nose
            var weirdMap = new PrtMap<PrtSeq<PrtSeq<PrtInt>>, PrtBool>();
            weirdMap.Add(nestedList, PrtBool.PrtTrue);

            // Ensure that attempts to mutate any part of the key-list are denied
            Assert.Throws<PFrozenMutationException>(() =>
                nestedList[0] = new PrtSeq<PrtInt> {PrtValues.Box(2), PrtValues.Box(1)});

            Assert.Throws<PFrozenMutationException>(() =>
                nestedList[0][0] = PrtValues.Box(0));

            // Ensure that those attempts truly had no effect
            var keyList = weirdMap.Keys.First();
            Assert.AreEqual(listBackup, keyList);

            // Ensure that the other clone was not affected
            listBackup[0][0] = PrtValues.Box(0);
            Assert.AreNotEqual(listBackup, keyList);
        }
    }
}