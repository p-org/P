using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PChecker.Feedback;

namespace UnitTests
{
    /// <summary>
    /// Unit tests for the pluggable timeline representations that drive the feedback-guided
    /// search's diversity signal (--timeline-repr). The vector-clock comparison itself is
    /// trusted runtime code (VectorTime.CompareTo) and exercised end-to-end by the bake-off;
    /// here we test the token-generation logic, which is where the encoding decisions live.
    /// </summary>
    [TestFixture]
    public class TimelineRepresentationTest
    {
        private const char S = '\u001f';

        private static ISet<string> Tokens(ITimelineRepresentation repr, string receiver, params string[] labels)
        {
            foreach (var label in labels)
            {
                repr.RecordDelivery(receiver, "sender", label, null);
            }
            return repr.Tokens().ToHashSet();
        }

        // ── Pairwise (the byte-compatible default) ──

        [NUnit.Framework.Test]
        public void Pairwise_RecordsOrderedTypePairsPerReceiver()
        {
            var repr = new PairwiseLocalRepresentation();
            var tokens = Tokens(repr, "M", "A", "B", "A"); // A, then B, then A again

            // After B: (A,B). After the 2nd A: (A,A) and (B,A). Tokens use the U+001F layout.
            Assert.AreEqual(3, tokens.Count);
            Assert.IsTrue(tokens.Contains($"M{S}A{S}B{S}"));
            Assert.IsTrue(tokens.Contains($"M{S}A{S}A{S}"));
            Assert.IsTrue(tokens.Contains($"M{S}B{S}A{S}"));
        }

        [NUnit.Framework.Test]
        public void Pairwise_IsPerReceiverInstance()
        {
            // Same event order at two different receiver instances must not cross-contaminate.
            var repr = new PairwiseLocalRepresentation();
            repr.RecordDelivery("M1", "s", "A", null);
            repr.RecordDelivery("M2", "s", "B", null); // different receiver: no (M1,A,B) pair
            var tokens = repr.Tokens().ToHashSet();
            Assert.AreEqual(0, tokens.Count); // each receiver saw only one event so far
        }

        [NUnit.Framework.Test]
        public void Pairwise_Deterministic()
        {
            var a = Tokens(new PairwiseLocalRepresentation(), "M", "A", "B", "C");
            var b = Tokens(new PairwiseLocalRepresentation(), "M", "A", "B", "C");
            Assert.IsTrue(a.SetEquals(b));
        }

        // ── K-gram: strictly finer than pairwise where pairwise saturates ──

        [NUnit.Framework.Test]
        public void KGram_DistinguishesWherePairwiseSaturates()
        {
            // ABBA and BAAB have the SAME set of ordered event-type pairs, so pairwise
            // conflates them. k-grams (contiguous order) tell them apart.
            var pwAbba = Tokens(new PairwiseLocalRepresentation(), "M", "A", "B", "B", "A");
            var pwBaab = Tokens(new PairwiseLocalRepresentation(), "M", "B", "A", "A", "B");
            Assert.IsTrue(pwAbba.SetEquals(pwBaab), "precondition: pairwise saturates on ABBA vs BAAB");

            var kgAbba = Tokens(new KGramRepresentation(2), "M", "A", "B", "B", "A");
            var kgBaab = Tokens(new KGramRepresentation(2), "M", "B", "A", "A", "B");
            Assert.IsFalse(kgAbba.SetEquals(kgBaab), "kgram must distinguish ABBA from BAAB");
        }

        [NUnit.Framework.Test]
        public void KGram_EmitsGramsUpToLengthK()
        {
            var tokens = Tokens(new KGramRepresentation(2), "M", "A", "B");
            // len-1: A, B ; len-2: A>B
            Assert.IsTrue(tokens.Contains($"M{S}1{S}A"));
            Assert.IsTrue(tokens.Contains($"M{S}1{S}B"));
            Assert.IsTrue(tokens.Contains($"M{S}2{S}A>B"));
            Assert.AreEqual(3, tokens.Count);
        }

