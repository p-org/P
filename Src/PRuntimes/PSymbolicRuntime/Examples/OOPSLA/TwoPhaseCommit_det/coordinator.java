package psymbolic;
import psymbolic.commandline.*;
import psymbolic.valuesummary.*;
import psymbolic.runtime.*;
import psymbolic.runtime.scheduler.*;
import psymbolic.runtime.machine.*;
import psymbolic.runtime.logger.*;
import psymbolic.runtime.machine.buffer.*;
import psymbolic.runtime.machine.eventhandlers.*;
import java.util.List;
import java.util.ArrayList;
import java.util.Map;
import java.util.HashMap;
import java.util.function.Consumer;
import java.util.function.Function;

public class coordinator implements Program {
    
    public static Scheduler scheduler;
    
    @Override
    public void setScheduler (Scheduler s) { this.scheduler = s; }
    
    
    
    // Skipping EnumElem 'EQKEY'

    // Skipping EnumElem 'EQVAL'

    // Skipping EnumElem 'NEQKEY'

    // Skipping EnumElem 'NEQVAL'

    // Skipping EnumElem 'SUCCESS'

    // Skipping EnumElem 'ERROR'

    // Skipping EnumElem 'TIMEOUT'

    // Skipping PEnum 'tPreds'

    // Skipping PEnum 'tTransStatus'

    public static Event _null = new Event("_null");
    public static Event _halt = new Event("_halt");
    public static Event ePrepareReq = new Event("ePrepareReq");
    public static Event ePrepareResp = new Event("ePrepareResp");
    public static Event eCommitTrans = new Event("eCommitTrans");
    public static Event eWriteTransReq = new Event("eWriteTransReq");
    public static Event eWriteTransResp = new Event("eWriteTransResp");
    public static Event eReadTransReq = new Event("eReadTransReq");
    public static Event eReadTransResp = new Event("eReadTransResp");
    // Skipping Interface 'Coordinator'

    // Skipping Interface 'TestClient'

    // Skipping Interface 'Participant'

    // Skipping Interface 'Main'

    public static class Coordinator extends Machine {
        
