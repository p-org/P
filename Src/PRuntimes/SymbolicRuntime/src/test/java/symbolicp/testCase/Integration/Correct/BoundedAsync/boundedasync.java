import symbolicp.*;
import symbolicp.bdd.*;
import symbolicp.vs.*;
import symbolicp.runtime.*;
import symbolicp.run.*;

public class boundedasync implements Program {
    
    public static Scheduler scheduler;
    
    @Override
    public void setScheduler (Scheduler s) { this.scheduler = s; }
    
    
    static enum Events implements EventName {
        event_null,
        event_halt,
        event_unit,
        event_init,
        event_myCount,
        event_Req,
        event_Resp,
    }
    
    // Skipping Interface 'Main'

    // Skipping Interface 'Process'

    public static class machine_Main extends Machine {
        
        static State state_inits = new State("state_inits", 0) {
            @Override public void entry(Bdd pc_0, Machine machine, Outcome outcome, UnionVS payload) {
                ((machine_Main)machine).anonfunc_0(pc_0, machine.sendEffects, outcome);
            }
        };
        static State state_Sync = new State("state_Sync", 1) {
            @Override public void exit(Bdd pc, Machine machine) {
                ((machine_Main)machine).anonfunc_1(pc, machine.sendEffects);
            }
        };
        private PrimVS<Machine> var_p1 = new PrimVS<Machine>();
        private PrimVS<Machine> var_p2 = new PrimVS<Machine>();
        private PrimVS<Machine> var_p3 = new PrimVS<Machine>();
        private PrimVS<Integer> var_count = new PrimVS<Integer>(0);
        
        @Override
        public void reset() {
                super.reset();
                var_p1 = new PrimVS<Machine>();
                var_p2 = new PrimVS<Machine>();
                var_p3 = new PrimVS<Machine>();
                var_count = new PrimVS<Integer>(0);
        }
        
        public machine_Main(int id) {
            super("machine_Main", id, BufferSemantics.queue, state_inits, state_inits
                , state_Sync
                
            );
            state_inits.addHandlers(new GotoEventHandler(Events.event_unit, state_Sync));
            state_Sync.addHandlers(new EventHandler(Events.event_Req) {
                    @Override public void handleEvent(Bdd pc, UnionVS payload, Machine machine, Outcome outcome) {
                        ((machine_Main)machine).func_CountReq(pc, machine.sendEffects, outcome);
                    }
                },
                new GotoEventHandler(Events.event_Resp, state_Sync));
        }
        
