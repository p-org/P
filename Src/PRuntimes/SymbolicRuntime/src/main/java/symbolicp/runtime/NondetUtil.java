package symbolicp.runtime;

import symbolicp.bdd.Bdd;
import symbolicp.util.Checks;
import symbolicp.util.ValueSummaryUnionFind;
import symbolicp.vs.GuardedValue;
import symbolicp.vs.PrimVS;
import java.util.*;
import java.util.stream.Collectors;

public class NondetUtil {

    private static int log2(int bits)
    {
        if( bits == 0 )
            return 0; // or throw exception
        return 31 - Integer.numberOfLeadingZeros(bits);
    }

    private static List<Bdd> generateAllCombos(List<Bdd> bdds) {
        Bdd thisBdd = bdds.get(0);
        List<Bdd> remaining = bdds.subList(1, bdds.size());
        if (remaining.size() == 0) {
            List<Bdd> res = new ArrayList<>();
            res.add(thisBdd);
            res.add(thisBdd.not());
            return res;
        }
        List<Bdd> rec = generateAllCombos(remaining);
        List<Bdd> res = rec.stream().map(x -> x.and(thisBdd)).collect(Collectors.toList());
        res.addAll(rec.stream().map(x -> x.and(thisBdd.not())).collect(Collectors.toList()));
        return res;
    }

    public static PrimVS getNondetChoiceAlt(List<PrimVS> choices) {
        if (choices.size() == 0) return new PrimVS<>();
        if (choices.size() == 1) return choices.get(0);
        List<PrimVS> results = new ArrayList<>();
        PrimVS empty = choices.get(0).guard(Bdd.constFalse());
        List<Bdd> choiceVars = new ArrayList<>();

        int numVars = 1;
        while ((1 << numVars) - choices.size() < 0) {
            numVars++;
        }

        for (int i = 0; i < numVars; i++) {
            choiceVars.add(Bdd.newVar());
        }

        List<Bdd> choiceConds = generateAllCombos(choiceVars);

        Bdd accountedPc = Bdd.constFalse();
        for (int i = 0; i < choices.size(); i++) {
            PrimVS choice = choices.get(i).guard(choiceConds.get(i));
            results.add(choice);
            accountedPc = accountedPc.or(choice.getUniverse());
        }

        Bdd residualPc = accountedPc.not();

        int i = 0;
        for (PrimVS choice : choices) {
            if (residualPc.isConstFalse()) break;
            Bdd enabledCond = choice.getUniverse();
            PrimVS guarded = choice.guard(residualPc);
            results.add(guarded);
            residualPc = residualPc.and(enabledCond.not());
        }
        return empty.merge(results);
    }

    public static PrimVS getNondetChoice(List<PrimVS> choices) {
        if (choices.size() == 0) return new PrimVS<>();
        if (choices.size() == 1) return choices.get(0);
        List<PrimVS> results = new ArrayList<>();
        PrimVS empty = choices.get(0).guard(Bdd.constFalse());
        List<Bdd> choiceVars = new ArrayList<>();

        ValueSummaryUnionFind uf = new ValueSummaryUnionFind(choices);
        Collection<Set<PrimVS>> disjoint = uf.getDisjointSets();
        // only need to distinguish between things within disjoint sets
        Map<Set<PrimVS>, Bdd> universeMap = uf.getLastUniverseMap();

        int maxSize = 0;
        for (Set<PrimVS> set : disjoint) {
            int size = set.size();
            if (size > maxSize) {
                maxSize = size;
            }
        }

        int numVars = 1;
        while ((1 << numVars) - maxSize < 0) {
            numVars++;
        }

        for (int i = 0; i < numVars; i++) {
            choiceVars.add(Bdd.newVar());
        }

        List<Bdd> choiceConds = generateAllCombos(choiceVars);

        for (Set<PrimVS> set : disjoint) {
            results.addAll(getIntersectingNondetChoice(new ArrayList<>(set), choiceConds, universeMap.get(set)));
        }

        return empty.merge(results);
    }

    public static List<PrimVS> getIntersectingNondetChoice(List<PrimVS> choices, List<Bdd> choiceConds, Bdd universe) {
        List<PrimVS> results = new ArrayList<>();
        Bdd accountedPc = Bdd.constFalse();
        for (int i = 0; i < choices.size(); i++) {
            PrimVS choice = choices.get(i).guard(choiceConds.get(i));
            results.add(choice);
            accountedPc = accountedPc.or(choice.getUniverse());
        }

        Bdd residualPc = accountedPc.not().and(universe);

        for (PrimVS choice : choices) {
            if (residualPc.isConstFalse()) break;
            Bdd enabledCond = choice.getUniverse();
            PrimVS guarded = choice.guard(residualPc);
            results.add(guarded);
            residualPc = residualPc.and(enabledCond.not());
        }
        return results;
    }

    public static Bdd chooseGuard(int n, PrimVS choice) {
        Bdd guard = Bdd.constFalse();
        if (choice.getGuardedValues().size() <= n) return Bdd.constTrue();
        for (GuardedValue guardedValue : (List<GuardedValue>) choice.getGuardedValues()) {
            if (n == 0) break;
            guard = guard.or(guardedValue.guard);
            n--;
        }
        return guard;
    }

    public static PrimVS excludeChoice(Bdd guard, PrimVS choice) {
        PrimVS newChoice = choice.guard(guard.not());
        List<GuardedValue> guardedValues = newChoice.getGuardedValues();
        if (guardedValues.size() > 0) {
            newChoice.merge(new PrimVS(guardedValues.iterator().next().value).guard(guard));
        }
        return newChoice;
    }
}