        static State Init = new State("Init") {
            @Override public void entry(Guard pc_0, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Coordinator)machine).anonfun_0(pc_0, machine.sendBuffer, outcome, payload != null ? (ListVS<PrimitiveVS<Machine>>) ValueSummary.castFromAny(pc_0, new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_0), payload) : new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_0));
            }
            @Override public void exit(Guard pc, Machine machine) {
                ((Coordinator)machine).anonfun_1(pc, machine.sendBuffer);
            }
        };
        static State WaitForTransactions = new State("WaitForTransactions") {
        };
        static State WaitForPrepareResponses = new State("WaitForPrepareResponses") {
            @Override public void exit(Guard pc, Machine machine) {
                ((Coordinator)machine).anonfun_2(pc, machine.sendBuffer);
            }
        };
        private ListVS<PrimitiveVS<Machine>> var_participants = new ListVS<PrimitiveVS<Machine>>(Guard.constTrue());
        private NamedTupleVS var_pendingWrTrans = new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()));
        private PredVS<String> /* pred enum tPreds */ var_currTransId = new PredVS<String> /* pred enum tPreds */();
        private PrimitiveVS<Integer> var_countPrepareResponses = new PrimitiveVS<Integer>(0);
        private ListVS<PredVS<String> /* pred enum tPreds */> var_choices = new ListVS<PredVS<String> /* pred enum tPreds */>(Guard.constTrue());
        
        @Override
        public void reset() {
                super.reset();
                var_participants = new ListVS<PrimitiveVS<Machine>>(Guard.constTrue());
                var_pendingWrTrans = new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()));
                var_currTransId = new PredVS<String> /* pred enum tPreds */();
                var_countPrepareResponses = new PrimitiveVS<Integer>(0);
                var_choices = new ListVS<PredVS<String> /* pred enum tPreds */>(Guard.constTrue());
        }
        
        @Override
        public List<ValueSummary> getLocalState() {
                List<ValueSummary> res = super.getLocalState();
                res.add(var_participants);
                res.add(var_pendingWrTrans);
                res.add(var_currTransId);
                res.add(var_countPrepareResponses);
                res.add(var_choices);
                return res;
        }
        
        public Coordinator(int id) {
            super("Coordinator", id, EventBufferSemantics.queue, Init, Init
                , WaitForTransactions
                , WaitForPrepareResponses
                
            );
            Init.addHandlers();
            WaitForTransactions.addHandlers(new EventHandler(eWriteTransReq) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Coordinator)machine).anonfun_3(pc, machine.sendBuffer, outcome, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())), payload));
                    }
                },
                new EventHandler(eReadTransReq) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Coordinator)machine).anonfun_4(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PredVS<String> /* pred enum tPreds */()), payload));
                    }
                },
                new IgnoreEventHandler(ePrepareResp));
            WaitForPrepareResponses.addHandlers(new DeferEventHandler(eWriteTransReq)
                ,
                new EventHandler(ePrepareResp) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Coordinator)machine).anonfun_5(pc, machine.sendBuffer, outcome, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("participant", new PrimitiveVS<Machine>(), "transId", new PredVS<String> /* pred enum tPreds */(), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)), payload));
                    }
                },
                new EventHandler(eReadTransReq) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Coordinator)machine).anonfun_6(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PredVS<String> /* pred enum tPreds */()), payload));
                    }
                });
        }
        
        Guard 
        anonfun_0(
            Guard pc_1,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            ListVS<PrimitiveVS<Machine>> var_payload
        ) {
            PredVS<String> /* pred enum tPreds */ var_$tmp0 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_1);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp1 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_1);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp2 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_1);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp3 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_1);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp4 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_1);
            
            ListVS<PrimitiveVS<Machine>> temp_var_0;
            temp_var_0 = var_payload.restrict(pc_1);
            var_participants = var_participants.updateUnderGuard(pc_1, temp_var_0);
            
            PredVS<String> /* pred enum tPreds */ temp_var_1;
            temp_var_1 = new PredVS<String>("EQKEY").restrict(pc_1);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_1, temp_var_1);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_2 = var_choices.restrict(pc_1);    
            temp_var_2 = var_choices.restrict(pc_1).insert(new PrimitiveVS<Integer>(0).restrict(pc_1), var_$tmp0.restrict(pc_1));
            var_choices = var_choices.updateUnderGuard(pc_1, temp_var_2);
            
            PredVS<String> /* pred enum tPreds */ temp_var_3;
            temp_var_3 = new PredVS<String>("NEQKEY").restrict(pc_1);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_1, temp_var_3);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_4 = var_choices.restrict(pc_1);    
            temp_var_4 = var_choices.restrict(pc_1).insert(new PrimitiveVS<Integer>(1).restrict(pc_1), var_$tmp1.restrict(pc_1));
            var_choices = var_choices.updateUnderGuard(pc_1, temp_var_4);
            
            PredVS<String> /* pred enum tPreds */ temp_var_5;
            temp_var_5 = new PredVS<String>("EQVAL").restrict(pc_1);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_1, temp_var_5);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_6 = var_choices.restrict(pc_1);    
            temp_var_6 = var_choices.restrict(pc_1).insert(new PrimitiveVS<Integer>(2).restrict(pc_1), var_$tmp2.restrict(pc_1));
            var_choices = var_choices.updateUnderGuard(pc_1, temp_var_6);
            
            PredVS<String> /* pred enum tPreds */ temp_var_7;
            temp_var_7 = new PredVS<String>("NEQVAL").restrict(pc_1);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_1, temp_var_7);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_8 = var_choices.restrict(pc_1);    
            temp_var_8 = var_choices.restrict(pc_1).insert(new PrimitiveVS<Integer>(3).restrict(pc_1), var_$tmp3.restrict(pc_1));
            var_choices = var_choices.updateUnderGuard(pc_1, temp_var_8);
            
            PredVS<String> /* pred enum tPreds */ temp_var_9;
            temp_var_9 = new PredVS<String> /* pred enum tPreds */(var_choices.restrict(pc_1), pc_1);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_1, temp_var_9);
            
            PredVS<String> /* pred enum tPreds */ temp_var_10;
            temp_var_10 = var_$tmp4.restrict(pc_1);
            var_currTransId = var_currTransId.updateUnderGuard(pc_1, temp_var_10);
            
            outcome.addGuardedGoto(pc_1, WaitForTransactions);
            pc_1 = Guard.constFalse();
            
            return pc_1;
        }
        
        void 
        anonfun_1(
            Guard pc_2,
            EventBuffer effects
        ) {
        }
        
        Guard 
        anonfun_3(
            Guard pc_3,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            NamedTupleVS var_wTrans
        ) {
            PredVS<String> /* pred enum tPreds */ var_$tmp0 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_3);
            
            PrimitiveVS<Event> var_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_3);
            
            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_3);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp3 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_3);
            
            NamedTupleVS var_$tmp4 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_3);
            
            NamedTupleVS var_$tmp5 =
                new NamedTupleVS("coordinator", new PrimitiveVS<Machine>(), "transId", new PredVS<String> /* pred enum tPreds */(), "rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_3);
            
            PrimitiveVS<Event> var_inline_0_message =
                new PrimitiveVS<Event>(_null).restrict(pc_3);
            
            NamedTupleVS var_inline_0_payload =
                new NamedTupleVS("coordinator", new PrimitiveVS<Machine>(), "transId", new PredVS<String> /* pred enum tPreds */(), "rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_3);
            
            PrimitiveVS<Integer> var_local_0_i =
                new PrimitiveVS<Integer>(0).restrict(pc_3);
            
            PrimitiveVS<Integer> var_local_0_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_3);
            
            PrimitiveVS<Boolean> var_local_0_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_3);
            
            PrimitiveVS<Boolean> var_local_0_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_3);
            
            PrimitiveVS<Machine> var_local_0_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_3);
            
            PrimitiveVS<Machine> var_local_0_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_3);
            
            PrimitiveVS<Event> var_local_0_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_3);
            
            UnionVS var_local_0_$tmp6 =
                new UnionVS().restrict(pc_3);
            
            PrimitiveVS<Integer> var_local_0_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_3);
            
            NamedTupleVS temp_var_11;
            temp_var_11 = var_wTrans.restrict(pc_3);
            var_pendingWrTrans = var_pendingWrTrans.updateUnderGuard(pc_3, temp_var_11);
            
            PredVS<String> /* pred enum tPreds */ temp_var_12;
            temp_var_12 = new PredVS<String> /* pred enum tPreds */(var_choices.restrict(pc_3), pc_3);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_3, temp_var_12);
            
            PredVS<String> /* pred enum tPreds */ temp_var_13;
            temp_var_13 = var_$tmp0.restrict(pc_3);
            var_currTransId = var_currTransId.updateUnderGuard(pc_3, temp_var_13);
            
            PrimitiveVS<Event> temp_var_14;
            temp_var_14 = new PrimitiveVS<Event>(ePrepareReq).restrict(pc_3);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_3, temp_var_14);
            
            PrimitiveVS<Machine> temp_var_15;
            temp_var_15 = new PrimitiveVS<Machine>(this).restrict(pc_3);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_3, temp_var_15);
            
            PredVS<String> /* pred enum tPreds */ temp_var_16;
            temp_var_16 = var_currTransId.restrict(pc_3);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_3, temp_var_16);
            
            NamedTupleVS temp_var_17;
            temp_var_17 = (NamedTupleVS)((var_wTrans.restrict(pc_3)).getField("rec"));
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_3, temp_var_17);
            
            NamedTupleVS temp_var_18;
            temp_var_18 = new NamedTupleVS("coordinator", var_$tmp2.restrict(pc_3), "transId", var_$tmp3.restrict(pc_3), "rec", var_$tmp4.restrict(pc_3));
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_3, temp_var_18);
            
            PrimitiveVS<Event> temp_var_19;
            temp_var_19 = var_$tmp1.restrict(pc_3);
            var_inline_0_message = var_inline_0_message.updateUnderGuard(pc_3, temp_var_19);
            
            NamedTupleVS temp_var_20;
            temp_var_20 = var_$tmp5.restrict(pc_3);
            var_inline_0_payload = var_inline_0_payload.updateUnderGuard(pc_3, temp_var_20);
            
            PrimitiveVS<Integer> temp_var_21;
            temp_var_21 = new PrimitiveVS<Integer>(0).restrict(pc_3);
            var_local_0_i = var_local_0_i.updateUnderGuard(pc_3, temp_var_21);
            
            java.util.List<Guard> loop_exits_0 = new java.util.ArrayList<>();
            boolean loop_early_ret_0 = false;
            Guard pc_4 = pc_3;
            while (!pc_4.isFalse()) {
                PrimitiveVS<Integer> temp_var_22;
                temp_var_22 = var_participants.restrict(pc_4).size();
                var_local_0_$tmp0 = var_local_0_$tmp0.updateUnderGuard(pc_4, temp_var_22);
                
                PrimitiveVS<Boolean> temp_var_23;
                temp_var_23 = (var_local_0_i.restrict(pc_4)).apply(var_local_0_$tmp0.restrict(pc_4), (temp_var_24, temp_var_25) -> temp_var_24 < temp_var_25);
                var_local_0_$tmp1 = var_local_0_$tmp1.updateUnderGuard(pc_4, temp_var_23);
                
                PrimitiveVS<Boolean> temp_var_26;
                temp_var_26 = var_local_0_$tmp1.restrict(pc_4);
                var_local_0_$tmp2 = var_local_0_$tmp2.updateUnderGuard(pc_4, temp_var_26);
                
                PrimitiveVS<Boolean> temp_var_27 = var_local_0_$tmp2.restrict(pc_4);
                Guard pc_5 = BooleanVS.getTrueGuard(temp_var_27);
                Guard pc_6 = BooleanVS.getFalseGuard(temp_var_27);
                boolean jumpedOut_0 = false;
                boolean jumpedOut_1 = false;
                if (!pc_5.isFalse()) {
                    // 'then' branch
                }
                if (!pc_6.isFalse()) {
                    // 'else' branch
                    loop_exits_0.add(pc_6);
                    jumpedOut_1 = true;
                    pc_6 = Guard.constFalse();
                    
                }
                if (jumpedOut_0 || jumpedOut_1) {
                    pc_4 = pc_5.or(pc_6);
                }
                
                if (!pc_4.isFalse()) {
                    PrimitiveVS<Machine> temp_var_28;
                    temp_var_28 = var_participants.restrict(pc_4).get(var_local_0_i.restrict(pc_4));
                    var_local_0_$tmp3 = var_local_0_$tmp3.updateUnderGuard(pc_4, temp_var_28);
                    
                    PrimitiveVS<Machine> temp_var_29;
                    temp_var_29 = var_local_0_$tmp3.restrict(pc_4);
                    var_local_0_$tmp4 = var_local_0_$tmp4.updateUnderGuard(pc_4, temp_var_29);
                    
                    PrimitiveVS<Event> temp_var_30;
                    temp_var_30 = var_inline_0_message.restrict(pc_4);
                    var_local_0_$tmp5 = var_local_0_$tmp5.updateUnderGuard(pc_4, temp_var_30);
                    
                    UnionVS temp_var_31;
                    temp_var_31 = ValueSummary.castToAny(pc_4, var_inline_0_payload.restrict(pc_4));
                    var_local_0_$tmp6 = var_local_0_$tmp6.updateUnderGuard(pc_4, temp_var_31);
                    
                    effects.send(pc_4, var_local_0_$tmp4.restrict(pc_4), var_local_0_$tmp5.restrict(pc_4), new UnionVS(var_local_0_$tmp6.restrict(pc_4)));
                    
                    PrimitiveVS<Integer> temp_var_32;
                    temp_var_32 = (var_local_0_i.restrict(pc_4)).apply(new PrimitiveVS<Integer>(1).restrict(pc_4), (temp_var_33, temp_var_34) -> temp_var_33 + temp_var_34);
                    var_local_0_$tmp7 = var_local_0_$tmp7.updateUnderGuard(pc_4, temp_var_32);
                    
                    PrimitiveVS<Integer> temp_var_35;
                    temp_var_35 = var_local_0_$tmp7.restrict(pc_4);
                    var_local_0_i = var_local_0_i.updateUnderGuard(pc_4, temp_var_35);
                    
                }
            }
            if (loop_early_ret_0) {
                pc_3 = Guard.orMany(loop_exits_0);
            }
            
            outcome.addGuardedGoto(pc_3, WaitForPrepareResponses);
            pc_3 = Guard.constFalse();
            
            return pc_3;
        }
        
        void 
        anonfun_4(
            Guard pc_7,
            EventBuffer effects,
            NamedTupleVS var_rTrans
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_7);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_7);
            
            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_7);
            
            NamedTupleVS var_$tmp3 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PredVS<String> /* pred enum tPreds */()).restrict(pc_7);
            
            PrimitiveVS<Machine> temp_var_36;
            temp_var_36 = (PrimitiveVS<Machine>) scheduler.getNextElement(var_participants.restrict(pc_7), pc_7);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_7, temp_var_36);
            
            PrimitiveVS<Machine> temp_var_37;
            temp_var_37 = var_$tmp0.restrict(pc_7);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_7, temp_var_37);
            
            PrimitiveVS<Event> temp_var_38;
            temp_var_38 = new PrimitiveVS<Event>(eReadTransReq).restrict(pc_7);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_7, temp_var_38);
            
            NamedTupleVS temp_var_39;
            temp_var_39 = var_rTrans.restrict(pc_7);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_7, temp_var_39);
            
            effects.send(pc_7, var_$tmp1.restrict(pc_7), var_$tmp2.restrict(pc_7), new UnionVS(var_$tmp3.restrict(pc_7)));
            
        }
        
        Guard 
        anonfun_5(
            Guard pc_8,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            NamedTupleVS var_resp
        ) {
            PredVS<String> /* pred enum tPreds */ var_$tmp0 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_8);
            
            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_8);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp2 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_8);
            
            PrimitiveVS<Boolean> var_$tmp3 =
                new PrimitiveVS<Boolean>(false).restrict(pc_8);
            
            PrimitiveVS<Integer> var_$tmp4 =
                new PrimitiveVS<Integer>(0).restrict(pc_8);
            
            PrimitiveVS<Integer> var_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_8);
            
            PrimitiveVS<Boolean> var_$tmp6 =
                new PrimitiveVS<Boolean>(false).restrict(pc_8);
            
            PrimitiveVS<Event> var_local_2_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_8);
            
            PredVS<String> /* pred enum tPreds */ var_local_2_$tmp1 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_8);
            
            PrimitiveVS<Machine> var_local_2_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_8);
            
            PrimitiveVS<Machine> var_local_2_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_8);
            
            PrimitiveVS<Event> var_local_2_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_8);
            
            PredVS<String> /* pred enum tPreds */ var_local_2_$tmp5 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_8);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_local_2_$tmp6 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_8);
            
            NamedTupleVS var_local_2_$tmp7 =
                new NamedTupleVS("transId", new PredVS<String> /* pred enum tPreds */(), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_8);
            
            PrimitiveVS<Event> var_local_2_inline_1_message =
                new PrimitiveVS<Event>(_null).restrict(pc_8);
            
            PredVS<String> /* pred enum tPreds */ var_local_2_inline_1_payload =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_8);
            
            PrimitiveVS<Integer> var_local_2_local_1_i =
                new PrimitiveVS<Integer>(0).restrict(pc_8);
            
            PrimitiveVS<Integer> var_local_2_local_1_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_8);
            
            PrimitiveVS<Boolean> var_local_2_local_1_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_8);
            
            PrimitiveVS<Boolean> var_local_2_local_1_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_8);
            
            PrimitiveVS<Machine> var_local_2_local_1_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_8);
            
            PrimitiveVS<Machine> var_local_2_local_1_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_8);
            
            PrimitiveVS<Event> var_local_2_local_1_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_8);
            
            UnionVS var_local_2_local_1_$tmp6 =
                new UnionVS().restrict(pc_8);
            
            PrimitiveVS<Integer> var_local_2_local_1_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_8);
            
            PredVS<String> /* pred enum tPreds */ temp_var_40;
            temp_var_40 = (PredVS<String> /* pred enum tPreds */)((var_resp.restrict(pc_8)).getField("transId"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_8, temp_var_40);
            
            PrimitiveVS<Boolean> temp_var_41;
            temp_var_41 = var_currTransId.restrict(pc_8).symbolicEquals(var_$tmp0.restrict(pc_8), pc_8);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_8, temp_var_41);
            
            PrimitiveVS<Boolean> temp_var_42 = var_$tmp1.restrict(pc_8);
            Guard pc_9 = BooleanVS.getTrueGuard(temp_var_42);
            Guard pc_10 = BooleanVS.getFalseGuard(temp_var_42);
            boolean jumpedOut_2 = false;
            boolean jumpedOut_3 = false;
            if (!pc_9.isFalse()) {
                // 'then' branch
                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_43;
                temp_var_43 = (PrimitiveVS<Integer> /* enum tTransStatus */)((var_resp.restrict(pc_9)).getField("status"));
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_9, temp_var_43);
                
                PrimitiveVS<Boolean> temp_var_44;
                temp_var_44 = var_$tmp2.restrict(pc_9).symbolicEquals(new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_9), pc_9);
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_9, temp_var_44);
                
                PrimitiveVS<Boolean> temp_var_45 = var_$tmp3.restrict(pc_9);
                Guard pc_11 = BooleanVS.getTrueGuard(temp_var_45);
                Guard pc_12 = BooleanVS.getFalseGuard(temp_var_45);
                boolean jumpedOut_4 = false;
                boolean jumpedOut_5 = false;
                if (!pc_11.isFalse()) {
                    // 'then' branch
                    PrimitiveVS<Integer> temp_var_46;
                    temp_var_46 = (var_countPrepareResponses.restrict(pc_11)).apply(new PrimitiveVS<Integer>(1).restrict(pc_11), (temp_var_47, temp_var_48) -> temp_var_47 + temp_var_48);
                    var_$tmp4 = var_$tmp4.updateUnderGuard(pc_11, temp_var_46);
                    
                    PrimitiveVS<Integer> temp_var_49;
                    temp_var_49 = var_$tmp4.restrict(pc_11);
                    var_countPrepareResponses = var_countPrepareResponses.updateUnderGuard(pc_11, temp_var_49);
                    
                    PrimitiveVS<Integer> temp_var_50;
                    temp_var_50 = var_participants.restrict(pc_11).size();
                    var_$tmp5 = var_$tmp5.updateUnderGuard(pc_11, temp_var_50);
                    
                    PrimitiveVS<Boolean> temp_var_51;
                    temp_var_51 = var_countPrepareResponses.restrict(pc_11).symbolicEquals(var_$tmp5.restrict(pc_11), pc_11);
                    var_$tmp6 = var_$tmp6.updateUnderGuard(pc_11, temp_var_51);
                    
                    PrimitiveVS<Boolean> temp_var_52 = var_$tmp6.restrict(pc_11);
                    Guard pc_13 = BooleanVS.getTrueGuard(temp_var_52);
                    Guard pc_14 = BooleanVS.getFalseGuard(temp_var_52);
                    boolean jumpedOut_6 = false;
                    boolean jumpedOut_7 = false;
                    if (!pc_13.isFalse()) {
                        // 'then' branch
                        PrimitiveVS<Event> temp_var_53;
                        temp_var_53 = new PrimitiveVS<Event>(eCommitTrans).restrict(pc_13);
                        var_local_2_$tmp0 = var_local_2_$tmp0.updateUnderGuard(pc_13, temp_var_53);
                        
                        PredVS<String> /* pred enum tPreds */ temp_var_54;
                        temp_var_54 = var_currTransId.restrict(pc_13);
                        var_local_2_$tmp1 = var_local_2_$tmp1.updateUnderGuard(pc_13, temp_var_54);
                        
                        PrimitiveVS<Event> temp_var_55;
                        temp_var_55 = var_local_2_$tmp0.restrict(pc_13);
                        var_local_2_inline_1_message = var_local_2_inline_1_message.updateUnderGuard(pc_13, temp_var_55);
                        
                        PredVS<String> /* pred enum tPreds */ temp_var_56;
                        temp_var_56 = var_local_2_$tmp1.restrict(pc_13);
                        var_local_2_inline_1_payload = var_local_2_inline_1_payload.updateUnderGuard(pc_13, temp_var_56);
                        
                        PrimitiveVS<Integer> temp_var_57;
                        temp_var_57 = new PrimitiveVS<Integer>(0).restrict(pc_13);
                        var_local_2_local_1_i = var_local_2_local_1_i.updateUnderGuard(pc_13, temp_var_57);
                        
                        java.util.List<Guard> loop_exits_1 = new java.util.ArrayList<>();
                        boolean loop_early_ret_1 = false;
                        Guard pc_15 = pc_13;
                        while (!pc_15.isFalse()) {
                            PrimitiveVS<Integer> temp_var_58;
                            temp_var_58 = var_participants.restrict(pc_15).size();
                            var_local_2_local_1_$tmp0 = var_local_2_local_1_$tmp0.updateUnderGuard(pc_15, temp_var_58);
                            
                            PrimitiveVS<Boolean> temp_var_59;
                            temp_var_59 = (var_local_2_local_1_i.restrict(pc_15)).apply(var_local_2_local_1_$tmp0.restrict(pc_15), (temp_var_60, temp_var_61) -> temp_var_60 < temp_var_61);
                            var_local_2_local_1_$tmp1 = var_local_2_local_1_$tmp1.updateUnderGuard(pc_15, temp_var_59);
                            
                            PrimitiveVS<Boolean> temp_var_62;
                            temp_var_62 = var_local_2_local_1_$tmp1.restrict(pc_15);
                            var_local_2_local_1_$tmp2 = var_local_2_local_1_$tmp2.updateUnderGuard(pc_15, temp_var_62);
                            
                            PrimitiveVS<Boolean> temp_var_63 = var_local_2_local_1_$tmp2.restrict(pc_15);
                            Guard pc_16 = BooleanVS.getTrueGuard(temp_var_63);
                            Guard pc_17 = BooleanVS.getFalseGuard(temp_var_63);
                            boolean jumpedOut_8 = false;
                            boolean jumpedOut_9 = false;
                            if (!pc_16.isFalse()) {
                                // 'then' branch
                            }
                            if (!pc_17.isFalse()) {
                                // 'else' branch
                                loop_exits_1.add(pc_17);
                                jumpedOut_9 = true;
                                pc_17 = Guard.constFalse();
                                
                            }
                            if (jumpedOut_8 || jumpedOut_9) {
                                pc_15 = pc_16.or(pc_17);
                            }
                            
                            if (!pc_15.isFalse()) {
                                PrimitiveVS<Machine> temp_var_64;
                                temp_var_64 = var_participants.restrict(pc_15).get(var_local_2_local_1_i.restrict(pc_15));
                                var_local_2_local_1_$tmp3 = var_local_2_local_1_$tmp3.updateUnderGuard(pc_15, temp_var_64);
                                
                                PrimitiveVS<Machine> temp_var_65;
                                temp_var_65 = var_local_2_local_1_$tmp3.restrict(pc_15);
                                var_local_2_local_1_$tmp4 = var_local_2_local_1_$tmp4.updateUnderGuard(pc_15, temp_var_65);
                                
                                PrimitiveVS<Event> temp_var_66;
                                temp_var_66 = var_local_2_inline_1_message.restrict(pc_15);
                                var_local_2_local_1_$tmp5 = var_local_2_local_1_$tmp5.updateUnderGuard(pc_15, temp_var_66);
                                
                                UnionVS temp_var_67;
                                temp_var_67 = ValueSummary.castToAny(pc_15, var_local_2_inline_1_payload.restrict(pc_15));
                                var_local_2_local_1_$tmp6 = var_local_2_local_1_$tmp6.updateUnderGuard(pc_15, temp_var_67);
                                
                                effects.send(pc_15, var_local_2_local_1_$tmp4.restrict(pc_15), var_local_2_local_1_$tmp5.restrict(pc_15), new UnionVS(var_local_2_local_1_$tmp6.restrict(pc_15)));
                                
                                PrimitiveVS<Integer> temp_var_68;
                                temp_var_68 = (var_local_2_local_1_i.restrict(pc_15)).apply(new PrimitiveVS<Integer>(1).restrict(pc_15), (temp_var_69, temp_var_70) -> temp_var_69 + temp_var_70);
                                var_local_2_local_1_$tmp7 = var_local_2_local_1_$tmp7.updateUnderGuard(pc_15, temp_var_68);
                                
                                PrimitiveVS<Integer> temp_var_71;
                                temp_var_71 = var_local_2_local_1_$tmp7.restrict(pc_15);
                                var_local_2_local_1_i = var_local_2_local_1_i.updateUnderGuard(pc_15, temp_var_71);
                                
                            }
                        }
                        if (loop_early_ret_1) {
                            pc_13 = Guard.orMany(loop_exits_1);
                            jumpedOut_6 = true;
                        }
                        
                        PrimitiveVS<Machine> temp_var_72;
                        temp_var_72 = (PrimitiveVS<Machine>)((var_pendingWrTrans.restrict(pc_13)).getField("client"));
                        var_local_2_$tmp2 = var_local_2_$tmp2.updateUnderGuard(pc_13, temp_var_72);
                        
                        PrimitiveVS<Machine> temp_var_73;
                        temp_var_73 = var_local_2_$tmp2.restrict(pc_13);
                        var_local_2_$tmp3 = var_local_2_$tmp3.updateUnderGuard(pc_13, temp_var_73);
                        
                        PrimitiveVS<Event> temp_var_74;
                        temp_var_74 = new PrimitiveVS<Event>(eWriteTransResp).restrict(pc_13);
                        var_local_2_$tmp4 = var_local_2_$tmp4.updateUnderGuard(pc_13, temp_var_74);
                        
                        PredVS<String> /* pred enum tPreds */ temp_var_75;
                        temp_var_75 = var_currTransId.restrict(pc_13);
                        var_local_2_$tmp5 = var_local_2_$tmp5.updateUnderGuard(pc_13, temp_var_75);
                        
                        PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_76;
                        temp_var_76 = new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_13);
                        var_local_2_$tmp6 = var_local_2_$tmp6.updateUnderGuard(pc_13, temp_var_76);
                        
                        NamedTupleVS temp_var_77;
                        temp_var_77 = new NamedTupleVS("transId", var_local_2_$tmp5.restrict(pc_13), "status", var_local_2_$tmp6.restrict(pc_13));
                        var_local_2_$tmp7 = var_local_2_$tmp7.updateUnderGuard(pc_13, temp_var_77);
                        
                        effects.send(pc_13, var_local_2_$tmp3.restrict(pc_13), var_local_2_$tmp4.restrict(pc_13), new UnionVS(var_local_2_$tmp7.restrict(pc_13)));
                        
                        outcome.addGuardedGoto(pc_13, WaitForTransactions);
                        pc_13 = Guard.constFalse();
                        jumpedOut_6 = true;
                        
                    }
                    if (!pc_14.isFalse()) {
                        // 'else' branch
                    }
                    if (jumpedOut_6 || jumpedOut_7) {
                        pc_11 = pc_13.or(pc_14);
                        jumpedOut_4 = true;
                    }
                    
                    if (!pc_11.isFalse()) {
                    }
                }
                if (!pc_12.isFalse()) {
                    // 'else' branch
                }
                if (jumpedOut_4 || jumpedOut_5) {
                    pc_9 = pc_11.or(pc_12);
                    jumpedOut_2 = true;
                }
                
                if (!pc_9.isFalse()) {
                }
            }
            if (!pc_10.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_2 || jumpedOut_3) {
                pc_8 = pc_9.or(pc_10);
            }
            
            if (!pc_8.isFalse()) {
            }
            return pc_8;
        }
        
        void 
        anonfun_6(
            Guard pc_18,
            EventBuffer effects,
            NamedTupleVS var_rTrans
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_18);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_18);
            
            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_18);
            
            NamedTupleVS var_$tmp3 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PredVS<String> /* pred enum tPreds */()).restrict(pc_18);
            
            PrimitiveVS<Machine> temp_var_78;
            temp_var_78 = (PrimitiveVS<Machine>) scheduler.getNextElement(var_participants.restrict(pc_18), pc_18);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_18, temp_var_78);
            
            PrimitiveVS<Machine> temp_var_79;
            temp_var_79 = var_$tmp0.restrict(pc_18);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_18, temp_var_79);
            
            PrimitiveVS<Event> temp_var_80;
            temp_var_80 = new PrimitiveVS<Event>(eReadTransReq).restrict(pc_18);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_18, temp_var_80);
            
            NamedTupleVS temp_var_81;
            temp_var_81 = var_rTrans.restrict(pc_18);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_18, temp_var_81);
            
            effects.send(pc_18, var_$tmp1.restrict(pc_18), var_$tmp2.restrict(pc_18), new UnionVS(var_$tmp3.restrict(pc_18)));
            
        }
        
        void 
        anonfun_2(
            Guard pc_19,
            EventBuffer effects
        ) {
            PrimitiveVS<Integer> temp_var_82;
            temp_var_82 = new PrimitiveVS<Integer>(0).restrict(pc_19);
            var_countPrepareResponses = var_countPrepareResponses.updateUnderGuard(pc_19, temp_var_82);
            
        }
        
        void 
        DoGlobalCommit(
            Guard pc_20,
            EventBuffer effects
        ) {
            PrimitiveVS<Event> var_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_20);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp1 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_20);
            
            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_20);
            
            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_20);
            
            PrimitiveVS<Event> var_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_20);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp5 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_20);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp6 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_20);
            
            NamedTupleVS var_$tmp7 =
                new NamedTupleVS("transId", new PredVS<String> /* pred enum tPreds */(), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_20);
            
            PrimitiveVS<Event> var_inline_1_message =
                new PrimitiveVS<Event>(_null).restrict(pc_20);
            
            PredVS<String> /* pred enum tPreds */ var_inline_1_payload =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_20);
            
            PrimitiveVS<Integer> var_local_1_i =
                new PrimitiveVS<Integer>(0).restrict(pc_20);
            
            PrimitiveVS<Integer> var_local_1_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_20);
            
            PrimitiveVS<Boolean> var_local_1_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_20);
            
            PrimitiveVS<Boolean> var_local_1_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_20);
            
            PrimitiveVS<Machine> var_local_1_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_20);
            
            PrimitiveVS<Machine> var_local_1_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_20);
            
            PrimitiveVS<Event> var_local_1_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_20);
            
            UnionVS var_local_1_$tmp6 =
                new UnionVS().restrict(pc_20);
            
            PrimitiveVS<Integer> var_local_1_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_20);
            
            PrimitiveVS<Event> temp_var_83;
            temp_var_83 = new PrimitiveVS<Event>(eCommitTrans).restrict(pc_20);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_20, temp_var_83);
            
            PredVS<String> /* pred enum tPreds */ temp_var_84;
            temp_var_84 = var_currTransId.restrict(pc_20);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_20, temp_var_84);
            
            PrimitiveVS<Event> temp_var_85;
            temp_var_85 = var_$tmp0.restrict(pc_20);
            var_inline_1_message = var_inline_1_message.updateUnderGuard(pc_20, temp_var_85);
            
            PredVS<String> /* pred enum tPreds */ temp_var_86;
            temp_var_86 = var_$tmp1.restrict(pc_20);
            var_inline_1_payload = var_inline_1_payload.updateUnderGuard(pc_20, temp_var_86);
            
            PrimitiveVS<Integer> temp_var_87;
            temp_var_87 = new PrimitiveVS<Integer>(0).restrict(pc_20);
            var_local_1_i = var_local_1_i.updateUnderGuard(pc_20, temp_var_87);
            
            java.util.List<Guard> loop_exits_2 = new java.util.ArrayList<>();
            boolean loop_early_ret_2 = false;
            Guard pc_21 = pc_20;
            while (!pc_21.isFalse()) {
                PrimitiveVS<Integer> temp_var_88;
                temp_var_88 = var_participants.restrict(pc_21).size();
                var_local_1_$tmp0 = var_local_1_$tmp0.updateUnderGuard(pc_21, temp_var_88);
                
                PrimitiveVS<Boolean> temp_var_89;
                temp_var_89 = (var_local_1_i.restrict(pc_21)).apply(var_local_1_$tmp0.restrict(pc_21), (temp_var_90, temp_var_91) -> temp_var_90 < temp_var_91);
                var_local_1_$tmp1 = var_local_1_$tmp1.updateUnderGuard(pc_21, temp_var_89);
                
                PrimitiveVS<Boolean> temp_var_92;
                temp_var_92 = var_local_1_$tmp1.restrict(pc_21);
                var_local_1_$tmp2 = var_local_1_$tmp2.updateUnderGuard(pc_21, temp_var_92);
                
                PrimitiveVS<Boolean> temp_var_93 = var_local_1_$tmp2.restrict(pc_21);
                Guard pc_22 = BooleanVS.getTrueGuard(temp_var_93);
                Guard pc_23 = BooleanVS.getFalseGuard(temp_var_93);
                boolean jumpedOut_10 = false;
                boolean jumpedOut_11 = false;
                if (!pc_22.isFalse()) {
                    // 'then' branch
                }
                if (!pc_23.isFalse()) {
                    // 'else' branch
                    loop_exits_2.add(pc_23);
                    jumpedOut_11 = true;
                    pc_23 = Guard.constFalse();
                    
                }
                if (jumpedOut_10 || jumpedOut_11) {
                    pc_21 = pc_22.or(pc_23);
                }
                
                if (!pc_21.isFalse()) {
                    PrimitiveVS<Machine> temp_var_94;
                    temp_var_94 = var_participants.restrict(pc_21).get(var_local_1_i.restrict(pc_21));
                    var_local_1_$tmp3 = var_local_1_$tmp3.updateUnderGuard(pc_21, temp_var_94);
                    
                    PrimitiveVS<Machine> temp_var_95;
                    temp_var_95 = var_local_1_$tmp3.restrict(pc_21);
                    var_local_1_$tmp4 = var_local_1_$tmp4.updateUnderGuard(pc_21, temp_var_95);
                    
                    PrimitiveVS<Event> temp_var_96;
                    temp_var_96 = var_inline_1_message.restrict(pc_21);
                    var_local_1_$tmp5 = var_local_1_$tmp5.updateUnderGuard(pc_21, temp_var_96);
                    
                    UnionVS temp_var_97;
                    temp_var_97 = ValueSummary.castToAny(pc_21, var_inline_1_payload.restrict(pc_21));
                    var_local_1_$tmp6 = var_local_1_$tmp6.updateUnderGuard(pc_21, temp_var_97);
                    
                    effects.send(pc_21, var_local_1_$tmp4.restrict(pc_21), var_local_1_$tmp5.restrict(pc_21), new UnionVS(var_local_1_$tmp6.restrict(pc_21)));
                    
                    PrimitiveVS<Integer> temp_var_98;
                    temp_var_98 = (var_local_1_i.restrict(pc_21)).apply(new PrimitiveVS<Integer>(1).restrict(pc_21), (temp_var_99, temp_var_100) -> temp_var_99 + temp_var_100);
                    var_local_1_$tmp7 = var_local_1_$tmp7.updateUnderGuard(pc_21, temp_var_98);
                    
                    PrimitiveVS<Integer> temp_var_101;
                    temp_var_101 = var_local_1_$tmp7.restrict(pc_21);
                    var_local_1_i = var_local_1_i.updateUnderGuard(pc_21, temp_var_101);
                    
                }
            }
            if (loop_early_ret_2) {
                pc_20 = Guard.orMany(loop_exits_2);
            }
            
            PrimitiveVS<Machine> temp_var_102;
            temp_var_102 = (PrimitiveVS<Machine>)((var_pendingWrTrans.restrict(pc_20)).getField("client"));
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_20, temp_var_102);
            
            PrimitiveVS<Machine> temp_var_103;
            temp_var_103 = var_$tmp2.restrict(pc_20);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_20, temp_var_103);
            
            PrimitiveVS<Event> temp_var_104;
            temp_var_104 = new PrimitiveVS<Event>(eWriteTransResp).restrict(pc_20);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_20, temp_var_104);
            
            PredVS<String> /* pred enum tPreds */ temp_var_105;
            temp_var_105 = var_currTransId.restrict(pc_20);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_20, temp_var_105);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_106;
            temp_var_106 = new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_20);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_20, temp_var_106);
            
            NamedTupleVS temp_var_107;
            temp_var_107 = new NamedTupleVS("transId", var_$tmp5.restrict(pc_20), "status", var_$tmp6.restrict(pc_20));
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_20, temp_var_107);
            
            effects.send(pc_20, var_$tmp3.restrict(pc_20), var_$tmp4.restrict(pc_20), new UnionVS(var_$tmp7.restrict(pc_20)));
            
        }
        
        void 
        BroadcastToAllParticipants(
            Guard pc_24,
            EventBuffer effects,
            PrimitiveVS<Event> var_message,
            UnionVS var_payload
        ) {
            PrimitiveVS<Integer> var_i =
                new PrimitiveVS<Integer>(0).restrict(pc_24);
            
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_24);
            
            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_24);
            
            PrimitiveVS<Boolean> var_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_24);
            
            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_24);
            
            PrimitiveVS<Machine> var_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_24);
            
            PrimitiveVS<Event> var_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_24);
            
            UnionVS var_$tmp6 =
                new UnionVS().restrict(pc_24);
            
            PrimitiveVS<Integer> var_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_24);
            
            PrimitiveVS<Integer> temp_var_108;
            temp_var_108 = new PrimitiveVS<Integer>(0).restrict(pc_24);
            var_i = var_i.updateUnderGuard(pc_24, temp_var_108);
            
            java.util.List<Guard> loop_exits_3 = new java.util.ArrayList<>();
            boolean loop_early_ret_3 = false;
            Guard pc_25 = pc_24;
            while (!pc_25.isFalse()) {
                PrimitiveVS<Integer> temp_var_109;
                temp_var_109 = var_participants.restrict(pc_25).size();
                var_$tmp0 = var_$tmp0.updateUnderGuard(pc_25, temp_var_109);
                
                PrimitiveVS<Boolean> temp_var_110;
                temp_var_110 = (var_i.restrict(pc_25)).apply(var_$tmp0.restrict(pc_25), (temp_var_111, temp_var_112) -> temp_var_111 < temp_var_112);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_25, temp_var_110);
                
                PrimitiveVS<Boolean> temp_var_113;
                temp_var_113 = var_$tmp1.restrict(pc_25);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_25, temp_var_113);
                
                PrimitiveVS<Boolean> temp_var_114 = var_$tmp2.restrict(pc_25);
                Guard pc_26 = BooleanVS.getTrueGuard(temp_var_114);
                Guard pc_27 = BooleanVS.getFalseGuard(temp_var_114);
                boolean jumpedOut_12 = false;
                boolean jumpedOut_13 = false;
                if (!pc_26.isFalse()) {
                    // 'then' branch
                }
                if (!pc_27.isFalse()) {
                    // 'else' branch
                    loop_exits_3.add(pc_27);
                    jumpedOut_13 = true;
                    pc_27 = Guard.constFalse();
                    
                }
                if (jumpedOut_12 || jumpedOut_13) {
                    pc_25 = pc_26.or(pc_27);
                }
                
                if (!pc_25.isFalse()) {
                    PrimitiveVS<Machine> temp_var_115;
                    temp_var_115 = var_participants.restrict(pc_25).get(var_i.restrict(pc_25));
                    var_$tmp3 = var_$tmp3.updateUnderGuard(pc_25, temp_var_115);
                    
                    PrimitiveVS<Machine> temp_var_116;
                    temp_var_116 = var_$tmp3.restrict(pc_25);
                    var_$tmp4 = var_$tmp4.updateUnderGuard(pc_25, temp_var_116);
                    
                    PrimitiveVS<Event> temp_var_117;
                    temp_var_117 = var_message.restrict(pc_25);
                    var_$tmp5 = var_$tmp5.updateUnderGuard(pc_25, temp_var_117);
                    
                    UnionVS temp_var_118;
                    temp_var_118 = ValueSummary.castToAny(pc_25, var_payload.restrict(pc_25));
                    var_$tmp6 = var_$tmp6.updateUnderGuard(pc_25, temp_var_118);
                    
                    effects.send(pc_25, var_$tmp4.restrict(pc_25), var_$tmp5.restrict(pc_25), new UnionVS(var_$tmp6.restrict(pc_25)));
                    
                    PrimitiveVS<Integer> temp_var_119;
                    temp_var_119 = (var_i.restrict(pc_25)).apply(new PrimitiveVS<Integer>(1).restrict(pc_25), (temp_var_120, temp_var_121) -> temp_var_120 + temp_var_121);
                    var_$tmp7 = var_$tmp7.updateUnderGuard(pc_25, temp_var_119);
                    
                    PrimitiveVS<Integer> temp_var_122;
                    temp_var_122 = var_$tmp7.restrict(pc_25);
                    var_i = var_i.updateUnderGuard(pc_25, temp_var_122);
                    
                }
            }
            if (loop_early_ret_3) {
                pc_24 = Guard.orMany(loop_exits_3);
            }
            
        }
        
    }
    
    public static class TestClient extends Machine {
        
        static State Init = new State("Init") {
            @Override public void entry(Guard pc_28, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((TestClient)machine).anonfun_7(pc_28, machine.sendBuffer, outcome, payload != null ? (PrimitiveVS<Machine>) ValueSummary.castFromAny(pc_28, new PrimitiveVS<Machine>().restrict(pc_28), payload) : new PrimitiveVS<Machine>().restrict(pc_28));
            }
        };
        static State ChoosePre = new State("ChoosePre") {
            @Override public void entry(Guard pc_29, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((TestClient)machine).anonfun_8(pc_29, machine.sendBuffer, outcome);
            }
        };
        static State SendPreWrites = new State("SendPreWrites") {
            @Override public void entry(Guard pc_30, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((TestClient)machine).anonfun_9(pc_30, machine.sendBuffer);
            }
        };
        static State SendSelectWrite = new State("SendSelectWrite") {
            @Override public void entry(Guard pc_31, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((TestClient)machine).anonfun_10(pc_31, machine.sendBuffer, outcome);
            }
        };
        static State SendPost = new State("SendPost") {
        };
        private PrimitiveVS<Machine> var_coordinator = new PrimitiveVS<Machine>();
        private NamedTupleVS var_currTransaction = new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */());
        private NamedTupleVS var_currWriteResponse = new NamedTupleVS("transId", new PredVS<String> /* pred enum tPreds */(), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0));
        
        @Override
        public void reset() {
                super.reset();
                var_coordinator = new PrimitiveVS<Machine>();
                var_currTransaction = new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */());
                var_currWriteResponse = new NamedTupleVS("transId", new PredVS<String> /* pred enum tPreds */(), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0));
        }
        
        @Override
        public List<ValueSummary> getLocalState() {
                List<ValueSummary> res = super.getLocalState();
                res.add(var_coordinator);
                res.add(var_currTransaction);
                res.add(var_currWriteResponse);
                return res;
        }
        
        public TestClient(int id) {
            super("TestClient", id, EventBufferSemantics.queue, Init, Init
                , ChoosePre
                , SendPreWrites
                , SendSelectWrite
                , SendPost
                
            );
            Init.addHandlers();
            ChoosePre.addHandlers();
            SendPreWrites.addHandlers(new GotoEventHandler(eWriteTransResp, ChoosePre));
            SendSelectWrite.addHandlers();
            SendPost.addHandlers(new EventHandler(eWriteTransResp) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((TestClient)machine).anonfun_11(pc, machine.sendBuffer);
                    }
                },
                new EventHandler(eReadTransResp) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((TestClient)machine).anonfun_12(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)), payload));
                    }
                });
        }
        
        Guard 
        anonfun_7(
            Guard pc_32,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            PrimitiveVS<Machine> var_payload
        ) {
            PrimitiveVS<Machine> temp_var_123;
            temp_var_123 = var_payload.restrict(pc_32);
            var_coordinator = var_coordinator.updateUnderGuard(pc_32, temp_var_123);
            
            outcome.addGuardedGoto(pc_32, ChoosePre);
            pc_32 = Guard.constFalse();
            
            return pc_32;
        }
        
        Guard 
        anonfun_8(
            Guard pc_33,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveVS<Boolean> var_$tmp0 =
                new PrimitiveVS<Boolean>(false).restrict(pc_33);
            
            PrimitiveVS<Boolean> temp_var_124;
            temp_var_124 = scheduler.getNextBoolean(pc_33);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_33, temp_var_124);
            
            PrimitiveVS<Boolean> temp_var_125 = var_$tmp0.restrict(pc_33);
            Guard pc_34 = BooleanVS.getTrueGuard(temp_var_125);
            Guard pc_35 = BooleanVS.getFalseGuard(temp_var_125);
            boolean jumpedOut_14 = false;
            boolean jumpedOut_15 = false;
            if (!pc_34.isFalse()) {
                // 'then' branch
                outcome.addGuardedGoto(pc_34, SendPreWrites);
                pc_34 = Guard.constFalse();
                jumpedOut_14 = true;
                
            }
            if (!pc_35.isFalse()) {
                // 'else' branch
                outcome.addGuardedGoto(pc_35, SendSelectWrite);
                pc_35 = Guard.constFalse();
                jumpedOut_15 = true;
                
            }
            if (jumpedOut_14 || jumpedOut_15) {
                pc_33 = pc_34.or(pc_35);
            }
            
            return pc_33;
        }
        
        void 
        anonfun_9(
            Guard pc_36,
            EventBuffer effects
        ) {
            NamedTupleVS var_$tmp0 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_36);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_36);
            
            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_36);
            
            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_36);
            
            NamedTupleVS var_$tmp4 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_36);
            
            NamedTupleVS var_$tmp5 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_36);
            
            ListVS<PredVS<String> /* pred enum tPreds */> var_local_3_keyChoices =
                new ListVS<PredVS<String> /* pred enum tPreds */>(Guard.constTrue()).restrict(pc_36);
            
            ListVS<PredVS<String> /* pred enum tPreds */> var_local_3_valChoices =
                new ListVS<PredVS<String> /* pred enum tPreds */>(Guard.constTrue()).restrict(pc_36);
            
            PredVS<String> /* pred enum tPreds */ var_local_3_$tmp0 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_36);
            
            PredVS<String> /* pred enum tPreds */ var_local_3_$tmp1 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_36);
            
            PredVS<String> /* pred enum tPreds */ var_local_3_$tmp2 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_36);
            
            PredVS<String> /* pred enum tPreds */ var_local_3_$tmp3 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_36);
            
            PredVS<String> /* pred enum tPreds */ var_local_3_$tmp4 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_36);
            
            PredVS<String> /* pred enum tPreds */ var_local_3_$tmp5 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_36);
            
            NamedTupleVS var_local_3_$tmp6 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_36);
            
            PredVS<String> /* pred enum tPreds */ temp_var_126;
            temp_var_126 = new PredVS<String>("EQKEY").restrict(pc_36);
            var_local_3_$tmp0 = var_local_3_$tmp0.updateUnderGuard(pc_36, temp_var_126);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_127 = var_local_3_keyChoices.restrict(pc_36);    
            temp_var_127 = var_local_3_keyChoices.restrict(pc_36).insert(new PrimitiveVS<Integer>(0).restrict(pc_36), var_local_3_$tmp0.restrict(pc_36));
            var_local_3_keyChoices = var_local_3_keyChoices.updateUnderGuard(pc_36, temp_var_127);
            
            PredVS<String> /* pred enum tPreds */ temp_var_128;
            temp_var_128 = new PredVS<String>("NEQKEY").restrict(pc_36);
            var_local_3_$tmp1 = var_local_3_$tmp1.updateUnderGuard(pc_36, temp_var_128);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_129 = var_local_3_keyChoices.restrict(pc_36);    
            temp_var_129 = var_local_3_keyChoices.restrict(pc_36).insert(new PrimitiveVS<Integer>(1).restrict(pc_36), var_local_3_$tmp1.restrict(pc_36));
            var_local_3_keyChoices = var_local_3_keyChoices.updateUnderGuard(pc_36, temp_var_129);
            
            PredVS<String> /* pred enum tPreds */ temp_var_130;
            temp_var_130 = new PredVS<String>("EQVAL").restrict(pc_36);
            var_local_3_$tmp2 = var_local_3_$tmp2.updateUnderGuard(pc_36, temp_var_130);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_131 = var_local_3_valChoices.restrict(pc_36);    
            temp_var_131 = var_local_3_valChoices.restrict(pc_36).insert(new PrimitiveVS<Integer>(0).restrict(pc_36), var_local_3_$tmp2.restrict(pc_36));
            var_local_3_valChoices = var_local_3_valChoices.updateUnderGuard(pc_36, temp_var_131);
            
            PredVS<String> /* pred enum tPreds */ temp_var_132;
            temp_var_132 = new PredVS<String>("NEQVAL").restrict(pc_36);
            var_local_3_$tmp3 = var_local_3_$tmp3.updateUnderGuard(pc_36, temp_var_132);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_133 = var_local_3_valChoices.restrict(pc_36);    
            temp_var_133 = var_local_3_valChoices.restrict(pc_36).insert(new PrimitiveVS<Integer>(1).restrict(pc_36), var_local_3_$tmp3.restrict(pc_36));
            var_local_3_valChoices = var_local_3_valChoices.updateUnderGuard(pc_36, temp_var_133);
            
            PredVS<String> /* pred enum tPreds */ temp_var_134;
            temp_var_134 = new PredVS<String> /* pred enum tPreds */(var_local_3_keyChoices.restrict(pc_36), pc_36);
            var_local_3_$tmp4 = var_local_3_$tmp4.updateUnderGuard(pc_36, temp_var_134);
            
            PredVS<String> /* pred enum tPreds */ temp_var_135;
            temp_var_135 = new PredVS<String> /* pred enum tPreds */(var_local_3_valChoices.restrict(pc_36), pc_36);
            var_local_3_$tmp5 = var_local_3_$tmp5.updateUnderGuard(pc_36, temp_var_135);
            
            NamedTupleVS temp_var_136;
            temp_var_136 = new NamedTupleVS("key", var_local_3_$tmp4.restrict(pc_36), "val", var_local_3_$tmp5.restrict(pc_36));
            var_local_3_$tmp6 = var_local_3_$tmp6.updateUnderGuard(pc_36, temp_var_136);
            
            NamedTupleVS temp_var_137;
            temp_var_137 = var_local_3_$tmp6.restrict(pc_36);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_36, temp_var_137);
            
            NamedTupleVS temp_var_138;
            temp_var_138 = var_$tmp0.restrict(pc_36);
            var_currTransaction = var_currTransaction.updateUnderGuard(pc_36, temp_var_138);
            
            PrimitiveVS<Machine> temp_var_139;
            temp_var_139 = var_coordinator.restrict(pc_36);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_36, temp_var_139);
            
            PrimitiveVS<Event> temp_var_140;
            temp_var_140 = new PrimitiveVS<Event>(eWriteTransReq).restrict(pc_36);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_36, temp_var_140);
            
            PrimitiveVS<Machine> temp_var_141;
            temp_var_141 = new PrimitiveVS<Machine>(this).restrict(pc_36);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_36, temp_var_141);
            
            NamedTupleVS temp_var_142;
            temp_var_142 = var_currTransaction.restrict(pc_36);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_36, temp_var_142);
            
            NamedTupleVS temp_var_143;
            temp_var_143 = new NamedTupleVS("client", var_$tmp3.restrict(pc_36), "rec", var_$tmp4.restrict(pc_36));
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_36, temp_var_143);
            
            effects.send(pc_36, var_$tmp1.restrict(pc_36), var_$tmp2.restrict(pc_36), new UnionVS(var_$tmp5.restrict(pc_36)));
            
        }
        
        Guard 
        anonfun_10(
            Guard pc_37,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PredVS<String> /* pred enum tPreds */ var_$tmp0 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_37);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp1 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_37);
            
            NamedTupleVS var_$tmp2 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_37);
            
            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_37);
            
            PrimitiveVS<Event> var_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_37);
            
            PrimitiveVS<Machine> var_$tmp5 =
                new PrimitiveVS<Machine>().restrict(pc_37);
            
            NamedTupleVS var_$tmp6 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_37);
            
            NamedTupleVS var_$tmp7 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_37);
            
            PredVS<String> /* pred enum tPreds */ temp_var_144;
            temp_var_144 = new PredVS<String>("EQKEY").restrict(pc_37);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_37, temp_var_144);
            
            PredVS<String> /* pred enum tPreds */ temp_var_145;
            temp_var_145 = new PredVS<String>("EQVAL").restrict(pc_37);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_37, temp_var_145);
            
            NamedTupleVS temp_var_146;
            temp_var_146 = new NamedTupleVS("key", var_$tmp0.restrict(pc_37), "val", var_$tmp1.restrict(pc_37));
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_37, temp_var_146);
            
            NamedTupleVS temp_var_147;
            temp_var_147 = var_$tmp2.restrict(pc_37);
            var_currTransaction = var_currTransaction.updateUnderGuard(pc_37, temp_var_147);
            
            PrimitiveVS<Machine> temp_var_148;
            temp_var_148 = var_coordinator.restrict(pc_37);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_37, temp_var_148);
            
            PrimitiveVS<Event> temp_var_149;
            temp_var_149 = new PrimitiveVS<Event>(eWriteTransReq).restrict(pc_37);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_37, temp_var_149);
            
            PrimitiveVS<Machine> temp_var_150;
            temp_var_150 = new PrimitiveVS<Machine>(this).restrict(pc_37);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_37, temp_var_150);
            
            NamedTupleVS temp_var_151;
            temp_var_151 = var_currTransaction.restrict(pc_37);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_37, temp_var_151);
            
            NamedTupleVS temp_var_152;
            temp_var_152 = new NamedTupleVS("client", var_$tmp5.restrict(pc_37), "rec", var_$tmp6.restrict(pc_37));
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_37, temp_var_152);
            
            effects.send(pc_37, var_$tmp3.restrict(pc_37), var_$tmp4.restrict(pc_37), new UnionVS(var_$tmp7.restrict(pc_37)));
            
            outcome.addGuardedGoto(pc_37, SendPost);
            pc_37 = Guard.constFalse();
            
            return pc_37;
        }
        
        void 
        anonfun_11(
            Guard pc_38,
            EventBuffer effects
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_38);
            
            PrimitiveVS<Event> var_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_38);
            
            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_38);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp3 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_38);
            
            NamedTupleVS var_$tmp4 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PredVS<String> /* pred enum tPreds */()).restrict(pc_38);
            
            PrimitiveVS<Machine> temp_var_153;
            temp_var_153 = var_coordinator.restrict(pc_38);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_38, temp_var_153);
            
            PrimitiveVS<Event> temp_var_154;
            temp_var_154 = new PrimitiveVS<Event>(eReadTransReq).restrict(pc_38);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_38, temp_var_154);
            
            PrimitiveVS<Machine> temp_var_155;
            temp_var_155 = new PrimitiveVS<Machine>(this).restrict(pc_38);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_38, temp_var_155);
            
            PredVS<String> /* pred enum tPreds */ temp_var_156;
            temp_var_156 = new PredVS<String>("EQKEY").restrict(pc_38);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_38, temp_var_156);
            
            NamedTupleVS temp_var_157;
            temp_var_157 = new NamedTupleVS("client", var_$tmp2.restrict(pc_38), "key", var_$tmp3.restrict(pc_38));
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_38, temp_var_157);
            
            effects.send(pc_38, var_$tmp0.restrict(pc_38), var_$tmp1.restrict(pc_38), new UnionVS(var_$tmp4.restrict(pc_38)));
            
        }
        
        void 
        anonfun_12(
            Guard pc_39,
            EventBuffer effects,
            NamedTupleVS var_resp
        ) {
            ListVS<PredVS<String> /* pred enum tPreds */> var_choices =
                new ListVS<PredVS<String> /* pred enum tPreds */>(Guard.constTrue()).restrict(pc_39);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp0 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_39);
            
            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_39);
            
            NamedTupleVS var_$tmp2 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_39);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp3 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_39);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp4 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_39);
            
            NamedTupleVS var_$tmp5 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_39);
            
            PrimitiveVS<Boolean> var_$tmp6 =
                new PrimitiveVS<Boolean>(false).restrict(pc_39);
            
            NamedTupleVS var_$tmp7 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_39);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp8 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_39);
            
            NamedTupleVS var_$tmp9 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_39);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp10 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_39);
            
            PrimitiveVS<String> var_$tmp11 =
                new PrimitiveVS<String>("").restrict(pc_39);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp12 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_39);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp13 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_39);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp14 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_39);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp15 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_39);
            
            NamedTupleVS var_$tmp16 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_39);
            
            PrimitiveVS<Machine> var_$tmp17 =
                new PrimitiveVS<Machine>().restrict(pc_39);
            
            PrimitiveVS<Event> var_$tmp18 =
                new PrimitiveVS<Event>(_null).restrict(pc_39);
            
            PrimitiveVS<Machine> var_$tmp19 =
                new PrimitiveVS<Machine>().restrict(pc_39);
            
            NamedTupleVS var_$tmp20 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_39);
            
            NamedTupleVS var_$tmp21 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_39);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_158;
            temp_var_158 = (PrimitiveVS<Integer> /* enum tTransStatus */)((var_resp.restrict(pc_39)).getField("status"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_39, temp_var_158);
            
            PrimitiveVS<Boolean> temp_var_159;
            temp_var_159 = var_$tmp0.restrict(pc_39).symbolicEquals(new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_39), pc_39);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_39, temp_var_159);
            
            PrimitiveVS<Boolean> temp_var_160 = var_$tmp1.restrict(pc_39);
            Guard pc_40 = BooleanVS.getTrueGuard(temp_var_160);
            Guard pc_41 = BooleanVS.getFalseGuard(temp_var_160);
            boolean jumpedOut_16 = false;
            boolean jumpedOut_17 = false;
            if (!pc_40.isFalse()) {
                // 'then' branch
                NamedTupleVS temp_var_161;
                temp_var_161 = (NamedTupleVS)((var_resp.restrict(pc_40)).getField("rec"));
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_40, temp_var_161);
                
                PredVS<String> /* pred enum tPreds */ temp_var_162;
                temp_var_162 = new PredVS<String>("EQKEY").restrict(pc_40);
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_40, temp_var_162);
                
                PredVS<String> /* pred enum tPreds */ temp_var_163;
                temp_var_163 = new PredVS<String>("EQVAL").restrict(pc_40);
                var_$tmp4 = var_$tmp4.updateUnderGuard(pc_40, temp_var_163);
                
                NamedTupleVS temp_var_164;
                temp_var_164 = new NamedTupleVS("key", var_$tmp3.restrict(pc_40), "val", var_$tmp4.restrict(pc_40));
                var_$tmp5 = var_$tmp5.updateUnderGuard(pc_40, temp_var_164);
                
                PrimitiveVS<Boolean> temp_var_165;
                temp_var_165 = var_$tmp2.restrict(pc_40).symbolicEquals(var_$tmp5.restrict(pc_40), pc_40);
                var_$tmp6 = var_$tmp6.updateUnderGuard(pc_40, temp_var_165);
                
                NamedTupleVS temp_var_166;
                temp_var_166 = (NamedTupleVS)((var_resp.restrict(pc_40)).getField("rec"));
                var_$tmp7 = var_$tmp7.updateUnderGuard(pc_40, temp_var_166);
                
                PredVS<String> /* pred enum tPreds */ temp_var_167;
                temp_var_167 = (PredVS<String> /* pred enum tPreds */)((var_$tmp7.restrict(pc_40)).getField("key"));
                var_$tmp8 = var_$tmp8.updateUnderGuard(pc_40, temp_var_167);
                
                NamedTupleVS temp_var_168;
                temp_var_168 = (NamedTupleVS)((var_resp.restrict(pc_40)).getField("rec"));
                var_$tmp9 = var_$tmp9.updateUnderGuard(pc_40, temp_var_168);
                
                PredVS<String> /* pred enum tPreds */ temp_var_169;
                temp_var_169 = (PredVS<String> /* pred enum tPreds */)((var_$tmp9.restrict(pc_40)).getField("val"));
                var_$tmp10 = var_$tmp10.updateUnderGuard(pc_40, temp_var_169);
                
                PrimitiveVS<String> temp_var_170;
                temp_var_170 = new PrimitiveVS<String>(String.format("value not equal {0} %s, {1} %s", var_$tmp8.restrict(pc_40), var_$tmp10.restrict(pc_40))).restrict(pc_40);
                var_$tmp11 = var_$tmp11.updateUnderGuard(pc_40, temp_var_170);
                
                Assert.progProp(!(var_$tmp6.restrict(pc_40)).getValues().contains(Boolean.FALSE), var_$tmp11.restrict(pc_40), scheduler, var_$tmp6.restrict(pc_40).getGuardFor(Boolean.FALSE));
            }
            if (!pc_41.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_16 || jumpedOut_17) {
                pc_39 = pc_40.or(pc_41);
            }
            
            PredVS<String> /* pred enum tPreds */ temp_var_171;
            temp_var_171 = new PredVS<String>("EQVAL").restrict(pc_39);
            var_$tmp12 = var_$tmp12.updateUnderGuard(pc_39, temp_var_171);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_172 = var_choices.restrict(pc_39);    
            temp_var_172 = var_choices.restrict(pc_39).insert(new PrimitiveVS<Integer>(0).restrict(pc_39), var_$tmp12.restrict(pc_39));
            var_choices = var_choices.updateUnderGuard(pc_39, temp_var_172);
            
            PredVS<String> /* pred enum tPreds */ temp_var_173;
            temp_var_173 = new PredVS<String>("NEQVAL").restrict(pc_39);
            var_$tmp13 = var_$tmp13.updateUnderGuard(pc_39, temp_var_173);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_174 = var_choices.restrict(pc_39);    
            temp_var_174 = var_choices.restrict(pc_39).insert(new PrimitiveVS<Integer>(1).restrict(pc_39), var_$tmp13.restrict(pc_39));
            var_choices = var_choices.updateUnderGuard(pc_39, temp_var_174);
            
            PredVS<String> /* pred enum tPreds */ temp_var_175;
            temp_var_175 = new PredVS<String>("NEQKEY").restrict(pc_39);
            var_$tmp14 = var_$tmp14.updateUnderGuard(pc_39, temp_var_175);
            
            PredVS<String> /* pred enum tPreds */ temp_var_176;
            temp_var_176 = new PredVS<String> /* pred enum tPreds */(var_choices.restrict(pc_39), pc_39);
            var_$tmp15 = var_$tmp15.updateUnderGuard(pc_39, temp_var_176);
            
            NamedTupleVS temp_var_177;
            temp_var_177 = new NamedTupleVS("key", var_$tmp14.restrict(pc_39), "val", var_$tmp15.restrict(pc_39));
            var_$tmp16 = var_$tmp16.updateUnderGuard(pc_39, temp_var_177);
            
            NamedTupleVS temp_var_178;
            temp_var_178 = var_$tmp16.restrict(pc_39);
            var_currTransaction = var_currTransaction.updateUnderGuard(pc_39, temp_var_178);
            
            PrimitiveVS<Machine> temp_var_179;
            temp_var_179 = var_coordinator.restrict(pc_39);
            var_$tmp17 = var_$tmp17.updateUnderGuard(pc_39, temp_var_179);
            
            PrimitiveVS<Event> temp_var_180;
            temp_var_180 = new PrimitiveVS<Event>(eWriteTransReq).restrict(pc_39);
            var_$tmp18 = var_$tmp18.updateUnderGuard(pc_39, temp_var_180);
            
            PrimitiveVS<Machine> temp_var_181;
            temp_var_181 = new PrimitiveVS<Machine>(this).restrict(pc_39);
            var_$tmp19 = var_$tmp19.updateUnderGuard(pc_39, temp_var_181);
            
            NamedTupleVS temp_var_182;
            temp_var_182 = var_currTransaction.restrict(pc_39);
            var_$tmp20 = var_$tmp20.updateUnderGuard(pc_39, temp_var_182);
            
            NamedTupleVS temp_var_183;
            temp_var_183 = new NamedTupleVS("client", var_$tmp19.restrict(pc_39), "rec", var_$tmp20.restrict(pc_39));
            var_$tmp21 = var_$tmp21.updateUnderGuard(pc_39, temp_var_183);
            
            effects.send(pc_39, var_$tmp17.restrict(pc_39), var_$tmp18.restrict(pc_39), new UnionVS(var_$tmp21.restrict(pc_39)));
            
        }
        
    }
    
    public static class Participant extends Machine {
        
        static State Init = new State("Init") {
            @Override public void entry(Guard pc_42, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Participant)machine).anonfun_13(pc_42, machine.sendBuffer, outcome);
            }
        };
        static State WaitForRequests = new State("WaitForRequests") {
        };
        private MapVS<String, PredVS<String> /* pred enum tPreds */> var_kvStore = new MapVS<String, PredVS<String> /* pred enum tPreds */>(Guard.constTrue());
        private MapVS<String, NamedTupleVS> var_pendingWriteTrans = new MapVS<String, NamedTupleVS>(Guard.constTrue());
        
        @Override
        public void reset() {
                super.reset();
                var_kvStore = new MapVS<String, PredVS<String> /* pred enum tPreds */>(Guard.constTrue());
                var_pendingWriteTrans = new MapVS<String, NamedTupleVS>(Guard.constTrue());
        }
        
        @Override
        public List<ValueSummary> getLocalState() {
                List<ValueSummary> res = super.getLocalState();
                res.add(var_kvStore);
                res.add(var_pendingWriteTrans);
                return res;
        }
        
        public Participant(int id) {
            super("Participant", id, EventBufferSemantics.queue, Init, Init
                , WaitForRequests
                
            );
            Init.addHandlers();
            WaitForRequests.addHandlers(new EventHandler(eCommitTrans) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Participant)machine).anonfun_14(pc, machine.sendBuffer, (PredVS<String> /* pred enum tPreds */) ValueSummary.castFromAny(pc, new PredVS<String> /* pred enum tPreds */(), payload));
                    }
                },
                new EventHandler(ePrepareReq) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Participant)machine).anonfun_15(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("coordinator", new PrimitiveVS<Machine>(), "transId", new PredVS<String> /* pred enum tPreds */(), "rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())), payload));
                    }
                },
                new EventHandler(eReadTransReq) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Participant)machine).anonfun_16(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PredVS<String> /* pred enum tPreds */()), payload));
                    }
                });
        }
        
        Guard 
        anonfun_13(
            Guard pc_43,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            outcome.addGuardedGoto(pc_43, WaitForRequests);
            pc_43 = Guard.constFalse();
            
            return pc_43;
        }
        
        void 
        anonfun_14(
            Guard pc_44,
            EventBuffer effects,
            PredVS<String> /* pred enum tPreds */ var_transId
        ) {
            NamedTupleVS var_transaction =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_44);
            
            PrimitiveVS<Boolean> var_$tmp0 =
                new PrimitiveVS<Boolean>(false).restrict(pc_44);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp1 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_44);
            
            MapVS<String, NamedTupleVS> var_$tmp2 =
                new MapVS<String, NamedTupleVS>(Guard.constTrue()).restrict(pc_44);
            
            PrimitiveVS<String> var_$tmp3 =
                new PrimitiveVS<String>("").restrict(pc_44);
            
            NamedTupleVS var_$tmp4 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_44);
            
            NamedTupleVS var_$tmp5 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_44);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp6 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_44);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp7 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_44);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp8 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_44);
            
            PrimitiveVS<Boolean> temp_var_184;
            temp_var_184 = var_pendingWriteTrans.restrict(pc_44).containsKey(var_transId.restrict(pc_44));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_44, temp_var_184);
            
            PredVS<String> /* pred enum tPreds */ temp_var_185;
            temp_var_185 = var_transId.restrict(pc_44);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_44, temp_var_185);
            
            MapVS<String, NamedTupleVS> temp_var_186;
            temp_var_186 = var_pendingWriteTrans.restrict(pc_44);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_44, temp_var_186);
            
            PrimitiveVS<String> temp_var_187;
            temp_var_187 = new PrimitiveVS<String>(String.format("Commit request for a non-pending transaction, transId: {0}, pendingTrans: {1}", var_$tmp1.restrict(pc_44), var_$tmp2.restrict(pc_44))).restrict(pc_44);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_44, temp_var_187);
            
            Assert.progProp(!(var_$tmp0.restrict(pc_44)).getValues().contains(Boolean.FALSE), var_$tmp3.restrict(pc_44), scheduler, var_$tmp0.restrict(pc_44).getGuardFor(Boolean.FALSE));
            NamedTupleVS temp_var_188;
            temp_var_188 = var_pendingWriteTrans.restrict(pc_44).get(var_transId.restrict(pc_44));
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_44, temp_var_188);
            
            NamedTupleVS temp_var_189;
            temp_var_189 = var_$tmp4.restrict(pc_44);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_44, temp_var_189);
            
            NamedTupleVS temp_var_190;
            temp_var_190 = var_$tmp5.restrict(pc_44);
            var_transaction = var_transaction.updateUnderGuard(pc_44, temp_var_190);
            
            PredVS<String> /* pred enum tPreds */ temp_var_191;
            temp_var_191 = (PredVS<String> /* pred enum tPreds */)((var_transaction.restrict(pc_44)).getField("key"));
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_44, temp_var_191);
            
            PredVS<String> /* pred enum tPreds */ temp_var_192;
            temp_var_192 = (PredVS<String> /* pred enum tPreds */)((var_transaction.restrict(pc_44)).getField("val"));
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_44, temp_var_192);
            
            PredVS<String> /* pred enum tPreds */ temp_var_193;
            temp_var_193 = var_$tmp7.restrict(pc_44);
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_44, temp_var_193);
            
            MapVS<String, PredVS<String> /* pred enum tPreds */> temp_var_194 = var_kvStore.restrict(pc_44);    
            PredVS<String> /* pred enum tPreds */ temp_var_196 = var_$tmp6.restrict(pc_44);
            PredVS<String> /* pred enum tPreds */ temp_var_195;
            temp_var_195 = var_$tmp8.restrict(pc_44);
            temp_var_194 = temp_var_194.put(temp_var_196, temp_var_195);
            var_kvStore = var_kvStore.updateUnderGuard(pc_44, temp_var_194);
            
            MapVS<String, NamedTupleVS> temp_var_197 = var_pendingWriteTrans.restrict(pc_44);    
            temp_var_197 = var_pendingWriteTrans.restrict(pc_44).remove(var_transId.restrict(pc_44));
            var_pendingWriteTrans = var_pendingWriteTrans.updateUnderGuard(pc_44, temp_var_197);
            
        }
        
        void 
        anonfun_15(
            Guard pc_45,
            EventBuffer effects,
            NamedTupleVS var_prepareReq
        ) {
            PredVS<String> /* pred enum tPreds */ var_$tmp0 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_45);
            
            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_45);
            
            PrimitiveVS<Boolean> var_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_45);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp3 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_45);
            
            MapVS<String, NamedTupleVS> var_$tmp4 =
                new MapVS<String, NamedTupleVS>(Guard.constTrue()).restrict(pc_45);
            
            PrimitiveVS<String> var_$tmp5 =
                new PrimitiveVS<String>("").restrict(pc_45);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp6 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_45);
            
            NamedTupleVS var_$tmp7 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_45);
            
            NamedTupleVS var_$tmp8 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_45);
            
            PrimitiveVS<Machine> var_$tmp9 =
                new PrimitiveVS<Machine>().restrict(pc_45);
            
            PrimitiveVS<Machine> var_$tmp10 =
                new PrimitiveVS<Machine>().restrict(pc_45);
            
            PrimitiveVS<Event> var_$tmp11 =
                new PrimitiveVS<Event>(_null).restrict(pc_45);
            
            PrimitiveVS<Machine> var_$tmp12 =
                new PrimitiveVS<Machine>().restrict(pc_45);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp13 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_45);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp14 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_45);
            
            NamedTupleVS var_$tmp15 =
                new NamedTupleVS("participant", new PrimitiveVS<Machine>(), "transId", new PredVS<String> /* pred enum tPreds */(), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_45);
            
            PredVS<String> /* pred enum tPreds */ temp_var_198;
            temp_var_198 = (PredVS<String> /* pred enum tPreds */)((var_prepareReq.restrict(pc_45)).getField("transId"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_45, temp_var_198);
            
            PrimitiveVS<Boolean> temp_var_199;
            temp_var_199 = var_pendingWriteTrans.restrict(pc_45).containsKey(var_$tmp0.restrict(pc_45));
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_45, temp_var_199);
            
            PrimitiveVS<Boolean> temp_var_200;
            temp_var_200 = (var_$tmp1.restrict(pc_45)).apply((temp_var_201) -> !temp_var_201);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_45, temp_var_200);
            
            PredVS<String> /* pred enum tPreds */ temp_var_202;
            temp_var_202 = (PredVS<String> /* pred enum tPreds */)((var_prepareReq.restrict(pc_45)).getField("transId"));
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_45, temp_var_202);
            
            MapVS<String, NamedTupleVS> temp_var_203;
            temp_var_203 = var_pendingWriteTrans.restrict(pc_45);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_45, temp_var_203);
            
            PrimitiveVS<String> temp_var_204;
            temp_var_204 = new PrimitiveVS<String>(String.format("Duplicate transaction ids not allowed!, received transId: {0}, pending transactions: {1}", var_$tmp3.restrict(pc_45), var_$tmp4.restrict(pc_45))).restrict(pc_45);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_45, temp_var_204);
            
            Assert.progProp(!(var_$tmp2.restrict(pc_45)).getValues().contains(Boolean.FALSE), var_$tmp5.restrict(pc_45), scheduler, var_$tmp2.restrict(pc_45).getGuardFor(Boolean.FALSE));
            PredVS<String> /* pred enum tPreds */ temp_var_205;
            temp_var_205 = (PredVS<String> /* pred enum tPreds */)((var_prepareReq.restrict(pc_45)).getField("transId"));
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_45, temp_var_205);
            
            NamedTupleVS temp_var_206;
            temp_var_206 = (NamedTupleVS)((var_prepareReq.restrict(pc_45)).getField("rec"));
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_45, temp_var_206);
            
            NamedTupleVS temp_var_207;
            temp_var_207 = var_$tmp7.restrict(pc_45);
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_45, temp_var_207);
            
            MapVS<String, NamedTupleVS> temp_var_208 = var_pendingWriteTrans.restrict(pc_45);    
            PredVS<String> /* pred enum tPreds */ temp_var_210 = var_$tmp6.restrict(pc_45);
            NamedTupleVS temp_var_209;
            temp_var_209 = var_$tmp8.restrict(pc_45);
            temp_var_208 = temp_var_208.put(temp_var_210, temp_var_209);
            var_pendingWriteTrans = var_pendingWriteTrans.updateUnderGuard(pc_45, temp_var_208);
            
            PrimitiveVS<Machine> temp_var_211;
            temp_var_211 = (PrimitiveVS<Machine>)((var_prepareReq.restrict(pc_45)).getField("coordinator"));
            var_$tmp9 = var_$tmp9.updateUnderGuard(pc_45, temp_var_211);
            
            PrimitiveVS<Machine> temp_var_212;
            temp_var_212 = var_$tmp9.restrict(pc_45);
            var_$tmp10 = var_$tmp10.updateUnderGuard(pc_45, temp_var_212);
            
            PrimitiveVS<Event> temp_var_213;
            temp_var_213 = new PrimitiveVS<Event>(ePrepareResp).restrict(pc_45);
            var_$tmp11 = var_$tmp11.updateUnderGuard(pc_45, temp_var_213);
            
            PrimitiveVS<Machine> temp_var_214;
            temp_var_214 = new PrimitiveVS<Machine>(this).restrict(pc_45);
            var_$tmp12 = var_$tmp12.updateUnderGuard(pc_45, temp_var_214);
            
            PredVS<String> /* pred enum tPreds */ temp_var_215;
            temp_var_215 = (PredVS<String> /* pred enum tPreds */)((var_prepareReq.restrict(pc_45)).getField("transId"));
            var_$tmp13 = var_$tmp13.updateUnderGuard(pc_45, temp_var_215);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_216;
            temp_var_216 = new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_45);
            var_$tmp14 = var_$tmp14.updateUnderGuard(pc_45, temp_var_216);
            
            NamedTupleVS temp_var_217;
            temp_var_217 = new NamedTupleVS("participant", var_$tmp12.restrict(pc_45), "transId", var_$tmp13.restrict(pc_45), "status", var_$tmp14.restrict(pc_45));
            var_$tmp15 = var_$tmp15.updateUnderGuard(pc_45, temp_var_217);
            
            effects.send(pc_45, var_$tmp10.restrict(pc_45), var_$tmp11.restrict(pc_45), new UnionVS(var_$tmp15.restrict(pc_45)));
            
        }
        
        void 
        anonfun_16(
            Guard pc_46,
            EventBuffer effects,
            NamedTupleVS var_req
        ) {
            PredVS<String> /* pred enum tPreds */ var_$tmp0 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_46);
            
            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_46);
            
            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_46);
            
            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_46);
            
            PrimitiveVS<Event> var_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_46);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp5 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_46);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp6 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_46);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp7 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_46);
            
            NamedTupleVS var_$tmp8 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_46);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp9 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_46);
            
            NamedTupleVS var_$tmp10 =
                new NamedTupleVS("rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_46);
            
            PrimitiveVS<Machine> var_$tmp11 =
                new PrimitiveVS<Machine>().restrict(pc_46);
            
            PrimitiveVS<Machine> var_$tmp12 =
                new PrimitiveVS<Machine>().restrict(pc_46);
            
            PrimitiveVS<Event> var_$tmp13 =
                new PrimitiveVS<Event>(_null).restrict(pc_46);
            
            NamedTupleVS var_$tmp14 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_46);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp15 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_46);
            
            NamedTupleVS var_$tmp16 =
                new NamedTupleVS("rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_46);
            
            PredVS<String> /* pred enum tPreds */ temp_var_218;
            temp_var_218 = (PredVS<String> /* pred enum tPreds */)((var_req.restrict(pc_46)).getField("key"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_46, temp_var_218);
            
            PrimitiveVS<Boolean> temp_var_219;
            temp_var_219 = var_kvStore.restrict(pc_46).containsKey(var_$tmp0.restrict(pc_46));
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_46, temp_var_219);
            
            PrimitiveVS<Boolean> temp_var_220 = var_$tmp1.restrict(pc_46);
            Guard pc_47 = BooleanVS.getTrueGuard(temp_var_220);
            Guard pc_48 = BooleanVS.getFalseGuard(temp_var_220);
            boolean jumpedOut_18 = false;
            boolean jumpedOut_19 = false;
            if (!pc_47.isFalse()) {
                // 'then' branch
                PrimitiveVS<Machine> temp_var_221;
                temp_var_221 = (PrimitiveVS<Machine>)((var_req.restrict(pc_47)).getField("client"));
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_47, temp_var_221);
                
                PrimitiveVS<Machine> temp_var_222;
                temp_var_222 = var_$tmp2.restrict(pc_47);
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_47, temp_var_222);
                
                PrimitiveVS<Event> temp_var_223;
                temp_var_223 = new PrimitiveVS<Event>(eReadTransResp).restrict(pc_47);
                var_$tmp4 = var_$tmp4.updateUnderGuard(pc_47, temp_var_223);
                
                PredVS<String> /* pred enum tPreds */ temp_var_224;
                temp_var_224 = (PredVS<String> /* pred enum tPreds */)((var_req.restrict(pc_47)).getField("key"));
                var_$tmp5 = var_$tmp5.updateUnderGuard(pc_47, temp_var_224);
                
                PredVS<String> /* pred enum tPreds */ temp_var_225;
                temp_var_225 = (PredVS<String> /* pred enum tPreds */)((var_req.restrict(pc_47)).getField("key"));
                var_$tmp6 = var_$tmp6.updateUnderGuard(pc_47, temp_var_225);
                
                PredVS<String> /* pred enum tPreds */ temp_var_226;
                temp_var_226 = var_kvStore.restrict(pc_47).get(var_$tmp6.restrict(pc_47));
                var_$tmp7 = var_$tmp7.updateUnderGuard(pc_47, temp_var_226);
                
                NamedTupleVS temp_var_227;
                temp_var_227 = new NamedTupleVS("key", var_$tmp5.restrict(pc_47), "val", var_$tmp7.restrict(pc_47));
                var_$tmp8 = var_$tmp8.updateUnderGuard(pc_47, temp_var_227);
                
                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_228;
                temp_var_228 = new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_47);
                var_$tmp9 = var_$tmp9.updateUnderGuard(pc_47, temp_var_228);
                
                NamedTupleVS temp_var_229;
                temp_var_229 = new NamedTupleVS("rec", var_$tmp8.restrict(pc_47), "status", var_$tmp9.restrict(pc_47));
                var_$tmp10 = var_$tmp10.updateUnderGuard(pc_47, temp_var_229);
                
                effects.send(pc_47, var_$tmp3.restrict(pc_47), var_$tmp4.restrict(pc_47), new UnionVS(var_$tmp10.restrict(pc_47)));
                
            }
            if (!pc_48.isFalse()) {
                // 'else' branch
                PrimitiveVS<Machine> temp_var_230;
                temp_var_230 = (PrimitiveVS<Machine>)((var_req.restrict(pc_48)).getField("client"));
                var_$tmp11 = var_$tmp11.updateUnderGuard(pc_48, temp_var_230);
                
                PrimitiveVS<Machine> temp_var_231;
                temp_var_231 = var_$tmp11.restrict(pc_48);
                var_$tmp12 = var_$tmp12.updateUnderGuard(pc_48, temp_var_231);
                
                PrimitiveVS<Event> temp_var_232;
                temp_var_232 = new PrimitiveVS<Event>(eReadTransResp).restrict(pc_48);
                var_$tmp13 = var_$tmp13.updateUnderGuard(pc_48, temp_var_232);
                
                NamedTupleVS temp_var_233;
                temp_var_233 = new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_48);
                var_$tmp14 = var_$tmp14.updateUnderGuard(pc_48, temp_var_233);
                
                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_234;
                temp_var_234 = new PrimitiveVS<Integer>(1 /* enum tTransStatus elem ERROR */).restrict(pc_48);
                var_$tmp15 = var_$tmp15.updateUnderGuard(pc_48, temp_var_234);
                
                NamedTupleVS temp_var_235;
                temp_var_235 = new NamedTupleVS("rec", var_$tmp14.restrict(pc_48), "status", var_$tmp15.restrict(pc_48));
                var_$tmp16 = var_$tmp16.updateUnderGuard(pc_48, temp_var_235);
                
                effects.send(pc_48, var_$tmp12.restrict(pc_48), var_$tmp13.restrict(pc_48), new UnionVS(var_$tmp16.restrict(pc_48)));
                
            }
            if (jumpedOut_18 || jumpedOut_19) {
                pc_46 = pc_47.or(pc_48);
            }
            
        }
        
    }
    
    public static class Main extends Machine {
        
        static State Init = new State("Init") {
            @Override public void entry(Guard pc_49, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Main)machine).anonfun_17(pc_49, machine.sendBuffer);
            }
        };
        
        @Override
        public void reset() {
                super.reset();
        }
        
        @Override
        public List<ValueSummary> getLocalState() {
                List<ValueSummary> res = super.getLocalState();
                return res;
        }
        
        public Main(int id) {
            super("Main", id, EventBufferSemantics.queue, Init, Init
                
            );
            Init.addHandlers();
        }
        
        void 
        anonfun_17(
            Guard pc_50,
            EventBuffer effects
        ) {
            PrimitiveVS<Machine> var_coord =
                new PrimitiveVS<Machine>().restrict(pc_50);
            
            ListVS<PrimitiveVS<Machine>> var_participants =
                new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_50);
            
            PrimitiveVS<Integer> var_i =
                new PrimitiveVS<Integer>(0).restrict(pc_50);
            
            PrimitiveVS<Boolean> var_$tmp0 =
                new PrimitiveVS<Boolean>(false).restrict(pc_50);
            
            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_50);
            
            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_50);
            
            PrimitiveVS<Integer> var_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_50);
            
            ListVS<PrimitiveVS<Machine>> var_$tmp4 =
                new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_50);
            
            PrimitiveVS<Machine> var_$tmp5 =
                new PrimitiveVS<Machine>().restrict(pc_50);
            
            PrimitiveVS<Machine> var_$tmp6 =
                new PrimitiveVS<Machine>().restrict(pc_50);
            
            java.util.List<Guard> loop_exits_4 = new java.util.ArrayList<>();
            boolean loop_early_ret_4 = false;
            Guard pc_51 = pc_50;
            while (!pc_51.isFalse()) {
                PrimitiveVS<Boolean> temp_var_236;
                temp_var_236 = (var_i.restrict(pc_51)).apply(new PrimitiveVS<Integer>(2).restrict(pc_51), (temp_var_237, temp_var_238) -> temp_var_237 < temp_var_238);
                var_$tmp0 = var_$tmp0.updateUnderGuard(pc_51, temp_var_236);
                
                PrimitiveVS<Boolean> temp_var_239;
                temp_var_239 = var_$tmp0.restrict(pc_51);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_51, temp_var_239);
                
                PrimitiveVS<Boolean> temp_var_240 = var_$tmp1.restrict(pc_51);
                Guard pc_52 = BooleanVS.getTrueGuard(temp_var_240);
                Guard pc_53 = BooleanVS.getFalseGuard(temp_var_240);
                boolean jumpedOut_20 = false;
                boolean jumpedOut_21 = false;
                if (!pc_52.isFalse()) {
                    // 'then' branch
                }
                if (!pc_53.isFalse()) {
                    // 'else' branch
                    loop_exits_4.add(pc_53);
                    jumpedOut_21 = true;
                    pc_53 = Guard.constFalse();
                    
                }
                if (jumpedOut_20 || jumpedOut_21) {
                    pc_51 = pc_52.or(pc_53);
                }
                
                if (!pc_51.isFalse()) {
                    PrimitiveVS<Machine> temp_var_241;
                    temp_var_241 = effects.create(pc_51, scheduler, Participant.class, (i) -> new Participant(i));
                    var_$tmp2 = var_$tmp2.updateUnderGuard(pc_51, temp_var_241);
                    
                    ListVS<PrimitiveVS<Machine>> temp_var_242 = var_participants.restrict(pc_51);    
                    temp_var_242 = var_participants.restrict(pc_51).insert(var_i.restrict(pc_51), var_$tmp2.restrict(pc_51));
                    var_participants = var_participants.updateUnderGuard(pc_51, temp_var_242);
                    
                    PrimitiveVS<Integer> temp_var_243;
                    temp_var_243 = (var_i.restrict(pc_51)).apply(new PrimitiveVS<Integer>(1).restrict(pc_51), (temp_var_244, temp_var_245) -> temp_var_244 + temp_var_245);
                    var_$tmp3 = var_$tmp3.updateUnderGuard(pc_51, temp_var_243);
                    
                    PrimitiveVS<Integer> temp_var_246;
                    temp_var_246 = var_$tmp3.restrict(pc_51);
                    var_i = var_i.updateUnderGuard(pc_51, temp_var_246);
                    
                }
            }
            if (loop_early_ret_4) {
                pc_50 = Guard.orMany(loop_exits_4);
            }
            
            ListVS<PrimitiveVS<Machine>> temp_var_247;
            temp_var_247 = var_participants.restrict(pc_50);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_50, temp_var_247);
            
            PrimitiveVS<Machine> temp_var_248;
            temp_var_248 = effects.create(pc_50, scheduler, Coordinator.class, new UnionVS (var_$tmp4.restrict(pc_50)), (i) -> new Coordinator(i));
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_50, temp_var_248);
            
            PrimitiveVS<Machine> temp_var_249;
            temp_var_249 = var_$tmp5.restrict(pc_50);
            var_coord = var_coord.updateUnderGuard(pc_50, temp_var_249);
            
            PrimitiveVS<Machine> temp_var_250;
            temp_var_250 = var_coord.restrict(pc_50);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_50, temp_var_250);
            
            effects.create(pc_50, scheduler, TestClient.class, new UnionVS (var_$tmp6.restrict(pc_50)), (i) -> new TestClient(i));
            
        }
        
    }
    
    // Skipping TypeDef 'tPrepareReq'

    // Skipping TypeDef 'tPrepareResp'

    // Skipping TypeDef 'tRecord'

    // Skipping TypeDef 'tWriteTransReq'

    // Skipping TypeDef 'tWriteTransResp'

    // Skipping TypeDef 'tReadTransReq'

    // Skipping TypeDef 'tReadTransResp'

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
