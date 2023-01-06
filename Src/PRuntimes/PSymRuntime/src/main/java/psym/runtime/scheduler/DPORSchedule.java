package psym.runtime.scheduler;

import psym.valuesummary.*;
import psym.runtime.machine.Machine;
import psym.runtime.Message;

import lombok.Getter;
import lombok.Setter;

import java.util.ArrayList;
import java.util.List;
import java.util.Set;
import java.util.HashSet;
import java.util.Map;
import java.util.HashMap;
import java.util.stream.Collectors;

public class DPORSchedule extends Schedule {

    @Override
    public Choice newChoice() {
        return new DPORChoice();
    }

    public class DPORChoice extends Schedule.Choice {

        @Getter
        private List<PrimitiveVS<Machine>> backtrackSenderStored = null;

        @Getter
        Set<PrimitiveVS<Machine>> sleepTargets = new HashSet<>();
        @Setter @Getter
        PrimitiveVS<Machine> toTarget = new PrimitiveVS<>();
        @Getter
        VectorClockVS scheduledClock = new VectorClockVS(Guard.constFalse());
        @Setter @Getter
        List<PrimitiveVS<Machine>> toExplore = new ArrayList<>();
        @Getter
        Map<PrimitiveVS<Machine>, Message> backtrackMessages = new HashMap<>();

        public void addToTarget(PrimitiveVS<Machine> m) {
            toTarget = toTarget.merge(m);
        }

        public Guard getSleepUniverse() {
            Guard sleepUniverse = Guard.constFalse();
            for (PrimitiveVS<Machine> tgt : sleepTargets) {
                sleepUniverse = sleepUniverse.or(tgt.getUniverse());
            }
            return sleepUniverse;
        }

        public boolean isSleepEmpty() {
            return sleepTargets.isEmpty();
        }

        @Override
        public void addRepeatSender(PrimitiveVS<Machine> choice) {
            super.addRepeatSender(choice);
            for(GuardedValue<Machine> machine : choice.getGuardedValues()) {
                scheduledClock = scheduledClock.merge(machine.getValue().getClock().restrict(machine.getGuard()));
            }
        }

        @Override
        public Choice restrict(Guard pc) {
            DPORChoice c = (DPORChoice) newChoice();
            c.repeatSender = repeatSender.restrict(pc);
            c.repeatBool = repeatBool.restrict(pc);
            c.repeatInt = repeatInt.restrict(pc);
            c.repeatElement = repeatElement.restrict(pc);
            c.backtrackSender = backtrackSender.stream().map(x -> x.restrict(pc)).filter(x -> !x.isEmptyVS()).collect(Collectors.toList());
            c.backtrackBool = backtrackBool.stream().map(x -> x.restrict(pc)).filter(x -> !x.isEmptyVS()).collect(Collectors.toList());
            c.backtrackInt = backtrackInt.stream().map(x -> x.restrict(pc)).filter(x -> !x.isEmptyVS()).collect(Collectors.toList());
            c.backtrackElement = backtrackElement.stream().map(x -> x.restrict(pc)).filter(x -> !x.isEmptyVS()).collect(Collectors.toList());
            c.sleepTargets = sleepTargets.stream().map(x -> x.restrict(pc)).filter(x -> !x.isEmptyVS()).collect(Collectors.toSet());
            c.backtrackMessages = new HashMap<>();
            for (Map.Entry<PrimitiveVS<Machine>, Message> entry : backtrackMessages.entrySet()) {
                c.backtrackMessages.put(entry.getKey().restrict(pc), entry.getValue().restrict(pc));
            }
            return c;
        }

        @Override
        public void addBacktrackSender(PrimitiveVS<Machine> choice) {
            super.addBacktrackSender(choice);
            Message current = new Message();
            for (GuardedValue<Machine> machine : choice.getGuardedValues()) {
                current = current.merge(machine.getValue().sendBuffer.peek(machine.getGuard()));
            }
            backtrackMessages.put(choice, current);
        }

        @Override
        public List<PrimitiveVS<Machine>> getBacktrackSender() {
            List<PrimitiveVS<Machine>> backtrack = new ArrayList();
            for (PrimitiveVS<Machine> sender : super.getBacktrackSender()) {
                Guard cannotBacktrack = Guard.constFalse();
                for (PrimitiveVS<Machine> sleepTarget : sleepTargets) {
                    Message current = backtrackMessages.get(sender);
                    cannotBacktrack = cannotBacktrack.or(current.getTarget().symbolicEquals(sleepTarget, sleepTarget.getUniverse().and(current.getUniverse())).getGuardFor(true));
                }
                backtrackSenderStored = backtrack;
                PrimitiveVS<Machine> toAdd = sender.restrict(cannotBacktrack.not());
                if (!toAdd.getUniverse().isFalse())
                    backtrack.add(sender.restrict(cannotBacktrack.not()));
            }
            return backtrack;
        }

/*
        @Override
        public void addSenderChoice(PrimitiveVS<Machine> choice) {
            senderChoice = choice;
            // don't try to add the event since it may not be "current"
            // can look the event up in the full backtrack set
        }
*/