        Bdd 
        anonfunc_0(
            Bdd pc_1,
            EffectCollection effects,
            Outcome outcome
        ) {
            PrimVS<Machine> var_$tmp0 =
                new PrimVS<Machine>().guard(pc_1);
            
            PrimVS<Machine> var_$tmp1 =
                new PrimVS<Machine>().guard(pc_1);
            
            PrimVS<Machine> var_$tmp2 =
                new PrimVS<Machine>().guard(pc_1);
            
            PrimVS<Machine> var_$tmp3 =
                new PrimVS<Machine>().guard(pc_1);
            
            PrimVS<Machine> var_$tmp4 =
                new PrimVS<Machine>().guard(pc_1);
            
            PrimVS<Machine> var_$tmp5 =
                new PrimVS<Machine>().guard(pc_1);
            
            PrimVS<Machine> var_$tmp6 =
                new PrimVS<Machine>().guard(pc_1);
            
            PrimVS<EventName> var_$tmp7 =
                new PrimVS<EventName>(Events.event_null).guard(pc_1);
            
            PrimVS<Machine> var_$tmp8 =
                new PrimVS<Machine>().guard(pc_1);
            
            PrimVS<Machine> var_$tmp9 =
                new PrimVS<Machine>().guard(pc_1);
            
            TupleVS var_$tmp10 =
                new TupleVS(new PrimVS<Machine>().guard(pc_1), new PrimVS<Machine>().guard(pc_1));
            
            PrimVS<Machine> var_$tmp11 =
                new PrimVS<Machine>().guard(pc_1);
            
            PrimVS<EventName> var_$tmp12 =
                new PrimVS<EventName>(Events.event_null).guard(pc_1);
            
            PrimVS<Machine> var_$tmp13 =
                new PrimVS<Machine>().guard(pc_1);
            
            PrimVS<Machine> var_$tmp14 =
                new PrimVS<Machine>().guard(pc_1);
            
            TupleVS var_$tmp15 =
                new TupleVS(new PrimVS<Machine>().guard(pc_1), new PrimVS<Machine>().guard(pc_1));
            
            PrimVS<Machine> var_$tmp16 =
                new PrimVS<Machine>().guard(pc_1);
            
            PrimVS<EventName> var_$tmp17 =
                new PrimVS<EventName>(Events.event_null).guard(pc_1);
            
            PrimVS<Machine> var_$tmp18 =
                new PrimVS<Machine>().guard(pc_1);
            
            PrimVS<Machine> var_$tmp19 =
                new PrimVS<Machine>().guard(pc_1);
            
            TupleVS var_$tmp20 =
                new TupleVS(new PrimVS<Machine>().guard(pc_1), new PrimVS<Machine>().guard(pc_1));
            
            PrimVS<EventName> var_$tmp21 =
                new PrimVS<EventName>(Events.event_null).guard(pc_1);
            
            PrimVS<Machine> temp_var_0;
            temp_var_0 = new PrimVS<Machine>(this).guard(pc_1);
            var_$tmp0 = var_$tmp0.update(pc_1, temp_var_0);
            
            PrimVS<Machine> temp_var_1;
            temp_var_1 = effects.create(pc_1, scheduler, machine_Process.class, new UnionVS (var_$tmp0.guard(pc_1)), (i) -> new machine_Process(i));
            var_$tmp1 = var_$tmp1.update(pc_1, temp_var_1);
            
            PrimVS<Machine> temp_var_2;
            temp_var_2 = var_$tmp1.guard(pc_1);
            var_p1 = var_p1.update(pc_1, temp_var_2);
            
            PrimVS<Machine> temp_var_3;
            temp_var_3 = new PrimVS<Machine>(this).guard(pc_1);
            var_$tmp2 = var_$tmp2.update(pc_1, temp_var_3);
            
            PrimVS<Machine> temp_var_4;
            temp_var_4 = effects.create(pc_1, scheduler, machine_Process.class, new UnionVS (var_$tmp2.guard(pc_1)), (i) -> new machine_Process(i));
            var_$tmp3 = var_$tmp3.update(pc_1, temp_var_4);
            
            PrimVS<Machine> temp_var_5;
            temp_var_5 = var_$tmp3.guard(pc_1);
            var_p2 = var_p2.update(pc_1, temp_var_5);
            
            PrimVS<Machine> temp_var_6;
            temp_var_6 = new PrimVS<Machine>(this).guard(pc_1);
            var_$tmp4 = var_$tmp4.update(pc_1, temp_var_6);
            
            PrimVS<Machine> temp_var_7;
            temp_var_7 = effects.create(pc_1, scheduler, machine_Process.class, new UnionVS (var_$tmp4.guard(pc_1)), (i) -> new machine_Process(i));
            var_$tmp5 = var_$tmp5.update(pc_1, temp_var_7);
            
            PrimVS<Machine> temp_var_8;
            temp_var_8 = var_$tmp5.guard(pc_1);
            var_p3 = var_p3.update(pc_1, temp_var_8);
            
            PrimVS<Machine> temp_var_9;
            temp_var_9 = var_p1.guard(pc_1);
            var_$tmp6 = var_$tmp6.update(pc_1, temp_var_9);
            
            PrimVS<EventName> temp_var_10;
            temp_var_10 = new PrimVS<EventName>(Events.event_init).guard(pc_1);
            var_$tmp7 = var_$tmp7.update(pc_1, temp_var_10);
            
            PrimVS<Machine> temp_var_11;
            temp_var_11 = var_p3.guard(pc_1);
            var_$tmp8 = var_$tmp8.update(pc_1, temp_var_11);
            
            PrimVS<Machine> temp_var_12;
            temp_var_12 = var_p2.guard(pc_1);
            var_$tmp9 = var_$tmp9.update(pc_1, temp_var_12);
            
            TupleVS temp_var_13;
            temp_var_13 = new TupleVS(var_$tmp8.guard(pc_1), var_$tmp9.guard(pc_1));
            var_$tmp10 = var_$tmp10.update(pc_1, temp_var_13);
            
            effects.send(pc_1, var_$tmp6.guard(pc_1), var_$tmp7.guard(pc_1), new UnionVS(var_$tmp10.guard(pc_1)));
            
            PrimVS<Machine> temp_var_14;
            temp_var_14 = var_p2.guard(pc_1);
            var_$tmp11 = var_$tmp11.update(pc_1, temp_var_14);
            
            PrimVS<EventName> temp_var_15;
            temp_var_15 = new PrimVS<EventName>(Events.event_init).guard(pc_1);
            var_$tmp12 = var_$tmp12.update(pc_1, temp_var_15);
            
            PrimVS<Machine> temp_var_16;
            temp_var_16 = var_p3.guard(pc_1);
            var_$tmp13 = var_$tmp13.update(pc_1, temp_var_16);
            
            PrimVS<Machine> temp_var_17;
            temp_var_17 = var_p1.guard(pc_1);
            var_$tmp14 = var_$tmp14.update(pc_1, temp_var_17);
            
            TupleVS temp_var_18;
            temp_var_18 = new TupleVS(var_$tmp13.guard(pc_1), var_$tmp14.guard(pc_1));
            var_$tmp15 = var_$tmp15.update(pc_1, temp_var_18);
            
            effects.send(pc_1, var_$tmp11.guard(pc_1), var_$tmp12.guard(pc_1), new UnionVS(var_$tmp15.guard(pc_1)));
            
            PrimVS<Machine> temp_var_19;
            temp_var_19 = var_p3.guard(pc_1);
            var_$tmp16 = var_$tmp16.update(pc_1, temp_var_19);
            
            PrimVS<EventName> temp_var_20;
            temp_var_20 = new PrimVS<EventName>(Events.event_init).guard(pc_1);
            var_$tmp17 = var_$tmp17.update(pc_1, temp_var_20);
            
            PrimVS<Machine> temp_var_21;
            temp_var_21 = var_p1.guard(pc_1);
            var_$tmp18 = var_$tmp18.update(pc_1, temp_var_21);
            
            PrimVS<Machine> temp_var_22;
            temp_var_22 = var_p2.guard(pc_1);
            var_$tmp19 = var_$tmp19.update(pc_1, temp_var_22);
            
            TupleVS temp_var_23;
            temp_var_23 = new TupleVS(var_$tmp18.guard(pc_1), var_$tmp19.guard(pc_1));
            var_$tmp20 = var_$tmp20.update(pc_1, temp_var_23);
            
            effects.send(pc_1, var_$tmp16.guard(pc_1), var_$tmp17.guard(pc_1), new UnionVS(var_$tmp20.guard(pc_1)));
            
            PrimVS<Integer> temp_var_24;
            temp_var_24 = new PrimVS<Integer>(0).guard(pc_1);
            var_count = var_count.update(pc_1, temp_var_24);
            
            PrimVS<EventName> temp_var_25;
            temp_var_25 = new PrimVS<EventName>(Events.event_unit).guard(pc_1);
            var_$tmp21 = var_$tmp21.update(pc_1, temp_var_25);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.addGuardedRaise(pc_1, var_$tmp21.guard(pc_1));
            pc_1 = Bdd.constFalse();
            
            return pc_1;
        }
        
