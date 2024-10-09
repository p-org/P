using NUnit.Framework;
using PChecker.Runtime.Values;

namespace UnitTests
{
    [TestFixture]
    public class CSharpValuesTest
    {
        [Test]
        public void TestPBoolOverloading()
        {
            PBool boolT = true;
            PBool boolF = false;
            Assert.AreEqual(boolT, boolF || boolT);
            Assert.AreEqual(boolT, boolT || boolF);
            Assert.AreEqual(boolT, !boolF);
            Assert.AreEqual(boolF, boolT && boolF);
            Assert.AreEqual(boolT, boolT && boolT);
        }

        [Test]
        public void TestPFloatComparisions()
        {
            PBool boolT = true;
            PBool boolF = false;
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
        public void TestPFloatOverloading()
        {
            PFloat float1 = 1.0;
            PFloat float2 = 2.0;
            PFloat float3 = 3.0;
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
        public void TestPIntComparisions()
        {
            PBool boolT = true;
            PBool boolF = false;
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
        public void TestPIntOverloading()
        {
            PInt int1 = 1;
            PInt int2 = 2;
            PInt int3 = 3;
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
    }
}