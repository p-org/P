import symbolicp.*;
import symbolicp.bdd.*;
import symbolicp.vs.*;
import symbolicp.runtime.*;
import symbolicp.run.*;

public class tokenring implements Program {
    
    public static Scheduler scheduler;
    
    @Override
    public void setScheduler (Scheduler s) { this.scheduler = s; }
    
    
    static enum Events implements EventName {
        event_null,
        event_halt,
        event_Empty,
        event_Sending,
        event_Done,
        event_Unit,
        event_Next,
        event_Send,
        event_Ready,
    }
    
    // Skipping Interface 'Node'

    // Skipping Interface 'Main'

    public static class machine_Node extends Machine {
        
        static State state_Init_Main_Node = new State("state_Init_Main_Node", 0) {
            @Override public void entry(Bdd pc_0, Machine machine, Outcome outcome, UnionVS payload) {
                ((machine_Node)machine).anonfunc_0(pc_0, machine.sendEffects, payload != null ? (PrimVS<Machine>) ValueSummary.fromAny(pc_0, new PrimVS<Machine>().guard(pc_0).getClass(), payload) : new PrimVS<Machine>().guard(pc_0));
            }
        };
        static State state_Wait_Main_Node = new State("state_Wait_Main_Node", 1) {
        };
        static State state_SetNext_Main_Node = new State("state_SetNext_Main_Node", 2) {
            @Override public void entry(Bdd pc_1, Machine machine, Outcome outcome, UnionVS payload) {
                ((machine_Node)machine).anonfunc_1(pc_1, machine.sendEffects, outcome, payload != null ? (PrimVS<Machine>) ValueSummary.fromAny(pc_1, new PrimVS<Machine>().guard(pc_1).getClass(), payload) : new PrimVS<Machine>().guard(pc_1));
            }
        };
        static State state_SendEmpty_Main_Node = new State("state_SendEmpty_Main_Node", 3) {
            @Override public void entry(Bdd pc_2, Machine machine, Outcome outcome, UnionVS payload) {
                ((machine_Node)machine).anonfunc_2(pc_2, machine.sendEffects, outcome);
            }
        };
        static State state_StartSending_Main_Node = new State("state_StartSending_Main_Node", 4) {
            @Override public void entry(Bdd pc_3, Machine machine, Outcome outcome, UnionVS payload) {
                ((machine_Node)machine).anonfunc_3(pc_3, machine.sendEffects, outcome, payload != null ? (PrimVS<Machine>) ValueSummary.fromAny(pc_3, new PrimVS<Machine>().guard(pc_3).getClass(), payload) : new PrimVS<Machine>().guard(pc_3));
            }
        };
        static State state_KeepSending_Main_Node = new State("state_KeepSending_Main_Node", 5) {
            @Override public void entry(Bdd pc_4, Machine machine, Outcome outcome, UnionVS payload) {
                ((machine_Node)machine).anonfunc_4(pc_4, machine.sendEffects, outcome, payload != null ? (PrimVS<Machine>) ValueSummary.fromAny(pc_4, new PrimVS<Machine>().guard(pc_4).getClass(), payload) : new PrimVS<Machine>().guard(pc_4));
            }
        };
        static State state_StopSending_Main_Node = new State("state_StopSending_Main_Node", 6) {
            @Override public void entry(Bdd pc_5, Machine machine, Outcome outcome, UnionVS payload) {
                ((machine_Node)machine).anonfunc_5(pc_5, machine.sendEffects, outcome, payload != null ? (PrimVS<Machine>) ValueSummary.fromAny(pc_5, new PrimVS<Machine>().guard(pc_5).getClass(), payload) : new PrimVS<Machine>().guard(pc_5));
            }
        };
        private PrimVS<Machine> var_NextMachine = new PrimVS<Machine>();
        private PrimVS<Machine> var_MyRing = new PrimVS<Machine>();
        
        @Override
        public void reset() {
                super.reset();
                var_NextMachine = new PrimVS<Machine>();
                var_MyRing = new PrimVS<Machine>();
        }
        
        public machine_Node(int id) {
            super("machine_Node", id, BufferSemantics.queue, state_Init_Main_Node, state_Init_Main_Node
                , state_Wait_Main_Node
                , state_SetNext_Main_Node
                , state_SendEmpty_Main_Node
                , state_StartSending_Main_Node
                , state_KeepSending_Main_Node
                , state_StopSending_Main_Node
                
            );
            state_Init_Main_Node.addHandlers(new GotoEventHandler(Events.event_Next, state_SetNext_Main_Node));
            state_Wait_Main_Node.addHandlers(new GotoEventHandler(Events.event_Empty, state_SendEmpty_Main_Node),
                new GotoEventHandler(Events.event_Send, state_StartSending_Main_Node),
                new GotoEventHandler(Events.event_Sending, state_KeepSending_Main_Node),
                new GotoEventHandler(Events.event_Done, state_StopSending_Main_Node));
            state_SetNext_Main_Node.addHandlers(new GotoEventHandler(Events.event_Unit, state_Wait_Main_Node));
            state_SendEmpty_Main_Node.addHandlers(new GotoEventHandler(Events.event_Unit, state_Wait_Main_Node));
            state_StartSending_Main_Node.addHandlers(new GotoEventHandler(Events.event_Unit, state_Wait_Main_Node));
            state_KeepSending_Main_Node.addHandlers(new GotoEventHandler(Events.event_Unit, state_Wait_Main_Node));
            state_StopSending_Main_Node.addHandlers(new GotoEventHandler(Events.event_Unit, state_Wait_Main_Node));
        }
        
        void 
        anonfunc_0(
            Bdd pc_6,
            EffectCollection effects,
            PrimVS<Machine> var_payload
        ) {
            PrimVS<Machine> temp_var_0;
            temp_var_0 = var_payload.guard(pc_6);
            var_MyRing = var_MyRing.update(pc_6, temp_var_0);
            
        }
        
        Bdd 
        anonfunc_1(
            Bdd pc_7,
            EffectCollection effects,
            Outcome outcome,
            PrimVS<Machine> var_payload
        ) {
            PrimVS<Machine> var_$tmp0 =
                new PrimVS<Machine>().guard(pc_7);
            
            PrimVS<EventName> var_$tmp1 =
                new PrimVS<EventName>(Events.event_null).guard(pc_7);
            
            PrimVS<EventName> var_$tmp2 =
                new PrimVS<EventName>(Events.event_null).guard(pc_7);
            
            PrimVS<Machine> temp_var_1;
            temp_var_1 = var_payload.guard(pc_7);
            var_NextMachine = var_NextMachine.update(pc_7, temp_var_1);
            
            PrimVS<Machine> temp_var_2;
            temp_var_2 = var_MyRing.guard(pc_7);
            var_$tmp0 = var_$tmp0.update(pc_7, temp_var_2);
            
            PrimVS<EventName> temp_var_3;
            temp_var_3 = new PrimVS<EventName>(Events.event_Ready).guard(pc_7);
            var_$tmp1 = var_$tmp1.update(pc_7, temp_var_3);
            
            effects.send(pc_7, var_$tmp0.guard(pc_7), var_$tmp1.guard(pc_7), null);
            
            PrimVS<EventName> temp_var_4;
            temp_var_4 = new PrimVS<EventName>(Events.event_Unit).guard(pc_7);
            var_$tmp2 = var_$tmp2.update(pc_7, temp_var_4);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.addGuardedRaise(pc_7, var_$tmp2.guard(pc_7));
            pc_7 = Bdd.constFalse();
            
            return pc_7;
        }
        
