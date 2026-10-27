using System;
using System.Collections.Generic;
using NUnit.Framework;
using PChecker.Runtime.Values;

namespace UnitTests
{
    /// <summary>
    /// P values are copied on assignment, so tuples must not share their storage with the
    /// arrays they are created from (foreign code may reuse them), with their clones, or with
    /// the collections they are stored in. Generated code creates tuples with FromOwnedValues,
    /// which keeps the array it is given, since it always passes a new one.
    /// </summary>
    [TestFixture]
    public class TupleValuesTest
    {
        private static PSeq Seq(params int[] items)
        {
            var seq = new PSeq();
            foreach (var item in items)
            {
                seq.Add((PInt)item);
            }

            return seq;
        }

        [Test]
        public void TupleDoesNotKeepTheArrayItIsCreatedFrom()
        {
            var buffer = new IPValue[] { (PInt)1, (PInt)2 };
            var first = new PTuple(buffer);
            buffer[0] = (PInt)3;
            buffer[1] = (PInt)4;
            var second = new PTuple(buffer);

            Assert.AreEqual((PInt)1, first[0]);
            Assert.AreEqual((PInt)2, first[1]);
            Assert.AreEqual((PInt)3, second[0]);

            second[0] = (PInt)5;
            Assert.AreEqual((PInt)3, buffer[0], "changing a tuple changed the array it was created from");
        }

        [Test]
        public void TupleDoesNotChangeTheArrayItIsCreatedFrom()
        {
            var seq = Seq(1);
            var buffer = new IPValue[] { seq, (PInt)2 };
            var tuple = new PTuple(buffer);

            Assert.AreSame(seq, buffer[0], "the constructor replaced an element of the caller's array");
            // As on master, a tuple of several values stores copies of them...
            Assert.AreNotSame(seq, tuple[0]);
            seq.Add((PInt)7);
            Assert.AreEqual(1, ((PSeq)tuple[0]).Count);
        }

        [Test]
        public void TupleOfOneValueStoresIt()
        {
            // ...but a tuple of one value stores it as is, as on master. P has no tuple types of
            // one field, so only foreign code creates such tuples.
            var seq = Seq(1);
            var tuple = new PTuple(seq);
            Assert.AreSame(seq, tuple[0]);
            Assert.AreEqual(1, tuple.fieldValues.Count);

            var ofNull = new PTuple(null);
            Assert.AreEqual(1, ofNull.fieldValues.Count);
            Assert.IsNull(ofNull[0]);
        }

        [Test]
        public void NamedTupleDoesNotKeepTheArraysItIsCreatedFrom()
        {
            var names = new[] { "a", "b" };
            var buffer = new IPValue[] { (PInt)1, (PInt)2 };
            var first = new PNamedTuple(names, buffer);
            buffer[0] = (PInt)3;
            buffer[1] = (PInt)4;
            var second = new PNamedTuple(names, buffer);
            names[0] = "x";

            Assert.AreEqual((PInt)1, first["a"]);
            Assert.AreEqual((PInt)2, first["b"]);
            Assert.AreEqual((PInt)3, second["a"]);
            CollectionAssert.AreEqual(new[] { "a", "b" }, first.fieldNames);
            Assert.AreEqual("<a:1, b:2, >", first.ToString());

            second["a"] = (PInt)5;
            Assert.AreEqual((PInt)3, buffer[0], "changing a tuple changed the array it was created from");
        }

        [Test]
        public void NamedTupleOfNullHasOneField()
        {
            var tuple = new PNamedTuple(new[] { "a" }, null);
            Assert.AreEqual(1, tuple.fieldValues.Count);
            Assert.IsNull(tuple["a"]);
        }

        [Test]
        public void FromOwnedValuesKeepsTheArray()
        {
            var values = new IPValue[] { Seq(1), (PInt)2 };
            var tuple = PTuple.FromOwnedValues(values);
            Assert.AreSame(values[0], tuple[0], "FromOwnedValues copied a value");

            var names = new[] { "a", "b" };
            var named = PNamedTuple.FromOwnedValues(names, values);
            Assert.AreSame(values[1], named["b"]);
            Assert.AreEqual(1, PTuple.FromOwnedValues(null).fieldValues.Count);
        }

        [Test]
        public void ClonesAreIndependent()
        {
            var tuple = PTuple.FromOwnedValues(Seq(1), (PInt)2);
            var clone = (PTuple)tuple.Clone();
            Assert.IsTrue(tuple.Equals(clone));

            ((PSeq)clone[0]).Add((PInt)3);
            clone[1] = (PInt)4;
            Assert.AreEqual(1, ((PSeq)tuple[0]).Count);
            Assert.AreEqual((PInt)2, tuple[1]);

            var names = new[] { "a", "b" };
            var named = PNamedTuple.FromOwnedValues(names, Seq(1), (PInt)2);
            var namedClone = (PNamedTuple)named.Clone();
            Assert.IsTrue(named.Equals(namedClone));

            ((PSeq)namedClone["a"]).Add((PInt)3);
            namedClone["b"] = (PInt)4;
            Assert.AreEqual(1, ((PSeq)named["a"]).Count);
            Assert.AreEqual((PInt)2, named["b"]);
            CollectionAssert.AreEqual(names, namedClone.fieldNames);
        }

        [Test]
        public void TuplesInCollectionsAreCopies()
        {
            var names = new[] { "a", "b" };
            var named = PNamedTuple.FromOwnedValues(names, Seq(1), (PInt)2);
            var tuple = PTuple.FromOwnedValues(named, (PInt)0);

            var seq = new PSeq();
            seq.Add(tuple);
            var map = new PMap();
            map.Add((PInt)0, tuple);

            ((PSeq)named["a"]).Add((PInt)9);
            named["b"] = (PInt)9;
            tuple[1] = (PInt)9;

            foreach (var stored in new[] { (PTuple)seq[0], (PTuple)map[(PInt)0] })
            {
                var storedNamed = (PNamedTuple)stored[0];
                Assert.AreEqual(1, ((PSeq)storedNamed["a"]).Count);
                Assert.AreEqual((PInt)2, storedNamed["b"]);
                Assert.AreEqual((PInt)0, stored[1]);
            }

            // Copying the collection copies the tuples in it.
            var seqCopy = (PSeq)seq.Clone();
            ((PNamedTuple)((PTuple)seqCopy[0])[0])["b"] = (PInt)5;
            Assert.AreEqual((PInt)2, ((PNamedTuple)((PTuple)seq[0])[0])["b"]);
        }

        [Test]
        public void SharedFieldNamesCannotBeChanged()
        {
            var names = new[] { "a", "b" };
            var first = PNamedTuple.FromOwnedValues(names, (PInt)1, (PInt)2);
            var second = PNamedTuple.FromOwnedValues(names, (PInt)3, (PInt)4);

            // The names are shared by all tuples of the type, and the view of them is read-only
            // even when cast to a writable interface.
            Assert.AreSame(first.fieldNames, second.fieldNames);
            Assert.AreEqual(1, first.fieldNames.IndexOf("b"));
            Assert.IsTrue(first.fieldNames.Contains("a"));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)first.fieldNames)[0] = "x");
            Assert.AreEqual((PInt)3, second["a"]);
        }
    }
}
