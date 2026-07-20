// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text;

namespace PChecker.Feedback;

/// <summary>
/// A timeline representation turns the sequence of event deliveries observed in one
/// schedule into a set of canonical string tokens. <see cref="TimelineObserver"/> then
/// (a) sorts+joins the tokens into the abstract-timeline string (novelty gate +
/// ExploredTimelines metric) and (b) MinHashes them for the diversity score.
///
/// The representation is the diversity signal that drives the feedback-guided search,
/// so it is selectable (<c>--timeline-repr</c>) with <see cref="PairwiseLocalRepresentation"/>
/// as the default (byte-compatible with the historical behavior). Everything is computed
/// post-hoc from a finished schedule, so this never affects scheduling / record-replay.
///
/// Convention: fields inside a token are separated by U+001F ("\u001f") so distinct field
/// tuples never alias, and the pairwise token's bytes match the legacy hash input exactly.
/// </summary>
public interface ITimelineRepresentation
{
    /// <summary>Field separator used inside tokens (a non-printing control char).</summary>
    const char Sep = '\u001f';

    /// <summary>Record one delivered (dequeued) event, in delivery order.</summary>
    /// <param name="receiver">Receiver machine instance name.</param>
    /// <param name="sender">Sender machine instance name (may be empty).</param>
    /// <param name="label">Abstracted event label (type name, optionally payload-enriched).</param>
    /// <param name="deliveryTime">Receiver's vector clock at delivery (post-merge); may be null.</param>
    void RecordDelivery(string receiver, string sender, string label, VectorTime deliveryTime);

    /// <summary>The canonical token set for this schedule.</summary>
    IReadOnlyCollection<string> Tokens();

    /// <summary>Factory: build the representation named by <paramref name="repr"/>.</summary>
    static ITimelineRepresentation Create(string repr, int kgram)
    {
        switch ((repr ?? "pairwise").ToLowerInvariant())
        {
            case "kgram": return new KGramRepresentation(kgram);
            case "causal": return new CausalRepresentation();
            case "hybrid": return new HybridRepresentation(kgram);
            case "pairwise":
            default: return new PairwiseLocalRepresentation();
        }
    }
}

/// <summary>
/// The historical representation: per receiver instance, the set of ordered pairs of
/// event labels "(a) was dequeued before (b) at least once at this receiver". Local to
/// each receiver (no cross-machine causality). The token layout "receiver\u001fa\u001fb\u001f"
/// makes its downstream FNV-1a hash identical to the legacy StableHash(receiver, a, b),
/// so the default search behavior is preserved exactly.
/// </summary>
public sealed class PairwiseLocalRepresentation : ITimelineRepresentation
{
    private const char S = '\u001f';
    private readonly HashSet<string> _tokens = new();
    private readonly Dictionary<string, HashSet<string>> _seenPerReceiver = new();

    public void RecordDelivery(string receiver, string sender, string label, VectorTime deliveryTime)
    {
        if (!_seenPerReceiver.TryGetValue(receiver, out var seen))
        {
            seen = new HashSet<string>();
            _seenPerReceiver[receiver] = seen;
        }
        foreach (var prev in seen)
        {
            _tokens.Add($"{receiver}{S}{prev}{S}{label}{S}");
        }
        seen.Add(label);
    }

    public IReadOnlyCollection<string> Tokens() => _tokens;
}

/// <summary>
/// Higher-resolution LOCAL representation: per receiver, the set of contiguous
/// subsequences (grams) of length 1..k of the delivery sequence. Strictly finer than
/// pairwise (it distinguishes e.g. ABAB from AABB, which pairwise conflates once all
/// pairs appear) and does not saturate as quickly. Still local — no cross-machine order.
/// </summary>
public sealed class KGramRepresentation : ITimelineRepresentation
{
    private const char S = '\u001f';
    private readonly int _k;
    private readonly HashSet<string> _tokens = new();
    private readonly Dictionary<string, List<string>> _seqPerReceiver = new();

    public KGramRepresentation(int k) => _k = k < 1 ? 1 : k;

    public void RecordDelivery(string receiver, string sender, string label, VectorTime deliveryTime)
    {
        if (!_seqPerReceiver.TryGetValue(receiver, out var seq))
        {
            seq = new List<string>();
            _seqPerReceiver[receiver] = seq;
        }
        seq.Add(label);
        int end = seq.Count - 1;
        // Emit every gram of length 1..k ending at this delivery.
        for (int len = 1; len <= _k && end - (len - 1) >= 0; len++)
        {
            var sb = new StringBuilder();
            sb.Append(receiver).Append(S).Append(len).Append(S);
            for (int i = end - (len - 1); i <= end; i++)
            {
                if (i > end - (len - 1))
                {
                    sb.Append('>');
                }
                sb.Append(seq[i]);
            }
            _tokens.Add(sb.ToString());
        }
    }