        Bdd 
        func_CountReq(
            Bdd pc_2,
            EffectCollection effects,
            Outcome outcome
        ) {
            PrimVS<Integer> var_$tmp0 =
                new PrimVS<Integer>(0).guard(pc_2);
            
            PrimVS<Boolean> var_$tmp1 =
                new PrimVS<Boolean>(false).guard(pc_2);
            
            PrimVS<EventName> var_$tmp2 =
                new PrimVS<EventName>(Events.event_null).guard(pc_2);
            
            PrimVS<Integer> temp_var_26;
            temp_var_26 = (var_count.guard(pc_2)).apply2(new PrimVS<Integer>(1).guard(pc_2), (temp_var_27, temp_var_28) -> temp_var_27 + temp_var_28);
            var_$tmp0 = var_$tmp0.update(pc_2, temp_var_26);
            
            PrimVS<Integer> temp_var_29;
            temp_var_29 = var_$tmp0.guard(pc_2);
            var_count = var_count.update(pc_2, temp_var_29);
            
            PrimVS<Boolean> temp_var_30;
            temp_var_30 = (var_count.guard(pc_2)).apply2(new PrimVS<Integer>(3).guard(pc_2), (temp_var_31, temp_var_32) -> temp_var_31.equals(temp_var_32));
            var_$tmp1 = var_$tmp1.update(pc_2, temp_var_30);
            
            PrimVS<Boolean> temp_var_33 = var_$tmp1.guard(pc_2);
            Bdd pc_3 = BoolUtils.trueCond(temp_var_33);
            Bdd pc_4 = BoolUtils.falseCond(temp_var_33);
            boolean jumpedOut_0 = false;
            boolean jumpedOut_1 = false;
            if (!pc_3.isConstFalse()) {
                // 'then' branch
                PrimVS<Integer> temp_var_34;
                temp_var_34 = new PrimVS<Integer>(0).guard(pc_3);
                var_count = var_count.update(pc_3, temp_var_34);
                
                PrimVS<EventName> temp_var_35;
                temp_var_35 = new PrimVS<EventName>(Events.event_Resp).guard(pc_3);
                var_$tmp2 = var_$tmp2.update(pc_3, temp_var_35);
                
                // NOTE (TODO): We currently perform no typechecking on the payload!
                outcome.addGuardedRaise(pc_3, var_$tmp2.guard(pc_3));
                pc_3 = Bdd.constFalse();
                jumpedOut_0 = true;
                
            }
            if (!pc_4.isConstFalse()) {
                // 'else' branch
            }
            if (jumpedOut_0 || jumpedOut_1) {
                pc_2 = pc_3.or(pc_4);
            }
            
            if (!pc_2.isConstFalse()) {
            }
            return pc_2;
        }
        
