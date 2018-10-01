using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using PSharpExtensions;

namespace UnitTests
{
    [TestFixture]
    public class PSharpValuesTest
    {
        [Test]
        public void TestPrtIntOverloading()
        {
            var int1 = PValues.Box(1);
            var int2 = PValues.Box(2);
            var int3 = PValues.Box(3);
            Assert.AreEqual(int3, int1 + int2);
            Assert.AreEqual(int1, int3 - int2);
            Assert.AreEqual(int2, int1 * int2);
            Assert.AreEqual(int1, int3 / int2);
            Assert.AreEqual(PValues.Box(6), int1 + int2 + int3);
            Assert.AreEqual(PValues.Box(6), int1 * int2 * int3);
            Assert.AreEqual(int1, int3 / (int1 + int2));
            Assert.AreEqual(-int1, int2 - int3);
            Assert.AreEqual(+int1, int1);
            Assert.AreEqual(int1, +int1);
        }

        [Test]
        public void TestPrtFloatOverloading()
        {
            var float1 = PValues.Box(1.0);
            var float2 = PValues.Box(2.0);
            var float3 = PValues.Box(3.0);
            Assert.AreEqual(float3, float1 + float2);
            Assert.AreEqual(float1, float3 - float2);
            Assert.AreEqual(float2, float1 * float2);
            Assert.AreEqual(PValues.Box(1.5), float3 / float2);
            Assert.AreEqual(PValues.Box(6.0), float1 + float2 + float3);
            Assert.AreEqual(PValues.Box(6.0), float1 * float2 * float3);
            Assert.AreEqual(float1, float3 / (float1 + float2));
            Assert.AreEqual(-float1, float2 - float3);
            Assert.AreEqual(+float1, float1);
            Assert.AreEqual(float1, +float1);
        }

        [Test]
        public void TestPrtBoolOverloading()
        {
            var boolT = PValues.Box(true);
            var boolF = PValues.Box(false);
            Assert.AreEqual(boolT, boolF || boolT);
            Assert.AreEqual(boolT, boolT || boolF);
            Assert.AreEqual(boolT, !boolF);
            Assert.AreEqual(boolF, boolT && boolF);
            Assert.AreEqual(boolT, boolT && boolT);
        }

        [Test]
        public void TestPrtIntComparisions()
        {
            var boolT = PValues.Box(true);
            var boolF = PValues.Box(false);
            Assert.AreEqual(boolT, PValues.Box(1) < PValues.Box(2));
            Assert.AreEqual(boolF, PValues.Box(1) < PValues.Box(1));
            Assert.AreEqual(boolT, PValues.Box(1) <= PValues.Box(1));
            Assert.AreEqual(boolT, PValues.Box(1) <= PValues.Box(2));
            Assert.AreEqual(boolT, PValues.Box(1) == PValues.Box(1));
            Assert.AreEqual(boolT, PValues.Box(1) != PValues.Box(2));
            Assert.AreEqual(boolT, PValues.Box(1) >= PValues.Box(1));
            Assert.AreEqual(boolT, PValues.Box(1) >= PValues.Box(0));
            Assert.AreEqual(boolT, PValues.Box(1) > PValues.Box(0));
        }

        [Test]
        public void TestPrtFloatComparisions()
        {
            var boolT = PValues.Box(true);
            var boolF = PValues.Box(false);
            Assert.AreEqual(boolT, PValues.Box(1.0) < PValues.Box(2.0));
            Assert.AreEqual(boolF, PValues.Box(1.0) < PValues.Box(1.0));
            Assert.AreEqual(boolT, PValues.Box(1.0) <= PValues.Box(1.0));
            Assert.AreEqual(boolT, PValues.Box(1.0) <= PValues.Box(2.0));
            Assert.AreEqual(boolT, PValues.Box(1.0) == PValues.Box(1.0));
            Assert.AreEqual(boolT, PValues.Box(1.0) != PValues.Box(2.0));
            Assert.AreEqual(boolT, PValues.Box(1.0) >= PValues.Box(1.0));
            Assert.AreEqual(boolT, PValues.Box(1.0) >= PValues.Box(0.0));
            Assert.AreEqual(boolT, PValues.Box(1.0) > PValues.Box(0.0));
        }

        [Test]
        public void TestPrtHashCodesAreValueDependent()
        {
            var list = new PrtSequence<PrtIntValue> {PValues.Box(1)};
            var hashCode1 = list.GetHashCode();
            list.Add(PValues.Box(2));
            var clonedList = list.Clone();
            var hashCode2 = list.GetHashCode();
            var hashCodeCloned2 = clonedList.GetHashCode();
            list.RemoveAt(1);
            var hashCode3 = list.GetHashCode();
            Assert.AreEqual(hashCode1, hashCode3);
            Assert.AreEqual(hashCode2, hashCodeCloned2);
            Assert.AreNotEqual(hashCode1, hashCode2);
            Assert.AreNotEqual(hashCode1, hashCodeCloned2);
        }

        [Test]
        public void TestPrtSequenceEquality()
        {
            // var t : (a:int, b:float);
            // t as (b:int, a:float)

            var list1 = new PrtSequence<PrtSequence<PrtIntValue>>();
            var list2 = new PrtSequence<PrtSequence<PrtIntValue>>();

            var subList = new PrtSequence<PrtIntValue> {PValues.Box(1), PValues.Box(2), PValues.Box(3)};
            var subList2 = (PrtSequence<PrtIntValue>)subList.Clone();

            list1.Add(subList);
            list2.Add(subList2);

            Assert.AreEqual(list1, list2);
            Assert.True(Equals(list1, list2));

            list1[0].Add(PValues.Box(4));

            Assert.AreNotEqual(list1, list2);
            Assert.False(Equals(list1, list2));
        }
    }
}