        public void addSleepTarget(PrimitiveVS<Machine> choice) {
            sleepTargets.add(choice);
        }

        public void clearSleep() {
            backtrackSenderStored = null;
            sleepTargets = new HashSet<>();
        }

        @Override
        public void clear() {
            super.clear();
            clearSleep();
        }

    }

/*
    private void propagateSleepSetSymbolic(int depth, PrimitiveVS<Machine> choice) {
       propagateSleepSet(depth, choice);
       for (GuardedValue<Machine> machine : choice.getGuardedValues()) {
            Message current = machine.getValue().sendBuffer.peek(machine.getGuard());
            for (GuardedValue<Machine> target : current.getTarget().getGuardedValues()) {
                
            }
            propagateSleepSet(depth, choice.restrict(message.getGuard()));
            DPORChoice choiceElement = (DPORChoice) getChoice(depth);
            choiceElement.getToTarget();
            
            Machine target = 
            current = current.merge(machine.getValue().sendBuffer.peek(machine.getGuard()));
        }
    }
*/


    public void buildNextToExplore() {
        for (int j = getChoices().size() - 1; j > 0; j--) {
            DPORChoice toDelay = (DPORChoice) getChoice(j);
            boolean found = false;
            for (int i = j; i < getChoices().size(); i++) {
                DPORChoice choiceElement = (DPORChoice) getChoice(i);
                Guard notLessThan = toDelay.getScheduledClock().cmp(choiceElement.getScheduledClock()).getGuardFor(-1).not();
                Guard shouldSwap = notLessThan.and(toDelay.getToTarget().symbolicEquals(choiceElement.getToTarget(), choiceElement.getToTarget().getUniverse()).getGuardFor(true));
                if (shouldSwap.isFalse()) continue;
                List<PrimitiveVS<Machine>> targets = new ArrayList<>();
                Guard sequenceGuard = shouldSwap;
                for (int k = j; k < i; k++) {
                    DPORChoice earlierElement = (DPORChoice) getChoice(k).restrict(sequenceGuard);
                    Guard lessThan = earlierElement.getScheduledClock().cmp(choiceElement.getScheduledClock()).getGuardFor(-1);
                    if (!lessThan.isFalse()) {
                        sequenceGuard = lessThan; 
                        targets.add(earlierElement.getToTarget());
                        earlierElement.clear();
                    }
                }
                targets.add(toDelay.getToTarget());
                toDelay.setToExplore(targets); 
            }
        }
    }

    private void propagateSleepSet(int depth, PrimitiveVS<Machine> choice) {
        Message current = new Message();
        for (GuardedValue<Machine> machine : choice.getGuardedValues()) {
            current = current.merge(machine.getValue().sendBuffer.peek(machine.getGuard()));
        }
        DPORChoice choiceElement = (DPORChoice) getChoice(depth);
        if (depth > 0) {
            DPORChoice prevChoice = (DPORChoice) getChoice(depth-1);
            for (PrimitiveVS<Machine> sleepTarget : prevChoice.getSleepTargets()) {
                Guard independentCondition = sleepTarget.symbolicEquals(current.getTarget(), sleepTarget.getUniverse().and(current.getUniverse())).getGuardFor(false);
                if(!sleepTarget.getUniverse().and(independentCondition).isFalse())
                    choiceElement.addSleepTarget(sleepTarget.restrict(independentCondition));
            }
        }
        choiceElement.setToTarget(current.getTarget());
    }

    private void propagateSleepSet(int depth) {
        DPORChoice choiceElement = (DPORChoice) getChoice(depth);
        if (depth > 0) {
            DPORChoice prevChoice = (DPORChoice) getChoice(depth-1);
            for (PrimitiveVS<Machine> sleepTarget : prevChoice.getSleepTargets()) {
                choiceElement.addSleepTarget(sleepTarget);
            }
        }
    }

    private void propagateSleepSetTo(int depth, PrimitiveVS<Machine> target) {
        DPORChoice choiceElement = (DPORChoice) getChoice(depth);
        if (depth > 0) {
            DPORChoice prevChoice = (DPORChoice) getChoice(depth-1);
            for (PrimitiveVS<Machine> sleepTarget : prevChoice.getSleepTargets()) {
                Guard independentCondition = sleepTarget.symbolicEquals(target, sleepTarget.getUniverse().and(target.getUniverse())).getGuardFor(false);
                if(!sleepTarget.getUniverse().and(independentCondition).isFalse())
                    choiceElement.addSleepTarget(sleepTarget.restrict(independentCondition));
            }
        }
    }

    public void updateSleepSets() {
        for (int i = 0; i < getChoices().size(); i++) {
            DPORChoice choice = (DPORChoice) getChoice(i);
            if (!choice.getToTarget().getUniverse().isFalse()) {
                choice.addSleepTarget(choice.getToTarget());
                propagateSleepSetTo(i, choice.getToTarget());
                choice.setToTarget(new PrimitiveVS<>());
            }
        }
    }