    public IReadOnlyCollection<string> Tokens() => _tokens;
}

/// <summary>
/// The principled CAUSAL representation: the happens-before partial order over delivery
/// events, abstracted to labels. For every pair of deliveries it emits either an ordered
/// token "hb a b" (a happens-before b) or a concurrency token "co a b" (causally
/// independent). Happens-before combines (1) message causality via the runtime's vector
/// clocks (<see cref="VectorTime.CompareTo"/>) and (2) per-receiver program order (the
/// observation order of deliveries at the same receiver), which the clocks alone miss
/// because the runtime only ticks a machine's own component on SEND, not on receive.
/// This is the cross-machine generalization of the pairwise representation.
/// </summary>
public sealed class CausalRepresentation : ITimelineRepresentation
{
    private const char S = '\u001f';
    private readonly List<(string receiver, string label, VectorTime time)> _deliveries = new();
    private readonly HashSet<string> _tokens = new();

    public void RecordDelivery(string receiver, string sender, string label, VectorTime deliveryTime)
    {
        var snap = deliveryTime == null ? null : new VectorTime(deliveryTime);
        foreach (var (pReceiver, pLabel, pTime) in _deliveries)
        {
            // VectorTime.CompareTo follows the IComparable convention (its doc comment is
            // inverted): negative => `this` happens-before `other`; positive => after; 0 => concurrent/equal.
            int cmp = (pTime != null && snap != null) ? pTime.CompareTo(snap) : 0;
            _tokens.Add(PairToken(cmp, pReceiver == receiver, pLabel, label));
        }
        _deliveries.Add((receiver, label, snap));
    }

    /// <summary>
    /// Token for one ordered pair of deliveries (prior, current), given the vector-clock
    /// comparison <paramref name="cmp"/> (negative: prior happens-before current; positive:
    /// after; 0: concurrent/equal) and whether they share a receiver. Equal/incomparable
    /// clocks at the same receiver are ordered by program order (prior first); otherwise the
    /// deliveries are genuinely concurrent. Pure + deterministic (unit-tested).
    /// </summary>
    public static string PairToken(int cmp, bool sameReceiver, string priorLabel, string currentLabel)
    {
        if (cmp < 0)
        {
            return Hb(priorLabel, currentLabel);   // prior -> current
        }
        if (cmp > 0)
        {
            return Hb(currentLabel, priorLabel);   // current -> prior
        }
        if (sameReceiver)
        {
            return Hb(priorLabel, currentLabel);   // equal clocks, same receiver => program order
        }
        return Co(priorLabel, currentLabel);       // genuinely concurrent
    }

    private static string Hb(string from, string to) => $"hb{S}{from}{S}{to}";

    private static string Co(string a, string b)
    {
        // Canonical order so concurrency is symmetric: co(a,b) == co(b,a).
        return string.CompareOrdinal(a, b) <= 0 ? $"co{S}{a}{S}{b}" : $"co{S}{b}{S}{a}";
    }

    public IReadOnlyCollection<string> Tokens() => _tokens;
}

/// <summary>
/// Union of the causal (cross-machine order) and k-gram (local resolution) token sets,
/// prefixed so the two families never collide.
/// </summary>
public sealed class HybridRepresentation : ITimelineRepresentation
{
    private const char S = '\u001f';
    private readonly CausalRepresentation _causal = new();
    private readonly KGramRepresentation _kgram;

    public HybridRepresentation(int k) => _kgram = new KGramRepresentation(k);

    public void RecordDelivery(string receiver, string sender, string label, VectorTime deliveryTime)
    {
        _causal.RecordDelivery(receiver, sender, label, deliveryTime);
        _kgram.RecordDelivery(receiver, sender, label, deliveryTime);
    }

    public IReadOnlyCollection<string> Tokens()
    {
        var tokens = new HashSet<string>();
        foreach (var t in _causal.Tokens())
        {
            tokens.Add("C" + S + t);
        }
        foreach (var t in _kgram.Tokens())
        {
            tokens.Add("K" + S + t);
        }
        return tokens;
    }
}