        Bdd 
        anonfunc_2(
            Bdd pc_8,
            EffectCollection effects,
            Outcome outcome
        ) {
            PrimVS<Machine> var_$tmp0 =
                new PrimVS<Machine>().guard(pc_8);
            
            PrimVS<EventName> var_$tmp1 =
                new PrimVS<EventName>(Events.event_null).guard(pc_8);
            
            PrimVS<EventName> var_$tmp2 =
                new PrimVS<EventName>(Events.event_null).guard(pc_8);
            
            PrimVS<Machine> temp_var_5;
            temp_var_5 = var_NextMachine.guard(pc_8);
            var_$tmp0 = var_$tmp0.update(pc_8, temp_var_5);
            
            PrimVS<EventName> temp_var_6;
            temp_var_6 = new PrimVS<EventName>(Events.event_Empty).guard(pc_8);
            var_$tmp1 = var_$tmp1.update(pc_8, temp_var_6);
            
            effects.send(pc_8, var_$tmp0.guard(pc_8), var_$tmp1.guard(pc_8), null);
            
            PrimVS<EventName> temp_var_7;
            temp_var_7 = new PrimVS<EventName>(Events.event_Unit).guard(pc_8);
            var_$tmp2 = var_$tmp2.update(pc_8, temp_var_7);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.addGuardedRaise(pc_8, var_$tmp2.guard(pc_8));
            pc_8 = Bdd.constFalse();
            
            return pc_8;
        }
        
        Bdd 
        anonfunc_3(
            Bdd pc_9,
            EffectCollection effects,
            Outcome outcome,
            PrimVS<Machine> var_payload
        ) {
            PrimVS<Machine> var_$tmp0 =
                new PrimVS<Machine>().guard(pc_9);
            
            PrimVS<EventName> var_$tmp1 =
                new PrimVS<EventName>(Events.event_null).guard(pc_9);
            
            PrimVS<Machine> var_$tmp2 =
                new PrimVS<Machine>().guard(pc_9);
            
            PrimVS<EventName> var_$tmp3 =
                new PrimVS<EventName>(Events.event_null).guard(pc_9);
            
            PrimVS<Machine> temp_var_8;
            temp_var_8 = var_NextMachine.guard(pc_9);
            var_$tmp0 = var_$tmp0.update(pc_9, temp_var_8);
            
            PrimVS<EventName> temp_var_9;
            temp_var_9 = new PrimVS<EventName>(Events.event_Sending).guard(pc_9);
            var_$tmp1 = var_$tmp1.update(pc_9, temp_var_9);
            
            PrimVS<Machine> temp_var_10;
            temp_var_10 = var_payload.guard(pc_9);
            var_$tmp2 = var_$tmp2.update(pc_9, temp_var_10);
            
            effects.send(pc_9, var_$tmp0.guard(pc_9), var_$tmp1.guard(pc_9), new UnionVS(var_$tmp2.guard(pc_9)));
            
            PrimVS<EventName> temp_var_11;
            temp_var_11 = new PrimVS<EventName>(Events.event_Unit).guard(pc_9);
            var_$tmp3 = var_$tmp3.update(pc_9, temp_var_11);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.addGuardedRaise(pc_9, var_$tmp3.guard(pc_9));
            pc_9 = Bdd.constFalse();
            
            return pc_9;
        }
        
        Bdd 
        anonfunc_4(
            Bdd pc_10,
            EffectCollection effects,
            Outcome outcome,
            PrimVS<Machine> var_payload
        ) {
            PrimVS<Boolean> var_$tmp0 =
                new PrimVS<Boolean>(false).guard(pc_10);
            
            PrimVS<Machine> var_$tmp1 =
                new PrimVS<Machine>().guard(pc_10);
            
            PrimVS<EventName> var_$tmp2 =
                new PrimVS<EventName>(Events.event_null).guard(pc_10);
            
            PrimVS<Machine> var_$tmp3 =
                new PrimVS<Machine>().guard(pc_10);
            
            PrimVS<Machine> var_$tmp4 =
                new PrimVS<Machine>().guard(pc_10);
            
            PrimVS<EventName> var_$tmp5 =
                new PrimVS<EventName>(Events.event_null).guard(pc_10);
            
            PrimVS<Machine> var_$tmp6 =
                new PrimVS<Machine>().guard(pc_10);
            
            PrimVS<EventName> var_$tmp7 =
                new PrimVS<EventName>(Events.event_null).guard(pc_10);
            
            PrimVS<Boolean> temp_var_12;
            temp_var_12 = (var_payload.guard(pc_10)).apply2(new PrimVS<Machine>(this).guard(pc_10), (temp_var_13, temp_var_14) -> temp_var_13.equals(temp_var_14));
            var_$tmp0 = var_$tmp0.update(pc_10, temp_var_12);
            
            PrimVS<Boolean> temp_var_15 = var_$tmp0.guard(pc_10);
            Bdd pc_11 = BoolUtils.trueCond(temp_var_15);
            Bdd pc_12 = BoolUtils.falseCond(temp_var_15);
            boolean jumpedOut_0 = false;
            boolean jumpedOut_1 = false;
            if (!pc_11.isConstFalse()) {
                // 'then' branch
                PrimVS<Machine> temp_var_16;
                temp_var_16 = var_NextMachine.guard(pc_11);
                var_$tmp1 = var_$tmp1.update(pc_11, temp_var_16);
                
                PrimVS<EventName> temp_var_17;
                temp_var_17 = new PrimVS<EventName>(Events.event_Done).guard(pc_11);
                var_$tmp2 = var_$tmp2.update(pc_11, temp_var_17);
                
                PrimVS<Machine> temp_var_18;
                temp_var_18 = new PrimVS<Machine>(this).guard(pc_11);
                var_$tmp3 = var_$tmp3.update(pc_11, temp_var_18);
                
                effects.send(pc_11, var_$tmp1.guard(pc_11), var_$tmp2.guard(pc_11), new UnionVS(var_$tmp3.guard(pc_11)));
                
            }
            if (!pc_12.isConstFalse()) {
                // 'else' branch
                PrimVS<Machine> temp_var_19;
                temp_var_19 = var_NextMachine.guard(pc_12);
                var_$tmp4 = var_$tmp4.update(pc_12, temp_var_19);
                
                PrimVS<EventName> temp_var_20;
                temp_var_20 = new PrimVS<EventName>(Events.event_Sending).guard(pc_12);
                var_$tmp5 = var_$tmp5.update(pc_12, temp_var_20);
                
                PrimVS<Machine> temp_var_21;
                temp_var_21 = var_payload.guard(pc_12);
                var_$tmp6 = var_$tmp6.update(pc_12, temp_var_21);
                
                effects.send(pc_12, var_$tmp4.guard(pc_12), var_$tmp5.guard(pc_12), new UnionVS(var_$tmp6.guard(pc_12)));
                
            }
            if (jumpedOut_0 || jumpedOut_1) {
                pc_10 = pc_11.or(pc_12);
            }
            
            PrimVS<EventName> temp_var_22;
            temp_var_22 = new PrimVS<EventName>(Events.event_Unit).guard(pc_10);
            var_$tmp7 = var_$tmp7.update(pc_10, temp_var_22);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.addGuardedRaise(pc_10, var_$tmp7.guard(pc_10));
            pc_10 = Bdd.constFalse();
            
            return pc_10;
        }
        