        void 
        anonfunc_1(
            Bdd pc_5,
            EffectCollection effects
        ) {
            PrimVS<Machine> var_$tmp0 =
                new PrimVS<Machine>().guard(pc_5);
            
            PrimVS<EventName> var_$tmp1 =
                new PrimVS<EventName>(Events.event_null).guard(pc_5);
            
            PrimVS<Machine> var_$tmp2 =
                new PrimVS<Machine>().guard(pc_5);
            
            PrimVS<EventName> var_$tmp3 =
                new PrimVS<EventName>(Events.event_null).guard(pc_5);
            
            PrimVS<Machine> var_$tmp4 =
                new PrimVS<Machine>().guard(pc_5);
            
            PrimVS<EventName> var_$tmp5 =
                new PrimVS<EventName>(Events.event_null).guard(pc_5);
            
            PrimVS<Machine> temp_var_36;
            temp_var_36 = var_p1.guard(pc_5);
            var_$tmp0 = var_$tmp0.update(pc_5, temp_var_36);
            
            PrimVS<EventName> temp_var_37;
            temp_var_37 = new PrimVS<EventName>(Events.event_Resp).guard(pc_5);
            var_$tmp1 = var_$tmp1.update(pc_5, temp_var_37);
            
            effects.send(pc_5, var_$tmp0.guard(pc_5), var_$tmp1.guard(pc_5), null);
            
            PrimVS<Machine> temp_var_38;
            temp_var_38 = var_p2.guard(pc_5);
            var_$tmp2 = var_$tmp2.update(pc_5, temp_var_38);
            
            PrimVS<EventName> temp_var_39;
            temp_var_39 = new PrimVS<EventName>(Events.event_Resp).guard(pc_5);
            var_$tmp3 = var_$tmp3.update(pc_5, temp_var_39);
            
            effects.send(pc_5, var_$tmp2.guard(pc_5), var_$tmp3.guard(pc_5), null);
            
            PrimVS<Machine> temp_var_40;
            temp_var_40 = var_p3.guard(pc_5);
            var_$tmp4 = var_$tmp4.update(pc_5, temp_var_40);
            
            PrimVS<EventName> temp_var_41;
            temp_var_41 = new PrimVS<EventName>(Events.event_Resp).guard(pc_5);
            var_$tmp5 = var_$tmp5.update(pc_5, temp_var_41);
            
            effects.send(pc_5, var_$tmp4.guard(pc_5), var_$tmp5.guard(pc_5), null);
            
        }
        
    }
    
    public static class machine_Process extends Machine {
        
