package psymbolic.runtime;

import psymbolic.util.ValueSummaryUnionFind;
import psymbolic.valuesummary.GuardedValue;
import psymbolic.valuesummary.PrimitiveVS
import psymbolic.valuesummary.bdd.PjbddImpl;

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

    public static PrimitiveVS getNondetChoiceAlt(List<PrimitiveVS> choices) {
        if (choices.size() == 0) return new PrimitiveVS<>();
        if (choices.size() == 1) return choices.get(0);
        List<PrimitiveVS> results = new ArrayList<>();
        PrimitiveVS empty = choices.get(0).guard(Bdd.constFalse());
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
            PrimitiveVS choice = choices.get(i).guard(choiceConds.get(i));
            results.add(choice);
            accountedPc = accountedPc.or(choice.getUniverse());
        }

        Bdd residualPc = accountedPc.not();

        int i = 0;
        for (PrimitiveVS choice : choices) {
            if (residualPc.isConstFalse()) break;
            Bdd enabledCond = choice.getUniverse();
            PrimitiveVS guarded = choice.guard(residualPc);
            results.add(guarded);
            residualPc = residualPc.and(enabledCond.not());
        }
        return empty.merge(results);
    }

    public static PrimitiveVS getNondetChoice(List<PrimitiveVS> choices) {
        if (choices.size() == 0) return new PrimitiveVS<>();
        if (choices.size() == 1) return choices.get(0);
        List<PrimitiveVS> results = new ArrayList<>();
        PrimitiveVS empty = choices.get(0).guard(Bdd.constFalse());
        List<Bdd> choiceVars = new ArrayList<>();

        ValueSummaryUnionFind uf = new ValueSummaryUnionFind(choices);
        Collection<Set<PrimitiveVS>> disjoint = uf.getDisjointSets();
        // only need to distinguish between things within disjoint sets
        Map<Set<PrimitiveVS>, Bdd> universeMap = uf.getLastUniverseMap();

        int maxSize = 0;
        for (Set<PrimitiveVS> set : disjoint) {
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

        for (Set<PrimitiveVS> set : disjoint) {
            results.addAll(getIntersectingNondetChoice(new ArrayList<>(set), choiceConds, universeMap.get(set)));
        }

        return empty.merge(results);
    }

    public static List<PrimitiveVS> getIntersectingNondetChoice(List<PrimitiveVS> choices, List<Bdd> choiceConds, Bdd universe) {
        List<PrimitiveVS> results = new ArrayList<>();
        Bdd accountedPc = Bdd.constFalse();
        for (int i = 0; i < choices.size(); i++) {
            PrimitiveVS choice = choices.get(i).guard(choiceConds.get(i));
            results.add(choice);
            accountedPc = accountedPc.or(choice.getUniverse());
        }

        Bdd residualPc = accountedPc.not().and(universe);

        for (PrimitiveVS choice : choices) {
            if (residualPc.isConstFalse()) break;
            Bdd enabledCond = choice.getUniverse();
            PrimitiveVS guarded = choice.guard(residualPc);
            results.add(guarded);
            residualPc = residualPc.and(enabledCond.not());
        }
        return results;
    }

    public static Bdd chooseGuard(int n, PrimitiveVS choice) {
        Bdd guard = Bdd.constFalse();
        if (choice.getGuardedValues().size() <= n) return Bdd.constTrue();
        for (GuardedValue guardedValue : (List<GuardedValue>) choice.getGuardedValues()) {
            if (n == 0) break;
            guard = guard.or(guardedValue.guard);
            n--;
        }
        return guard;
    }

    public static PrimitiveVS excludeChoice(Bdd guard, PrimitiveVS choice) {
        PrimitiveVS newChoice = choice.guard(guard.not());
        List<GuardedValue> guardedValues = newChoice.getGuardedValues();
        if (guardedValues.size() > 0) {
            newChoice.merge(new PrimitiveVS(guardedValues.iterator().next().value).guard(guard));
        }
        return newChoice;
    }
}