        Bdd 
        anonfunc_5(
            Bdd pc_13,
            EffectCollection effects,
            Outcome outcome,
            PrimVS<Machine> var_payload
        ) {
            PrimVS<Boolean> var_$tmp0 =
                new PrimVS<Boolean>(false).guard(pc_13);
            
            PrimVS<Machine> var_$tmp1 =
                new PrimVS<Machine>().guard(pc_13);
            
            PrimVS<EventName> var_$tmp2 =
                new PrimVS<EventName>(Events.event_null).guard(pc_13);
            
            PrimVS<Machine> var_$tmp3 =
                new PrimVS<Machine>().guard(pc_13);
            
            PrimVS<EventName> var_$tmp4 =
                new PrimVS<EventName>(Events.event_null).guard(pc_13);
            
            PrimVS<Boolean> temp_var_23;
            temp_var_23 = (var_payload.guard(pc_13)).apply2(new PrimVS<Machine>(this).guard(pc_13), (temp_var_24, temp_var_25) -> !temp_var_24.equals(temp_var_25));
            var_$tmp0 = var_$tmp0.update(pc_13, temp_var_23);
            
            PrimVS<Boolean> temp_var_26 = var_$tmp0.guard(pc_13);
            Bdd pc_14 = BoolUtils.trueCond(temp_var_26);
            Bdd pc_15 = BoolUtils.falseCond(temp_var_26);
            boolean jumpedOut_2 = false;
            boolean jumpedOut_3 = false;
            if (!pc_14.isConstFalse()) {
                // 'then' branch
                PrimVS<Machine> temp_var_27;
                temp_var_27 = var_NextMachine.guard(pc_14);
                var_$tmp1 = var_$tmp1.update(pc_14, temp_var_27);
                
                PrimVS<EventName> temp_var_28;
                temp_var_28 = new PrimVS<EventName>(Events.event_Done).guard(pc_14);
                var_$tmp2 = var_$tmp2.update(pc_14, temp_var_28);
                
                PrimVS<Machine> temp_var_29;
                temp_var_29 = var_payload.guard(pc_14);
                var_$tmp3 = var_$tmp3.update(pc_14, temp_var_29);
                
                effects.send(pc_14, var_$tmp1.guard(pc_14), var_$tmp2.guard(pc_14), new UnionVS(var_$tmp3.guard(pc_14)));
                
            }
            if (!pc_15.isConstFalse()) {
                // 'else' branch
            }
            if (jumpedOut_2 || jumpedOut_3) {
                pc_13 = pc_14.or(pc_15);
            }
            
            PrimVS<EventName> temp_var_30;
            temp_var_30 = new PrimVS<EventName>(Events.event_Unit).guard(pc_13);
            var_$tmp4 = var_$tmp4.update(pc_13, temp_var_30);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.addGuardedRaise(pc_13, var_$tmp4.guard(pc_13));
            pc_13 = Bdd.constFalse();
            
            return pc_13;
        }
        
    }
    
    public static class machine_Main extends Machine {
        
        static State state_Boot_Main_Ring4 = new State("state_Boot_Main_Ring4", 0) {
            @Override public void entry(Bdd pc_16, Machine machine, Outcome outcome, UnionVS payload) {
                ((machine_Main)machine).anonfunc_6(pc_16, machine.sendEffects, outcome);
            }
        };
        static State state_Stabilize_Main_Ring4 = new State("state_Stabilize_Main_Ring4", 1) {
            @Override public void entry(Bdd pc_17, Machine machine, Outcome outcome, UnionVS payload) {
                ((machine_Main)machine).anonfunc_7(pc_17, machine.sendEffects, outcome);
            }
        };
        static State state_RandomComm_Main_Ring4 = new State("state_RandomComm_Main_Ring4", 2) {
            @Override public void entry(Bdd pc_18, Machine machine, Outcome outcome, UnionVS payload) {
                ((machine_Main)machine).anonfunc_8(pc_18, machine.sendEffects, outcome);
            }
        };
        private PrimVS<Machine> var_N1 = new PrimVS<Machine>();
        private PrimVS<Machine> var_N2 = new PrimVS<Machine>();
        private PrimVS<Machine> var_N3 = new PrimVS<Machine>();
        private PrimVS<Machine> var_N4 = new PrimVS<Machine>();
        private PrimVS<Integer> var_ReadyCount = new PrimVS<Integer>(0);
        private PrimVS<Boolean> var_Rand1 = new PrimVS<Boolean>(false);
        private PrimVS<Boolean> var_Rand2 = new PrimVS<Boolean>(false);
        private PrimVS<Machine> var_RandSrc = new PrimVS<Machine>();
        private PrimVS<Machine> var_RandDst = new PrimVS<Machine>();
        private PrimVS<Integer> var_loopCount = new PrimVS<Integer>(0);
        
        @Override
        public void reset() {
                super.reset();
                var_N1 = new PrimVS<Machine>();
                var_N2 = new PrimVS<Machine>();
                var_N3 = new PrimVS<Machine>();
                var_N4 = new PrimVS<Machine>();
                var_ReadyCount = new PrimVS<Integer>(0);
                var_Rand1 = new PrimVS<Boolean>(false);
                var_Rand2 = new PrimVS<Boolean>(false);
                var_RandSrc = new PrimVS<Machine>();
                var_RandDst = new PrimVS<Machine>();
                var_loopCount = new PrimVS<Integer>(0);
        }
        