        static State state__init = new State("state__init", 0) {
            @Override public void entry(Bdd pc_6, Machine machine, Outcome outcome, UnionVS payload) {
                ((machine_Process)machine).anonfunc_2(pc_6, machine.sendEffects, outcome, payload != null ? (PrimVS<Machine>) ValueSummary.fromAny(pc_6, new PrimVS<Machine>().guard(pc_6).getClass(), payload) : new PrimVS<Machine>().guard(pc_6));
            }
        };
        static State state_inits = new State("state_inits", 1) {
            @Override public void entry(Bdd pc_7, Machine machine, Outcome outcome, UnionVS payload) {
                ((machine_Process)machine).anonfunc_3(pc_7, machine.sendEffects, payload != null ? (UnionVS) ValueSummary.fromAny(pc_7, new UnionVS().guard(pc_7).getClass(), payload) : new UnionVS().guard(pc_7));
            }
        };
        static State state_SendCount = new State("state_SendCount", 2) {
            @Override public void entry(Bdd pc_8, Machine machine, Outcome outcome, UnionVS payload) {
                ((machine_Process)machine).anonfunc_4(pc_8, machine.sendEffects, outcome);
            }
        };
        static State state_done = new State("state_done", 3) {
        };
        private PrimVS<Integer> var_count = new PrimVS<Integer>(0);
        private PrimVS<Machine> var_parent = new PrimVS<Machine>();
        private PrimVS<Machine> var_other1 = new PrimVS<Machine>();
        private PrimVS<Machine> var_other2 = new PrimVS<Machine>();
        
        @Override
        public void reset() {
                super.reset();
                var_count = new PrimVS<Integer>(0);
                var_parent = new PrimVS<Machine>();
                var_other1 = new PrimVS<Machine>();
                var_other2 = new PrimVS<Machine>();
        }
        
        public machine_Process(int id) {
            super("machine_Process", id, BufferSemantics.queue, state__init, state__init
                , state_inits
                , state_SendCount
                , state_done
                
            );
            state__init.addHandlers(new GotoEventHandler(Events.event_unit, state_inits));
            state_inits.addHandlers(new GotoEventHandler(Events.event_myCount, state_inits),
                new EventHandler(Events.event_init) {
                    @Override public void handleEvent(Bdd pc, UnionVS payload, Machine machine, Outcome outcome) {
                        ((machine_Process)machine).anonfunc_5(pc, machine.sendEffects, (TupleVS) ValueSummary.fromAny(pc, new TupleVS(new PrimVS<Machine>(), new PrimVS<Machine>()).getClass(), payload));
                    }
                },
                new GotoEventHandler(Events.event_Resp, state_SendCount));
            state_SendCount.addHandlers(new GotoEventHandler(Events.event_unit, state_done),
                new GotoEventHandler(Events.event_Resp, state_SendCount),
                new EventHandler(Events.event_myCount) {
                    @Override public void handleEvent(Bdd pc, UnionVS payload, Machine machine, Outcome outcome) {
                        ((machine_Process)machine).anonfunc_6(pc, machine.sendEffects, (PrimVS<Integer>) ValueSummary.fromAny(pc, new PrimVS<Integer>(0).getClass(), payload));
                    }
                });
            state_done.addHandlers(new IgnoreEventHandler(Events.event_Resp),
                new IgnoreEventHandler(Events.event_myCount));
        }
        
        Bdd 
        anonfunc_2(
            Bdd pc_9,
            EffectCollection effects,
            Outcome outcome,
            PrimVS<Machine> var_payload
        ) {
            PrimVS<EventName> var_$tmp0 =
                new PrimVS<EventName>(Events.event_null).guard(pc_9);
            
            PrimVS<Machine> temp_var_42;
            temp_var_42 = var_payload.guard(pc_9);
            var_parent = var_parent.update(pc_9, temp_var_42);
            
            PrimVS<EventName> temp_var_43;
            temp_var_43 = new PrimVS<EventName>(Events.event_unit).guard(pc_9);
            var_$tmp0 = var_$tmp0.update(pc_9, temp_var_43);
            
            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.addGuardedRaise(pc_9, var_$tmp0.guard(pc_9));
            pc_9 = Bdd.constFalse();
            
            return pc_9;
        }
        
        void 
        anonfunc_3(
            Bdd pc_10,
            EffectCollection effects,
            UnionVS var_payload
        ) {
            PrimVS<Integer> temp_var_44;
            temp_var_44 = new PrimVS<Integer>(0).guard(pc_10);
            var_count = var_count.update(pc_10, temp_var_44);
            
        }
        