        // ── Causal: the pure happens-before token decision ──

        [NUnit.Framework.Test]
        public void Causal_PairToken_OrdersByHappensBefore()
        {
            // cmp < 0: prior happens-before current  => hb prior -> current
            Assert.AreEqual($"hb{S}A{S}B", CausalRepresentation.PairToken(-1, false, "A", "B"));
            // cmp > 0: prior happens-after current    => hb current -> prior
            Assert.AreEqual($"hb{S}B{S}A", CausalRepresentation.PairToken(1, false, "A", "B"));
        }

        [NUnit.Framework.Test]
        public void Causal_PairToken_SameReceiverTieBreaksByProgramOrder()
        {
            // Equal/incomparable clocks at the same receiver => program order (prior first).
            Assert.AreEqual($"hb{S}A{S}B", CausalRepresentation.PairToken(0, true, "A", "B"));
        }

        [NUnit.Framework.Test]
        public void Causal_PairToken_DifferentReceiverConcurrentIsSymmetric()
        {
            // Incomparable clocks at different receivers => concurrent, canonicalized so the
            // token is order-independent.
            var t1 = CausalRepresentation.PairToken(0, false, "A", "B");
            var t2 = CausalRepresentation.PairToken(0, false, "B", "A");
            Assert.AreEqual(t1, t2);
            Assert.AreEqual($"co{S}A{S}B", t1);
        }

        [NUnit.Framework.Test]
        public void Causal_NullClocks_UseProgramOrderAndConcurrency()
        {
            // With no clocks (null), same-receiver deliveries are program-ordered and
            // cross-receiver deliveries are concurrent.
            var repr = new CausalRepresentation();
            repr.RecordDelivery("M", "s", "A", null);
            repr.RecordDelivery("M", "s", "B", null);   // same receiver => hb A -> B
            repr.RecordDelivery("N", "s", "C", null);   // different receiver => concurrent with A, B
            var tokens = repr.Tokens().ToHashSet();
            Assert.IsTrue(tokens.Contains($"hb{S}A{S}B"));
            Assert.IsTrue(tokens.Contains($"co{S}A{S}C"));
            Assert.IsTrue(tokens.Contains($"co{S}B{S}C"));
        }

        // ── Hybrid: union of causal + kgram, prefixed ──

        [NUnit.Framework.Test]
        public void Hybrid_UnionsCausalAndKGramTokens()
        {
            var hybrid = Tokens(new HybridRepresentation(2), "M", "A", "B");
            Assert.IsTrue(hybrid.Any(t => t.StartsWith("C" + S)), "should contain causal tokens");
            Assert.IsTrue(hybrid.Any(t => t.StartsWith("K" + S)), "should contain kgram tokens");
        }

        // ── Factory + edge cases ──

        [NUnit.Framework.Test]
        public void Factory_SelectsRepresentation()
        {
            Assert.IsInstanceOf<PairwiseLocalRepresentation>(ITimelineRepresentation.Create("pairwise", 3));
            Assert.IsInstanceOf<KGramRepresentation>(ITimelineRepresentation.Create("kgram", 3));
            Assert.IsInstanceOf<CausalRepresentation>(ITimelineRepresentation.Create("causal", 3));
            Assert.IsInstanceOf<HybridRepresentation>(ITimelineRepresentation.Create("hybrid", 3));
            // Unknown / null => default pairwise.
            Assert.IsInstanceOf<PairwiseLocalRepresentation>(ITimelineRepresentation.Create("nonsense", 3));
            Assert.IsInstanceOf<PairwiseLocalRepresentation>(ITimelineRepresentation.Create(null, 3));
        }

        [NUnit.Framework.Test]
        public void EmptySchedule_HasNoTokens()
        {
            foreach (var repr in new ITimelineRepresentation[]
            {
                new PairwiseLocalRepresentation(), new KGramRepresentation(3),
                new CausalRepresentation(), new HybridRepresentation(3),
            })
            {
                Assert.AreEqual(0, repr.Tokens().Count);
            }
        }
    }
}
