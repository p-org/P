package pcontainment;
import pcontainment.commandline.*;
import pcontainment.valuesummary.*;
import pcontainment.valuesummary.ai.*;
import pcontainment.runtime.*;
import pcontainment.runtime.scheduler.*;
import pcontainment.runtime.machine.*;
import pcontainment.runtime.logger.*;
import pcontainment.runtime.machine.buffer.*;
import pcontainment.runtime.machine.eventhandlers.*;
import java.util.List;
import java.util.ArrayList;
import java.util.Map;
import java.util.HashMap;
import java.util.function.Consumer;
import java.util.function.Function;

public class osr implements Program {
    
    public static Scheduler scheduler;
    
    @Override
    public void setScheduler (Scheduler s) { this.scheduler = s; }
    
    
    
    public static Event _null = new Event("_null");
    public static Event _halt = new Event("_halt");
    public static Event eD0Entry = new Event("eD0Entry");
    public static Event eD0Exit = new Event("eD0Exit");
    public static Event eTimerFired = new Event("eTimerFired");
    public static Event eSwitchStatusChange = new Event("eSwitchStatusChange");
    public static Event eTransferSuccess = new Event("eTransferSuccess");
    public static Event eTransferFailure = new Event("eTransferFailure");
    public static Event eStopTimer = new Event("eStopTimer");
    public static Event eUpdateBarGraphStateUsingControlTransfer = new Event("eUpdateBarGraphStateUsingControlTransfer");
    public static Event eSetLedStateToUnstableUsingControlTransfer = new Event("eSetLedStateToUnstableUsingControlTransfer");
    public static Event eStartDebounceTimer = new Event("eStartDebounceTimer");
    public static Event eSetLedStateToStableUsingControlTransfer = new Event("eSetLedStateToStableUsingControlTransfer");
    public static Event eStoppingSuccess = new Event("eStoppingSuccess");
    public static Event eStoppingFailure = new Event("eStoppingFailure");
    public static Event eOperationSuccess = new Event("eOperationSuccess");
    public static Event eOperationFailure = new Event("eOperationFailure");
    public static Event eTimerStopped = new Event("eTimerStopped");
    public static Event eYes = new Event("eYes");
    public static Event eNo = new Event("eNo");
    public static Event eUnit = new Event("eUnit");
    // Skipping Interface 'Main'

    // Skipping Interface 'Switch'

    // Skipping Interface 'LED'

    // Skipping Interface 'Timer'

    // Skipping Interface 'OSRDriver'

    public static class Main extends Machine {
        