        void 
        anonfunc_5(
            Bdd pc_11,
            EffectCollection effects,
            TupleVS var_payload
        ) {
            TupleVS var_$tmp0 =
                new TupleVS(new PrimVS<Machine>().guard(pc_11), new PrimVS<Machine>().guard(pc_11));
            
            TupleVS temp_var_45;
            temp_var_45 = var_payload.guard(pc_11);
            var_$tmp0 = var_$tmp0.update(pc_11, temp_var_45);
            
            func_initaction(pc_11, effects, var_$tmp0.guard(pc_11));
            
        }
        
        void 
        func_initaction(
            Bdd pc_12,
            EffectCollection effects,
            TupleVS var_payload
        ) {
            PrimVS<Machine> var_$tmp0 =
                new PrimVS<Machine>().guard(pc_12);
            
            PrimVS<Machine> var_$tmp1 =
                new PrimVS<Machine>().guard(pc_12);
            
            PrimVS<Machine> var_$tmp2 =
                new PrimVS<Machine>().guard(pc_12);
            
            PrimVS<Machine> var_$tmp3 =
                new PrimVS<Machine>().guard(pc_12);
            
            PrimVS<Machine> var_$tmp4 =
                new PrimVS<Machine>().guard(pc_12);
            
            PrimVS<EventName> var_$tmp5 =
                new PrimVS<EventName>(Events.event_null).guard(pc_12);
            
            PrimVS<Machine> temp_var_46;
            temp_var_46 = (PrimVS<Machine>)(var_payload.guard(pc_12)).getField(0);
            var_$tmp0 = var_$tmp0.update(pc_12, temp_var_46);
            
            PrimVS<Machine> temp_var_47;
            temp_var_47 = var_$tmp0.guard(pc_12);
            var_$tmp1 = var_$tmp1.update(pc_12, temp_var_47);
            
            PrimVS<Machine> temp_var_48;
            temp_var_48 = var_$tmp1.guard(pc_12);
            var_other1 = var_other1.update(pc_12, temp_var_48);
            
            PrimVS<Machine> temp_var_49;
            temp_var_49 = (PrimVS<Machine>)(var_payload.guard(pc_12)).getField(1);
            var_$tmp2 = var_$tmp2.update(pc_12, temp_var_49);
            
            PrimVS<Machine> temp_var_50;
            temp_var_50 = var_$tmp2.guard(pc_12);
            var_$tmp3 = var_$tmp3.update(pc_12, temp_var_50);
            
            PrimVS<Machine> temp_var_51;
            temp_var_51 = var_$tmp3.guard(pc_12);
            var_other2 = var_other2.update(pc_12, temp_var_51);
            
            PrimVS<Machine> temp_var_52;
            temp_var_52 = var_parent.guard(pc_12);
            var_$tmp4 = var_$tmp4.update(pc_12, temp_var_52);
            
            PrimVS<EventName> temp_var_53;
            temp_var_53 = new PrimVS<EventName>(Events.event_Req).guard(pc_12);
            var_$tmp5 = var_$tmp5.update(pc_12, temp_var_53);
            
            effects.send(pc_12, var_$tmp4.guard(pc_12), var_$tmp5.guard(pc_12), null);
            
        }
        
