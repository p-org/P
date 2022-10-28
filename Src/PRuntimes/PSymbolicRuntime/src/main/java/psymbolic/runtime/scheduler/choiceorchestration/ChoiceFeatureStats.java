package psymbolic.runtime.scheduler.choiceorchestration;

import psymbolic.runtime.Event;
import psymbolic.runtime.Message;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.machine.State;
import psymbolic.valuesummary.Guard;
import psymbolic.valuesummary.GuardedValue;
import psymbolic.valuesummary.PrimitiveVS;
import psymbolic.valuesummary.ValueSummary;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;

public class ChoiceFeatureStats {
    public HashMap<ChoiceFeature, Integer> featureToId;
    public List<ChoiceFeature> allFeatures;

    public ChoiceFeatureStats() {
        featureToId = new HashMap<>();
        allFeatures = new ArrayList<>();
    }

    public ChoiceFeature newChoiceFeature(Machine src, Machine tgt, State s, Event e) {
        ChoiceFeature feature = new ChoiceFeature(src, tgt, s, e);
        Integer id = featureToId.get(feature);
        if (id == null) {
            feature.setId(allFeatures.size());
            feature.setReward(new ChoiceReward());
            allFeatures.add(feature);
            featureToId.put(feature, feature.getId());
            return feature;
        } else {
            return allFeatures.get(id);
        }
    }

    public List<ChoiceFeature> getFeatureList(List<ValueSummary> choices, boolean isData) {
        List<ChoiceFeature> result = new ArrayList<>();
        if (isData) {
            // TODO
        } else {
            for (ValueSummary choice: choices) {
                assert(choice instanceof PrimitiveVS);
                addScheduleFeatures(result, (PrimitiveVS) choice);
            }
        }
        return result;
    }

    public ChoiceReward getChoiceRewardCumulative(PrimitiveVS choice) {
        List<ChoiceFeature> choiceFeatureList = new ArrayList<>();
        addScheduleFeatures(choiceFeatureList, choice);;
        ChoiceReward result = new ChoiceReward();
        for (ChoiceFeature f: choiceFeatureList) {
            result.addReward(f.getReward());
        }
        return result;
    }

    private void addScheduleFeatures(List<ChoiceFeature> result, PrimitiveVS choice) {
        List<GuardedValue> guardedValues = choice.getGuardedValues();
        for (GuardedValue gv: guardedValues) {
            Guard g = gv.getGuard();
            assert(gv.getValue() instanceof Machine);
            Machine source = (Machine) gv.getValue();
            addMachineFeatures(result, source, g);
        }
    }

    private void addMachineFeatures(List<ChoiceFeature> result, Machine source, Guard pc) {
        Message msg = source.sendBuffer.peek(pc);
        assert(msg != null);

        for (GuardedValue<State> stateGv : source.getCurrentState().restrict(pc).getGuardedValues()) {
            State state = stateGv.getValue();
            for (GuardedValue<Machine> targetGv : msg.getTarget().restrict(stateGv.getGuard()).getGuardedValues()) {
                Machine target = targetGv.getValue();
                for (GuardedValue<Event> eventGv : msg.getEvent().restrict(targetGv.getGuard()).getGuardedValues()) {
                    Event event = eventGv.getValue();
                    ChoiceFeature newFeature = newChoiceFeature(source, target, state, event);
                    result.add(newFeature);
                }
            }
        }
    }
}