        static State User_Init = new State("User_Init") {
            @Override public void entry(Guard pc_0, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Main)machine).anonfun_0(pc_0, machine.sendBuffer, outcome);
            }
        };
        static State S0 = new State("S0") {
            @Override public void entry(Guard pc_1, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Main)machine).anonfun_1(pc_1, machine.sendBuffer, outcome);
            }
        };
        static State S1 = new State("S1") {
            @Override public void entry(Guard pc_2, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Main)machine).anonfun_2(pc_2, machine.sendBuffer, outcome);
            }
        };
        private PrimitiveDomainVS<Machine> var_Driver = new PrimitiveDomainVS<Machine>();
        private PrimitiveDomainVS<Integer> var_count = new PrimitiveDomainVS<Integer>(0);
        
        @Override
        public void reset() {
                super.reset();
                var_Driver = new PrimitiveDomainVS<Machine>();
                var_count = new PrimitiveDomainVS<Integer>(0);
        }
        
        public Main(int id) {
            super("Main", id, EventBufferSemantics.queue, User_Init, User_Init
                , S0
                , S1
                
            );
            User_Init.addHandlers(new GotoEventHandler(eUnit, S0));
            S0.addHandlers(new EventHandler(eUnit) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Main)machine).anonfun_3(pc, machine.sendBuffer, outcome);
                    }
                });
            S1.addHandlers(new GotoEventHandler(eUnit, S0));
        }
        
        Guard 
        anonfun_0(
            Guard pc_3,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveDomainVS<Machine> var_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_3);
            
            PrimitiveDomainVS<Event> var_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_3);
            
            PrimitiveDomainVS<Machine> temp_var_0;
            temp_var_0 = effects.create(pc_3, scheduler, OSRDriver.class, (i) -> new OSRDriver(i)).toPrimitiveDomainVS();
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_3, temp_var_0);
            
            PrimitiveDomainVS<Machine> temp_var_1;
            temp_var_1 = var_$tmp0.restrict(pc_3);
            var_Driver = var_Driver.updateUnderGuard(pc_3, temp_var_1);
            
            PrimitiveDomainVS<Event> temp_var_2;
            temp_var_2 = new PrimitiveDomainVS<Event>(eUnit).restrict(pc_3);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_3, temp_var_2);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_3, var_$tmp1.restrict(pc_3));
            pc_3 = Guard.constFalse();
            
            return pc_3;
        }
        
        Guard 
        anonfun_1(
            Guard pc_4,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveDomainVS<Machine> var_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_4);
            
            PrimitiveDomainVS<Event> var_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_4);
            
            PrimitiveDomainVS<Event> var_$tmp2 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_4);
            
            PrimitiveDomainVS<Machine> temp_var_3;
            temp_var_3 = var_Driver.restrict(pc_4);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_4, temp_var_3);
            
            PrimitiveDomainVS<Event> temp_var_4;
            temp_var_4 = new PrimitiveDomainVS<Event>(eD0Entry).restrict(pc_4);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_4, temp_var_4);
            
            effects.send(pc_4, (var_$tmp0.restrict(pc_4)).toPrimitiveVS(), (var_$tmp1.restrict(pc_4)).toPrimitiveVS(), null);
            
            PrimitiveDomainVS<Event> temp_var_5;
            temp_var_5 = new PrimitiveDomainVS<Event>(eUnit).restrict(pc_4);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_4, temp_var_5);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_4, var_$tmp2.restrict(pc_4));
            pc_4 = Guard.constFalse();
            
            return pc_4;
        }
        
        Guard 
        anonfun_3(
            Guard pc_5,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveDomainVS<Boolean> var_$tmp0 =
                new PrimitiveDomainVS<Boolean>(false).restrict(pc_5);
            
            PrimitiveDomainVS<Integer> var_$tmp1 =
                new PrimitiveDomainVS<Integer>(0).restrict(pc_5);
            
            PrimitiveDomainVS<Boolean> temp_var_6;
            temp_var_6 = (var_count.restrict(pc_5)).apply(new PrimitiveDomainVS<Integer>(3).restrict(pc_5), (temp_var_7, temp_var_8) -> temp_var_7 < temp_var_8);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_5, temp_var_6);
            
            PrimitiveDomainVS<Boolean> temp_var_9 = var_$tmp0.restrict(pc_5);
            Guard pc_6 = (temp_var_9).getGuardFor(true);
            Guard pc_7 = (temp_var_9).getGuardFor(false);
            boolean jumpedOut_0 = false;
            boolean jumpedOut_1 = false;
            if (!pc_6.isFalse()) {
                // 'then' branch
                PrimitiveDomainVS<Integer> temp_var_10;
                temp_var_10 = (var_count.restrict(pc_6)).apply(new PrimitiveDomainVS<Integer>(1).restrict(pc_6), (temp_var_11, temp_var_12) -> temp_var_11 + temp_var_12);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_6, temp_var_10);
                
                PrimitiveDomainVS<Integer> temp_var_13;
                temp_var_13 = var_$tmp1.restrict(pc_6);
                var_count = var_count.updateUnderGuard(pc_6, temp_var_13);
                
                outcome.addGuardedGoto(pc_6, S1);
                pc_6 = Guard.constFalse();
                jumpedOut_0 = true;
                
            }
            if (!pc_7.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_0 || jumpedOut_1) {
                pc_5 = pc_6.or(pc_7);
            }
            
            if (!pc_5.isFalse()) {
            }
            return pc_5;
        }
        
        Guard 
        anonfun_2(
            Guard pc_8,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveDomainVS<Machine> var_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_8);
            
            PrimitiveDomainVS<Event> var_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_8);
            
            PrimitiveDomainVS<Event> var_$tmp2 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_8);
            
            PrimitiveDomainVS<Machine> temp_var_14;
            temp_var_14 = var_Driver.restrict(pc_8);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_8, temp_var_14);
            
            PrimitiveDomainVS<Event> temp_var_15;
            temp_var_15 = new PrimitiveDomainVS<Event>(eD0Exit).restrict(pc_8);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_8, temp_var_15);
            
            effects.send(pc_8, (var_$tmp0.restrict(pc_8)).toPrimitiveVS(), (var_$tmp1.restrict(pc_8)).toPrimitiveVS(), null);
            
            PrimitiveDomainVS<Event> temp_var_16;
            temp_var_16 = new PrimitiveDomainVS<Event>(eUnit).restrict(pc_8);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_8, temp_var_16);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_8, var_$tmp2.restrict(pc_8));
            pc_8 = Guard.constFalse();
            
            return pc_8;
        }
        
    }
    
    public static class Switch extends Machine {
        
        static State _Init = new State("_Init") {
            @Override public void entry(Guard pc_9, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Switch)machine).anonfun_4(pc_9, machine.sendBuffer, outcome, payload != null ? (PrimitiveDomainVS<Machine>) ValueSummary.castFromAny(pc_9, new PrimitiveDomainVS<Machine>().restrict(pc_9), payload) : new PrimitiveDomainVS<Machine>().restrict(pc_9));
            }
        };
        static State Switch_Init = new State("Switch_Init") {
            @Override public void entry(Guard pc_10, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Switch)machine).anonfun_5(pc_10, machine.sendBuffer, outcome);
            }
        };
        static State ChangeSwitchStatus = new State("ChangeSwitchStatus") {
            @Override public void entry(Guard pc_11, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Switch)machine).anonfun_6(pc_11, machine.sendBuffer, outcome);
            }
        };
        private PrimitiveDomainVS<Machine> var_Driver = new PrimitiveDomainVS<Machine>();
        private PrimitiveDomainVS<Integer> var_count = new PrimitiveDomainVS<Integer>(0);
        
        @Override
        public void reset() {
                super.reset();
                var_Driver = new PrimitiveDomainVS<Machine>();
                var_count = new PrimitiveDomainVS<Integer>(0);
        }
        
        public Switch(int id) {
            super("Switch", id, EventBufferSemantics.queue, _Init, _Init
                , Switch_Init
                , ChangeSwitchStatus
                
            );
            _Init.addHandlers(new GotoEventHandler(eUnit, Switch_Init));
            Switch_Init.addHandlers(new GotoEventHandler(eUnit, ChangeSwitchStatus));
            ChangeSwitchStatus.addHandlers(new GotoEventHandler(eUnit, ChangeSwitchStatus));
        }
        
        Guard 
        anonfun_4(
            Guard pc_12,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            PrimitiveDomainVS<Machine> var_payload
        ) {
            PrimitiveDomainVS<Event> var_$tmp0 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_12);
            
            PrimitiveDomainVS<Machine> temp_var_17;
            temp_var_17 = var_payload.restrict(pc_12);
            var_Driver = var_Driver.updateUnderGuard(pc_12, temp_var_17);
            
            PrimitiveDomainVS<Event> temp_var_18;
            temp_var_18 = new PrimitiveDomainVS<Event>(eUnit).restrict(pc_12);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_12, temp_var_18);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_12, var_$tmp0.restrict(pc_12));
            pc_12 = Guard.constFalse();
            
            return pc_12;
        }
        
        Guard 
        anonfun_5(
            Guard pc_13,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveDomainVS<Event> var_$tmp0 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_13);
            
            PrimitiveDomainVS<Event> temp_var_19;
            temp_var_19 = new PrimitiveDomainVS<Event>(eUnit).restrict(pc_13);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_13, temp_var_19);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_13, var_$tmp0.restrict(pc_13));
            pc_13 = Guard.constFalse();
            
            return pc_13;
        }
        
        Guard 
        anonfun_6(
            Guard pc_14,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveDomainVS<Machine> var_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_14);
            
            PrimitiveDomainVS<Event> var_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_14);
            
            PrimitiveDomainVS<Integer> var_$tmp2 =
                new PrimitiveDomainVS<Integer>(0).restrict(pc_14);
            
            PrimitiveDomainVS<Boolean> var_$tmp3 =
                new PrimitiveDomainVS<Boolean>(false).restrict(pc_14);
            
            PrimitiveDomainVS<Event> var_$tmp4 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_14);
            
            PrimitiveDomainVS<Machine> temp_var_20;
            temp_var_20 = var_Driver.restrict(pc_14);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_14, temp_var_20);
            
            PrimitiveDomainVS<Event> temp_var_21;
            temp_var_21 = new PrimitiveDomainVS<Event>(eSwitchStatusChange).restrict(pc_14);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_14, temp_var_21);
            
            effects.send(pc_14, (var_$tmp0.restrict(pc_14)).toPrimitiveVS(), (var_$tmp1.restrict(pc_14)).toPrimitiveVS(), null);
            
            PrimitiveDomainVS<Integer> temp_var_22;
            temp_var_22 = (var_count.restrict(pc_14)).apply(new PrimitiveDomainVS<Integer>(1).restrict(pc_14), (temp_var_23, temp_var_24) -> temp_var_23 + temp_var_24);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_14, temp_var_22);
            
            PrimitiveDomainVS<Integer> temp_var_25;
            temp_var_25 = var_$tmp2.restrict(pc_14);
            var_count = var_count.updateUnderGuard(pc_14, temp_var_25);
            
            PrimitiveDomainVS<Boolean> temp_var_26;
            temp_var_26 = (var_count.restrict(pc_14)).apply(new PrimitiveDomainVS<Integer>(3).restrict(pc_14), (temp_var_27, temp_var_28) -> temp_var_27 < temp_var_28);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_14, temp_var_26);
            
            PrimitiveDomainVS<Boolean> temp_var_29 = var_$tmp3.restrict(pc_14);
            Guard pc_15 = (temp_var_29).getGuardFor(true);
            Guard pc_16 = (temp_var_29).getGuardFor(false);
            boolean jumpedOut_2 = false;
            boolean jumpedOut_3 = false;
            if (!pc_15.isFalse()) {
                // 'then' branch
                PrimitiveDomainVS<Event> temp_var_30;
                temp_var_30 = new PrimitiveDomainVS<Event>(eUnit).restrict(pc_15);
                var_$tmp4 = var_$tmp4.updateUnderGuard(pc_15, temp_var_30);
                
                // NOTE (TODO): We currently perform no typechecking on the payload!
                outcome.raiseGuardedEvent(pc_15, var_$tmp4.restrict(pc_15));
                pc_15 = Guard.constFalse();
                jumpedOut_2 = true;
                
            }
            if (!pc_16.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_2 || jumpedOut_3) {
                pc_14 = pc_15.or(pc_16);
            }
            
            if (!pc_14.isFalse()) {
            }
            return pc_14;
        }
        
    }
    
    public static class LED extends Machine {
        
        static State _Init = new State("_Init") {
            @Override public void entry(Guard pc_17, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((LED)machine).anonfun_7(pc_17, machine.sendBuffer, outcome, payload != null ? (PrimitiveDomainVS<Machine>) ValueSummary.castFromAny(pc_17, new PrimitiveDomainVS<Machine>().restrict(pc_17), payload) : new PrimitiveDomainVS<Machine>().restrict(pc_17));
            }
        };
        static State LED_Init = new State("LED_Init") {
            @Override public void entry(Guard pc_18, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((LED)machine).anonfun_8(pc_18, machine.sendBuffer);
            }
        };
        static State ProcessUpdateLED = new State("ProcessUpdateLED") {
            @Override public void entry(Guard pc_19, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((LED)machine).anonfun_9(pc_19, machine.sendBuffer, outcome);
            }
        };
        static State UnstableLED = new State("UnstableLED") {
            @Override public void entry(Guard pc_20, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((LED)machine).anonfun_10(pc_20, machine.sendBuffer);
            }
        };
        static State StableLED = new State("StableLED") {
            @Override public void entry(Guard pc_21, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((LED)machine).anonfun_11(pc_21, machine.sendBuffer, outcome);
            }
        };
        private PrimitiveDomainVS<Machine> var_Driver = new PrimitiveDomainVS<Machine>();
        
        @Override
        public void reset() {
                super.reset();
                var_Driver = new PrimitiveDomainVS<Machine>();
        }
        
        public LED(int id) {
            super("LED", id, EventBufferSemantics.queue, _Init, _Init
                , LED_Init
                , ProcessUpdateLED
                , UnstableLED
                , StableLED
                
            );
            _Init.addHandlers(new GotoEventHandler(eUnit, LED_Init));
            LED_Init.addHandlers(new GotoEventHandler(eUpdateBarGraphStateUsingControlTransfer, ProcessUpdateLED),
                new GotoEventHandler(eSetLedStateToUnstableUsingControlTransfer, UnstableLED),
                new GotoEventHandler(eSetLedStateToStableUsingControlTransfer, StableLED));
            ProcessUpdateLED.addHandlers(new GotoEventHandler(eUnit, LED_Init));
            UnstableLED.addHandlers(new GotoEventHandler(eSetLedStateToStableUsingControlTransfer, LED_Init),
                new GotoEventHandler(eUpdateBarGraphStateUsingControlTransfer, ProcessUpdateLED));
            StableLED.addHandlers(new GotoEventHandler(eUnit, LED_Init));
        }
        
        Guard 
        anonfun_7(
            Guard pc_22,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            PrimitiveDomainVS<Machine> var_payload
        ) {
            PrimitiveDomainVS<Event> var_$tmp0 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_22);
            
            PrimitiveDomainVS<Machine> temp_var_31;
            temp_var_31 = var_payload.restrict(pc_22);
            var_Driver = var_Driver.updateUnderGuard(pc_22, temp_var_31);
            
            PrimitiveDomainVS<Event> temp_var_32;
            temp_var_32 = new PrimitiveDomainVS<Event>(eUnit).restrict(pc_22);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_22, temp_var_32);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_22, var_$tmp0.restrict(pc_22));
            pc_22 = Guard.constFalse();
            
            return pc_22;
        }
        
        void 
        anonfun_8(
            Guard pc_23,
            EventBuffer effects
        ) {
        }
        
        Guard 
        anonfun_9(
            Guard pc_24,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveDomainVS<Boolean> var_$tmp0 =
                new PrimitiveDomainVS<Boolean>(false).restrict(pc_24);
            
            PrimitiveDomainVS<Machine> var_$tmp1 =
                new PrimitiveDomainVS<Machine>().restrict(pc_24);
            
            PrimitiveDomainVS<Event> var_$tmp2 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_24);
            
            PrimitiveDomainVS<Machine> var_$tmp3 =
                new PrimitiveDomainVS<Machine>().restrict(pc_24);
            
            PrimitiveDomainVS<Event> var_$tmp4 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_24);
            
            PrimitiveDomainVS<Event> var_$tmp5 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_24);
            
            PrimitiveDomainVS<Boolean> temp_var_33;
            temp_var_33 = scheduler.getNextBoolean(pc_24).toPrimitiveDomainVS();
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_24, temp_var_33);
            
            PrimitiveDomainVS<Boolean> temp_var_34 = var_$tmp0.restrict(pc_24);
            Guard pc_25 = (temp_var_34).getGuardFor(true);
            Guard pc_26 = (temp_var_34).getGuardFor(false);
            boolean jumpedOut_4 = false;
            boolean jumpedOut_5 = false;
            if (!pc_25.isFalse()) {
                // 'then' branch
                PrimitiveDomainVS<Machine> temp_var_35;
                temp_var_35 = var_Driver.restrict(pc_25);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_25, temp_var_35);
                
                PrimitiveDomainVS<Event> temp_var_36;
                temp_var_36 = new PrimitiveDomainVS<Event>(eTransferSuccess).restrict(pc_25);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_25, temp_var_36);
                
                effects.send(pc_25, (var_$tmp1.restrict(pc_25)).toPrimitiveVS(), (var_$tmp2.restrict(pc_25)).toPrimitiveVS(), null);
                
            }
            if (!pc_26.isFalse()) {
                // 'else' branch
                PrimitiveDomainVS<Machine> temp_var_37;
                temp_var_37 = var_Driver.restrict(pc_26);
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_26, temp_var_37);
                
                PrimitiveDomainVS<Event> temp_var_38;
                temp_var_38 = new PrimitiveDomainVS<Event>(eTransferFailure).restrict(pc_26);
                var_$tmp4 = var_$tmp4.updateUnderGuard(pc_26, temp_var_38);
                
                effects.send(pc_26, (var_$tmp3.restrict(pc_26)).toPrimitiveVS(), (var_$tmp4.restrict(pc_26)).toPrimitiveVS(), null);
                
            }
            if (jumpedOut_4 || jumpedOut_5) {
                pc_24 = pc_25.or(pc_26);
            }
            
            PrimitiveDomainVS<Event> temp_var_39;
            temp_var_39 = new PrimitiveDomainVS<Event>(eUnit).restrict(pc_24);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_24, temp_var_39);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_24, var_$tmp5.restrict(pc_24));
            pc_24 = Guard.constFalse();
            
            return pc_24;
        }
        
        void 
        anonfun_10(
            Guard pc_27,
            EventBuffer effects
        ) {
            PrimitiveDomainVS<Machine> var_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_27);
            
            PrimitiveDomainVS<Event> var_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_27);
            
            PrimitiveDomainVS<Machine> temp_var_40;
            temp_var_40 = var_Driver.restrict(pc_27);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_27, temp_var_40);
            
            PrimitiveDomainVS<Event> temp_var_41;
            temp_var_41 = new PrimitiveDomainVS<Event>(eTransferSuccess).restrict(pc_27);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_27, temp_var_41);
            
            effects.send(pc_27, (var_$tmp0.restrict(pc_27)).toPrimitiveVS(), (var_$tmp1.restrict(pc_27)).toPrimitiveVS(), null);
            
        }
        
        Guard 
        anonfun_11(
            Guard pc_28,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveDomainVS<Machine> var_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_28);
            
            PrimitiveDomainVS<Event> var_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_28);
            
            PrimitiveDomainVS<Event> var_$tmp2 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_28);
            
            PrimitiveDomainVS<Machine> temp_var_42;
            temp_var_42 = var_Driver.restrict(pc_28);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_28, temp_var_42);
            
            PrimitiveDomainVS<Event> temp_var_43;
            temp_var_43 = new PrimitiveDomainVS<Event>(eTransferSuccess).restrict(pc_28);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_28, temp_var_43);
            
            effects.send(pc_28, (var_$tmp0.restrict(pc_28)).toPrimitiveVS(), (var_$tmp1.restrict(pc_28)).toPrimitiveVS(), null);
            
            PrimitiveDomainVS<Event> temp_var_44;
            temp_var_44 = new PrimitiveDomainVS<Event>(eUnit).restrict(pc_28);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_28, temp_var_44);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_28, var_$tmp2.restrict(pc_28));
            pc_28 = Guard.constFalse();
            
            return pc_28;
        }
        
    }
    
    public static class Timer extends Machine {
        
        static State _Init = new State("_Init") {
            @Override public void entry(Guard pc_29, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Timer)machine).anonfun_12(pc_29, machine.sendBuffer, outcome, payload != null ? (PrimitiveDomainVS<Machine>) ValueSummary.castFromAny(pc_29, new PrimitiveDomainVS<Machine>().restrict(pc_29), payload) : new PrimitiveDomainVS<Machine>().restrict(pc_29));
            }
        };
        static State Timer_Init = new State("Timer_Init") {
            @Override public void entry(Guard pc_30, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Timer)machine).anonfun_13(pc_30, machine.sendBuffer);
            }
        };
        static State TimerStarted = new State("TimerStarted") {
            @Override public void entry(Guard pc_31, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Timer)machine).anonfun_14(pc_31, machine.sendBuffer, outcome);
            }
        };
        static State SendTimerFired = new State("SendTimerFired") {
            @Override public void entry(Guard pc_32, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Timer)machine).anonfun_15(pc_32, machine.sendBuffer, outcome);
            }
        };
        static State ConsmachineeringStoppingTimer = new State("ConsmachineeringStoppingTimer") {
            @Override public void entry(Guard pc_33, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Timer)machine).anonfun_16(pc_33, machine.sendBuffer, outcome);
            }
        };
        private PrimitiveDomainVS<Machine> var_Driver = new PrimitiveDomainVS<Machine>();
        
        @Override
        public void reset() {
                super.reset();
                var_Driver = new PrimitiveDomainVS<Machine>();
        }
        
        public Timer(int id) {
            super("Timer", id, EventBufferSemantics.queue, _Init, _Init
                , Timer_Init
                , TimerStarted
                , SendTimerFired
                , ConsmachineeringStoppingTimer
                
            );
            _Init.addHandlers(new GotoEventHandler(eUnit, Timer_Init));
            Timer_Init.addHandlers(new IgnoreEventHandler(eStopTimer),
                new GotoEventHandler(eStartDebounceTimer, TimerStarted));
            TimerStarted.addHandlers(new DeferEventHandler(eStartDebounceTimer)
                ,
                new GotoEventHandler(eUnit, SendTimerFired),
                new GotoEventHandler(eStopTimer, ConsmachineeringStoppingTimer));
            SendTimerFired.addHandlers(new DeferEventHandler(eStartDebounceTimer)
                ,
                new GotoEventHandler(eUnit, Timer_Init));
            ConsmachineeringStoppingTimer.addHandlers(new DeferEventHandler(eStartDebounceTimer)
                ,
                new GotoEventHandler(eUnit, Timer_Init));
        }
        
        Guard 
        anonfun_12(
            Guard pc_34,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            PrimitiveDomainVS<Machine> var_payload
        ) {
            PrimitiveDomainVS<Event> var_$tmp0 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_34);
            
            PrimitiveDomainVS<Machine> temp_var_45;
            temp_var_45 = var_payload.restrict(pc_34);
            var_Driver = var_Driver.updateUnderGuard(pc_34, temp_var_45);
            
            PrimitiveDomainVS<Event> temp_var_46;
            temp_var_46 = new PrimitiveDomainVS<Event>(eUnit).restrict(pc_34);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_34, temp_var_46);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_34, var_$tmp0.restrict(pc_34));
            pc_34 = Guard.constFalse();
            
            return pc_34;
        }
        
        void 
        anonfun_13(
            Guard pc_35,
            EventBuffer effects
        ) {
        }
        
        Guard 
        anonfun_14(
            Guard pc_36,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveDomainVS<Boolean> var_$tmp0 =
                new PrimitiveDomainVS<Boolean>(false).restrict(pc_36);
            
            PrimitiveDomainVS<Event> var_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_36);
            
            PrimitiveDomainVS<Boolean> temp_var_47;
            temp_var_47 = scheduler.getNextBoolean(pc_36).toPrimitiveDomainVS();
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_36, temp_var_47);
            
            PrimitiveDomainVS<Boolean> temp_var_48 = var_$tmp0.restrict(pc_36);
            Guard pc_37 = (temp_var_48).getGuardFor(true);
            Guard pc_38 = (temp_var_48).getGuardFor(false);
            boolean jumpedOut_6 = false;
            boolean jumpedOut_7 = false;
            if (!pc_37.isFalse()) {
                // 'then' branch
                PrimitiveDomainVS<Event> temp_var_49;
                temp_var_49 = new PrimitiveDomainVS<Event>(eUnit).restrict(pc_37);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_37, temp_var_49);
                
                // NOTE (TODO): We currently perform no typechecking on the payload!
                outcome.raiseGuardedEvent(pc_37, var_$tmp1.restrict(pc_37));
                pc_37 = Guard.constFalse();
                jumpedOut_6 = true;
                
            }
            if (!pc_38.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_6 || jumpedOut_7) {
                pc_36 = pc_37.or(pc_38);
            }
            
            if (!pc_36.isFalse()) {
            }
            return pc_36;
        }
        
        Guard 
        anonfun_15(
            Guard pc_39,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveDomainVS<Machine> var_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_39);
            
            PrimitiveDomainVS<Event> var_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_39);
            
            PrimitiveDomainVS<Event> var_$tmp2 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_39);
            
            PrimitiveDomainVS<Machine> temp_var_50;
            temp_var_50 = var_Driver.restrict(pc_39);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_39, temp_var_50);
            
            PrimitiveDomainVS<Event> temp_var_51;
            temp_var_51 = new PrimitiveDomainVS<Event>(eTimerFired).restrict(pc_39);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_39, temp_var_51);
            
            effects.send(pc_39, (var_$tmp0.restrict(pc_39)).toPrimitiveVS(), (var_$tmp1.restrict(pc_39)).toPrimitiveVS(), null);
            
            PrimitiveDomainVS<Event> temp_var_52;
            temp_var_52 = new PrimitiveDomainVS<Event>(eUnit).restrict(pc_39);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_39, temp_var_52);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_39, var_$tmp2.restrict(pc_39));
            pc_39 = Guard.constFalse();
            
            return pc_39;
        }
        
        Guard 
        anonfun_16(
            Guard pc_40,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveDomainVS<Boolean> var_$tmp0 =
                new PrimitiveDomainVS<Boolean>(false).restrict(pc_40);
            
            PrimitiveDomainVS<Machine> var_$tmp1 =
                new PrimitiveDomainVS<Machine>().restrict(pc_40);
            
            PrimitiveDomainVS<Event> var_$tmp2 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_40);
            
            PrimitiveDomainVS<Machine> var_$tmp3 =
                new PrimitiveDomainVS<Machine>().restrict(pc_40);
            
            PrimitiveDomainVS<Event> var_$tmp4 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_40);
            
            PrimitiveDomainVS<Machine> var_$tmp5 =
                new PrimitiveDomainVS<Machine>().restrict(pc_40);
            
            PrimitiveDomainVS<Event> var_$tmp6 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_40);
            
            PrimitiveDomainVS<Event> var_$tmp7 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_40);
            
            PrimitiveDomainVS<Boolean> temp_var_53;
            temp_var_53 = scheduler.getNextBoolean(pc_40).toPrimitiveDomainVS();
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_40, temp_var_53);
            
            PrimitiveDomainVS<Boolean> temp_var_54 = var_$tmp0.restrict(pc_40);
            Guard pc_41 = (temp_var_54).getGuardFor(true);
            Guard pc_42 = (temp_var_54).getGuardFor(false);
            boolean jumpedOut_8 = false;
            boolean jumpedOut_9 = false;
            if (!pc_41.isFalse()) {
                // 'then' branch
                PrimitiveDomainVS<Machine> temp_var_55;
                temp_var_55 = var_Driver.restrict(pc_41);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_41, temp_var_55);
                
                PrimitiveDomainVS<Event> temp_var_56;
                temp_var_56 = new PrimitiveDomainVS<Event>(eStoppingFailure).restrict(pc_41);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_41, temp_var_56);
                
                effects.send(pc_41, (var_$tmp1.restrict(pc_41)).toPrimitiveVS(), (var_$tmp2.restrict(pc_41)).toPrimitiveVS(), null);
                
                PrimitiveDomainVS<Machine> temp_var_57;
                temp_var_57 = var_Driver.restrict(pc_41);
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_41, temp_var_57);
                
                PrimitiveDomainVS<Event> temp_var_58;
                temp_var_58 = new PrimitiveDomainVS<Event>(eTimerFired).restrict(pc_41);
                var_$tmp4 = var_$tmp4.updateUnderGuard(pc_41, temp_var_58);
                
                effects.send(pc_41, (var_$tmp3.restrict(pc_41)).toPrimitiveVS(), (var_$tmp4.restrict(pc_41)).toPrimitiveVS(), null);
                
            }
            if (!pc_42.isFalse()) {
                // 'else' branch
                PrimitiveDomainVS<Machine> temp_var_59;
                temp_var_59 = var_Driver.restrict(pc_42);
                var_$tmp5 = var_$tmp5.updateUnderGuard(pc_42, temp_var_59);
                
                PrimitiveDomainVS<Event> temp_var_60;
                temp_var_60 = new PrimitiveDomainVS<Event>(eStoppingSuccess).restrict(pc_42);
                var_$tmp6 = var_$tmp6.updateUnderGuard(pc_42, temp_var_60);
                
                effects.send(pc_42, (var_$tmp5.restrict(pc_42)).toPrimitiveVS(), (var_$tmp6.restrict(pc_42)).toPrimitiveVS(), null);
                
            }
            if (jumpedOut_8 || jumpedOut_9) {
                pc_40 = pc_41.or(pc_42);
            }
            
            PrimitiveDomainVS<Event> temp_var_61;
            temp_var_61 = new PrimitiveDomainVS<Event>(eUnit).restrict(pc_40);
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_40, temp_var_61);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_40, var_$tmp7.restrict(pc_40));
            pc_40 = Guard.constFalse();
            
            return pc_40;
        }
        
    }
    
    public static class OSRDriver extends Machine {
        
        static State Driver_Init = new State("Driver_Init") {
            @Override public void entry(Guard pc_43, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((OSRDriver)machine).anonfun_17(pc_43, machine.sendBuffer, outcome);
            }
        };
        static State sDxDriver = new State("sDxDriver") {
            @Override public void entry(Guard pc_44, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((OSRDriver)machine).anonfun_18(pc_44, machine.sendBuffer);
            }
        };
        static State sCompleteD0EntryDriver = new State("sCompleteD0EntryDriver") {
            @Override public void entry(Guard pc_45, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((OSRDriver)machine).anonfun_19(pc_45, machine.sendBuffer, outcome);
            }
        };
        static State sWaitingForSwitchStatusChangeDriver = new State("sWaitingForSwitchStatusChangeDriver") {
            @Override public void entry(Guard pc_46, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((OSRDriver)machine).anonfun_20(pc_46, machine.sendBuffer);
            }
        };
        static State sCompletingD0ExitDriver = new State("sCompletingD0ExitDriver") {
            @Override public void entry(Guard pc_47, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((OSRDriver)machine).anonfun_21(pc_47, machine.sendBuffer, outcome);
            }
        };
        static State sStoringSwitchAndCheckingIfStateChangedDriver = new State("sStoringSwitchAndCheckingIfStateChangedDriver") {
            @Override public void entry(Guard pc_48, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((OSRDriver)machine).anonfun_22(pc_48, machine.sendBuffer, outcome);
            }
        };
        static State sUpdatingBarGraphStateDriver = new State("sUpdatingBarGraphStateDriver") {
            @Override public void entry(Guard pc_49, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((OSRDriver)machine).anonfun_23(pc_49, machine.sendBuffer);
            }
        };
        static State sUpdatingLedStateToUnstableDriver = new State("sUpdatingLedStateToUnstableDriver") {
            @Override public void entry(Guard pc_50, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((OSRDriver)machine).anonfun_24(pc_50, machine.sendBuffer);
            }
        };
        static State sWaitingForTimerDriver = new State("sWaitingForTimerDriver") {
            @Override public void entry(Guard pc_51, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((OSRDriver)machine).anonfun_25(pc_51, machine.sendBuffer);
            }
        };
        static State sUpdatingLedStateToStableDriver = new State("sUpdatingLedStateToStableDriver") {
            @Override public void entry(Guard pc_52, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((OSRDriver)machine).anonfun_26(pc_52, machine.sendBuffer);
            }
        };
        static State sStoppingTimerOnStatusChangeDriver = new State("sStoppingTimerOnStatusChangeDriver") {
            @Override public void entry(Guard pc_53, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((OSRDriver)machine).anonfun_27(pc_53, machine.sendBuffer, outcome);
            }
        };
        static State sStoppingTimerOnD0ExitDriver = new State("sStoppingTimerOnD0ExitDriver") {
            @Override public void entry(Guard pc_54, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((OSRDriver)machine).anonfun_28(pc_54, machine.sendBuffer, outcome);
            }
        };
        static State sStoppingTimerDriver = new State("sStoppingTimerDriver") {
            @Override public void entry(Guard pc_55, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((OSRDriver)machine).anonfun_29(pc_55, machine.sendBuffer);
            }
        };
        static State sWaitingForTimerToFlushDriver = new State("sWaitingForTimerToFlushDriver") {
            @Override public void entry(Guard pc_56, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((OSRDriver)machine).anonfun_30(pc_56, machine.sendBuffer);
            }
        };
        private PrimitiveDomainVS<Machine> var_TimerV = new PrimitiveDomainVS<Machine>();
        private PrimitiveDomainVS<Machine> var_LEDV = new PrimitiveDomainVS<Machine>();
        private PrimitiveDomainVS<Machine> var_SwitchV = new PrimitiveDomainVS<Machine>();
        private PrimitiveDomainVS<Boolean> var_check = new PrimitiveDomainVS<Boolean>(false);
        
        @Override
        public void reset() {
                super.reset();
                var_TimerV = new PrimitiveDomainVS<Machine>();
                var_LEDV = new PrimitiveDomainVS<Machine>();
                var_SwitchV = new PrimitiveDomainVS<Machine>();
                var_check = new PrimitiveDomainVS<Boolean>(false);
        }
        
        public OSRDriver(int id) {
            super("OSRDriver", id, EventBufferSemantics.queue, Driver_Init, Driver_Init
                , sDxDriver
                , sCompleteD0EntryDriver
                , sWaitingForSwitchStatusChangeDriver
                , sCompletingD0ExitDriver
                , sStoringSwitchAndCheckingIfStateChangedDriver
                , sUpdatingBarGraphStateDriver
                , sUpdatingLedStateToUnstableDriver
                , sWaitingForTimerDriver
                , sUpdatingLedStateToStableDriver
                , sStoppingTimerOnStatusChangeDriver
                , sStoppingTimerOnD0ExitDriver
                , sStoppingTimerDriver
                , sWaitingForTimerToFlushDriver
                
            );
            Driver_Init.addHandlers(new DeferEventHandler(eSwitchStatusChange)
                ,
                new GotoEventHandler(eUnit, sDxDriver));
            sDxDriver.addHandlers(new DeferEventHandler(eSwitchStatusChange)
                ,
                new IgnoreEventHandler(eD0Exit),
                new GotoEventHandler(eD0Entry, sCompleteD0EntryDriver));
            sCompleteD0EntryDriver.addHandlers(new DeferEventHandler(eSwitchStatusChange)
                ,
                new GotoEventHandler(eOperationSuccess, sWaitingForSwitchStatusChangeDriver));
            sWaitingForSwitchStatusChangeDriver.addHandlers(new IgnoreEventHandler(eD0Entry),
                new GotoEventHandler(eD0Exit, sCompletingD0ExitDriver),
                new GotoEventHandler(eSwitchStatusChange, sStoringSwitchAndCheckingIfStateChangedDriver));
            sCompletingD0ExitDriver.addHandlers(new GotoEventHandler(eOperationSuccess, sDxDriver));
            sStoringSwitchAndCheckingIfStateChangedDriver.addHandlers(new IgnoreEventHandler(eD0Entry),
                new GotoEventHandler(eYes, sUpdatingBarGraphStateDriver),
                new GotoEventHandler(eNo, sWaitingForTimerDriver));
            sUpdatingBarGraphStateDriver.addHandlers(new IgnoreEventHandler(eD0Entry),
                new DeferEventHandler(eD0Exit)
                ,
                new DeferEventHandler(eSwitchStatusChange)
                ,
                new GotoEventHandler(eTransferSuccess, sUpdatingLedStateToUnstableDriver),
                new GotoEventHandler(eTransferFailure, sUpdatingLedStateToUnstableDriver));
            sUpdatingLedStateToUnstableDriver.addHandlers(new DeferEventHandler(eD0Exit)
                ,
                new DeferEventHandler(eSwitchStatusChange)
                ,
                new IgnoreEventHandler(eD0Entry),
                new GotoEventHandler(eTransferSuccess, sWaitingForTimerDriver));
            sWaitingForTimerDriver.addHandlers(new IgnoreEventHandler(eD0Entry),
                new GotoEventHandler(eTimerFired, sUpdatingLedStateToStableDriver),
                new GotoEventHandler(eSwitchStatusChange, sStoppingTimerOnStatusChangeDriver),
                new GotoEventHandler(eD0Exit, sStoppingTimerOnD0ExitDriver));
            sUpdatingLedStateToStableDriver.addHandlers(new IgnoreEventHandler(eD0Entry),
                new DeferEventHandler(eD0Exit)
                ,
                new DeferEventHandler(eSwitchStatusChange)
                ,
                new GotoEventHandler(eTransferSuccess, sWaitingForSwitchStatusChangeDriver));
            sStoppingTimerOnStatusChangeDriver.addHandlers(new IgnoreEventHandler(eD0Entry),
                new DeferEventHandler(eD0Exit)
                ,
                new DeferEventHandler(eSwitchStatusChange)
                ,
                new GotoEventHandler(eTimerStopped, sStoringSwitchAndCheckingIfStateChangedDriver));
            sStoppingTimerOnD0ExitDriver.addHandlers(new DeferEventHandler(eD0Exit)
                ,
                new DeferEventHandler(eSwitchStatusChange)
                ,
                new IgnoreEventHandler(eD0Entry),
                new GotoEventHandler(eTimerStopped, sCompletingD0ExitDriver));
            sStoppingTimerDriver.addHandlers(new IgnoreEventHandler(eD0Entry),
                new DeferEventHandler(eSwitchStatusChange)
                ,
                new GotoEventHandler(eStoppingSuccess, sCompletingD0ExitDriver),
                new GotoEventHandler(eStoppingFailure, sWaitingForTimerToFlushDriver),
                new GotoEventHandler(eTimerFired, sCompletingD0ExitDriver));
            sWaitingForTimerToFlushDriver.addHandlers(new DeferEventHandler(eD0Exit)
                ,
                new DeferEventHandler(eSwitchStatusChange)
                ,
                new IgnoreEventHandler(eD0Entry),
                new GotoEventHandler(eTimerFired, sCompletingD0ExitDriver));
        }
        
        Guard 
        anonfun_17(
            Guard pc_57,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveDomainVS<Machine> var_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_57);
            
            PrimitiveDomainVS<Machine> var_$tmp1 =
                new PrimitiveDomainVS<Machine>().restrict(pc_57);
            
            PrimitiveDomainVS<Machine> var_$tmp2 =
                new PrimitiveDomainVS<Machine>().restrict(pc_57);
            
            PrimitiveDomainVS<Machine> var_$tmp3 =
                new PrimitiveDomainVS<Machine>().restrict(pc_57);
            
            PrimitiveDomainVS<Machine> var_$tmp4 =
                new PrimitiveDomainVS<Machine>().restrict(pc_57);
            
            PrimitiveDomainVS<Machine> var_$tmp5 =
                new PrimitiveDomainVS<Machine>().restrict(pc_57);
            
            PrimitiveDomainVS<Event> var_$tmp6 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_57);
            
            PrimitiveDomainVS<Machine> temp_var_62;
            temp_var_62 = new PrimitiveDomainVS<Machine>(this).restrict(pc_57);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_57, temp_var_62);
            
            PrimitiveDomainVS<Machine> temp_var_63;
            temp_var_63 = effects.create(pc_57, scheduler, Timer.class, new UnionVS (var_$tmp0.restrict(pc_57)), (i) -> new Timer(i)).toPrimitiveDomainVS();
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_57, temp_var_63);
            
            PrimitiveDomainVS<Machine> temp_var_64;
            temp_var_64 = var_$tmp1.restrict(pc_57);
            var_TimerV = var_TimerV.updateUnderGuard(pc_57, temp_var_64);
            
            PrimitiveDomainVS<Machine> temp_var_65;
            temp_var_65 = new PrimitiveDomainVS<Machine>(this).restrict(pc_57);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_57, temp_var_65);
            
            PrimitiveDomainVS<Machine> temp_var_66;
            temp_var_66 = effects.create(pc_57, scheduler, LED.class, new UnionVS (var_$tmp2.restrict(pc_57)), (i) -> new LED(i)).toPrimitiveDomainVS();
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_57, temp_var_66);
            
            PrimitiveDomainVS<Machine> temp_var_67;
            temp_var_67 = var_$tmp3.restrict(pc_57);
            var_LEDV = var_LEDV.updateUnderGuard(pc_57, temp_var_67);
            
            PrimitiveDomainVS<Machine> temp_var_68;
            temp_var_68 = new PrimitiveDomainVS<Machine>(this).restrict(pc_57);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_57, temp_var_68);
            
            PrimitiveDomainVS<Machine> temp_var_69;
            temp_var_69 = effects.create(pc_57, scheduler, Switch.class, new UnionVS (var_$tmp4.restrict(pc_57)), (i) -> new Switch(i)).toPrimitiveDomainVS();
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_57, temp_var_69);
            
            PrimitiveDomainVS<Machine> temp_var_70;
            temp_var_70 = var_$tmp5.restrict(pc_57);
            var_SwitchV = var_SwitchV.updateUnderGuard(pc_57, temp_var_70);
            
            PrimitiveDomainVS<Event> temp_var_71;
            temp_var_71 = new PrimitiveDomainVS<Event>(eUnit).restrict(pc_57);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_57, temp_var_71);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_57, var_$tmp6.restrict(pc_57));
            pc_57 = Guard.constFalse();
            
            return pc_57;
        }
        
        void 
        anonfun_18(
            Guard pc_58,
            EventBuffer effects
        ) {
        }
        
        Guard 
        anonfun_19(
            Guard pc_59,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveDomainVS<Event> var_$tmp0 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_59);
            
            PrimitiveDomainVS<Event> temp_var_72;
            temp_var_72 = new PrimitiveDomainVS<Event>(eOperationSuccess).restrict(pc_59);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_59, temp_var_72);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_59, var_$tmp0.restrict(pc_59));
            pc_59 = Guard.constFalse();
            
            return pc_59;
        }
        
        void 
        CompleteDStateTransition(
            Guard pc_60,
            EventBuffer effects
        ) {
        }
        
        void 
        anonfun_20(
            Guard pc_61,
            EventBuffer effects
        ) {
        }
        
        Guard 
        anonfun_21(
            Guard pc_62,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveDomainVS<Event> var_$tmp0 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_62);
            
            PrimitiveDomainVS<Event> temp_var_73;
            temp_var_73 = new PrimitiveDomainVS<Event>(eOperationSuccess).restrict(pc_62);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_62, temp_var_73);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_62, var_$tmp0.restrict(pc_62));
            pc_62 = Guard.constFalse();
            
            return pc_62;
        }
        
        void 
        StoreSwitchAndEnableSwitchStatusChange(
            Guard pc_63,
            EventBuffer effects
        ) {
        }
        
        PrimitiveDomainVS<Boolean> 
        CheckIfSwitchStatusChanged(
            Guard pc_64,
            EventBuffer effects
        ) {
            PrimitiveDomainVS<Boolean> var_$tmp0 =
                new PrimitiveDomainVS<Boolean>(false).restrict(pc_64);
            
            PrimitiveDomainVS<Boolean> retval = new PrimitiveDomainVS<Boolean>(new PrimitiveDomainVS<Boolean>(false).restrict(pc_64));
            PrimitiveDomainVS<Boolean> temp_var_74;
            temp_var_74 = scheduler.getNextBoolean(pc_64).toPrimitiveDomainVS();
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_64, temp_var_74);
            
            PrimitiveDomainVS<Boolean> temp_var_75 = var_$tmp0.restrict(pc_64);
            Guard pc_65 = (temp_var_75).getGuardFor(true);
            Guard pc_66 = (temp_var_75).getGuardFor(false);
            boolean jumpedOut_10 = false;
            boolean jumpedOut_11 = false;
            if (!pc_65.isFalse()) {
                // 'then' branch
                retval = retval.updateUnderGuard(pc_65, new PrimitiveDomainVS<Boolean>(true).restrict(pc_65));
                pc_65 = Guard.constFalse();
                jumpedOut_10 = true;
                
            }
            if (!pc_66.isFalse()) {
                // 'else' branch
                retval = retval.updateUnderGuard(pc_66, new PrimitiveDomainVS<Boolean>(false).restrict(pc_66));
                pc_66 = Guard.constFalse();
                jumpedOut_11 = true;
                
            }
            if (jumpedOut_10 || jumpedOut_11) {
                pc_64 = pc_65.or(pc_66);
            }
            
            return retval;
        }
        
        void 
        UpdateBarGraphStateUsingControlTransfer(
            Guard pc_67,
            EventBuffer effects
        ) {
            PrimitiveDomainVS<Machine> var_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_67);
            
            PrimitiveDomainVS<Event> var_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_67);
            
            PrimitiveDomainVS<Machine> temp_var_76;
            temp_var_76 = var_LEDV.restrict(pc_67);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_67, temp_var_76);
            
            PrimitiveDomainVS<Event> temp_var_77;
            temp_var_77 = new PrimitiveDomainVS<Event>(eUpdateBarGraphStateUsingControlTransfer).restrict(pc_67);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_67, temp_var_77);
            
            effects.send(pc_67, (var_$tmp0.restrict(pc_67)).toPrimitiveVS(), (var_$tmp1.restrict(pc_67)).toPrimitiveVS(), null);
            
        }
        
        void 
        SetLedStateToStableUsingControlTransfer(
            Guard pc_68,
            EventBuffer effects
        ) {
            PrimitiveDomainVS<Machine> var_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_68);
            
            PrimitiveDomainVS<Event> var_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_68);
            
            PrimitiveDomainVS<Machine> temp_var_78;
            temp_var_78 = var_LEDV.restrict(pc_68);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_68, temp_var_78);
            
            PrimitiveDomainVS<Event> temp_var_79;
            temp_var_79 = new PrimitiveDomainVS<Event>(eSetLedStateToStableUsingControlTransfer).restrict(pc_68);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_68, temp_var_79);
            
            effects.send(pc_68, (var_$tmp0.restrict(pc_68)).toPrimitiveVS(), (var_$tmp1.restrict(pc_68)).toPrimitiveVS(), null);
            
        }
        
        void 
        SetLedStateToUnstableUsingControlTransfer(
            Guard pc_69,
            EventBuffer effects
        ) {
            PrimitiveDomainVS<Machine> var_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_69);
            
            PrimitiveDomainVS<Event> var_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_69);
            
            PrimitiveDomainVS<Machine> temp_var_80;
            temp_var_80 = var_LEDV.restrict(pc_69);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_69, temp_var_80);
            
            PrimitiveDomainVS<Event> temp_var_81;
            temp_var_81 = new PrimitiveDomainVS<Event>(eSetLedStateToUnstableUsingControlTransfer).restrict(pc_69);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_69, temp_var_81);
            
            effects.send(pc_69, (var_$tmp0.restrict(pc_69)).toPrimitiveVS(), (var_$tmp1.restrict(pc_69)).toPrimitiveVS(), null);
            
        }
        
        void 
        StartDebounceTimer(
            Guard pc_70,
            EventBuffer effects
        ) {
            PrimitiveDomainVS<Machine> var_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_70);
            
            PrimitiveDomainVS<Event> var_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_70);
            
            PrimitiveDomainVS<Machine> temp_var_82;
            temp_var_82 = var_TimerV.restrict(pc_70);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_70, temp_var_82);
            
            PrimitiveDomainVS<Event> temp_var_83;
            temp_var_83 = new PrimitiveDomainVS<Event>(eStartDebounceTimer).restrict(pc_70);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_70, temp_var_83);
            
            effects.send(pc_70, (var_$tmp0.restrict(pc_70)).toPrimitiveVS(), (var_$tmp1.restrict(pc_70)).toPrimitiveVS(), null);
            
        }
        
        Guard 
        anonfun_22(
            Guard pc_71,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveDomainVS<Boolean> var_$tmp0 =
                new PrimitiveDomainVS<Boolean>(false).restrict(pc_71);
            
            PrimitiveDomainVS<Event> var_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_71);
            
            PrimitiveDomainVS<Event> var_$tmp2 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_71);
            
            PrimitiveDomainVS<Boolean> var_local_3_$tmp0 =
                new PrimitiveDomainVS<Boolean>(false).restrict(pc_71);
            
            PrimitiveDomainVS<Boolean> temp_var_84;
            temp_var_84 = scheduler.getNextBoolean(pc_71).toPrimitiveDomainVS();
            var_local_3_$tmp0 = var_local_3_$tmp0.updateUnderGuard(pc_71, temp_var_84);
            
            PrimitiveDomainVS<Boolean> temp_var_85 = var_local_3_$tmp0.restrict(pc_71);
            Guard pc_72 = (temp_var_85).getGuardFor(true);
            Guard pc_73 = (temp_var_85).getGuardFor(false);
            boolean jumpedOut_12 = false;
            boolean jumpedOut_13 = false;
            if (!pc_72.isFalse()) {
                // 'then' branch
                PrimitiveDomainVS<Boolean> temp_var_86;
                temp_var_86 = new PrimitiveDomainVS<Boolean>(true).restrict(pc_72);
                var_$tmp0 = var_$tmp0.updateUnderGuard(pc_72, temp_var_86);
                
            }
            if (!pc_73.isFalse()) {
                // 'else' branch
                PrimitiveDomainVS<Boolean> temp_var_87;
                temp_var_87 = new PrimitiveDomainVS<Boolean>(false).restrict(pc_73);
                var_$tmp0 = var_$tmp0.updateUnderGuard(pc_73, temp_var_87);
                
            }
            if (jumpedOut_12 || jumpedOut_13) {
                pc_71 = pc_72.or(pc_73);
            }
            
            PrimitiveDomainVS<Boolean> temp_var_88;
            temp_var_88 = var_$tmp0.restrict(pc_71);
            var_check = var_check.updateUnderGuard(pc_71, temp_var_88);
            
            PrimitiveDomainVS<Boolean> temp_var_89 = var_check.restrict(pc_71);
            Guard pc_74 = (temp_var_89).getGuardFor(true);
            Guard pc_75 = (temp_var_89).getGuardFor(false);
            boolean jumpedOut_14 = false;
            boolean jumpedOut_15 = false;
            if (!pc_74.isFalse()) {
                // 'then' branch
                PrimitiveDomainVS<Event> temp_var_90;
                temp_var_90 = new PrimitiveDomainVS<Event>(eYes).restrict(pc_74);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_74, temp_var_90);
                
                // NOTE (TODO): We currently perform no typechecking on the payload!
                outcome.raiseGuardedEvent(pc_74, var_$tmp1.restrict(pc_74));
                pc_74 = Guard.constFalse();
                jumpedOut_14 = true;
                
            }
            if (!pc_75.isFalse()) {
                // 'else' branch
                PrimitiveDomainVS<Event> temp_var_91;
                temp_var_91 = new PrimitiveDomainVS<Event>(eNo).restrict(pc_75);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_75, temp_var_91);
                
                // NOTE (TODO): We currently perform no typechecking on the payload!
                outcome.raiseGuardedEvent(pc_75, var_$tmp2.restrict(pc_75));
                pc_75 = Guard.constFalse();
                jumpedOut_15 = true;
                
            }
            if (jumpedOut_14 || jumpedOut_15) {
                pc_71 = pc_74.or(pc_75);
            }
            
            return pc_71;
        }
        
        void 
        anonfun_23(
            Guard pc_76,
            EventBuffer effects
        ) {
            PrimitiveDomainVS<Machine> var_local_4_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_76);
            
            PrimitiveDomainVS<Event> var_local_4_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_76);
            
            PrimitiveDomainVS<Machine> temp_var_92;
            temp_var_92 = var_LEDV.restrict(pc_76);
            var_local_4_$tmp0 = var_local_4_$tmp0.updateUnderGuard(pc_76, temp_var_92);
            
            PrimitiveDomainVS<Event> temp_var_93;
            temp_var_93 = new PrimitiveDomainVS<Event>(eUpdateBarGraphStateUsingControlTransfer).restrict(pc_76);
            var_local_4_$tmp1 = var_local_4_$tmp1.updateUnderGuard(pc_76, temp_var_93);
            
            effects.send(pc_76, (var_local_4_$tmp0.restrict(pc_76)).toPrimitiveVS(), (var_local_4_$tmp1.restrict(pc_76)).toPrimitiveVS(), null);
            
        }
        
        void 
        anonfun_24(
            Guard pc_77,
            EventBuffer effects
        ) {
            PrimitiveDomainVS<Machine> var_local_5_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_77);
            
            PrimitiveDomainVS<Event> var_local_5_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_77);
            
            PrimitiveDomainVS<Machine> temp_var_94;
            temp_var_94 = var_LEDV.restrict(pc_77);
            var_local_5_$tmp0 = var_local_5_$tmp0.updateUnderGuard(pc_77, temp_var_94);
            
            PrimitiveDomainVS<Event> temp_var_95;
            temp_var_95 = new PrimitiveDomainVS<Event>(eSetLedStateToUnstableUsingControlTransfer).restrict(pc_77);
            var_local_5_$tmp1 = var_local_5_$tmp1.updateUnderGuard(pc_77, temp_var_95);
            
            effects.send(pc_77, (var_local_5_$tmp0.restrict(pc_77)).toPrimitiveVS(), (var_local_5_$tmp1.restrict(pc_77)).toPrimitiveVS(), null);
            
        }
        
        void 
        anonfun_25(
            Guard pc_78,
            EventBuffer effects
        ) {
            PrimitiveDomainVS<Machine> var_local_6_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_78);
            
            PrimitiveDomainVS<Event> var_local_6_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_78);
            
            PrimitiveDomainVS<Machine> temp_var_96;
            temp_var_96 = var_TimerV.restrict(pc_78);
            var_local_6_$tmp0 = var_local_6_$tmp0.updateUnderGuard(pc_78, temp_var_96);
            
            PrimitiveDomainVS<Event> temp_var_97;
            temp_var_97 = new PrimitiveDomainVS<Event>(eStartDebounceTimer).restrict(pc_78);
            var_local_6_$tmp1 = var_local_6_$tmp1.updateUnderGuard(pc_78, temp_var_97);
            
            effects.send(pc_78, (var_local_6_$tmp0.restrict(pc_78)).toPrimitiveVS(), (var_local_6_$tmp1.restrict(pc_78)).toPrimitiveVS(), null);
            
        }
        
        void 
        anonfun_26(
            Guard pc_79,
            EventBuffer effects
        ) {
            PrimitiveDomainVS<Machine> var_local_7_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_79);
            
            PrimitiveDomainVS<Event> var_local_7_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_79);
            
            PrimitiveDomainVS<Machine> temp_var_98;
            temp_var_98 = var_LEDV.restrict(pc_79);
            var_local_7_$tmp0 = var_local_7_$tmp0.updateUnderGuard(pc_79, temp_var_98);
            
            PrimitiveDomainVS<Event> temp_var_99;
            temp_var_99 = new PrimitiveDomainVS<Event>(eSetLedStateToStableUsingControlTransfer).restrict(pc_79);
            var_local_7_$tmp1 = var_local_7_$tmp1.updateUnderGuard(pc_79, temp_var_99);
            
            effects.send(pc_79, (var_local_7_$tmp0.restrict(pc_79)).toPrimitiveVS(), (var_local_7_$tmp1.restrict(pc_79)).toPrimitiveVS(), null);
            
        }
        
        Guard 
        anonfun_27(
            Guard pc_80,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            outcome.addGuardedGoto(pc_80, sStoppingTimerDriver);
            pc_80 = Guard.constFalse();
            
            return pc_80;
        }
        
        Guard 
        anonfun_28(
            Guard pc_81,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            outcome.addGuardedGoto(pc_81, sStoppingTimerDriver);
            pc_81 = Guard.constFalse();
            
            return pc_81;
        }
        
        void 
        anonfun_29(
            Guard pc_82,
            EventBuffer effects
        ) {
            PrimitiveDomainVS<Machine> var_$tmp0 =
                new PrimitiveDomainVS<Machine>().restrict(pc_82);
            
            PrimitiveDomainVS<Event> var_$tmp1 =
                new PrimitiveDomainVS<Event>(_null).restrict(pc_82);
            
            PrimitiveDomainVS<Machine> temp_var_100;
            temp_var_100 = var_TimerV.restrict(pc_82);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_82, temp_var_100);
            
            PrimitiveDomainVS<Event> temp_var_101;
            temp_var_101 = new PrimitiveDomainVS<Event>(eStopTimer).restrict(pc_82);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_82, temp_var_101);
            
            effects.send(pc_82, (var_$tmp0.restrict(pc_82)).toPrimitiveVS(), (var_$tmp1.restrict(pc_82)).toPrimitiveVS(), null);
            
        }
        
        void 
        anonfun_30(
            Guard pc_83,
            EventBuffer effects
        ) {
        }
        
    }
    
    // Skipping Implementation 'DefaultImpl'

    Map<Event, List<Monitor>> listeners = new HashMap<>();
    List<Monitor> monitors = new ArrayList<>();
    private boolean listenersInitialized = false;
    
    public Map<Event, List<Monitor>> getMonitorMap() {
            if (listenersInitialized) return listeners;
            listenersInitialized = true;
            return listeners;
    }
    
    
    public List<Monitor> getMonitorList() {
            if (!listenersInitialized) getMonitorMap();
            return monitors;
    }
    private static Machine start = new Main(0);
    
    @Override
    public Machine getStart() { return start; }
    
}