        void 
        func_ConfirmThatInSync(
            Bdd pc_13,
            EffectCollection effects,
            PrimVS<Integer> var_payload
        ) {
            PrimVS<Boolean> var_$tmp0 =
                new PrimVS<Boolean>(false).guard(pc_13);
            
            PrimVS<Integer> var_$tmp1 =
                new PrimVS<Integer>(0).guard(pc_13);
            
            PrimVS<Boolean> var_$tmp2 =
                new PrimVS<Boolean>(false).guard(pc_13);
            
            PrimVS<Boolean> var_$tmp3 =
                new PrimVS<Boolean>(false).guard(pc_13);
            
            PrimVS<String> var_$tmp4 =
                new PrimVS<String>("").guard(pc_13);
            
            PrimVS<Boolean> temp_var_54;
            temp_var_54 = (var_count.guard(pc_13)).apply2(var_payload.guard(pc_13), (temp_var_55, temp_var_56) -> temp_var_55 <= temp_var_56);
            var_$tmp0 = var_$tmp0.update(pc_13, temp_var_54);
            
            PrimVS<Boolean> temp_var_57;
            temp_var_57 = var_$tmp0.guard(pc_13);
            var_$tmp3 = var_$tmp3.update(pc_13, temp_var_57);
            
            PrimVS<Boolean> temp_var_58 = var_$tmp3.guard(pc_13);
            Bdd pc_14 = BoolUtils.trueCond(temp_var_58);
            Bdd pc_15 = BoolUtils.falseCond(temp_var_58);
            boolean jumpedOut_2 = false;
            boolean jumpedOut_3 = false;
            if (!pc_14.isConstFalse()) {
                // 'then' branch
                PrimVS<Integer> temp_var_59;
                temp_var_59 = (var_payload.guard(pc_14)).apply2(new PrimVS<Integer>(1).guard(pc_14), (temp_var_60, temp_var_61) -> temp_var_60 - temp_var_61);
                var_$tmp1 = var_$tmp1.update(pc_14, temp_var_59);
                
                PrimVS<Boolean> temp_var_62;
                temp_var_62 = (var_count.guard(pc_14)).apply2(var_$tmp1.guard(pc_14), (temp_var_63, temp_var_64) -> temp_var_63 >= temp_var_64);
                var_$tmp2 = var_$tmp2.update(pc_14, temp_var_62);
                
                PrimVS<Boolean> temp_var_65;
                temp_var_65 = var_$tmp2.guard(pc_14);
                var_$tmp3 = var_$tmp3.update(pc_14, temp_var_65);
                
            }
            if (jumpedOut_2 || jumpedOut_3) {
                pc_13 = pc_14.or(pc_15);
            }
            
            PrimVS<String> temp_var_66;
            temp_var_66 = new PrimVS<String>(String.format("")).guard(pc_13);
            var_$tmp4 = var_$tmp4.update(pc_13, temp_var_66);
            
            Assert.prop(!(var_$tmp3.guard(pc_13)).getValues().contains(Boolean.FALSE), "Plang.Compiler.TypeChecker.AST.Expressions.VariableAccessExpr", scheduler, pc_13);
        }
        