    @Override
    public void addRepeatSender(PrimitiveVS<Machine> choice, int depth) {
        super.addRepeatSender(choice, depth);
        ((DPORChoice) getChoice(depth)).clearSleep();
        propagateSleepSet(depth, choice);
        getBacktrackSender(depth);
    }

    @Override
    public void addRepeatBool(PrimitiveVS<Boolean> choice, int depth) {
        super.addRepeatBool(choice, depth);
        propagateSleepSet(depth);
    }

    @Override
    public void addRepeatInt(PrimitiveVS<Integer> choice, int depth) {
        super.addRepeatInt(choice, depth);
        propagateSleepSet(depth);
    }

    @Override
    public void addRepeatElement(PrimitiveVS<ValueSummary> choice, int depth) {
        super.addRepeatElement(choice, depth);
        propagateSleepSet(depth);
    }


    /** Update the backtrack and freeze sets based on senders at this step
    * @param senders The machines with messages being sent at this step
    */
/*
    public void compare(PrimitiveVS<Machine> senders) {
        Event pending = new Event();
        List<Event> toMerge = new ArrayList<>();
        for (GuardedValue<Machine> restrictedValue : senders.getGuardedValues()) {
            EffectCollection effects = restrictedValue.value.sendEffects;
            if (!effects.isEmpty())
                toMerge.add(effects.peek(restrictedValue.restrict.and(effects.enabledCond(Event::canRun).getGuard(true))));
        }
        pending = pending.merge(toMerge);
        for (GuardedValue<Machine> pendingTgt : pending.getMachine().getGuardedValues()) {
            int size = size();
            boolean found = false;
            for (int i = size - 1; i >= 0 && !found; i--) {
                Event event = getRepeatChoice(i).eventChosen;
                event = event.restrict(event.getName().getGuard(EventName.Init.instance).not());
                PrimitiveVS<Machine> target = event.getMachine();
                for (GuardedValue<Machine> tgt : target.getGuardedValues()) {
                    if (tgt.getValue().equals(pendingTgt.getValue())) {
                        Guard cmpUniverse = pendingTgt.getGuard().and(tgt.getGuard());
                        // make sure that there isn't a happens-before relationship between the pending and the
                        // potential choice to replace:
                        PrimitiveVS<Integer> cmp = pending.restrict(cmpUniverse).getVectorClock().cmp(event.restrict(cmpUniverse).getVectorClock());
                        Guard notAfter = cmp.getGuard(1).not();
                        Event prePending = pending.restrict(notAfter.and(pendingTgt.getGuard()));
                        PrimitiveVS<Machine> preSenders = senders.restrict(notAfter.and(pendingTgt.getGuard()));
                        if (prePending.isEmptyVS()) { continue; }
                        DPORChoice choice = (DPORChoice) this.choices.get(i);
                        if (!choice.isFrozen()) {
                            PrimitiveVS<Machine> backtrack = getPrePending(prePending, preSenders, i);
                            if (!backtrack.isEmptyVS()) {
                                choice.addSenderChoice(backtrack);
                                choice.freeze();
                                found = true;
                            }
                        }
                    }
                }
            }
        }
    }
*/
/*
    Guard canSchedule(PrimitiveVS<Machine> machine, int i) {
        PrimitiveVS<Machine> backtrack = super.getBacktrackSender(i);
        Choice choices = super.getBacktrackChoice(i);
        Guard cond = Guard.constFalse();
        for (GuardedValue<Machine> restrictedValue0 : choices.senderChoice.getGuardedValues()) {
            for (GuardedValue<Machine> restrictedValue1 : machine.getGuardedValues()) {
                if (restrictedValue0.value.equals(restrictedValue1.value)) {
                    cond = cond.or(restrictedValue0.restrict);
                }
            }
        }
        return cond;
    }

    PrimitiveVS<Machine> getPrePending(Event pending, PrimitiveVS<Machine> pendingSenders, int i) {
        VectorClockVS pendingClock = pending.getVectorClock();
        for (int j = i; j < size(); j++) {
            // either it IS the pending event
            Choice backtrack = super.getBacktrackChoice(i);
            Guard cond = canSchedule(pendingSenders, i);
            if (!cond.isConstFalse()) // if it is enabled at i
                return backtrack.senderChoice.restrict(cond);
            // or it happens-before the pending event
            Choice previous = getRepeatChoice(j);
            VectorClockVS other = previous.eventChosen.getVectorClock();
            PrimitiveVS<Integer> cmp = other.restrict(pending.getUniverse()).cmp(pendingClock);
            PrimitiveVS<Boolean> before = IntUtils.lessThan(cmp, 1); // true when other is or happens-before pending
            Guard happensBeforeCond = before.getGuard(true);
            if (!happensBeforeCond.isConstFalse()) {
                PrimitiveVS<Machine> queue = previous.senderChoice.restrict(happensBeforeCond);
                Choice choices = super.getBacktrackChoice(i);
                // need to make sure this happens-before event is schedulable at i instead
                cond = canSchedule(queue, i);
                if (!cond.isConstFalse()) // if it is enabled at i
                    return choices.senderChoice.restrict(cond); // run the happens-before event
            }
        }
        return new PrimitiveVS<>();
    }
*/
}