        public machine_Main(int id) {
            super("machine_Main", id, BufferSemantics.queue, state_Boot_Main_Ring4, state_Boot_Main_Ring4
                , state_Stabilize_Main_Ring4
                , state_RandomComm_Main_Ring4
                
            );
            state_Boot_Main_Ring4.addHandlers(new DeferEventHandler(Events.event_Ready)
                ,
                new GotoEventHandler(Events.event_Unit, state_Stabilize_Main_Ring4));
            state_Stabilize_Main_Ring4.addHandlers(new GotoEventHandler(Events.event_Ready, state_Stabilize_Main_Ring4),
                new GotoEventHandler(Events.event_Unit, state_RandomComm_Main_Ring4));
            state_RandomComm_Main_Ring4.addHandlers(new GotoEventHandler(Events.event_Unit, state_RandomComm_Main_Ring4));
        }
        
        Bdd 
        anonfunc_6(
            Bdd pc_19,
            EffectCollection effects,
            Outcome outcome
        ) {
            PrimVS<Machine> var_$tmp0 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<Machine> var_$tmp1 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<Machine> var_$tmp2 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<Machine> var_$tmp3 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<Machine> var_$tmp4 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<Machine> var_$tmp5 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<Machine> var_$tmp6 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<Machine> var_$tmp7 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<Machine> var_$tmp8 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<EventName> var_$tmp9 =
                new PrimVS<EventName>(Events.event_null).guard(pc_19);
            
            PrimVS<Machine> var_$tmp10 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<Machine> var_$tmp11 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<EventName> var_$tmp12 =
                new PrimVS<EventName>(Events.event_null).guard(pc_19);
            
            PrimVS<Machine> var_$tmp13 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<Machine> var_$tmp14 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<EventName> var_$tmp15 =
                new PrimVS<EventName>(Events.event_null).guard(pc_19);
            
            PrimVS<Machine> var_$tmp16 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<Machine> var_$tmp17 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<EventName> var_$tmp18 =
                new PrimVS<EventName>(Events.event_null).guard(pc_19);
            
            PrimVS<Machine> var_$tmp19 =
                new PrimVS<Machine>().guard(pc_19);
            
            PrimVS<Integer> var_$tmp20 =
                new PrimVS<Integer>(0).guard(pc_19);
            
            PrimVS<EventName> var_$tmp21 =
                new PrimVS<EventName>(Events.event_null).guard(pc_19);
            
            PrimVS<Machine> temp_var_31;
            temp_var_31 = new PrimVS<Machine>(this).guard(pc_19);
            var_$tmp0 = var_$tmp0.update(pc_19, temp_var_31);
            
            PrimVS<Machine> temp_var_32;
            temp_var_32 = effects.create(pc_19, scheduler, machine_Node.class, new UnionVS (var_$tmp0.guard(pc_19)), (i) -> new machine_Node(i));
            var_$tmp1 = var_$tmp1.update(pc_19, temp_var_32);
            
            PrimVS<Machine> temp_var_33;
            temp_var_33 = var_$tmp1.guard(pc_19);
            var_N1 = var_N1.update(pc_19, temp_var_33);
            
            PrimVS<Machine> temp_var_34;
            temp_var_34 = new PrimVS<Machine>(this).guard(pc_19);
            var_$tmp2 = var_$tmp2.update(pc_19, temp_var_34);
            
            PrimVS<Machine> temp_var_35;
            temp_var_35 = effects.create(pc_19, scheduler, machine_Node.class, new UnionVS (var_$tmp2.guard(pc_19)), (i) -> new machine_Node(i));
            var_$tmp3 = var_$tmp3.update(pc_19, temp_var_35);
            
            PrimVS<Machine> temp_var_36;
            temp_var_36 = var_$tmp3.guard(pc_19);
            var_N2 = var_N2.update(pc_19, temp_var_36);
            
            PrimVS<Machine> temp_var_37;
            temp_var_37 = new PrimVS<Machine>(this).guard(pc_19);
            var_$tmp4 = var_$tmp4.update(pc_19, temp_var_37);
            
            PrimVS<Machine> temp_var_38;
            temp_var_38 = effects.create(pc_19, scheduler, machine_Node.class, new UnionVS (var_$tmp4.guard(pc_19)), (i) -> new machine_Node(i));
            var_$tmp5 = var_$tmp5.update(pc_19, temp_var_38);
            
            PrimVS<Machine> temp_var_39;
            temp_var_39 = var_$tmp5.guard(pc_19);
            var_N3 = var_N3.update(pc_19, temp_var_39);
            
            PrimVS<Machine> temp_var_40;
            temp_var_40 = new PrimVS<Machine>(this).guard(pc_19);
            var_$tmp6 = var_$tmp6.update(pc_19, temp_var_40);
            
            PrimVS<Machine> temp_var_41;
            temp_var_41 = effects.create(pc_19, scheduler, machine_Node.class, new UnionVS (var_$tmp6.guard(pc_19)), (i) -> new machine_Node(i));
            var_$tmp7 = var_$tmp7.update(pc_19, temp_var_41);
            
            PrimVS<Machine> temp_var_42;
            temp_var_42 = var_$tmp7.guard(pc_19);
            var_N4 = var_N4.update(pc_19, temp_var_42);
            
            PrimVS<Machine> temp_var_43;
            temp_var_43 = var_N1.guard(pc_19);
            var_$tmp8 = var_$tmp8.update(pc_19, temp_var_43);
            
            PrimVS<EventName> temp_var_44;
            temp_var_44 = new PrimVS<EventName>(Events.event_Next).guard(pc_19);
            var_$tmp9 = var_$tmp9.update(pc_19, temp_var_44);
            
            PrimVS<Machine> temp_var_45;
            temp_var_45 = var_N2.guard(pc_19);
            var_$tmp10 = var_$tmp10.update(pc_19, temp_var_45);
            
            effects.send(pc_19, var_$tmp8.guard(pc_19), var_$tmp9.guard(pc_19), new UnionVS(var_$tmp10.guard(pc_19)));
            
            PrimVS<Machine> temp_var_46;
            temp_var_46 = var_N2.guard(pc_19);
            var_$tmp11 = var_$tmp11.update(pc_19, temp_var_46);
            
            PrimVS<EventName> temp_var_47;
            temp_var_47 = new PrimVS<EventName>(Events.event_Next).guard(pc_19);
            var_$tmp12 = var_$tmp12.update(pc_19, temp_var_47);
            
            PrimVS<Machine> temp_var_48;
            temp_var_48 = var_N3.guard(pc_19);
            var_$tmp13 = var_$tmp13.update(pc_19, temp_var_48);
            
            effects.send(pc_19, var_$tmp11.guard(pc_19), var_$tmp12.guard(pc_19), new UnionVS(var_$tmp13.guard(pc_19)));
            
            PrimVS<Machine> temp_var_49;
            temp_var_49 = var_N3.guard(pc_19);
            var_$tmp14 = var_$tmp14.update(pc_19, temp_var_49);
            
            PrimVS<EventName> temp_var_50;
            temp_var_50 = new PrimVS<EventName>(Events.event_Next).guard(pc_19);
            var_$tmp15 = var_$tmp15.update(pc_19, temp_var_50);
            
            PrimVS<Machine> temp_var_51;
            temp_var_51 = var_N4.guard(pc_19);
            var_$tmp16 = var_$tmp16.update(pc_19, temp_var_51);
            
            effects.send(pc_19, var_$tmp14.guard(pc_19), var_$tmp15.guard(pc_19), new UnionVS(var_$tmp16.guard(pc_19)));
            
            PrimVS<Machine> temp_var_52;
            temp_var_52 = var_N4.guard(pc_19);
            var_$tmp17 = var_$tmp17.update(pc_19, temp_var_52);
            
            PrimVS<EventName> temp_var_53;
            temp_var_53 = new PrimVS<EventName>(Events.event_Next).guard(pc_19);
            var_$tmp18 = var_$tmp18.update(pc_19, temp_var_53);
            
            PrimVS<Machine> temp_var_54;
            temp_var_54 = var_N1.guard(pc_19);
            var_$tmp19 = var_$tmp19.update(pc_19, temp_var_54);
            
            effects.send(pc_19, var_$tmp17.guard(pc_19), var_$tmp18.guard(pc_19), new UnionVS(var_$tmp19.guard(pc_19)));
            
            PrimVS<Integer> temp_var_55;
            temp_var_55 = (new PrimVS<Integer>(1).guard(pc_19)).apply((temp_var_56) -> -temp_var_56);
            var_$tmp20 = var_$tmp20.update(pc_19, temp_var_55);
            
            PrimVS<Integer> temp_var_57;
            temp_var_57 = var_$tmp20.guard(pc_19);
            var_ReadyCount = var_ReadyCount.update(pc_19, temp_var_57);
            
            PrimVS<EventName> temp_var_58;
            temp_var_58 = new PrimVS<EventName>(Events.event_Unit).guard(pc_19);
            var_$tmp21 = var_$tmp21.update(pc_19, temp_var_58);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.addGuardedRaise(pc_19, var_$tmp21.guard(pc_19));
            pc_19 = Bdd.constFalse();
            
            return pc_19;
        }
        