        Bdd 
        anonfunc_4(
            Bdd pc_16,
            EffectCollection effects,
            Outcome outcome
        ) {
            PrimVS<Integer> var_$tmp0 =
                new PrimVS<Integer>(0).guard(pc_16);
            
            PrimVS<Machine> var_$tmp1 =
                new PrimVS<Machine>().guard(pc_16);
            
            PrimVS<EventName> var_$tmp2 =
                new PrimVS<EventName>(Events.event_null).guard(pc_16);
            
            PrimVS<Integer> var_$tmp3 =
                new PrimVS<Integer>(0).guard(pc_16);
            
            PrimVS<Machine> var_$tmp4 =
                new PrimVS<Machine>().guard(pc_16);
            
            PrimVS<EventName> var_$tmp5 =
                new PrimVS<EventName>(Events.event_null).guard(pc_16);
            
            PrimVS<Integer> var_$tmp6 =
                new PrimVS<Integer>(0).guard(pc_16);
            
            PrimVS<Machine> var_$tmp7 =
                new PrimVS<Machine>().guard(pc_16);
            
            PrimVS<EventName> var_$tmp8 =
                new PrimVS<EventName>(Events.event_null).guard(pc_16);
            
            PrimVS<Boolean> var_$tmp9 =
                new PrimVS<Boolean>(false).guard(pc_16);
            
            PrimVS<EventName> var_$tmp10 =
                new PrimVS<EventName>(Events.event_null).guard(pc_16);
            
            PrimVS<Integer> temp_var_67;
            temp_var_67 = (var_count.guard(pc_16)).apply2(new PrimVS<Integer>(1).guard(pc_16), (temp_var_68, temp_var_69) -> temp_var_68 + temp_var_69);
            var_$tmp0 = var_$tmp0.update(pc_16, temp_var_67);
            
            PrimVS<Integer> temp_var_70;
            temp_var_70 = var_$tmp0.guard(pc_16);
            var_count = var_count.update(pc_16, temp_var_70);
            
            PrimVS<Machine> temp_var_71;
            temp_var_71 = var_other1.guard(pc_16);
            var_$tmp1 = var_$tmp1.update(pc_16, temp_var_71);
            
            PrimVS<EventName> temp_var_72;
            temp_var_72 = new PrimVS<EventName>(Events.event_myCount).guard(pc_16);
            var_$tmp2 = var_$tmp2.update(pc_16, temp_var_72);
            
            PrimVS<Integer> temp_var_73;
            temp_var_73 = var_count.guard(pc_16);
            var_$tmp3 = var_$tmp3.update(pc_16, temp_var_73);
            
            effects.send(pc_16, var_$tmp1.guard(pc_16), var_$tmp2.guard(pc_16), new UnionVS(var_$tmp3.guard(pc_16)));
            
            PrimVS<Machine> temp_var_74;
            temp_var_74 = var_other2.guard(pc_16);
            var_$tmp4 = var_$tmp4.update(pc_16, temp_var_74);
            
            PrimVS<EventName> temp_var_75;
            temp_var_75 = new PrimVS<EventName>(Events.event_myCount).guard(pc_16);
            var_$tmp5 = var_$tmp5.update(pc_16, temp_var_75);
            
            PrimVS<Integer> temp_var_76;
            temp_var_76 = var_count.guard(pc_16);
            var_$tmp6 = var_$tmp6.update(pc_16, temp_var_76);
            
            effects.send(pc_16, var_$tmp4.guard(pc_16), var_$tmp5.guard(pc_16), new UnionVS(var_$tmp6.guard(pc_16)));
            
            PrimVS<Machine> temp_var_77;
            temp_var_77 = var_parent.guard(pc_16);
            var_$tmp7 = var_$tmp7.update(pc_16, temp_var_77);
            
            PrimVS<EventName> temp_var_78;
            temp_var_78 = new PrimVS<EventName>(Events.event_Req).guard(pc_16);
            var_$tmp8 = var_$tmp8.update(pc_16, temp_var_78);
            
            effects.send(pc_16, var_$tmp7.guard(pc_16), var_$tmp8.guard(pc_16), null);
            
            PrimVS<Boolean> temp_var_79;
            temp_var_79 = (var_count.guard(pc_16)).apply2(new PrimVS<Integer>(10).guard(pc_16), (temp_var_80, temp_var_81) -> temp_var_80 > temp_var_81);
            var_$tmp9 = var_$tmp9.update(pc_16, temp_var_79);
            
            PrimVS<Boolean> temp_var_82 = var_$tmp9.guard(pc_16);
            Bdd pc_17 = BoolUtils.trueCond(temp_var_82);
            Bdd pc_18 = BoolUtils.falseCond(temp_var_82);
            boolean jumpedOut_4 = false;
            boolean jumpedOut_5 = false;
            if (!pc_17.isConstFalse()) {
                // 'then' branch
                PrimVS<EventName> temp_var_83;
                temp_var_83 = new PrimVS<EventName>(Events.event_unit).guard(pc_17);
                var_$tmp10 = var_$tmp10.update(pc_17, temp_var_83);
                
                // NOTE (TODO): We currently perform no typechecking on the payload!
                outcome.addGuardedRaise(pc_17, var_$tmp10.guard(pc_17));
                pc_17 = Bdd.constFalse();
                jumpedOut_4 = true;
                
            }
            if (!pc_18.isConstFalse()) {
                // 'else' branch
            }
            if (jumpedOut_4 || jumpedOut_5) {
                pc_16 = pc_17.or(pc_18);
            }
            
            if (!pc_16.isConstFalse()) {
            }
            return pc_16;
        }
        
        void 
        anonfunc_6(
            Bdd pc_19,
            EffectCollection effects,
            PrimVS<Integer> var_payload
        ) {
            PrimVS<Integer> var_$tmp0 =
                new PrimVS<Integer>(0).guard(pc_19);
            
            PrimVS<Integer> temp_var_84;
            temp_var_84 = var_payload.guard(pc_19);
            var_$tmp0 = var_$tmp0.update(pc_19, temp_var_84);
            
            func_ConfirmThatInSync(pc_19, effects, var_$tmp0.guard(pc_19));
            
        }
        
    }
    
    // Skipping Implementation 'DefaultImpl'

    private static Machine start = new machine_Main(0);
    
    @Override
    public Machine getStart() { return start; }
    
}
