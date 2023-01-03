package psym.runtime.scheduler;

import psym.commandline.Program;
import psym.valuesummary.*;
import psym.runtime.machine.Machine;
import psym.commandline.PSymConfiguration;

import java.util.ArrayList;
import java.util.List;

public class DPORScheduler extends IterativeBoundedScheduler {

    @Override
    public Schedule getNewSchedule() {
        return new DPORSchedule();
    }

    public DPORScheduler(PSymConfiguration config, Program p) {
        super(config, p);
    }

    // don't use the guards because they may not match
    List<PrimitiveVS<Machine>> toExplore = new ArrayList<>();

    @Override
    public List<PrimitiveVS> getNextSenderChoices() {
        List<PrimitiveVS> senderChoices = super.getNextSenderChoices(); 
        if (toExplore.isEmpty()) {
            toExplore = ((DPORSchedule.DPORChoice) getSchedule().getChoice(getDepth())).getToExplore();
        }
        if (!toExplore.isEmpty()) {
          Guard canExplore = Guard.constFalse();
          List<PrimitiveVS> newSenderChoices = new ArrayList<>();
          for (PrimitiveVS choice : senderChoices) {
             for (GuardedValue<Machine> sender : toExplore.get(0).getGuardedValues()) {
                 canExplore = canExplore.or(choice.symbolicEquals(new PrimitiveVS<>(sender.getValue()), choice.getUniverse()).getGuardFor(true)); 
             }
             newSenderChoices.add(choice.restrict(canExplore));
          }
          toExplore.remove(0);
          return newSenderChoices;
        }
        return senderChoices;
    }

    @Override
    public void postIterationCleanup() {
        ((DPORSchedule) getSchedule()).buildNextToExplore();
        super.postIterationCleanup();
        ((DPORSchedule) getSchedule()).updateSleepSets();
    }
}