        Bdd 
        anonfunc_7(
            Bdd pc_20,
            EffectCollection effects,
            Outcome outcome
        ) {
            PrimVS<Integer> var_$tmp0 =
                new PrimVS<Integer>(0).guard(pc_20);
            
            PrimVS<Boolean> var_$tmp1 =
                new PrimVS<Boolean>(false).guard(pc_20);
            
            PrimVS<EventName> var_$tmp2 =
                new PrimVS<EventName>(Events.event_null).guard(pc_20);
            
            PrimVS<Integer> temp_var_59;
            temp_var_59 = (var_ReadyCount.guard(pc_20)).apply2(new PrimVS<Integer>(1).guard(pc_20), (temp_var_60, temp_var_61) -> temp_var_60 + temp_var_61);
            var_$tmp0 = var_$tmp0.update(pc_20, temp_var_59);
            
            PrimVS<Integer> temp_var_62;
            temp_var_62 = var_$tmp0.guard(pc_20);
            var_ReadyCount = var_ReadyCount.update(pc_20, temp_var_62);
            
            PrimVS<Boolean> temp_var_63;
            temp_var_63 = (var_ReadyCount.guard(pc_20)).apply2(new PrimVS<Integer>(4).guard(pc_20), (temp_var_64, temp_var_65) -> temp_var_64.equals(temp_var_65));
            var_$tmp1 = var_$tmp1.update(pc_20, temp_var_63);
            
            PrimVS<Boolean> temp_var_66 = var_$tmp1.guard(pc_20);
            Bdd pc_21 = BoolUtils.trueCond(temp_var_66);
            Bdd pc_22 = BoolUtils.falseCond(temp_var_66);
            boolean jumpedOut_4 = false;
            boolean jumpedOut_5 = false;
            if (!pc_21.isConstFalse()) {
                // 'then' branch
                PrimVS<EventName> temp_var_67;
                temp_var_67 = new PrimVS<EventName>(Events.event_Unit).guard(pc_21);
                var_$tmp2 = var_$tmp2.update(pc_21, temp_var_67);
                
                // NOTE (TODO): We currently perform no typechecking on the payload!
                outcome.addGuardedRaise(pc_21, var_$tmp2.guard(pc_21));
                pc_21 = Bdd.constFalse();
                jumpedOut_4 = true;
                
            }
            if (!pc_22.isConstFalse()) {
                // 'else' branch
            }
            if (jumpedOut_4 || jumpedOut_5) {
                pc_20 = pc_21.or(pc_22);
            }
            
            if (!pc_20.isConstFalse()) {
            }
            return pc_20;
        }
        
        Bdd 
        anonfunc_8(
            Bdd pc_23,
            EffectCollection effects,
            Outcome outcome
        ) {
            PrimVS<Boolean> var_$tmp0 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp1 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp2 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp3 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp4 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp5 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp6 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp7 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp8 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp9 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp10 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp11 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp12 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp13 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp14 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp15 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp16 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Boolean> var_$tmp17 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Machine> var_$tmp18 =
                new PrimVS<Machine>().guard(pc_23);
            
            PrimVS<EventName> var_$tmp19 =
                new PrimVS<EventName>(Events.event_null).guard(pc_23);
            
            PrimVS<Machine> var_$tmp20 =
                new PrimVS<Machine>().guard(pc_23);
            
            PrimVS<Boolean> var_$tmp21 =
                new PrimVS<Boolean>(false).guard(pc_23);
            
            PrimVS<Integer> var_$tmp22 =
                new PrimVS<Integer>(0).guard(pc_23);
            
            PrimVS<EventName> var_$tmp23 =
                new PrimVS<EventName>(Events.event_null).guard(pc_23);
            
            PrimVS<Boolean> temp_var_68;
            temp_var_68 = scheduler.getNextBoolean(pc_23);
            var_$tmp0 = var_$tmp0.update(pc_23, temp_var_68);
            
            PrimVS<Boolean> temp_var_69 = var_$tmp0.guard(pc_23);
            Bdd pc_24 = BoolUtils.trueCond(temp_var_69);
            Bdd pc_25 = BoolUtils.falseCond(temp_var_69);
            boolean jumpedOut_6 = false;
            boolean jumpedOut_7 = false;
            if (!pc_24.isConstFalse()) {
                // 'then' branch
                PrimVS<Boolean> temp_var_70;
                temp_var_70 = new PrimVS<Boolean>(true).guard(pc_24);
                var_Rand1 = var_Rand1.update(pc_24, temp_var_70);
                
            }
            if (!pc_25.isConstFalse()) {
                // 'else' branch
                PrimVS<Boolean> temp_var_71;
                temp_var_71 = new PrimVS<Boolean>(false).guard(pc_25);
                var_Rand1 = var_Rand1.update(pc_25, temp_var_71);
                
            }
            if (jumpedOut_6 || jumpedOut_7) {
                pc_23 = pc_24.or(pc_25);
            }
            
            PrimVS<Boolean> temp_var_72;
            temp_var_72 = scheduler.getNextBoolean(pc_23);
            var_$tmp1 = var_$tmp1.update(pc_23, temp_var_72);
            
            PrimVS<Boolean> temp_var_73 = var_$tmp1.guard(pc_23);
            Bdd pc_26 = BoolUtils.trueCond(temp_var_73);
            Bdd pc_27 = BoolUtils.falseCond(temp_var_73);
            boolean jumpedOut_8 = false;
            boolean jumpedOut_9 = false;
            if (!pc_26.isConstFalse()) {
                // 'then' branch
                PrimVS<Boolean> temp_var_74;
                temp_var_74 = new PrimVS<Boolean>(true).guard(pc_26);
                var_Rand2 = var_Rand2.update(pc_26, temp_var_74);
                
            }
            if (!pc_27.isConstFalse()) {
                // 'else' branch
                PrimVS<Boolean> temp_var_75;
                temp_var_75 = new PrimVS<Boolean>(false).guard(pc_27);
                var_Rand2 = var_Rand2.update(pc_27, temp_var_75);
                
            }
            if (jumpedOut_8 || jumpedOut_9) {
                pc_23 = pc_26.or(pc_27);
            }
            
            PrimVS<Boolean> temp_var_76;
            temp_var_76 = (var_Rand1.guard(pc_23)).apply((temp_var_77) -> !temp_var_77);
            var_$tmp2 = var_$tmp2.update(pc_23, temp_var_76);
            
            PrimVS<Boolean> temp_var_78;
            temp_var_78 = var_$tmp2.guard(pc_23);
            var_$tmp4 = var_$tmp4.update(pc_23, temp_var_78);
            
            PrimVS<Boolean> temp_var_79 = var_$tmp4.guard(pc_23);
            Bdd pc_28 = BoolUtils.trueCond(temp_var_79);
            Bdd pc_29 = BoolUtils.falseCond(temp_var_79);
            boolean jumpedOut_10 = false;
            boolean jumpedOut_11 = false;
            if (!pc_28.isConstFalse()) {
                // 'then' branch
                PrimVS<Boolean> temp_var_80;
                temp_var_80 = (var_Rand2.guard(pc_28)).apply((temp_var_81) -> !temp_var_81);
                var_$tmp3 = var_$tmp3.update(pc_28, temp_var_80);
                
                PrimVS<Boolean> temp_var_82;
                temp_var_82 = var_$tmp3.guard(pc_28);
                var_$tmp4 = var_$tmp4.update(pc_28, temp_var_82);
                
            }
            if (jumpedOut_10 || jumpedOut_11) {
                pc_23 = pc_28.or(pc_29);
            }
            
            PrimVS<Boolean> temp_var_83 = var_$tmp4.guard(pc_23);
            Bdd pc_30 = BoolUtils.trueCond(temp_var_83);
            Bdd pc_31 = BoolUtils.falseCond(temp_var_83);
            boolean jumpedOut_12 = false;
            boolean jumpedOut_13 = false;
            if (!pc_30.isConstFalse()) {
                // 'then' branch
                PrimVS<Machine> temp_var_84;
                temp_var_84 = var_N1.guard(pc_30);
                var_RandSrc = var_RandSrc.update(pc_30, temp_var_84);
                
            }
            if (!pc_31.isConstFalse()) {
                // 'else' branch
            }
            if (jumpedOut_12 || jumpedOut_13) {
                pc_23 = pc_30.or(pc_31);
            }
            
            PrimVS<Boolean> temp_var_85;
            temp_var_85 = (var_Rand1.guard(pc_23)).apply((temp_var_86) -> !temp_var_86);
            var_$tmp5 = var_$tmp5.update(pc_23, temp_var_85);
            
            PrimVS<Boolean> temp_var_87;
            temp_var_87 = var_$tmp5.guard(pc_23);
            var_$tmp6 = var_$tmp6.update(pc_23, temp_var_87);
            
            PrimVS<Boolean> temp_var_88 = var_$tmp6.guard(pc_23);
            Bdd pc_32 = BoolUtils.trueCond(temp_var_88);
            Bdd pc_33 = BoolUtils.falseCond(temp_var_88);
            boolean jumpedOut_14 = false;
            boolean jumpedOut_15 = false;
            if (!pc_32.isConstFalse()) {
                // 'then' branch
                PrimVS<Boolean> temp_var_89;
                temp_var_89 = var_Rand2.guard(pc_32);
                var_$tmp6 = var_$tmp6.update(pc_32, temp_var_89);
                
            }
            if (jumpedOut_14 || jumpedOut_15) {
                pc_23 = pc_32.or(pc_33);
            }
            
            PrimVS<Boolean> temp_var_90 = var_$tmp6.guard(pc_23);
            Bdd pc_34 = BoolUtils.trueCond(temp_var_90);
            Bdd pc_35 = BoolUtils.falseCond(temp_var_90);
            boolean jumpedOut_16 = false;
            boolean jumpedOut_17 = false;
            if (!pc_34.isConstFalse()) {
                // 'then' branch
                PrimVS<Machine> temp_var_91;
                temp_var_91 = var_N2.guard(pc_34);
                var_RandSrc = var_RandSrc.update(pc_34, temp_var_91);
                
            }
            if (!pc_35.isConstFalse()) {
                // 'else' branch
            }
            if (jumpedOut_16 || jumpedOut_17) {
                pc_23 = pc_34.or(pc_35);
            }
            
            PrimVS<Boolean> temp_var_92;
            temp_var_92 = var_Rand1.guard(pc_23);
            var_$tmp8 = var_$tmp8.update(pc_23, temp_var_92);
            
            PrimVS<Boolean> temp_var_93 = var_$tmp8.guard(pc_23);
            Bdd pc_36 = BoolUtils.trueCond(temp_var_93);
            Bdd pc_37 = BoolUtils.falseCond(temp_var_93);
            boolean jumpedOut_18 = false;
            boolean jumpedOut_19 = false;
            if (!pc_36.isConstFalse()) {
                // 'then' branch
                PrimVS<Boolean> temp_var_94;
                temp_var_94 = (var_Rand2.guard(pc_36)).apply((temp_var_95) -> !temp_var_95);
                var_$tmp7 = var_$tmp7.update(pc_36, temp_var_94);
                
                PrimVS<Boolean> temp_var_96;
                temp_var_96 = var_$tmp7.guard(pc_36);
                var_$tmp8 = var_$tmp8.update(pc_36, temp_var_96);
                
            }
            if (jumpedOut_18 || jumpedOut_19) {
                pc_23 = pc_36.or(pc_37);
            }
            
            PrimVS<Boolean> temp_var_97 = var_$tmp8.guard(pc_23);
            Bdd pc_38 = BoolUtils.trueCond(temp_var_97);
            Bdd pc_39 = BoolUtils.falseCond(temp_var_97);
            boolean jumpedOut_20 = false;
            boolean jumpedOut_21 = false;
            if (!pc_38.isConstFalse()) {
                // 'then' branch
                PrimVS<Machine> temp_var_98;
                temp_var_98 = var_N3.guard(pc_38);
                var_RandSrc = var_RandSrc.update(pc_38, temp_var_98);
                
            }
            if (!pc_39.isConstFalse()) {
                // 'else' branch
                PrimVS<Machine> temp_var_99;
                temp_var_99 = var_N4.guard(pc_39);
                var_RandSrc = var_RandSrc.update(pc_39, temp_var_99);
                
            }
            if (jumpedOut_20 || jumpedOut_21) {
                pc_23 = pc_38.or(pc_39);
            }
            
            PrimVS<Boolean> temp_var_100;
            temp_var_100 = scheduler.getNextBoolean(pc_23);
            var_$tmp9 = var_$tmp9.update(pc_23, temp_var_100);
            
            PrimVS<Boolean> temp_var_101 = var_$tmp9.guard(pc_23);
            Bdd pc_40 = BoolUtils.trueCond(temp_var_101);
            Bdd pc_41 = BoolUtils.falseCond(temp_var_101);
            boolean jumpedOut_22 = false;
            boolean jumpedOut_23 = false;
            if (!pc_40.isConstFalse()) {
                // 'then' branch
                PrimVS<Boolean> temp_var_102;
                temp_var_102 = new PrimVS<Boolean>(true).guard(pc_40);
                var_Rand1 = var_Rand1.update(pc_40, temp_var_102);
                
            }
            if (!pc_41.isConstFalse()) {
                // 'else' branch
                PrimVS<Boolean> temp_var_103;
                temp_var_103 = new PrimVS<Boolean>(false).guard(pc_41);
                var_Rand1 = var_Rand1.update(pc_41, temp_var_103);
                
            }
            if (jumpedOut_22 || jumpedOut_23) {
                pc_23 = pc_40.or(pc_41);
            }
            
            PrimVS<Boolean> temp_var_104;
            temp_var_104 = scheduler.getNextBoolean(pc_23);
            var_$tmp10 = var_$tmp10.update(pc_23, temp_var_104);
            
            PrimVS<Boolean> temp_var_105 = var_$tmp10.guard(pc_23);
            Bdd pc_42 = BoolUtils.trueCond(temp_var_105);
            Bdd pc_43 = BoolUtils.falseCond(temp_var_105);
            boolean jumpedOut_24 = false;
            boolean jumpedOut_25 = false;
            if (!pc_42.isConstFalse()) {
                // 'then' branch
                PrimVS<Boolean> temp_var_106;
                temp_var_106 = new PrimVS<Boolean>(true).guard(pc_42);
                var_Rand2 = var_Rand2.update(pc_42, temp_var_106);
                
            }
            if (!pc_43.isConstFalse()) {
                // 'else' branch
                PrimVS<Boolean> temp_var_107;
                temp_var_107 = new PrimVS<Boolean>(false).guard(pc_43);
                var_Rand2 = var_Rand2.update(pc_43, temp_var_107);
                
            }
            if (jumpedOut_24 || jumpedOut_25) {
                pc_23 = pc_42.or(pc_43);
            }
            
            PrimVS<Boolean> temp_var_108;
            temp_var_108 = (var_Rand1.guard(pc_23)).apply((temp_var_109) -> !temp_var_109);
            var_$tmp11 = var_$tmp11.update(pc_23, temp_var_108);
            
            PrimVS<Boolean> temp_var_110;
            temp_var_110 = var_$tmp11.guard(pc_23);
            var_$tmp13 = var_$tmp13.update(pc_23, temp_var_110);
            
            PrimVS<Boolean> temp_var_111 = var_$tmp13.guard(pc_23);
            Bdd pc_44 = BoolUtils.trueCond(temp_var_111);
            Bdd pc_45 = BoolUtils.falseCond(temp_var_111);
            boolean jumpedOut_26 = false;
            boolean jumpedOut_27 = false;
            if (!pc_44.isConstFalse()) {
                // 'then' branch
                PrimVS<Boolean> temp_var_112;
                temp_var_112 = (var_Rand2.guard(pc_44)).apply((temp_var_113) -> !temp_var_113);
                var_$tmp12 = var_$tmp12.update(pc_44, temp_var_112);
                
                PrimVS<Boolean> temp_var_114;
                temp_var_114 = var_$tmp12.guard(pc_44);
                var_$tmp13 = var_$tmp13.update(pc_44, temp_var_114);
                
            }
            if (jumpedOut_26 || jumpedOut_27) {
                pc_23 = pc_44.or(pc_45);
            }
            
            PrimVS<Boolean> temp_var_115 = var_$tmp13.guard(pc_23);
            Bdd pc_46 = BoolUtils.trueCond(temp_var_115);
            Bdd pc_47 = BoolUtils.falseCond(temp_var_115);
            boolean jumpedOut_28 = false;
            boolean jumpedOut_29 = false;
            if (!pc_46.isConstFalse()) {
                // 'then' branch
                PrimVS<Machine> temp_var_116;
                temp_var_116 = var_N1.guard(pc_46);
                var_RandDst = var_RandDst.update(pc_46, temp_var_116);
                
            }
            if (!pc_47.isConstFalse()) {
                // 'else' branch
            }
            if (jumpedOut_28 || jumpedOut_29) {
                pc_23 = pc_46.or(pc_47);
            }
            
            PrimVS<Boolean> temp_var_117;
            temp_var_117 = (var_Rand1.guard(pc_23)).apply((temp_var_118) -> !temp_var_118);
            var_$tmp14 = var_$tmp14.update(pc_23, temp_var_117);
            
            PrimVS<Boolean> temp_var_119;
            temp_var_119 = var_$tmp14.guard(pc_23);
            var_$tmp15 = var_$tmp15.update(pc_23, temp_var_119);
            
            PrimVS<Boolean> temp_var_120 = var_$tmp15.guard(pc_23);
            Bdd pc_48 = BoolUtils.trueCond(temp_var_120);
            Bdd pc_49 = BoolUtils.falseCond(temp_var_120);
            boolean jumpedOut_30 = false;
            boolean jumpedOut_31 = false;
            if (!pc_48.isConstFalse()) {
                // 'then' branch
                PrimVS<Boolean> temp_var_121;
                temp_var_121 = var_Rand2.guard(pc_48);
                var_$tmp15 = var_$tmp15.update(pc_48, temp_var_121);
                
            }
            if (jumpedOut_30 || jumpedOut_31) {
                pc_23 = pc_48.or(pc_49);
            }
            
            PrimVS<Boolean> temp_var_122 = var_$tmp15.guard(pc_23);
            Bdd pc_50 = BoolUtils.trueCond(temp_var_122);
            Bdd pc_51 = BoolUtils.falseCond(temp_var_122);
            boolean jumpedOut_32 = false;
            boolean jumpedOut_33 = false;
            if (!pc_50.isConstFalse()) {
                // 'then' branch
                PrimVS<Machine> temp_var_123;
                temp_var_123 = var_N2.guard(pc_50);
                var_RandDst = var_RandDst.update(pc_50, temp_var_123);
                
            }
            if (!pc_51.isConstFalse()) {
                // 'else' branch
            }
            if (jumpedOut_32 || jumpedOut_33) {
                pc_23 = pc_50.or(pc_51);
            }
            
            PrimVS<Boolean> temp_var_124;
            temp_var_124 = var_Rand1.guard(pc_23);
            var_$tmp17 = var_$tmp17.update(pc_23, temp_var_124);
            
            PrimVS<Boolean> temp_var_125 = var_$tmp17.guard(pc_23);
            Bdd pc_52 = BoolUtils.trueCond(temp_var_125);
            Bdd pc_53 = BoolUtils.falseCond(temp_var_125);
            boolean jumpedOut_34 = false;
            boolean jumpedOut_35 = false;
            if (!pc_52.isConstFalse()) {
                // 'then' branch
                PrimVS<Boolean> temp_var_126;
                temp_var_126 = (var_Rand2.guard(pc_52)).apply((temp_var_127) -> !temp_var_127);
                var_$tmp16 = var_$tmp16.update(pc_52, temp_var_126);
                
                PrimVS<Boolean> temp_var_128;
                temp_var_128 = var_$tmp16.guard(pc_52);
                var_$tmp17 = var_$tmp17.update(pc_52, temp_var_128);
                
            }
            if (jumpedOut_34 || jumpedOut_35) {
                pc_23 = pc_52.or(pc_53);
            }
            
            PrimVS<Boolean> temp_var_129 = var_$tmp17.guard(pc_23);
            Bdd pc_54 = BoolUtils.trueCond(temp_var_129);
            Bdd pc_55 = BoolUtils.falseCond(temp_var_129);
            boolean jumpedOut_36 = false;
            boolean jumpedOut_37 = false;
            if (!pc_54.isConstFalse()) {
                // 'then' branch
                PrimVS<Machine> temp_var_130;
                temp_var_130 = var_N3.guard(pc_54);
                var_RandDst = var_RandDst.update(pc_54, temp_var_130);
                
            }
            if (!pc_55.isConstFalse()) {
                // 'else' branch
                PrimVS<Machine> temp_var_131;
                temp_var_131 = var_N4.guard(pc_55);
                var_RandDst = var_RandDst.update(pc_55, temp_var_131);
                
            }
            if (jumpedOut_36 || jumpedOut_37) {
                pc_23 = pc_54.or(pc_55);
            }
            
            PrimVS<Machine> temp_var_132;
            temp_var_132 = var_RandSrc.guard(pc_23);
            var_$tmp18 = var_$tmp18.update(pc_23, temp_var_132);
            
            PrimVS<EventName> temp_var_133;
            temp_var_133 = new PrimVS<EventName>(Events.event_Send).guard(pc_23);
            var_$tmp19 = var_$tmp19.update(pc_23, temp_var_133);
            
            PrimVS<Machine> temp_var_134;
            temp_var_134 = var_RandDst.guard(pc_23);
            var_$tmp20 = var_$tmp20.update(pc_23, temp_var_134);
            
            effects.send(pc_23, var_$tmp18.guard(pc_23), var_$tmp19.guard(pc_23), new UnionVS(var_$tmp20.guard(pc_23)));
            
            PrimVS<Boolean> temp_var_135;
            temp_var_135 = (var_loopCount.guard(pc_23)).apply2(new PrimVS<Integer>(1).guard(pc_23), (temp_var_136, temp_var_137) -> temp_var_136 < temp_var_137);
            var_$tmp21 = var_$tmp21.update(pc_23, temp_var_135);
            
            PrimVS<Boolean> temp_var_138 = var_$tmp21.guard(pc_23);
            Bdd pc_56 = BoolUtils.trueCond(temp_var_138);
            Bdd pc_57 = BoolUtils.falseCond(temp_var_138);
            boolean jumpedOut_38 = false;
            boolean jumpedOut_39 = false;
            if (!pc_56.isConstFalse()) {
                // 'then' branch
                PrimVS<Integer> temp_var_139;
                temp_var_139 = (var_loopCount.guard(pc_56)).apply2(new PrimVS<Integer>(1).guard(pc_56), (temp_var_140, temp_var_141) -> temp_var_140 + temp_var_141);
                var_$tmp22 = var_$tmp22.update(pc_56, temp_var_139);
                
                PrimVS<Integer> temp_var_142;
                temp_var_142 = var_$tmp22.guard(pc_56);
                var_loopCount = var_loopCount.update(pc_56, temp_var_142);
                
                PrimVS<EventName> temp_var_143;
                temp_var_143 = new PrimVS<EventName>(Events.event_Unit).guard(pc_56);
                var_$tmp23 = var_$tmp23.update(pc_56, temp_var_143);
                
                // NOTE (TODO): We currently perform no typechecking on the payload!
                outcome.addGuardedRaise(pc_56, var_$tmp23.guard(pc_56));
                pc_56 = Bdd.constFalse();
                jumpedOut_38 = true;
                
            }
            if (!pc_57.isConstFalse()) {
                // 'else' branch
            }
            if (jumpedOut_38 || jumpedOut_39) {
                pc_23 = pc_56.or(pc_57);
            }
            
            if (!pc_23.isConstFalse()) {
            }
            return pc_23;
        }
        
    }
    
    // Skipping Implementation 'DefaultImpl'

    private static Machine start = new machine_Main(0);
    
    @Override
    public Machine getStart() { return start; }
    
}
