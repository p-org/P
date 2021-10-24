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

public class client implements Program {
    
    public static Scheduler scheduler;
    
    @Override
    public void setScheduler (Scheduler s) { this.scheduler = s; }
    
    
    
    // Skipping EnumElem 'SUCCESS'

    // Skipping EnumElem 'ERROR'

    // Skipping EnumElem 'TIMEOUT'

    // Skipping PEnum 'tTransStatus'

    public static Event _null = new Event("_null");
    public static Event _halt = new Event("_halt");
    public static Event eWriteTransReq = new Event("eWriteTransReq");
    public static Event eWriteTransResp = new Event("eWriteTransResp");
    public static Event eReadTransReq = new Event("eReadTransReq");
    public static Event eReadTransResp = new Event("eReadTransResp");
    public static Event ePrepareReq = new Event("ePrepareReq");
    public static Event ePrepareResp = new Event("ePrepareResp");
    public static Event eCommitTrans = new Event("eCommitTrans");
    public static Event eAbortTrans = new Event("eAbortTrans");
    public static Event eStartTimer = new Event("eStartTimer");
    public static Event eCancelTimer = new Event("eCancelTimer");
    public static Event eCancelTimerFailed = new Event("eCancelTimerFailed");
    public static Event eCancelTimerSuccess = new Event("eCancelTimerSuccess");
    public static Event eTimeOut = new Event("eTimeOut");
    // Skipping Interface 'Client'

    // Skipping Interface 'Coordinator'

    // Skipping Interface 'Participant'

    // Skipping Interface 'Main'

    // Skipping Interface 'Timer'

    public static class Client extends Machine {
        
        static State Init = new State("Init") {
            @Override public void entry(Guard pc_0, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Client)machine).anonfun_0(pc_0, machine.sendBuffer, outcome, payload != null ? (NamedTupleVS) ValueSummary.castFromAny(pc_0, new NamedTupleVS("coor", new PrimitiveVS<Machine>(), "n", new PrimitiveVS<Integer>(0)).restrict(pc_0), payload) : new NamedTupleVS("coor", new PrimitiveVS<Machine>(), "n", new PrimitiveVS<Integer>(0)).restrict(pc_0));
            }
        };
        static State SendWriteTransaction = new State("SendWriteTransaction") {
            @Override public void entry(Guard pc_1, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Client)machine).anonfun_1(pc_1, machine.sendBuffer);
            }
        };
        static State ConfirmTransaction = new State("ConfirmTransaction") {
            @Override public void entry(Guard pc_2, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Client)machine).anonfun_2(pc_2, machine.sendBuffer, payload != null ? (NamedTupleVS) ValueSummary.castFromAny(pc_2, new NamedTupleVS("transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_2), payload) : new NamedTupleVS("transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_2));
            }
        };
        private PrimitiveVS<Machine> var_coordinator = new PrimitiveVS<Machine>();
        private NamedTupleVS var_currTransaction = new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0));
        private PrimitiveVS<Integer> var_N = new PrimitiveVS<Integer>(0);
        private NamedTupleVS var_currWriteResponse = new NamedTupleVS("transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0));
        
        @Override
        public void reset() {
                super.reset();
                var_coordinator = new PrimitiveVS<Machine>();
                var_currTransaction = new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0));
                var_N = new PrimitiveVS<Integer>(0);
                var_currWriteResponse = new NamedTupleVS("transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0));
        }
        
        public Client(int id) {
            super("Client", id, EventBufferSemantics.queue, Init, Init
                , SendWriteTransaction
                , ConfirmTransaction
                
            );
            Init.addHandlers();
            SendWriteTransaction.addHandlers(new GotoEventHandler(eWriteTransResp, ConfirmTransaction));
            ConfirmTransaction.addHandlers(new EventHandler(eReadTransResp) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Client)machine).anonfun_3(pc, machine.sendBuffer, outcome, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)), payload));
                    }
                });
        }
        
        Guard 
        anonfun_0(
            Guard pc_3,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            NamedTupleVS var_payload
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_3);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_3);
            
            PrimitiveVS<Integer> var_$tmp2 =
                new PrimitiveVS<Integer>(0).restrict(pc_3);
            
            PrimitiveVS<Integer> var_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_3);
            
            PrimitiveVS<Machine> temp_var_0;
            temp_var_0 = (PrimitiveVS<Machine>)((var_payload.restrict(pc_3)).getField("coor"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_3, temp_var_0);
            
            PrimitiveVS<Machine> temp_var_1;
            temp_var_1 = var_$tmp0.restrict(pc_3);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_3, temp_var_1);
            
            PrimitiveVS<Machine> temp_var_2;
            temp_var_2 = var_$tmp1.restrict(pc_3);
            var_coordinator = var_coordinator.updateUnderGuard(pc_3, temp_var_2);
            
            PrimitiveVS<Integer> temp_var_3;
            temp_var_3 = (PrimitiveVS<Integer>)((var_payload.restrict(pc_3)).getField("n"));
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_3, temp_var_3);
            
            PrimitiveVS<Integer> temp_var_4;
            temp_var_4 = var_$tmp2.restrict(pc_3);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_3, temp_var_4);
            
            PrimitiveVS<Integer> temp_var_5;
            temp_var_5 = var_$tmp3.restrict(pc_3);
            var_N = var_N.updateUnderGuard(pc_3, temp_var_5);
            
            outcome.addGuardedGoto(pc_3, SendWriteTransaction);
            pc_3 = Guard.constFalse();
            
            return pc_3;
        }
        
        void 
        anonfun_1(
            Guard pc_4,
            EventBuffer effects
        ) {
            NamedTupleVS var_$tmp0 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_4);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_4);
            
            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_4);
            
            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_4);
            
            NamedTupleVS var_$tmp4 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_4);
            
            NamedTupleVS var_$tmp5 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0))).restrict(pc_4);
            
            PrimitiveVS<Integer> var_local_0_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_4);
            
            PrimitiveVS<Integer> var_local_0_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_4);
            
            NamedTupleVS var_local_0_$tmp2 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_4);
            
            PrimitiveVS<Integer> temp_var_6;
            temp_var_6 = scheduler.getNextInteger(new PrimitiveVS<Integer>(10).restrict(pc_4), pc_4);
            var_local_0_$tmp0 = var_local_0_$tmp0.updateUnderGuard(pc_4, temp_var_6);
            
            PrimitiveVS<Integer> temp_var_7;
            temp_var_7 = scheduler.getNextInteger(new PrimitiveVS<Integer>(10).restrict(pc_4), pc_4);
            var_local_0_$tmp1 = var_local_0_$tmp1.updateUnderGuard(pc_4, temp_var_7);
            
            NamedTupleVS temp_var_8;
            temp_var_8 = new NamedTupleVS("key", var_local_0_$tmp0.restrict(pc_4), "val", var_local_0_$tmp1.restrict(pc_4));
            var_local_0_$tmp2 = var_local_0_$tmp2.updateUnderGuard(pc_4, temp_var_8);
            
            NamedTupleVS temp_var_9;
            temp_var_9 = var_local_0_$tmp2.restrict(pc_4);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_4, temp_var_9);
            
            NamedTupleVS temp_var_10;
            temp_var_10 = var_$tmp0.restrict(pc_4);
            var_currTransaction = var_currTransaction.updateUnderGuard(pc_4, temp_var_10);
            
            PrimitiveVS<Machine> temp_var_11;
            temp_var_11 = var_coordinator.restrict(pc_4);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_4, temp_var_11);
            
            PrimitiveVS<Event> temp_var_12;
            temp_var_12 = new PrimitiveVS<Event>(eWriteTransReq).restrict(pc_4);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_4, temp_var_12);
            
            PrimitiveVS<Machine> temp_var_13;
            temp_var_13 = new PrimitiveVS<Machine>(this).restrict(pc_4);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_4, temp_var_13);
            
            NamedTupleVS temp_var_14;
            temp_var_14 = var_currTransaction.restrict(pc_4);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_4, temp_var_14);
            
            NamedTupleVS temp_var_15;
            temp_var_15 = new NamedTupleVS("client", var_$tmp3.restrict(pc_4), "rec", var_$tmp4.restrict(pc_4));
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_4, temp_var_15);
            
            effects.send(pc_4, var_$tmp1.restrict(pc_4), var_$tmp2.restrict(pc_4), new UnionVS(var_$tmp5.restrict(pc_4)));
            
        }
        
        void 
        anonfun_2(
            Guard pc_5,
            EventBuffer effects,
            NamedTupleVS var_writeResp
        ) {
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp0 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_5);
            
            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_5);
            
            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_5);
            
            PrimitiveVS<Event> var_$tmp3 =
                new PrimitiveVS<Event>(_null).restrict(pc_5);
            
            PrimitiveVS<Machine> var_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_5);
            
            PrimitiveVS<Integer> var_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_5);
            
            NamedTupleVS var_$tmp6 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PrimitiveVS<Integer>(0)).restrict(pc_5);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_16;
            temp_var_16 = (PrimitiveVS<Integer> /* enum tTransStatus */)((var_writeResp.restrict(pc_5)).getField("status"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_5, temp_var_16);
            
            PrimitiveVS<Boolean> temp_var_17;
            temp_var_17 = var_$tmp0.restrict(pc_5).symbolicEquals(new PrimitiveVS<Integer>(2 /* enum tTransStatus elem TIMEOUT */).restrict(pc_5), pc_5);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_5, temp_var_17);
            
            PrimitiveVS<Boolean> temp_var_18 = var_$tmp1.restrict(pc_5);
            Guard pc_6 = BooleanVS.getTrueGuard(temp_var_18);
            Guard pc_7 = BooleanVS.getFalseGuard(temp_var_18);
            boolean jumpedOut_0 = false;
            boolean jumpedOut_1 = false;
            if (!pc_6.isFalse()) {
                // 'then' branch
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
                NamedTupleVS temp_var_19;
                temp_var_19 = var_writeResp.restrict(pc_5);
                var_currWriteResponse = var_currWriteResponse.updateUnderGuard(pc_5, temp_var_19);
                
                PrimitiveVS<Machine> temp_var_20;
                temp_var_20 = var_coordinator.restrict(pc_5);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_5, temp_var_20);
                
                PrimitiveVS<Event> temp_var_21;
                temp_var_21 = new PrimitiveVS<Event>(eReadTransReq).restrict(pc_5);
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_5, temp_var_21);
                
                PrimitiveVS<Machine> temp_var_22;
                temp_var_22 = new PrimitiveVS<Machine>(this).restrict(pc_5);
                var_$tmp4 = var_$tmp4.updateUnderGuard(pc_5, temp_var_22);
                
                PrimitiveVS<Integer> temp_var_23;
                temp_var_23 = (PrimitiveVS<Integer>)((var_currTransaction.restrict(pc_5)).getField("key"));
                var_$tmp5 = var_$tmp5.updateUnderGuard(pc_5, temp_var_23);
                
                NamedTupleVS temp_var_24;
                temp_var_24 = new NamedTupleVS("client", var_$tmp4.restrict(pc_5), "key", var_$tmp5.restrict(pc_5));
                var_$tmp6 = var_$tmp6.updateUnderGuard(pc_5, temp_var_24);
                
                effects.send(pc_5, var_$tmp2.restrict(pc_5), var_$tmp3.restrict(pc_5), new UnionVS(var_$tmp6.restrict(pc_5)));
                
            }
        }
        
        Guard 
        anonfun_3(
            Guard pc_8,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            NamedTupleVS var_readResp
        ) {
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp0 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_8);
            
            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_8);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp2 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_8);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp3 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_8);
            
            PrimitiveVS<Boolean> var_$tmp4 =
                new PrimitiveVS<Boolean>(false).restrict(pc_8);
            
            PrimitiveVS<String> var_$tmp5 =
                new PrimitiveVS<String>("").restrict(pc_8);
            
            PrimitiveVS<Boolean> var_$tmp6 =
                new PrimitiveVS<Boolean>(false).restrict(pc_8);
            
            PrimitiveVS<Integer> var_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_8);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_25;
            temp_var_25 = (PrimitiveVS<Integer> /* enum tTransStatus */)((var_currWriteResponse.restrict(pc_8)).getField("status"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_8, temp_var_25);
            
            PrimitiveVS<Boolean> temp_var_26;
            temp_var_26 = var_$tmp0.restrict(pc_8).symbolicEquals(new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_8), pc_8);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_8, temp_var_26);
            
            PrimitiveVS<Boolean> temp_var_27 = var_$tmp1.restrict(pc_8);
            Guard pc_9 = BooleanVS.getTrueGuard(temp_var_27);
            Guard pc_10 = BooleanVS.getFalseGuard(temp_var_27);
            boolean jumpedOut_2 = false;
            boolean jumpedOut_3 = false;
            if (!pc_9.isFalse()) {
                // 'then' branch
                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_28;
                temp_var_28 = (PrimitiveVS<Integer> /* enum tTransStatus */)((var_readResp.restrict(pc_9)).getField("status"));
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_9, temp_var_28);
                
                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_29;
                temp_var_29 = (PrimitiveVS<Integer> /* enum tTransStatus */)((var_currWriteResponse.restrict(pc_9)).getField("status"));
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_9, temp_var_29);
                
                PrimitiveVS<Boolean> temp_var_30;
                temp_var_30 = var_$tmp2.restrict(pc_9).symbolicEquals(var_$tmp3.restrict(pc_9), pc_9);
                var_$tmp4 = var_$tmp4.updateUnderGuard(pc_9, temp_var_30);
                
                PrimitiveVS<String> temp_var_31;
                temp_var_31 = new PrimitiveVS<String>(String.format("Inconsistency!")).restrict(pc_9);
                var_$tmp5 = var_$tmp5.updateUnderGuard(pc_9, temp_var_31);
                
                Assert.progProp(!(var_$tmp4.restrict(pc_9)).getValues().contains(Boolean.FALSE), var_$tmp5.restrict(pc_9), scheduler, var_$tmp4.restrict(pc_9).getGuardFor(Boolean.FALSE));
            }
            if (!pc_10.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_2 || jumpedOut_3) {
                pc_8 = pc_9.or(pc_10);
            }
            
            PrimitiveVS<Boolean> temp_var_32;
            temp_var_32 = (var_N.restrict(pc_8)).apply(new PrimitiveVS<Integer>(0).restrict(pc_8), (temp_var_33, temp_var_34) -> temp_var_33 > temp_var_34);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_8, temp_var_32);
            
            PrimitiveVS<Boolean> temp_var_35 = var_$tmp6.restrict(pc_8);
            Guard pc_11 = BooleanVS.getTrueGuard(temp_var_35);
            Guard pc_12 = BooleanVS.getFalseGuard(temp_var_35);
            boolean jumpedOut_4 = false;
            boolean jumpedOut_5 = false;
            if (!pc_11.isFalse()) {
                // 'then' branch
                PrimitiveVS<Integer> temp_var_36;
                temp_var_36 = (var_N.restrict(pc_11)).apply(new PrimitiveVS<Integer>(1).restrict(pc_11), (temp_var_37, temp_var_38) -> temp_var_37 - temp_var_38);
                var_$tmp7 = var_$tmp7.updateUnderGuard(pc_11, temp_var_36);
                
                PrimitiveVS<Integer> temp_var_39;
                temp_var_39 = var_$tmp7.restrict(pc_11);
                var_N = var_N.updateUnderGuard(pc_11, temp_var_39);
                
                outcome.addGuardedGoto(pc_11, SendWriteTransaction);
                pc_11 = Guard.constFalse();
                jumpedOut_4 = true;
                
            }
            if (!pc_12.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_4 || jumpedOut_5) {
                pc_8 = pc_11.or(pc_12);
            }
            
            if (!pc_8.isFalse()) {
            }
            return pc_8;
        }
        
    }
    
    public static class Coordinator extends Machine {
        
        static State Init = new State("Init") {
            @Override public void entry(Guard pc_13, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Coordinator)machine).anonfun_4(pc_13, machine.sendBuffer, outcome, payload != null ? (ListVS<PrimitiveVS<Machine>>) ValueSummary.castFromAny(pc_13, new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_13), payload) : new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_13));
            }
            @Override public void exit(Guard pc, Machine machine) {
                ((Coordinator)machine).anonfun_5(pc, machine.sendBuffer);
            }
        };
        static State WaitForTransactions = new State("WaitForTransactions") {
        };
        static State WaitForPrepareResponses = new State("WaitForPrepareResponses") {
            @Override public void exit(Guard pc, Machine machine) {
                ((Coordinator)machine).anonfun_6(pc, machine.sendBuffer);
            }
        };
        private ListVS<PrimitiveVS<Machine>> var_participants = new ListVS<PrimitiveVS<Machine>>(Guard.constTrue());
        private NamedTupleVS var_pendingWrTrans = new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)));
        private PrimitiveVS<Integer> var_currTransId = new PrimitiveVS<Integer>(0);
        private PrimitiveVS<Machine> var_timer = new PrimitiveVS<Machine>();
        private PrimitiveVS<Integer> var_countPrepareResponses = new PrimitiveVS<Integer>(0);
        
        @Override
        public void reset() {
                super.reset();
                var_participants = new ListVS<PrimitiveVS<Machine>>(Guard.constTrue());
                var_pendingWrTrans = new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)));
                var_currTransId = new PrimitiveVS<Integer>(0);
                var_timer = new PrimitiveVS<Machine>();
                var_countPrepareResponses = new PrimitiveVS<Integer>(0);
        }
        
        public Coordinator(int id) {
            super("Coordinator", id, EventBufferSemantics.queue, Init, Init
                , WaitForTransactions
                , WaitForPrepareResponses
                
            );
            Init.addHandlers();
            WaitForTransactions.addHandlers(new EventHandler(eWriteTransReq) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Coordinator)machine).anonfun_7(pc, machine.sendBuffer, outcome, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0))), payload));
                    }
                },
                new EventHandler(eReadTransReq) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Coordinator)machine).anonfun_8(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PrimitiveVS<Integer>(0)), payload));
                    }
                },
                new IgnoreEventHandler(ePrepareResp),
                new IgnoreEventHandler(eTimeOut));
            WaitForPrepareResponses.addHandlers(new DeferEventHandler(eWriteTransReq)
                ,
                new EventHandler(ePrepareResp) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Coordinator)machine).anonfun_9(pc, machine.sendBuffer, outcome, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("participant", new PrimitiveVS<Machine>(), "transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)), payload));
                    }
                },
                new GotoEventHandler(eTimeOut, WaitForTransactions) {
                    @Override public void transitionFunction(Guard pc, Machine machine, UnionVS payload) {
                        ((Coordinator)machine).anonfun_10(pc, machine.sendBuffer);
                    }
                },
                new EventHandler(eReadTransReq) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Coordinator)machine).anonfun_11(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PrimitiveVS<Integer>(0)), payload));
                    }
                });
        }
        
        Guard 
        anonfun_4(
            Guard pc_14,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            ListVS<PrimitiveVS<Machine>> var_payload
        ) {
            PrimitiveVS<Integer> var_i =
                new PrimitiveVS<Integer>(0).restrict(pc_14);
            
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_14);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_14);
            
            PrimitiveVS<Machine> var_inline_1_client =
                new PrimitiveVS<Machine>().restrict(pc_14);
            
            PrimitiveVS<Machine> var_local_1_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_14);
            
            PrimitiveVS<Machine> var_local_1_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_14);
            
            ListVS<PrimitiveVS<Machine>> temp_var_40;
            temp_var_40 = var_payload.restrict(pc_14);
            var_participants = var_participants.updateUnderGuard(pc_14, temp_var_40);
            
            PrimitiveVS<Integer> temp_var_41;
            temp_var_41 = new PrimitiveVS<Integer>(0).restrict(pc_14);
            var_i = var_i.updateUnderGuard(pc_14, temp_var_41);
            
            PrimitiveVS<Integer> temp_var_42;
            temp_var_42 = new PrimitiveVS<Integer>(0).restrict(pc_14);
            var_currTransId = var_currTransId.updateUnderGuard(pc_14, temp_var_42);
            
            PrimitiveVS<Machine> temp_var_43;
            temp_var_43 = new PrimitiveVS<Machine>(this).restrict(pc_14);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_14, temp_var_43);
            
            PrimitiveVS<Machine> temp_var_44;
            temp_var_44 = var_$tmp0.restrict(pc_14);
            var_inline_1_client = var_inline_1_client.updateUnderGuard(pc_14, temp_var_44);
            
            PrimitiveVS<Machine> temp_var_45;
            temp_var_45 = var_inline_1_client.restrict(pc_14);
            var_local_1_$tmp0 = var_local_1_$tmp0.updateUnderGuard(pc_14, temp_var_45);
            
            PrimitiveVS<Machine> temp_var_46;
            temp_var_46 = effects.create(pc_14, scheduler, Timer.class, new UnionVS (var_local_1_$tmp0.restrict(pc_14)), (i) -> new Timer(i));
            var_local_1_$tmp1 = var_local_1_$tmp1.updateUnderGuard(pc_14, temp_var_46);
            
            PrimitiveVS<Machine> temp_var_47;
            temp_var_47 = var_local_1_$tmp1.restrict(pc_14);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_14, temp_var_47);
            
            PrimitiveVS<Machine> temp_var_48;
            temp_var_48 = var_$tmp1.restrict(pc_14);
            var_timer = var_timer.updateUnderGuard(pc_14, temp_var_48);
            
            outcome.addGuardedGoto(pc_14, WaitForTransactions);
            pc_14 = Guard.constFalse();
            
            return pc_14;
        }
        
        void 
        anonfun_5(
            Guard pc_15,
            EventBuffer effects
        ) {
        }
        
        Guard 
        anonfun_7(
            Guard pc_16,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            NamedTupleVS var_wTrans
        ) {
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_16);
            
            PrimitiveVS<Event> var_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_16);
            
            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_16);
            
            PrimitiveVS<Integer> var_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_16);
            
            NamedTupleVS var_$tmp4 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_16);
            
            NamedTupleVS var_$tmp5 =
                new NamedTupleVS("coordinator", new PrimitiveVS<Machine>(), "transId", new PrimitiveVS<Integer>(0), "rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0))).restrict(pc_16);
            
            PrimitiveVS<Machine> var_$tmp6 =
                new PrimitiveVS<Machine>().restrict(pc_16);
            
            PrimitiveVS<Integer> var_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_16);
            
            PrimitiveVS<Event> var_inline_2_message =
                new PrimitiveVS<Event>(_null).restrict(pc_16);
            
            NamedTupleVS var_inline_2_payload =
                new NamedTupleVS("coordinator", new PrimitiveVS<Machine>(), "transId", new PrimitiveVS<Integer>(0), "rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0))).restrict(pc_16);
            
            PrimitiveVS<Integer> var_local_2_i =
                new PrimitiveVS<Integer>(0).restrict(pc_16);
            
            PrimitiveVS<Integer> var_local_2_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_16);
            
            PrimitiveVS<Boolean> var_local_2_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_16);
            
            PrimitiveVS<Boolean> var_local_2_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_16);
            
            PrimitiveVS<Machine> var_local_2_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_16);
            
            PrimitiveVS<Machine> var_local_2_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_16);
            
            PrimitiveVS<Event> var_local_2_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_16);
            
            UnionVS var_local_2_$tmp6 =
                new UnionVS().restrict(pc_16);
            
            PrimitiveVS<Integer> var_local_2_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_16);
            
            PrimitiveVS<Machine> var_inline_3_timer =
                new PrimitiveVS<Machine>().restrict(pc_16);
            
            PrimitiveVS<Integer> var_inline_3_timeout =
                new PrimitiveVS<Integer>(0).restrict(pc_16);
            
            PrimitiveVS<Machine> var_local_3_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_16);
            
            PrimitiveVS<Event> var_local_3_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_16);
            
            PrimitiveVS<Integer> var_local_3_$tmp2 =
                new PrimitiveVS<Integer>(0).restrict(pc_16);
            
            NamedTupleVS temp_var_49;
            temp_var_49 = var_wTrans.restrict(pc_16);
            var_pendingWrTrans = var_pendingWrTrans.updateUnderGuard(pc_16, temp_var_49);
            
            PrimitiveVS<Integer> temp_var_50;
            temp_var_50 = (var_currTransId.restrict(pc_16)).apply(new PrimitiveVS<Integer>(1).restrict(pc_16), (temp_var_51, temp_var_52) -> temp_var_51 + temp_var_52);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_16, temp_var_50);
            
            PrimitiveVS<Integer> temp_var_53;
            temp_var_53 = var_$tmp0.restrict(pc_16);
            var_currTransId = var_currTransId.updateUnderGuard(pc_16, temp_var_53);
            
            PrimitiveVS<Event> temp_var_54;
            temp_var_54 = new PrimitiveVS<Event>(ePrepareReq).restrict(pc_16);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_16, temp_var_54);
            
            PrimitiveVS<Machine> temp_var_55;
            temp_var_55 = new PrimitiveVS<Machine>(this).restrict(pc_16);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_16, temp_var_55);
            
            PrimitiveVS<Integer> temp_var_56;
            temp_var_56 = var_currTransId.restrict(pc_16);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_16, temp_var_56);
            
            NamedTupleVS temp_var_57;
            temp_var_57 = (NamedTupleVS)((var_wTrans.restrict(pc_16)).getField("rec"));
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_16, temp_var_57);
            
            NamedTupleVS temp_var_58;
            temp_var_58 = new NamedTupleVS("coordinator", var_$tmp2.restrict(pc_16), "transId", var_$tmp3.restrict(pc_16), "rec", var_$tmp4.restrict(pc_16));
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_16, temp_var_58);
            
            PrimitiveVS<Event> temp_var_59;
            temp_var_59 = var_$tmp1.restrict(pc_16);
            var_inline_2_message = var_inline_2_message.updateUnderGuard(pc_16, temp_var_59);
            
            NamedTupleVS temp_var_60;
            temp_var_60 = var_$tmp5.restrict(pc_16);
            var_inline_2_payload = var_inline_2_payload.updateUnderGuard(pc_16, temp_var_60);
            
            PrimitiveVS<Integer> temp_var_61;
            temp_var_61 = new PrimitiveVS<Integer>(0).restrict(pc_16);
            var_local_2_i = var_local_2_i.updateUnderGuard(pc_16, temp_var_61);
            
            java.util.List<Guard> loop_exits_0 = new java.util.ArrayList<>();
            boolean loop_early_ret_0 = false;
            Guard pc_17 = pc_16;
            while (!pc_17.isFalse()) {
                PrimitiveVS<Integer> temp_var_62;
                temp_var_62 = var_participants.restrict(pc_17).size();
                var_local_2_$tmp0 = var_local_2_$tmp0.updateUnderGuard(pc_17, temp_var_62);
                
                PrimitiveVS<Boolean> temp_var_63;
                temp_var_63 = (var_local_2_i.restrict(pc_17)).apply(var_local_2_$tmp0.restrict(pc_17), (temp_var_64, temp_var_65) -> temp_var_64 < temp_var_65);
                var_local_2_$tmp1 = var_local_2_$tmp1.updateUnderGuard(pc_17, temp_var_63);
                
                PrimitiveVS<Boolean> temp_var_66;
                temp_var_66 = var_local_2_$tmp1.restrict(pc_17);
                var_local_2_$tmp2 = var_local_2_$tmp2.updateUnderGuard(pc_17, temp_var_66);
                
                PrimitiveVS<Boolean> temp_var_67 = var_local_2_$tmp2.restrict(pc_17);
                Guard pc_18 = BooleanVS.getTrueGuard(temp_var_67);
                Guard pc_19 = BooleanVS.getFalseGuard(temp_var_67);
                boolean jumpedOut_6 = false;
                boolean jumpedOut_7 = false;
                if (!pc_18.isFalse()) {
                    // 'then' branch
                }
                if (!pc_19.isFalse()) {
                    // 'else' branch
                    loop_exits_0.add(pc_19);
                    jumpedOut_7 = true;
                    pc_19 = Guard.constFalse();
                    
                }
                if (jumpedOut_6 || jumpedOut_7) {
                    pc_17 = pc_18.or(pc_19);
                }
                
                if (!pc_17.isFalse()) {
                    PrimitiveVS<Machine> temp_var_68;
                    temp_var_68 = var_participants.restrict(pc_17).get(var_local_2_i.restrict(pc_17));
                    var_local_2_$tmp3 = var_local_2_$tmp3.updateUnderGuard(pc_17, temp_var_68);
                    
                    PrimitiveVS<Machine> temp_var_69;
                    temp_var_69 = var_local_2_$tmp3.restrict(pc_17);
                    var_local_2_$tmp4 = var_local_2_$tmp4.updateUnderGuard(pc_17, temp_var_69);
                    
                    PrimitiveVS<Event> temp_var_70;
                    temp_var_70 = var_inline_2_message.restrict(pc_17);
                    var_local_2_$tmp5 = var_local_2_$tmp5.updateUnderGuard(pc_17, temp_var_70);
                    
                    UnionVS temp_var_71;
                    temp_var_71 = ValueSummary.castToAny(pc_17, var_inline_2_payload.restrict(pc_17));
                    var_local_2_$tmp6 = var_local_2_$tmp6.updateUnderGuard(pc_17, temp_var_71);
                    
                    effects.send(pc_17, var_local_2_$tmp4.restrict(pc_17), var_local_2_$tmp5.restrict(pc_17), new UnionVS(var_local_2_$tmp6.restrict(pc_17)));
                    
                    PrimitiveVS<Integer> temp_var_72;
                    temp_var_72 = (var_local_2_i.restrict(pc_17)).apply(new PrimitiveVS<Integer>(1).restrict(pc_17), (temp_var_73, temp_var_74) -> temp_var_73 + temp_var_74);
                    var_local_2_$tmp7 = var_local_2_$tmp7.updateUnderGuard(pc_17, temp_var_72);
                    
                    PrimitiveVS<Integer> temp_var_75;
                    temp_var_75 = var_local_2_$tmp7.restrict(pc_17);
                    var_local_2_i = var_local_2_i.updateUnderGuard(pc_17, temp_var_75);
                    
                }
            }
            if (loop_early_ret_0) {
                pc_16 = Guard.orMany(loop_exits_0);
            }
            
            PrimitiveVS<Machine> temp_var_76;
            temp_var_76 = var_timer.restrict(pc_16);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_16, temp_var_76);
            
            PrimitiveVS<Integer> temp_var_77;
            temp_var_77 = new PrimitiveVS<Integer>(100).restrict(pc_16);
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_16, temp_var_77);
            
            PrimitiveVS<Machine> temp_var_78;
            temp_var_78 = var_$tmp6.restrict(pc_16);
            var_inline_3_timer = var_inline_3_timer.updateUnderGuard(pc_16, temp_var_78);
            
            PrimitiveVS<Integer> temp_var_79;
            temp_var_79 = var_$tmp7.restrict(pc_16);
            var_inline_3_timeout = var_inline_3_timeout.updateUnderGuard(pc_16, temp_var_79);
            
            PrimitiveVS<Machine> temp_var_80;
            temp_var_80 = var_inline_3_timer.restrict(pc_16);
            var_local_3_$tmp0 = var_local_3_$tmp0.updateUnderGuard(pc_16, temp_var_80);
            
            PrimitiveVS<Event> temp_var_81;
            temp_var_81 = new PrimitiveVS<Event>(eStartTimer).restrict(pc_16);
            var_local_3_$tmp1 = var_local_3_$tmp1.updateUnderGuard(pc_16, temp_var_81);
            
            PrimitiveVS<Integer> temp_var_82;
            temp_var_82 = var_inline_3_timeout.restrict(pc_16);
            var_local_3_$tmp2 = var_local_3_$tmp2.updateUnderGuard(pc_16, temp_var_82);
            
            effects.send(pc_16, var_local_3_$tmp0.restrict(pc_16), var_local_3_$tmp1.restrict(pc_16), new UnionVS(var_local_3_$tmp2.restrict(pc_16)));
            
            outcome.addGuardedGoto(pc_16, WaitForPrepareResponses);
            pc_16 = Guard.constFalse();
            
            return pc_16;
        }
        
        void 
        anonfun_8(
            Guard pc_20,
            EventBuffer effects,
            NamedTupleVS var_rTrans
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_20);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_20);
            
            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_20);
            
            NamedTupleVS var_$tmp3 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PrimitiveVS<Integer>(0)).restrict(pc_20);
            
            PrimitiveVS<Machine> temp_var_83;
            temp_var_83 = (PrimitiveVS<Machine>) scheduler.getNextElement(var_participants.restrict(pc_20), pc_20);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_20, temp_var_83);
            
            PrimitiveVS<Machine> temp_var_84;
            temp_var_84 = var_$tmp0.restrict(pc_20);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_20, temp_var_84);
            
            PrimitiveVS<Event> temp_var_85;
            temp_var_85 = new PrimitiveVS<Event>(eReadTransReq).restrict(pc_20);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_20, temp_var_85);
            
            NamedTupleVS temp_var_86;
            temp_var_86 = var_rTrans.restrict(pc_20);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_20, temp_var_86);
            
            effects.send(pc_20, var_$tmp1.restrict(pc_20), var_$tmp2.restrict(pc_20), new UnionVS(var_$tmp3.restrict(pc_20)));
            
        }
        
        Guard 
        anonfun_9(
            Guard pc_21,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            NamedTupleVS var_resp
        ) {
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_21);
            
            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_21);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp2 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_21);
            
            PrimitiveVS<Boolean> var_$tmp3 =
                new PrimitiveVS<Boolean>(false).restrict(pc_21);
            
            PrimitiveVS<Integer> var_$tmp4 =
                new PrimitiveVS<Integer>(0).restrict(pc_21);
            
            PrimitiveVS<Integer> var_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_21);
            
            PrimitiveVS<Boolean> var_$tmp6 =
                new PrimitiveVS<Boolean>(false).restrict(pc_21);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp7 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_21);
            
            PrimitiveVS<Event> var_local_6_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_21);
            
            PrimitiveVS<Integer> var_local_6_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_21);
            
            PrimitiveVS<Machine> var_local_6_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_21);
            
            PrimitiveVS<Machine> var_local_6_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_21);
            
            PrimitiveVS<Event> var_local_6_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_21);
            
            PrimitiveVS<Integer> var_local_6_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_21);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_local_6_$tmp6 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_21);
            
            NamedTupleVS var_local_6_$tmp7 =
                new NamedTupleVS("transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_21);
            
            PrimitiveVS<Machine> var_local_6_$tmp8 =
                new PrimitiveVS<Machine>().restrict(pc_21);
            
            PrimitiveVS<Event> var_local_6_inline_4_message =
                new PrimitiveVS<Event>(_null).restrict(pc_21);
            
            PrimitiveVS<Integer> var_local_6_inline_4_payload =
                new PrimitiveVS<Integer>(0).restrict(pc_21);
            
            PrimitiveVS<Integer> var_local_6_local_4_i =
                new PrimitiveVS<Integer>(0).restrict(pc_21);
            
            PrimitiveVS<Integer> var_local_6_local_4_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_21);
            
            PrimitiveVS<Boolean> var_local_6_local_4_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_21);
            
            PrimitiveVS<Boolean> var_local_6_local_4_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_21);
            
            PrimitiveVS<Machine> var_local_6_local_4_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_21);
            
            PrimitiveVS<Machine> var_local_6_local_4_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_21);
            
            PrimitiveVS<Event> var_local_6_local_4_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_21);
            
            UnionVS var_local_6_local_4_$tmp6 =
                new UnionVS().restrict(pc_21);
            
            PrimitiveVS<Integer> var_local_6_local_4_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_21);
            
            PrimitiveVS<Machine> var_local_6_inline_5_timer =
                new PrimitiveVS<Machine>().restrict(pc_21);
            
            PrimitiveVS<Machine> var_local_6_local_5_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_21);
            
            PrimitiveVS<Event> var_local_6_local_5_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_21);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_inline_9_respStatus =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_21);
            
            PrimitiveVS<Event> var_local_9_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_21);
            
            PrimitiveVS<Integer> var_local_9_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_21);
            
            PrimitiveVS<Machine> var_local_9_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_21);
            
            PrimitiveVS<Machine> var_local_9_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_21);
            
            PrimitiveVS<Event> var_local_9_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_21);
            
            PrimitiveVS<Integer> var_local_9_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_21);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_local_9_$tmp6 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_21);
            
            NamedTupleVS var_local_9_$tmp7 =
                new NamedTupleVS("transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_21);
            
            PrimitiveVS<Machine> var_local_9_$tmp8 =
                new PrimitiveVS<Machine>().restrict(pc_21);
            
            PrimitiveVS<Event> var_local_9_inline_7_message =
                new PrimitiveVS<Event>(_null).restrict(pc_21);
            
            PrimitiveVS<Integer> var_local_9_inline_7_payload =
                new PrimitiveVS<Integer>(0).restrict(pc_21);
            
            PrimitiveVS<Integer> var_local_9_local_7_i =
                new PrimitiveVS<Integer>(0).restrict(pc_21);
            
            PrimitiveVS<Integer> var_local_9_local_7_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_21);
            
            PrimitiveVS<Boolean> var_local_9_local_7_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_21);
            
            PrimitiveVS<Boolean> var_local_9_local_7_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_21);
            
            PrimitiveVS<Machine> var_local_9_local_7_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_21);
            
            PrimitiveVS<Machine> var_local_9_local_7_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_21);
            
            PrimitiveVS<Event> var_local_9_local_7_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_21);
            
            UnionVS var_local_9_local_7_$tmp6 =
                new UnionVS().restrict(pc_21);
            
            PrimitiveVS<Integer> var_local_9_local_7_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_21);
            
            PrimitiveVS<Machine> var_local_9_inline_8_timer =
                new PrimitiveVS<Machine>().restrict(pc_21);
            
            PrimitiveVS<Machine> var_local_9_local_8_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_21);
            
            PrimitiveVS<Event> var_local_9_local_8_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_21);
            
            PrimitiveVS<Integer> temp_var_87;
            temp_var_87 = (PrimitiveVS<Integer>)((var_resp.restrict(pc_21)).getField("transId"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_21, temp_var_87);
            
            PrimitiveVS<Boolean> temp_var_88;
            temp_var_88 = var_currTransId.restrict(pc_21).symbolicEquals(var_$tmp0.restrict(pc_21), pc_21);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_21, temp_var_88);
            
            PrimitiveVS<Boolean> temp_var_89 = var_$tmp1.restrict(pc_21);
            Guard pc_22 = BooleanVS.getTrueGuard(temp_var_89);
            Guard pc_23 = BooleanVS.getFalseGuard(temp_var_89);
            boolean jumpedOut_8 = false;
            boolean jumpedOut_9 = false;
            if (!pc_22.isFalse()) {
                // 'then' branch
                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_90;
                temp_var_90 = (PrimitiveVS<Integer> /* enum tTransStatus */)((var_resp.restrict(pc_22)).getField("status"));
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_22, temp_var_90);
                
                PrimitiveVS<Boolean> temp_var_91;
                temp_var_91 = var_$tmp2.restrict(pc_22).symbolicEquals(new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_22), pc_22);
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_22, temp_var_91);
                
                PrimitiveVS<Boolean> temp_var_92 = var_$tmp3.restrict(pc_22);
                Guard pc_24 = BooleanVS.getTrueGuard(temp_var_92);
                Guard pc_25 = BooleanVS.getFalseGuard(temp_var_92);
                boolean jumpedOut_10 = false;
                boolean jumpedOut_11 = false;
                if (!pc_24.isFalse()) {
                    // 'then' branch
                    PrimitiveVS<Integer> temp_var_93;
                    temp_var_93 = (var_countPrepareResponses.restrict(pc_24)).apply(new PrimitiveVS<Integer>(1).restrict(pc_24), (temp_var_94, temp_var_95) -> temp_var_94 + temp_var_95);
                    var_$tmp4 = var_$tmp4.updateUnderGuard(pc_24, temp_var_93);
                    
                    PrimitiveVS<Integer> temp_var_96;
                    temp_var_96 = var_$tmp4.restrict(pc_24);
                    var_countPrepareResponses = var_countPrepareResponses.updateUnderGuard(pc_24, temp_var_96);
                    
                    PrimitiveVS<Integer> temp_var_97;
                    temp_var_97 = var_participants.restrict(pc_24).size();
                    var_$tmp5 = var_$tmp5.updateUnderGuard(pc_24, temp_var_97);
                    
                    PrimitiveVS<Boolean> temp_var_98;
                    temp_var_98 = var_countPrepareResponses.restrict(pc_24).symbolicEquals(var_$tmp5.restrict(pc_24), pc_24);
                    var_$tmp6 = var_$tmp6.updateUnderGuard(pc_24, temp_var_98);
                    
                    PrimitiveVS<Boolean> temp_var_99 = var_$tmp6.restrict(pc_24);
                    Guard pc_26 = BooleanVS.getTrueGuard(temp_var_99);
                    Guard pc_27 = BooleanVS.getFalseGuard(temp_var_99);
                    boolean jumpedOut_12 = false;
                    boolean jumpedOut_13 = false;
                    if (!pc_26.isFalse()) {
                        // 'then' branch
                        PrimitiveVS<Event> temp_var_100;
                        temp_var_100 = new PrimitiveVS<Event>(eCommitTrans).restrict(pc_26);
                        var_local_6_$tmp0 = var_local_6_$tmp0.updateUnderGuard(pc_26, temp_var_100);
                        
                        PrimitiveVS<Integer> temp_var_101;
                        temp_var_101 = var_currTransId.restrict(pc_26);
                        var_local_6_$tmp1 = var_local_6_$tmp1.updateUnderGuard(pc_26, temp_var_101);
                        
                        PrimitiveVS<Event> temp_var_102;
                        temp_var_102 = var_local_6_$tmp0.restrict(pc_26);
                        var_local_6_inline_4_message = var_local_6_inline_4_message.updateUnderGuard(pc_26, temp_var_102);
                        
                        PrimitiveVS<Integer> temp_var_103;
                        temp_var_103 = var_local_6_$tmp1.restrict(pc_26);
                        var_local_6_inline_4_payload = var_local_6_inline_4_payload.updateUnderGuard(pc_26, temp_var_103);
                        
                        PrimitiveVS<Integer> temp_var_104;
                        temp_var_104 = new PrimitiveVS<Integer>(0).restrict(pc_26);
                        var_local_6_local_4_i = var_local_6_local_4_i.updateUnderGuard(pc_26, temp_var_104);
                        
                        java.util.List<Guard> loop_exits_1 = new java.util.ArrayList<>();
                        boolean loop_early_ret_1 = false;
                        Guard pc_28 = pc_26;
                        while (!pc_28.isFalse()) {
                            PrimitiveVS<Integer> temp_var_105;
                            temp_var_105 = var_participants.restrict(pc_28).size();
                            var_local_6_local_4_$tmp0 = var_local_6_local_4_$tmp0.updateUnderGuard(pc_28, temp_var_105);
                            
                            PrimitiveVS<Boolean> temp_var_106;
                            temp_var_106 = (var_local_6_local_4_i.restrict(pc_28)).apply(var_local_6_local_4_$tmp0.restrict(pc_28), (temp_var_107, temp_var_108) -> temp_var_107 < temp_var_108);
                            var_local_6_local_4_$tmp1 = var_local_6_local_4_$tmp1.updateUnderGuard(pc_28, temp_var_106);
                            
                            PrimitiveVS<Boolean> temp_var_109;
                            temp_var_109 = var_local_6_local_4_$tmp1.restrict(pc_28);
                            var_local_6_local_4_$tmp2 = var_local_6_local_4_$tmp2.updateUnderGuard(pc_28, temp_var_109);
                            
                            PrimitiveVS<Boolean> temp_var_110 = var_local_6_local_4_$tmp2.restrict(pc_28);
                            Guard pc_29 = BooleanVS.getTrueGuard(temp_var_110);
                            Guard pc_30 = BooleanVS.getFalseGuard(temp_var_110);
                            boolean jumpedOut_14 = false;
                            boolean jumpedOut_15 = false;
                            if (!pc_29.isFalse()) {
                                // 'then' branch
                            }
                            if (!pc_30.isFalse()) {
                                // 'else' branch
                                loop_exits_1.add(pc_30);
                                jumpedOut_15 = true;
                                pc_30 = Guard.constFalse();
                                
                            }
                            if (jumpedOut_14 || jumpedOut_15) {
                                pc_28 = pc_29.or(pc_30);
                            }
                            
                            if (!pc_28.isFalse()) {
                                PrimitiveVS<Machine> temp_var_111;
                                temp_var_111 = var_participants.restrict(pc_28).get(var_local_6_local_4_i.restrict(pc_28));
                                var_local_6_local_4_$tmp3 = var_local_6_local_4_$tmp3.updateUnderGuard(pc_28, temp_var_111);
                                
                                PrimitiveVS<Machine> temp_var_112;
                                temp_var_112 = var_local_6_local_4_$tmp3.restrict(pc_28);
                                var_local_6_local_4_$tmp4 = var_local_6_local_4_$tmp4.updateUnderGuard(pc_28, temp_var_112);
                                
                                PrimitiveVS<Event> temp_var_113;
                                temp_var_113 = var_local_6_inline_4_message.restrict(pc_28);
                                var_local_6_local_4_$tmp5 = var_local_6_local_4_$tmp5.updateUnderGuard(pc_28, temp_var_113);
                                
                                UnionVS temp_var_114;
                                temp_var_114 = ValueSummary.castToAny(pc_28, var_local_6_inline_4_payload.restrict(pc_28));
                                var_local_6_local_4_$tmp6 = var_local_6_local_4_$tmp6.updateUnderGuard(pc_28, temp_var_114);
                                
                                effects.send(pc_28, var_local_6_local_4_$tmp4.restrict(pc_28), var_local_6_local_4_$tmp5.restrict(pc_28), new UnionVS(var_local_6_local_4_$tmp6.restrict(pc_28)));
                                
                                PrimitiveVS<Integer> temp_var_115;
                                temp_var_115 = (var_local_6_local_4_i.restrict(pc_28)).apply(new PrimitiveVS<Integer>(1).restrict(pc_28), (temp_var_116, temp_var_117) -> temp_var_116 + temp_var_117);
                                var_local_6_local_4_$tmp7 = var_local_6_local_4_$tmp7.updateUnderGuard(pc_28, temp_var_115);
                                
                                PrimitiveVS<Integer> temp_var_118;
                                temp_var_118 = var_local_6_local_4_$tmp7.restrict(pc_28);
                                var_local_6_local_4_i = var_local_6_local_4_i.updateUnderGuard(pc_28, temp_var_118);
                                
                            }
                        }
                        if (loop_early_ret_1) {
                            pc_26 = Guard.orMany(loop_exits_1);
                            jumpedOut_12 = true;
                        }
                        
                        PrimitiveVS<Machine> temp_var_119;
                        temp_var_119 = (PrimitiveVS<Machine>)((var_pendingWrTrans.restrict(pc_26)).getField("client"));
                        var_local_6_$tmp2 = var_local_6_$tmp2.updateUnderGuard(pc_26, temp_var_119);
                        
                        PrimitiveVS<Machine> temp_var_120;
                        temp_var_120 = var_local_6_$tmp2.restrict(pc_26);
                        var_local_6_$tmp3 = var_local_6_$tmp3.updateUnderGuard(pc_26, temp_var_120);
                        
                        PrimitiveVS<Event> temp_var_121;
                        temp_var_121 = new PrimitiveVS<Event>(eWriteTransResp).restrict(pc_26);
                        var_local_6_$tmp4 = var_local_6_$tmp4.updateUnderGuard(pc_26, temp_var_121);
                        
                        PrimitiveVS<Integer> temp_var_122;
                        temp_var_122 = var_currTransId.restrict(pc_26);
                        var_local_6_$tmp5 = var_local_6_$tmp5.updateUnderGuard(pc_26, temp_var_122);
                        
                        PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_123;
                        temp_var_123 = new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_26);
                        var_local_6_$tmp6 = var_local_6_$tmp6.updateUnderGuard(pc_26, temp_var_123);
                        
                        NamedTupleVS temp_var_124;
                        temp_var_124 = new NamedTupleVS("transId", var_local_6_$tmp5.restrict(pc_26), "status", var_local_6_$tmp6.restrict(pc_26));
                        var_local_6_$tmp7 = var_local_6_$tmp7.updateUnderGuard(pc_26, temp_var_124);
                        
                        effects.send(pc_26, var_local_6_$tmp3.restrict(pc_26), var_local_6_$tmp4.restrict(pc_26), new UnionVS(var_local_6_$tmp7.restrict(pc_26)));
                        
                        PrimitiveVS<Machine> temp_var_125;
                        temp_var_125 = var_timer.restrict(pc_26);
                        var_local_6_$tmp8 = var_local_6_$tmp8.updateUnderGuard(pc_26, temp_var_125);
                        
                        PrimitiveVS<Machine> temp_var_126;
                        temp_var_126 = var_local_6_$tmp8.restrict(pc_26);
                        var_local_6_inline_5_timer = var_local_6_inline_5_timer.updateUnderGuard(pc_26, temp_var_126);
                        
                        PrimitiveVS<Machine> temp_var_127;
                        temp_var_127 = var_local_6_inline_5_timer.restrict(pc_26);
                        var_local_6_local_5_$tmp0 = var_local_6_local_5_$tmp0.updateUnderGuard(pc_26, temp_var_127);
                        
                        PrimitiveVS<Event> temp_var_128;
                        temp_var_128 = new PrimitiveVS<Event>(eCancelTimer).restrict(pc_26);
                        var_local_6_local_5_$tmp1 = var_local_6_local_5_$tmp1.updateUnderGuard(pc_26, temp_var_128);
                        
                        effects.send(pc_26, var_local_6_local_5_$tmp0.restrict(pc_26), var_local_6_local_5_$tmp1.restrict(pc_26), null);
                        
                        outcome.addGuardedGoto(pc_26, WaitForTransactions);
                        pc_26 = Guard.constFalse();
                        jumpedOut_12 = true;
                        
                    }
                    if (!pc_27.isFalse()) {
                        // 'else' branch
                    }
                    if (jumpedOut_12 || jumpedOut_13) {
                        pc_24 = pc_26.or(pc_27);
                        jumpedOut_10 = true;
                    }
                    
                    if (!pc_24.isFalse()) {
                    }
                }
                if (!pc_25.isFalse()) {
                    // 'else' branch
                    PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_129;
                    temp_var_129 = new PrimitiveVS<Integer>(1 /* enum tTransStatus elem ERROR */).restrict(pc_25);
                    var_$tmp7 = var_$tmp7.updateUnderGuard(pc_25, temp_var_129);
                    
                    PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_130;
                    temp_var_130 = var_$tmp7.restrict(pc_25);
                    var_inline_9_respStatus = var_inline_9_respStatus.updateUnderGuard(pc_25, temp_var_130);
                    
                    PrimitiveVS<Event> temp_var_131;
                    temp_var_131 = new PrimitiveVS<Event>(eAbortTrans).restrict(pc_25);
                    var_local_9_$tmp0 = var_local_9_$tmp0.updateUnderGuard(pc_25, temp_var_131);
                    
                    PrimitiveVS<Integer> temp_var_132;
                    temp_var_132 = var_currTransId.restrict(pc_25);
                    var_local_9_$tmp1 = var_local_9_$tmp1.updateUnderGuard(pc_25, temp_var_132);
                    
                    PrimitiveVS<Event> temp_var_133;
                    temp_var_133 = var_local_9_$tmp0.restrict(pc_25);
                    var_local_9_inline_7_message = var_local_9_inline_7_message.updateUnderGuard(pc_25, temp_var_133);
                    
                    PrimitiveVS<Integer> temp_var_134;
                    temp_var_134 = var_local_9_$tmp1.restrict(pc_25);
                    var_local_9_inline_7_payload = var_local_9_inline_7_payload.updateUnderGuard(pc_25, temp_var_134);
                    
                    PrimitiveVS<Integer> temp_var_135;
                    temp_var_135 = new PrimitiveVS<Integer>(0).restrict(pc_25);
                    var_local_9_local_7_i = var_local_9_local_7_i.updateUnderGuard(pc_25, temp_var_135);
                    
                    java.util.List<Guard> loop_exits_2 = new java.util.ArrayList<>();
                    boolean loop_early_ret_2 = false;
                    Guard pc_31 = pc_25;
                    while (!pc_31.isFalse()) {
                        PrimitiveVS<Integer> temp_var_136;
                        temp_var_136 = var_participants.restrict(pc_31).size();
                        var_local_9_local_7_$tmp0 = var_local_9_local_7_$tmp0.updateUnderGuard(pc_31, temp_var_136);
                        
                        PrimitiveVS<Boolean> temp_var_137;
                        temp_var_137 = (var_local_9_local_7_i.restrict(pc_31)).apply(var_local_9_local_7_$tmp0.restrict(pc_31), (temp_var_138, temp_var_139) -> temp_var_138 < temp_var_139);
                        var_local_9_local_7_$tmp1 = var_local_9_local_7_$tmp1.updateUnderGuard(pc_31, temp_var_137);
                        
                        PrimitiveVS<Boolean> temp_var_140;
                        temp_var_140 = var_local_9_local_7_$tmp1.restrict(pc_31);
                        var_local_9_local_7_$tmp2 = var_local_9_local_7_$tmp2.updateUnderGuard(pc_31, temp_var_140);
                        
                        PrimitiveVS<Boolean> temp_var_141 = var_local_9_local_7_$tmp2.restrict(pc_31);
                        Guard pc_32 = BooleanVS.getTrueGuard(temp_var_141);
                        Guard pc_33 = BooleanVS.getFalseGuard(temp_var_141);
                        boolean jumpedOut_16 = false;
                        boolean jumpedOut_17 = false;
                        if (!pc_32.isFalse()) {
                            // 'then' branch
                        }
                        if (!pc_33.isFalse()) {
                            // 'else' branch
                            loop_exits_2.add(pc_33);
                            jumpedOut_17 = true;
                            pc_33 = Guard.constFalse();
                            
                        }
                        if (jumpedOut_16 || jumpedOut_17) {
                            pc_31 = pc_32.or(pc_33);
                        }
                        
                        if (!pc_31.isFalse()) {
                            PrimitiveVS<Machine> temp_var_142;
                            temp_var_142 = var_participants.restrict(pc_31).get(var_local_9_local_7_i.restrict(pc_31));
                            var_local_9_local_7_$tmp3 = var_local_9_local_7_$tmp3.updateUnderGuard(pc_31, temp_var_142);
                            
                            PrimitiveVS<Machine> temp_var_143;
                            temp_var_143 = var_local_9_local_7_$tmp3.restrict(pc_31);
                            var_local_9_local_7_$tmp4 = var_local_9_local_7_$tmp4.updateUnderGuard(pc_31, temp_var_143);
                            
                            PrimitiveVS<Event> temp_var_144;
                            temp_var_144 = var_local_9_inline_7_message.restrict(pc_31);
                            var_local_9_local_7_$tmp5 = var_local_9_local_7_$tmp5.updateUnderGuard(pc_31, temp_var_144);
                            
                            UnionVS temp_var_145;
                            temp_var_145 = ValueSummary.castToAny(pc_31, var_local_9_inline_7_payload.restrict(pc_31));
                            var_local_9_local_7_$tmp6 = var_local_9_local_7_$tmp6.updateUnderGuard(pc_31, temp_var_145);
                            
                            effects.send(pc_31, var_local_9_local_7_$tmp4.restrict(pc_31), var_local_9_local_7_$tmp5.restrict(pc_31), new UnionVS(var_local_9_local_7_$tmp6.restrict(pc_31)));
                            
                            PrimitiveVS<Integer> temp_var_146;
                            temp_var_146 = (var_local_9_local_7_i.restrict(pc_31)).apply(new PrimitiveVS<Integer>(1).restrict(pc_31), (temp_var_147, temp_var_148) -> temp_var_147 + temp_var_148);
                            var_local_9_local_7_$tmp7 = var_local_9_local_7_$tmp7.updateUnderGuard(pc_31, temp_var_146);
                            
                            PrimitiveVS<Integer> temp_var_149;
                            temp_var_149 = var_local_9_local_7_$tmp7.restrict(pc_31);
                            var_local_9_local_7_i = var_local_9_local_7_i.updateUnderGuard(pc_31, temp_var_149);
                            
                        }
                    }
                    if (loop_early_ret_2) {
                        pc_25 = Guard.orMany(loop_exits_2);
                        jumpedOut_11 = true;
                    }
                    
                    PrimitiveVS<Machine> temp_var_150;
                    temp_var_150 = (PrimitiveVS<Machine>)((var_pendingWrTrans.restrict(pc_25)).getField("client"));
                    var_local_9_$tmp2 = var_local_9_$tmp2.updateUnderGuard(pc_25, temp_var_150);
                    
                    PrimitiveVS<Machine> temp_var_151;
                    temp_var_151 = var_local_9_$tmp2.restrict(pc_25);
                    var_local_9_$tmp3 = var_local_9_$tmp3.updateUnderGuard(pc_25, temp_var_151);
                    
                    PrimitiveVS<Event> temp_var_152;
                    temp_var_152 = new PrimitiveVS<Event>(eWriteTransResp).restrict(pc_25);
                    var_local_9_$tmp4 = var_local_9_$tmp4.updateUnderGuard(pc_25, temp_var_152);
                    
                    PrimitiveVS<Integer> temp_var_153;
                    temp_var_153 = var_currTransId.restrict(pc_25);
                    var_local_9_$tmp5 = var_local_9_$tmp5.updateUnderGuard(pc_25, temp_var_153);
                    
                    PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_154;
                    temp_var_154 = var_inline_9_respStatus.restrict(pc_25);
                    var_local_9_$tmp6 = var_local_9_$tmp6.updateUnderGuard(pc_25, temp_var_154);
                    
                    NamedTupleVS temp_var_155;
                    temp_var_155 = new NamedTupleVS("transId", var_local_9_$tmp5.restrict(pc_25), "status", var_local_9_$tmp6.restrict(pc_25));
                    var_local_9_$tmp7 = var_local_9_$tmp7.updateUnderGuard(pc_25, temp_var_155);
                    
                    effects.send(pc_25, var_local_9_$tmp3.restrict(pc_25), var_local_9_$tmp4.restrict(pc_25), new UnionVS(var_local_9_$tmp7.restrict(pc_25)));
                    
                    PrimitiveVS<Machine> temp_var_156;
                    temp_var_156 = var_timer.restrict(pc_25);
                    var_local_9_$tmp8 = var_local_9_$tmp8.updateUnderGuard(pc_25, temp_var_156);
                    
                    PrimitiveVS<Machine> temp_var_157;
                    temp_var_157 = var_local_9_$tmp8.restrict(pc_25);
                    var_local_9_inline_8_timer = var_local_9_inline_8_timer.updateUnderGuard(pc_25, temp_var_157);
                    
                    PrimitiveVS<Machine> temp_var_158;
                    temp_var_158 = var_local_9_inline_8_timer.restrict(pc_25);
                    var_local_9_local_8_$tmp0 = var_local_9_local_8_$tmp0.updateUnderGuard(pc_25, temp_var_158);
                    
                    PrimitiveVS<Event> temp_var_159;
                    temp_var_159 = new PrimitiveVS<Event>(eCancelTimer).restrict(pc_25);
                    var_local_9_local_8_$tmp1 = var_local_9_local_8_$tmp1.updateUnderGuard(pc_25, temp_var_159);
                    
                    effects.send(pc_25, var_local_9_local_8_$tmp0.restrict(pc_25), var_local_9_local_8_$tmp1.restrict(pc_25), null);
                    
                    outcome.addGuardedGoto(pc_25, WaitForTransactions);
                    pc_25 = Guard.constFalse();
                    jumpedOut_11 = true;
                    
                }
                if (jumpedOut_10 || jumpedOut_11) {
                    pc_22 = pc_24.or(pc_25);
                    jumpedOut_8 = true;
                }
                
                if (!pc_22.isFalse()) {
                }
            }
            if (!pc_23.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_8 || jumpedOut_9) {
                pc_21 = pc_22.or(pc_23);
            }
            
            if (!pc_21.isFalse()) {
            }
            return pc_21;
        }
        
        void 
        anonfun_10(
            Guard pc_34,
            EventBuffer effects
        ) {
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp0 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_34);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_inline_10_respStatus =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_34);
            
            PrimitiveVS<Event> var_local_10_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_34);
            
            PrimitiveVS<Integer> var_local_10_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_34);
            
            PrimitiveVS<Machine> var_local_10_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_34);
            
            PrimitiveVS<Machine> var_local_10_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_34);
            
            PrimitiveVS<Event> var_local_10_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_34);
            
            PrimitiveVS<Integer> var_local_10_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_34);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_local_10_$tmp6 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_34);
            
            NamedTupleVS var_local_10_$tmp7 =
                new NamedTupleVS("transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_34);
            
            PrimitiveVS<Machine> var_local_10_$tmp8 =
                new PrimitiveVS<Machine>().restrict(pc_34);
            
            PrimitiveVS<Event> var_local_10_inline_7_message =
                new PrimitiveVS<Event>(_null).restrict(pc_34);
            
            PrimitiveVS<Integer> var_local_10_inline_7_payload =
                new PrimitiveVS<Integer>(0).restrict(pc_34);
            
            PrimitiveVS<Integer> var_local_10_local_7_i =
                new PrimitiveVS<Integer>(0).restrict(pc_34);
            
            PrimitiveVS<Integer> var_local_10_local_7_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_34);
            
            PrimitiveVS<Boolean> var_local_10_local_7_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_34);
            
            PrimitiveVS<Boolean> var_local_10_local_7_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_34);
            
            PrimitiveVS<Machine> var_local_10_local_7_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_34);
            
            PrimitiveVS<Machine> var_local_10_local_7_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_34);
            
            PrimitiveVS<Event> var_local_10_local_7_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_34);
            
            UnionVS var_local_10_local_7_$tmp6 =
                new UnionVS().restrict(pc_34);
            
            PrimitiveVS<Integer> var_local_10_local_7_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_34);
            
            PrimitiveVS<Machine> var_local_10_inline_8_timer =
                new PrimitiveVS<Machine>().restrict(pc_34);
            
            PrimitiveVS<Machine> var_local_10_local_8_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_34);
            
            PrimitiveVS<Event> var_local_10_local_8_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_34);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_160;
            temp_var_160 = new PrimitiveVS<Integer>(2 /* enum tTransStatus elem TIMEOUT */).restrict(pc_34);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_34, temp_var_160);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_161;
            temp_var_161 = var_$tmp0.restrict(pc_34);
            var_inline_10_respStatus = var_inline_10_respStatus.updateUnderGuard(pc_34, temp_var_161);
            
            PrimitiveVS<Event> temp_var_162;
            temp_var_162 = new PrimitiveVS<Event>(eAbortTrans).restrict(pc_34);
            var_local_10_$tmp0 = var_local_10_$tmp0.updateUnderGuard(pc_34, temp_var_162);
            
            PrimitiveVS<Integer> temp_var_163;
            temp_var_163 = var_currTransId.restrict(pc_34);
            var_local_10_$tmp1 = var_local_10_$tmp1.updateUnderGuard(pc_34, temp_var_163);
            
            PrimitiveVS<Event> temp_var_164;
            temp_var_164 = var_local_10_$tmp0.restrict(pc_34);
            var_local_10_inline_7_message = var_local_10_inline_7_message.updateUnderGuard(pc_34, temp_var_164);
            
            PrimitiveVS<Integer> temp_var_165;
            temp_var_165 = var_local_10_$tmp1.restrict(pc_34);
            var_local_10_inline_7_payload = var_local_10_inline_7_payload.updateUnderGuard(pc_34, temp_var_165);
            
            PrimitiveVS<Integer> temp_var_166;
            temp_var_166 = new PrimitiveVS<Integer>(0).restrict(pc_34);
            var_local_10_local_7_i = var_local_10_local_7_i.updateUnderGuard(pc_34, temp_var_166);
            
            java.util.List<Guard> loop_exits_3 = new java.util.ArrayList<>();
            boolean loop_early_ret_3 = false;
            Guard pc_35 = pc_34;
            while (!pc_35.isFalse()) {
                PrimitiveVS<Integer> temp_var_167;
                temp_var_167 = var_participants.restrict(pc_35).size();
                var_local_10_local_7_$tmp0 = var_local_10_local_7_$tmp0.updateUnderGuard(pc_35, temp_var_167);
                
                PrimitiveVS<Boolean> temp_var_168;
                temp_var_168 = (var_local_10_local_7_i.restrict(pc_35)).apply(var_local_10_local_7_$tmp0.restrict(pc_35), (temp_var_169, temp_var_170) -> temp_var_169 < temp_var_170);
                var_local_10_local_7_$tmp1 = var_local_10_local_7_$tmp1.updateUnderGuard(pc_35, temp_var_168);
                
                PrimitiveVS<Boolean> temp_var_171;
                temp_var_171 = var_local_10_local_7_$tmp1.restrict(pc_35);
                var_local_10_local_7_$tmp2 = var_local_10_local_7_$tmp2.updateUnderGuard(pc_35, temp_var_171);
                
                PrimitiveVS<Boolean> temp_var_172 = var_local_10_local_7_$tmp2.restrict(pc_35);
                Guard pc_36 = BooleanVS.getTrueGuard(temp_var_172);
                Guard pc_37 = BooleanVS.getFalseGuard(temp_var_172);
                boolean jumpedOut_18 = false;
                boolean jumpedOut_19 = false;
                if (!pc_36.isFalse()) {
                    // 'then' branch
                }
                if (!pc_37.isFalse()) {
                    // 'else' branch
                    loop_exits_3.add(pc_37);
                    jumpedOut_19 = true;
                    pc_37 = Guard.constFalse();
                    
                }
                if (jumpedOut_18 || jumpedOut_19) {
                    pc_35 = pc_36.or(pc_37);
                }
                
                if (!pc_35.isFalse()) {
                    PrimitiveVS<Machine> temp_var_173;
                    temp_var_173 = var_participants.restrict(pc_35).get(var_local_10_local_7_i.restrict(pc_35));
                    var_local_10_local_7_$tmp3 = var_local_10_local_7_$tmp3.updateUnderGuard(pc_35, temp_var_173);
                    
                    PrimitiveVS<Machine> temp_var_174;
                    temp_var_174 = var_local_10_local_7_$tmp3.restrict(pc_35);
                    var_local_10_local_7_$tmp4 = var_local_10_local_7_$tmp4.updateUnderGuard(pc_35, temp_var_174);
                    
                    PrimitiveVS<Event> temp_var_175;
                    temp_var_175 = var_local_10_inline_7_message.restrict(pc_35);
                    var_local_10_local_7_$tmp5 = var_local_10_local_7_$tmp5.updateUnderGuard(pc_35, temp_var_175);
                    
                    UnionVS temp_var_176;
                    temp_var_176 = ValueSummary.castToAny(pc_35, var_local_10_inline_7_payload.restrict(pc_35));
                    var_local_10_local_7_$tmp6 = var_local_10_local_7_$tmp6.updateUnderGuard(pc_35, temp_var_176);
                    
                    effects.send(pc_35, var_local_10_local_7_$tmp4.restrict(pc_35), var_local_10_local_7_$tmp5.restrict(pc_35), new UnionVS(var_local_10_local_7_$tmp6.restrict(pc_35)));
                    
                    PrimitiveVS<Integer> temp_var_177;
                    temp_var_177 = (var_local_10_local_7_i.restrict(pc_35)).apply(new PrimitiveVS<Integer>(1).restrict(pc_35), (temp_var_178, temp_var_179) -> temp_var_178 + temp_var_179);
                    var_local_10_local_7_$tmp7 = var_local_10_local_7_$tmp7.updateUnderGuard(pc_35, temp_var_177);
                    
                    PrimitiveVS<Integer> temp_var_180;
                    temp_var_180 = var_local_10_local_7_$tmp7.restrict(pc_35);
                    var_local_10_local_7_i = var_local_10_local_7_i.updateUnderGuard(pc_35, temp_var_180);
                    
                }
            }
            if (loop_early_ret_3) {
                pc_34 = Guard.orMany(loop_exits_3);
            }
            
            PrimitiveVS<Machine> temp_var_181;
            temp_var_181 = (PrimitiveVS<Machine>)((var_pendingWrTrans.restrict(pc_34)).getField("client"));
            var_local_10_$tmp2 = var_local_10_$tmp2.updateUnderGuard(pc_34, temp_var_181);
            
            PrimitiveVS<Machine> temp_var_182;
            temp_var_182 = var_local_10_$tmp2.restrict(pc_34);
            var_local_10_$tmp3 = var_local_10_$tmp3.updateUnderGuard(pc_34, temp_var_182);
            
            PrimitiveVS<Event> temp_var_183;
            temp_var_183 = new PrimitiveVS<Event>(eWriteTransResp).restrict(pc_34);
            var_local_10_$tmp4 = var_local_10_$tmp4.updateUnderGuard(pc_34, temp_var_183);
            
            PrimitiveVS<Integer> temp_var_184;
            temp_var_184 = var_currTransId.restrict(pc_34);
            var_local_10_$tmp5 = var_local_10_$tmp5.updateUnderGuard(pc_34, temp_var_184);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_185;
            temp_var_185 = var_inline_10_respStatus.restrict(pc_34);
            var_local_10_$tmp6 = var_local_10_$tmp6.updateUnderGuard(pc_34, temp_var_185);
            
            NamedTupleVS temp_var_186;
            temp_var_186 = new NamedTupleVS("transId", var_local_10_$tmp5.restrict(pc_34), "status", var_local_10_$tmp6.restrict(pc_34));
            var_local_10_$tmp7 = var_local_10_$tmp7.updateUnderGuard(pc_34, temp_var_186);
            
            effects.send(pc_34, var_local_10_$tmp3.restrict(pc_34), var_local_10_$tmp4.restrict(pc_34), new UnionVS(var_local_10_$tmp7.restrict(pc_34)));
            
            PrimitiveVS<Machine> temp_var_187;
            temp_var_187 = var_timer.restrict(pc_34);
            var_local_10_$tmp8 = var_local_10_$tmp8.updateUnderGuard(pc_34, temp_var_187);
            
            PrimitiveVS<Machine> temp_var_188;
            temp_var_188 = var_local_10_$tmp8.restrict(pc_34);
            var_local_10_inline_8_timer = var_local_10_inline_8_timer.updateUnderGuard(pc_34, temp_var_188);
            
            PrimitiveVS<Machine> temp_var_189;
            temp_var_189 = var_local_10_inline_8_timer.restrict(pc_34);
            var_local_10_local_8_$tmp0 = var_local_10_local_8_$tmp0.updateUnderGuard(pc_34, temp_var_189);
            
            PrimitiveVS<Event> temp_var_190;
            temp_var_190 = new PrimitiveVS<Event>(eCancelTimer).restrict(pc_34);
            var_local_10_local_8_$tmp1 = var_local_10_local_8_$tmp1.updateUnderGuard(pc_34, temp_var_190);
            
            effects.send(pc_34, var_local_10_local_8_$tmp0.restrict(pc_34), var_local_10_local_8_$tmp1.restrict(pc_34), null);
            
        }
        
        void 
        anonfun_11(
            Guard pc_38,
            EventBuffer effects,
            NamedTupleVS var_rTrans
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_38);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_38);
            
            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_38);
            
            NamedTupleVS var_$tmp3 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PrimitiveVS<Integer>(0)).restrict(pc_38);
            
            PrimitiveVS<Machine> temp_var_191;
            temp_var_191 = (PrimitiveVS<Machine>) scheduler.getNextElement(var_participants.restrict(pc_38), pc_38);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_38, temp_var_191);
            
            PrimitiveVS<Machine> temp_var_192;
            temp_var_192 = var_$tmp0.restrict(pc_38);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_38, temp_var_192);
            
            PrimitiveVS<Event> temp_var_193;
            temp_var_193 = new PrimitiveVS<Event>(eReadTransReq).restrict(pc_38);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_38, temp_var_193);
            
            NamedTupleVS temp_var_194;
            temp_var_194 = var_rTrans.restrict(pc_38);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_38, temp_var_194);
            
            effects.send(pc_38, var_$tmp1.restrict(pc_38), var_$tmp2.restrict(pc_38), new UnionVS(var_$tmp3.restrict(pc_38)));
            
        }
        
        void 
        anonfun_6(
            Guard pc_39,
            EventBuffer effects
        ) {
            PrimitiveVS<Integer> temp_var_195;
            temp_var_195 = new PrimitiveVS<Integer>(0).restrict(pc_39);
            var_countPrepareResponses = var_countPrepareResponses.updateUnderGuard(pc_39, temp_var_195);
            
        }
        
        void 
        DoGlobalAbort(
            Guard pc_40,
            EventBuffer effects,
            PrimitiveVS<Integer> /* enum tTransStatus */ var_respStatus
        ) {
            PrimitiveVS<Event> var_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_40);
            
            PrimitiveVS<Integer> var_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_40);
            
            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_40);
            
            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_40);
            
            PrimitiveVS<Event> var_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_40);
            
            PrimitiveVS<Integer> var_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_40);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp6 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_40);
            
            NamedTupleVS var_$tmp7 =
                new NamedTupleVS("transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_40);
            
            PrimitiveVS<Machine> var_$tmp8 =
                new PrimitiveVS<Machine>().restrict(pc_40);
            
            PrimitiveVS<Event> var_inline_7_message =
                new PrimitiveVS<Event>(_null).restrict(pc_40);
            
            PrimitiveVS<Integer> var_inline_7_payload =
                new PrimitiveVS<Integer>(0).restrict(pc_40);
            
            PrimitiveVS<Integer> var_local_7_i =
                new PrimitiveVS<Integer>(0).restrict(pc_40);
            
            PrimitiveVS<Integer> var_local_7_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_40);
            
            PrimitiveVS<Boolean> var_local_7_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_40);
            
            PrimitiveVS<Boolean> var_local_7_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_40);
            
            PrimitiveVS<Machine> var_local_7_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_40);
            
            PrimitiveVS<Machine> var_local_7_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_40);
            
            PrimitiveVS<Event> var_local_7_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_40);
            
            UnionVS var_local_7_$tmp6 =
                new UnionVS().restrict(pc_40);
            
            PrimitiveVS<Integer> var_local_7_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_40);
            
            PrimitiveVS<Machine> var_inline_8_timer =
                new PrimitiveVS<Machine>().restrict(pc_40);
            
            PrimitiveVS<Machine> var_local_8_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_40);
            
            PrimitiveVS<Event> var_local_8_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_40);
            
            PrimitiveVS<Event> temp_var_196;
            temp_var_196 = new PrimitiveVS<Event>(eAbortTrans).restrict(pc_40);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_40, temp_var_196);
            
            PrimitiveVS<Integer> temp_var_197;
            temp_var_197 = var_currTransId.restrict(pc_40);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_40, temp_var_197);
            
            PrimitiveVS<Event> temp_var_198;
            temp_var_198 = var_$tmp0.restrict(pc_40);
            var_inline_7_message = var_inline_7_message.updateUnderGuard(pc_40, temp_var_198);
            
            PrimitiveVS<Integer> temp_var_199;
            temp_var_199 = var_$tmp1.restrict(pc_40);
            var_inline_7_payload = var_inline_7_payload.updateUnderGuard(pc_40, temp_var_199);
            
            PrimitiveVS<Integer> temp_var_200;
            temp_var_200 = new PrimitiveVS<Integer>(0).restrict(pc_40);
            var_local_7_i = var_local_7_i.updateUnderGuard(pc_40, temp_var_200);
            
            java.util.List<Guard> loop_exits_4 = new java.util.ArrayList<>();
            boolean loop_early_ret_4 = false;
            Guard pc_41 = pc_40;
            while (!pc_41.isFalse()) {
                PrimitiveVS<Integer> temp_var_201;
                temp_var_201 = var_participants.restrict(pc_41).size();
                var_local_7_$tmp0 = var_local_7_$tmp0.updateUnderGuard(pc_41, temp_var_201);
                
                PrimitiveVS<Boolean> temp_var_202;
                temp_var_202 = (var_local_7_i.restrict(pc_41)).apply(var_local_7_$tmp0.restrict(pc_41), (temp_var_203, temp_var_204) -> temp_var_203 < temp_var_204);
                var_local_7_$tmp1 = var_local_7_$tmp1.updateUnderGuard(pc_41, temp_var_202);
                
                PrimitiveVS<Boolean> temp_var_205;
                temp_var_205 = var_local_7_$tmp1.restrict(pc_41);
                var_local_7_$tmp2 = var_local_7_$tmp2.updateUnderGuard(pc_41, temp_var_205);
                
                PrimitiveVS<Boolean> temp_var_206 = var_local_7_$tmp2.restrict(pc_41);
                Guard pc_42 = BooleanVS.getTrueGuard(temp_var_206);
                Guard pc_43 = BooleanVS.getFalseGuard(temp_var_206);
                boolean jumpedOut_20 = false;
                boolean jumpedOut_21 = false;
                if (!pc_42.isFalse()) {
                    // 'then' branch
                }
                if (!pc_43.isFalse()) {
                    // 'else' branch
                    loop_exits_4.add(pc_43);
                    jumpedOut_21 = true;
                    pc_43 = Guard.constFalse();
                    
                }
                if (jumpedOut_20 || jumpedOut_21) {
                    pc_41 = pc_42.or(pc_43);
                }
                
                if (!pc_41.isFalse()) {
                    PrimitiveVS<Machine> temp_var_207;
                    temp_var_207 = var_participants.restrict(pc_41).get(var_local_7_i.restrict(pc_41));
                    var_local_7_$tmp3 = var_local_7_$tmp3.updateUnderGuard(pc_41, temp_var_207);
                    
                    PrimitiveVS<Machine> temp_var_208;
                    temp_var_208 = var_local_7_$tmp3.restrict(pc_41);
                    var_local_7_$tmp4 = var_local_7_$tmp4.updateUnderGuard(pc_41, temp_var_208);
                    
                    PrimitiveVS<Event> temp_var_209;
                    temp_var_209 = var_inline_7_message.restrict(pc_41);
                    var_local_7_$tmp5 = var_local_7_$tmp5.updateUnderGuard(pc_41, temp_var_209);
                    
                    UnionVS temp_var_210;
                    temp_var_210 = ValueSummary.castToAny(pc_41, var_inline_7_payload.restrict(pc_41));
                    var_local_7_$tmp6 = var_local_7_$tmp6.updateUnderGuard(pc_41, temp_var_210);
                    
                    effects.send(pc_41, var_local_7_$tmp4.restrict(pc_41), var_local_7_$tmp5.restrict(pc_41), new UnionVS(var_local_7_$tmp6.restrict(pc_41)));
                    
                    PrimitiveVS<Integer> temp_var_211;
                    temp_var_211 = (var_local_7_i.restrict(pc_41)).apply(new PrimitiveVS<Integer>(1).restrict(pc_41), (temp_var_212, temp_var_213) -> temp_var_212 + temp_var_213);
                    var_local_7_$tmp7 = var_local_7_$tmp7.updateUnderGuard(pc_41, temp_var_211);
                    
                    PrimitiveVS<Integer> temp_var_214;
                    temp_var_214 = var_local_7_$tmp7.restrict(pc_41);
                    var_local_7_i = var_local_7_i.updateUnderGuard(pc_41, temp_var_214);
                    
                }
            }
            if (loop_early_ret_4) {
                pc_40 = Guard.orMany(loop_exits_4);
            }
            
            PrimitiveVS<Machine> temp_var_215;
            temp_var_215 = (PrimitiveVS<Machine>)((var_pendingWrTrans.restrict(pc_40)).getField("client"));
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_40, temp_var_215);
            
            PrimitiveVS<Machine> temp_var_216;
            temp_var_216 = var_$tmp2.restrict(pc_40);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_40, temp_var_216);
            
            PrimitiveVS<Event> temp_var_217;
            temp_var_217 = new PrimitiveVS<Event>(eWriteTransResp).restrict(pc_40);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_40, temp_var_217);
            
            PrimitiveVS<Integer> temp_var_218;
            temp_var_218 = var_currTransId.restrict(pc_40);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_40, temp_var_218);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_219;
            temp_var_219 = var_respStatus.restrict(pc_40);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_40, temp_var_219);
            
            NamedTupleVS temp_var_220;
            temp_var_220 = new NamedTupleVS("transId", var_$tmp5.restrict(pc_40), "status", var_$tmp6.restrict(pc_40));
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_40, temp_var_220);
            
            effects.send(pc_40, var_$tmp3.restrict(pc_40), var_$tmp4.restrict(pc_40), new UnionVS(var_$tmp7.restrict(pc_40)));
            
            PrimitiveVS<Machine> temp_var_221;
            temp_var_221 = var_timer.restrict(pc_40);
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_40, temp_var_221);
            
            PrimitiveVS<Machine> temp_var_222;
            temp_var_222 = var_$tmp8.restrict(pc_40);
            var_inline_8_timer = var_inline_8_timer.updateUnderGuard(pc_40, temp_var_222);
            
            PrimitiveVS<Machine> temp_var_223;
            temp_var_223 = var_inline_8_timer.restrict(pc_40);
            var_local_8_$tmp0 = var_local_8_$tmp0.updateUnderGuard(pc_40, temp_var_223);
            
            PrimitiveVS<Event> temp_var_224;
            temp_var_224 = new PrimitiveVS<Event>(eCancelTimer).restrict(pc_40);
            var_local_8_$tmp1 = var_local_8_$tmp1.updateUnderGuard(pc_40, temp_var_224);
            
            effects.send(pc_40, var_local_8_$tmp0.restrict(pc_40), var_local_8_$tmp1.restrict(pc_40), null);
            
        }
        
        void 
        DoGlobalCommit(
            Guard pc_44,
            EventBuffer effects
        ) {
            PrimitiveVS<Event> var_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_44);
            
            PrimitiveVS<Integer> var_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_44);
            
            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_44);
            
            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_44);
            
            PrimitiveVS<Event> var_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_44);
            
            PrimitiveVS<Integer> var_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_44);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp6 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_44);
            
            NamedTupleVS var_$tmp7 =
                new NamedTupleVS("transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_44);
            
            PrimitiveVS<Machine> var_$tmp8 =
                new PrimitiveVS<Machine>().restrict(pc_44);
            
            PrimitiveVS<Event> var_inline_4_message =
                new PrimitiveVS<Event>(_null).restrict(pc_44);
            
            PrimitiveVS<Integer> var_inline_4_payload =
                new PrimitiveVS<Integer>(0).restrict(pc_44);
            
            PrimitiveVS<Integer> var_local_4_i =
                new PrimitiveVS<Integer>(0).restrict(pc_44);
            
            PrimitiveVS<Integer> var_local_4_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_44);
            
            PrimitiveVS<Boolean> var_local_4_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_44);
            
            PrimitiveVS<Boolean> var_local_4_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_44);
            
            PrimitiveVS<Machine> var_local_4_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_44);
            
            PrimitiveVS<Machine> var_local_4_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_44);
            
            PrimitiveVS<Event> var_local_4_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_44);
            
            UnionVS var_local_4_$tmp6 =
                new UnionVS().restrict(pc_44);
            
            PrimitiveVS<Integer> var_local_4_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_44);
            
            PrimitiveVS<Machine> var_inline_5_timer =
                new PrimitiveVS<Machine>().restrict(pc_44);
            
            PrimitiveVS<Machine> var_local_5_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_44);
            
            PrimitiveVS<Event> var_local_5_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_44);
            
            PrimitiveVS<Event> temp_var_225;
            temp_var_225 = new PrimitiveVS<Event>(eCommitTrans).restrict(pc_44);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_44, temp_var_225);
            
            PrimitiveVS<Integer> temp_var_226;
            temp_var_226 = var_currTransId.restrict(pc_44);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_44, temp_var_226);
            
            PrimitiveVS<Event> temp_var_227;
            temp_var_227 = var_$tmp0.restrict(pc_44);
            var_inline_4_message = var_inline_4_message.updateUnderGuard(pc_44, temp_var_227);
            
            PrimitiveVS<Integer> temp_var_228;
            temp_var_228 = var_$tmp1.restrict(pc_44);
            var_inline_4_payload = var_inline_4_payload.updateUnderGuard(pc_44, temp_var_228);
            
            PrimitiveVS<Integer> temp_var_229;
            temp_var_229 = new PrimitiveVS<Integer>(0).restrict(pc_44);
            var_local_4_i = var_local_4_i.updateUnderGuard(pc_44, temp_var_229);
            
            java.util.List<Guard> loop_exits_5 = new java.util.ArrayList<>();
            boolean loop_early_ret_5 = false;
            Guard pc_45 = pc_44;
            while (!pc_45.isFalse()) {
                PrimitiveVS<Integer> temp_var_230;
                temp_var_230 = var_participants.restrict(pc_45).size();
                var_local_4_$tmp0 = var_local_4_$tmp0.updateUnderGuard(pc_45, temp_var_230);
                
                PrimitiveVS<Boolean> temp_var_231;
                temp_var_231 = (var_local_4_i.restrict(pc_45)).apply(var_local_4_$tmp0.restrict(pc_45), (temp_var_232, temp_var_233) -> temp_var_232 < temp_var_233);
                var_local_4_$tmp1 = var_local_4_$tmp1.updateUnderGuard(pc_45, temp_var_231);
                
                PrimitiveVS<Boolean> temp_var_234;
                temp_var_234 = var_local_4_$tmp1.restrict(pc_45);
                var_local_4_$tmp2 = var_local_4_$tmp2.updateUnderGuard(pc_45, temp_var_234);
                
                PrimitiveVS<Boolean> temp_var_235 = var_local_4_$tmp2.restrict(pc_45);
                Guard pc_46 = BooleanVS.getTrueGuard(temp_var_235);
                Guard pc_47 = BooleanVS.getFalseGuard(temp_var_235);
                boolean jumpedOut_22 = false;
                boolean jumpedOut_23 = false;
                if (!pc_46.isFalse()) {
                    // 'then' branch
                }
                if (!pc_47.isFalse()) {
                    // 'else' branch
                    loop_exits_5.add(pc_47);
                    jumpedOut_23 = true;
                    pc_47 = Guard.constFalse();
                    
                }
                if (jumpedOut_22 || jumpedOut_23) {
                    pc_45 = pc_46.or(pc_47);
                }
                
                if (!pc_45.isFalse()) {
                    PrimitiveVS<Machine> temp_var_236;
                    temp_var_236 = var_participants.restrict(pc_45).get(var_local_4_i.restrict(pc_45));
                    var_local_4_$tmp3 = var_local_4_$tmp3.updateUnderGuard(pc_45, temp_var_236);
                    
                    PrimitiveVS<Machine> temp_var_237;
                    temp_var_237 = var_local_4_$tmp3.restrict(pc_45);
                    var_local_4_$tmp4 = var_local_4_$tmp4.updateUnderGuard(pc_45, temp_var_237);
                    
                    PrimitiveVS<Event> temp_var_238;
                    temp_var_238 = var_inline_4_message.restrict(pc_45);
                    var_local_4_$tmp5 = var_local_4_$tmp5.updateUnderGuard(pc_45, temp_var_238);
                    
                    UnionVS temp_var_239;
                    temp_var_239 = ValueSummary.castToAny(pc_45, var_inline_4_payload.restrict(pc_45));
                    var_local_4_$tmp6 = var_local_4_$tmp6.updateUnderGuard(pc_45, temp_var_239);
                    
                    effects.send(pc_45, var_local_4_$tmp4.restrict(pc_45), var_local_4_$tmp5.restrict(pc_45), new UnionVS(var_local_4_$tmp6.restrict(pc_45)));
                    
                    PrimitiveVS<Integer> temp_var_240;
                    temp_var_240 = (var_local_4_i.restrict(pc_45)).apply(new PrimitiveVS<Integer>(1).restrict(pc_45), (temp_var_241, temp_var_242) -> temp_var_241 + temp_var_242);
                    var_local_4_$tmp7 = var_local_4_$tmp7.updateUnderGuard(pc_45, temp_var_240);
                    
                    PrimitiveVS<Integer> temp_var_243;
                    temp_var_243 = var_local_4_$tmp7.restrict(pc_45);
                    var_local_4_i = var_local_4_i.updateUnderGuard(pc_45, temp_var_243);
                    
                }
            }
            if (loop_early_ret_5) {
                pc_44 = Guard.orMany(loop_exits_5);
            }
            
            PrimitiveVS<Machine> temp_var_244;
            temp_var_244 = (PrimitiveVS<Machine>)((var_pendingWrTrans.restrict(pc_44)).getField("client"));
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_44, temp_var_244);
            
            PrimitiveVS<Machine> temp_var_245;
            temp_var_245 = var_$tmp2.restrict(pc_44);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_44, temp_var_245);
            
            PrimitiveVS<Event> temp_var_246;
            temp_var_246 = new PrimitiveVS<Event>(eWriteTransResp).restrict(pc_44);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_44, temp_var_246);
            
            PrimitiveVS<Integer> temp_var_247;
            temp_var_247 = var_currTransId.restrict(pc_44);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_44, temp_var_247);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_248;
            temp_var_248 = new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_44);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_44, temp_var_248);
            
            NamedTupleVS temp_var_249;
            temp_var_249 = new NamedTupleVS("transId", var_$tmp5.restrict(pc_44), "status", var_$tmp6.restrict(pc_44));
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_44, temp_var_249);
            
            effects.send(pc_44, var_$tmp3.restrict(pc_44), var_$tmp4.restrict(pc_44), new UnionVS(var_$tmp7.restrict(pc_44)));
            
            PrimitiveVS<Machine> temp_var_250;
            temp_var_250 = var_timer.restrict(pc_44);
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_44, temp_var_250);
            
            PrimitiveVS<Machine> temp_var_251;
            temp_var_251 = var_$tmp8.restrict(pc_44);
            var_inline_5_timer = var_inline_5_timer.updateUnderGuard(pc_44, temp_var_251);
            
            PrimitiveVS<Machine> temp_var_252;
            temp_var_252 = var_inline_5_timer.restrict(pc_44);
            var_local_5_$tmp0 = var_local_5_$tmp0.updateUnderGuard(pc_44, temp_var_252);
            
            PrimitiveVS<Event> temp_var_253;
            temp_var_253 = new PrimitiveVS<Event>(eCancelTimer).restrict(pc_44);
            var_local_5_$tmp1 = var_local_5_$tmp1.updateUnderGuard(pc_44, temp_var_253);
            
            effects.send(pc_44, var_local_5_$tmp0.restrict(pc_44), var_local_5_$tmp1.restrict(pc_44), null);
            
        }
        
        void 
        BroadcastToAllParticipants(
            Guard pc_48,
            EventBuffer effects,
            PrimitiveVS<Event> var_message,
            UnionVS var_payload
        ) {
            PrimitiveVS<Integer> var_i =
                new PrimitiveVS<Integer>(0).restrict(pc_48);
            
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_48);
            
            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_48);
            
            PrimitiveVS<Boolean> var_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_48);
            
            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_48);
            
            PrimitiveVS<Machine> var_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_48);
            
            PrimitiveVS<Event> var_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_48);
            
            UnionVS var_$tmp6 =
                new UnionVS().restrict(pc_48);
            
            PrimitiveVS<Integer> var_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_48);
            
            PrimitiveVS<Integer> temp_var_254;
            temp_var_254 = new PrimitiveVS<Integer>(0).restrict(pc_48);
            var_i = var_i.updateUnderGuard(pc_48, temp_var_254);
            
            java.util.List<Guard> loop_exits_6 = new java.util.ArrayList<>();
            boolean loop_early_ret_6 = false;
            Guard pc_49 = pc_48;
            while (!pc_49.isFalse()) {
                PrimitiveVS<Integer> temp_var_255;
                temp_var_255 = var_participants.restrict(pc_49).size();
                var_$tmp0 = var_$tmp0.updateUnderGuard(pc_49, temp_var_255);
                
                PrimitiveVS<Boolean> temp_var_256;
                temp_var_256 = (var_i.restrict(pc_49)).apply(var_$tmp0.restrict(pc_49), (temp_var_257, temp_var_258) -> temp_var_257 < temp_var_258);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_49, temp_var_256);
                
                PrimitiveVS<Boolean> temp_var_259;
                temp_var_259 = var_$tmp1.restrict(pc_49);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_49, temp_var_259);
                
                PrimitiveVS<Boolean> temp_var_260 = var_$tmp2.restrict(pc_49);
                Guard pc_50 = BooleanVS.getTrueGuard(temp_var_260);
                Guard pc_51 = BooleanVS.getFalseGuard(temp_var_260);
                boolean jumpedOut_24 = false;
                boolean jumpedOut_25 = false;
                if (!pc_50.isFalse()) {
                    // 'then' branch
                }
                if (!pc_51.isFalse()) {
                    // 'else' branch
                    loop_exits_6.add(pc_51);
                    jumpedOut_25 = true;
                    pc_51 = Guard.constFalse();
                    
                }
                if (jumpedOut_24 || jumpedOut_25) {
                    pc_49 = pc_50.or(pc_51);
                }
                
                if (!pc_49.isFalse()) {
                    PrimitiveVS<Machine> temp_var_261;
                    temp_var_261 = var_participants.restrict(pc_49).get(var_i.restrict(pc_49));
                    var_$tmp3 = var_$tmp3.updateUnderGuard(pc_49, temp_var_261);
                    
                    PrimitiveVS<Machine> temp_var_262;
                    temp_var_262 = var_$tmp3.restrict(pc_49);
                    var_$tmp4 = var_$tmp4.updateUnderGuard(pc_49, temp_var_262);
                    
                    PrimitiveVS<Event> temp_var_263;
                    temp_var_263 = var_message.restrict(pc_49);
                    var_$tmp5 = var_$tmp5.updateUnderGuard(pc_49, temp_var_263);
                    
                    UnionVS temp_var_264;
                    temp_var_264 = ValueSummary.castToAny(pc_49, var_payload.restrict(pc_49));
                    var_$tmp6 = var_$tmp6.updateUnderGuard(pc_49, temp_var_264);
                    
                    effects.send(pc_49, var_$tmp4.restrict(pc_49), var_$tmp5.restrict(pc_49), new UnionVS(var_$tmp6.restrict(pc_49)));
                    
                    PrimitiveVS<Integer> temp_var_265;
                    temp_var_265 = (var_i.restrict(pc_49)).apply(new PrimitiveVS<Integer>(1).restrict(pc_49), (temp_var_266, temp_var_267) -> temp_var_266 + temp_var_267);
                    var_$tmp7 = var_$tmp7.updateUnderGuard(pc_49, temp_var_265);
                    
                    PrimitiveVS<Integer> temp_var_268;
                    temp_var_268 = var_$tmp7.restrict(pc_49);
                    var_i = var_i.updateUnderGuard(pc_49, temp_var_268);
                    
                }
            }
            if (loop_early_ret_6) {
                pc_48 = Guard.orMany(loop_exits_6);
            }
            
        }
        
    }
    
    public static class Participant extends Machine {
        
        static State Init = new State("Init") {
            @Override public void entry(Guard pc_52, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Participant)machine).anonfun_12(pc_52, machine.sendBuffer, outcome);
            }
        };
        static State WaitForRequests = new State("WaitForRequests") {
        };
        private MapVS<Integer, PrimitiveVS<Integer>> var_kvStore = new MapVS<Integer, PrimitiveVS<Integer>>(Guard.constTrue());
        private MapVS<Integer, NamedTupleVS> var_pendingWriteTrans = new MapVS<Integer, NamedTupleVS>(Guard.constTrue());
        
        @Override
        public void reset() {
                super.reset();
                var_kvStore = new MapVS<Integer, PrimitiveVS<Integer>>(Guard.constTrue());
                var_pendingWriteTrans = new MapVS<Integer, NamedTupleVS>(Guard.constTrue());
        }
        
        public Participant(int id) {
            super("Participant", id, EventBufferSemantics.queue, Init, Init
                , WaitForRequests
                
            );
            Init.addHandlers();
            WaitForRequests.addHandlers(new EventHandler(eAbortTrans) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Participant)machine).anonfun_13(pc, machine.sendBuffer, (PrimitiveVS<Integer>) ValueSummary.castFromAny(pc, new PrimitiveVS<Integer>(0), payload));
                    }
                },
                new EventHandler(eCommitTrans) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Participant)machine).anonfun_14(pc, machine.sendBuffer, (PrimitiveVS<Integer>) ValueSummary.castFromAny(pc, new PrimitiveVS<Integer>(0), payload));
                    }
                },
                new EventHandler(ePrepareReq) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Participant)machine).anonfun_15(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("coordinator", new PrimitiveVS<Machine>(), "transId", new PrimitiveVS<Integer>(0), "rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0))), payload));
                    }
                },
                new EventHandler(eReadTransReq) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Participant)machine).anonfun_16(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PrimitiveVS<Integer>(0)), payload));
                    }
                });
        }
        
        Guard 
        anonfun_12(
            Guard pc_53,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            outcome.addGuardedGoto(pc_53, WaitForRequests);
            pc_53 = Guard.constFalse();
            
            return pc_53;
        }
        
        void 
        anonfun_13(
            Guard pc_54,
            EventBuffer effects,
            PrimitiveVS<Integer> var_transId
        ) {
            PrimitiveVS<Boolean> var_$tmp0 =
                new PrimitiveVS<Boolean>(false).restrict(pc_54);
            
            PrimitiveVS<Integer> var_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_54);
            
            MapVS<Integer, NamedTupleVS> var_$tmp2 =
                new MapVS<Integer, NamedTupleVS>(Guard.constTrue()).restrict(pc_54);
            
            PrimitiveVS<String> var_$tmp3 =
                new PrimitiveVS<String>("").restrict(pc_54);
            
            PrimitiveVS<Boolean> temp_var_269;
            temp_var_269 = var_pendingWriteTrans.restrict(pc_54).containsKey(var_transId.restrict(pc_54));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_54, temp_var_269);
            
            PrimitiveVS<Integer> temp_var_270;
            temp_var_270 = var_transId.restrict(pc_54);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_54, temp_var_270);
            
            MapVS<Integer, NamedTupleVS> temp_var_271;
            temp_var_271 = var_pendingWriteTrans.restrict(pc_54);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_54, temp_var_271);
            
            PrimitiveVS<String> temp_var_272;
            temp_var_272 = new PrimitiveVS<String>(String.format("Abort request for a non-pending transaction, transId: {0}, pendingTrans: {1}", var_$tmp1.restrict(pc_54), var_$tmp2.restrict(pc_54))).restrict(pc_54);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_54, temp_var_272);
            
            Assert.progProp(!(var_$tmp0.restrict(pc_54)).getValues().contains(Boolean.FALSE), var_$tmp3.restrict(pc_54), scheduler, var_$tmp0.restrict(pc_54).getGuardFor(Boolean.FALSE));
            MapVS<Integer, NamedTupleVS> temp_var_273 = var_pendingWriteTrans.restrict(pc_54);    
            temp_var_273 = var_pendingWriteTrans.restrict(pc_54).remove(var_transId.restrict(pc_54));
            var_pendingWriteTrans = var_pendingWriteTrans.updateUnderGuard(pc_54, temp_var_273);
            
        }
        
        void 
        anonfun_14(
            Guard pc_55,
            EventBuffer effects,
            PrimitiveVS<Integer> var_transId
        ) {
            NamedTupleVS var_transaction =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_55);
            
            PrimitiveVS<Boolean> var_$tmp0 =
                new PrimitiveVS<Boolean>(false).restrict(pc_55);
            
            PrimitiveVS<Integer> var_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_55);
            
            MapVS<Integer, NamedTupleVS> var_$tmp2 =
                new MapVS<Integer, NamedTupleVS>(Guard.constTrue()).restrict(pc_55);
            
            PrimitiveVS<String> var_$tmp3 =
                new PrimitiveVS<String>("").restrict(pc_55);
            
            NamedTupleVS var_$tmp4 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_55);
            
            NamedTupleVS var_$tmp5 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_55);
            
            PrimitiveVS<Integer> var_$tmp6 =
                new PrimitiveVS<Integer>(0).restrict(pc_55);
            
            PrimitiveVS<Integer> var_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_55);
            
            PrimitiveVS<Integer> var_$tmp8 =
                new PrimitiveVS<Integer>(0).restrict(pc_55);
            
            PrimitiveVS<Boolean> temp_var_274;
            temp_var_274 = var_pendingWriteTrans.restrict(pc_55).containsKey(var_transId.restrict(pc_55));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_55, temp_var_274);
            
            PrimitiveVS<Integer> temp_var_275;
            temp_var_275 = var_transId.restrict(pc_55);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_55, temp_var_275);
            
            MapVS<Integer, NamedTupleVS> temp_var_276;
            temp_var_276 = var_pendingWriteTrans.restrict(pc_55);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_55, temp_var_276);
            
            PrimitiveVS<String> temp_var_277;
            temp_var_277 = new PrimitiveVS<String>(String.format("Commit request for a non-pending transaction, transId: {0}, pendingTrans: {1}", var_$tmp1.restrict(pc_55), var_$tmp2.restrict(pc_55))).restrict(pc_55);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_55, temp_var_277);
            
            Assert.progProp(!(var_$tmp0.restrict(pc_55)).getValues().contains(Boolean.FALSE), var_$tmp3.restrict(pc_55), scheduler, var_$tmp0.restrict(pc_55).getGuardFor(Boolean.FALSE));
            NamedTupleVS temp_var_278;
            temp_var_278 = var_pendingWriteTrans.restrict(pc_55).get(var_transId.restrict(pc_55));
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_55, temp_var_278);
            
            NamedTupleVS temp_var_279;
            temp_var_279 = var_$tmp4.restrict(pc_55);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_55, temp_var_279);
            
            NamedTupleVS temp_var_280;
            temp_var_280 = var_$tmp5.restrict(pc_55);
            var_transaction = var_transaction.updateUnderGuard(pc_55, temp_var_280);
            
            PrimitiveVS<Integer> temp_var_281;
            temp_var_281 = (PrimitiveVS<Integer>)((var_transaction.restrict(pc_55)).getField("key"));
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_55, temp_var_281);
            
            PrimitiveVS<Integer> temp_var_282;
            temp_var_282 = (PrimitiveVS<Integer>)((var_transaction.restrict(pc_55)).getField("val"));
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_55, temp_var_282);
            
            PrimitiveVS<Integer> temp_var_283;
            temp_var_283 = var_$tmp7.restrict(pc_55);
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_55, temp_var_283);
            
            MapVS<Integer, PrimitiveVS<Integer>> temp_var_284 = var_kvStore.restrict(pc_55);    
            PrimitiveVS<Integer> temp_var_286 = var_$tmp6.restrict(pc_55);
            PrimitiveVS<Integer> temp_var_285;
            temp_var_285 = var_$tmp8.restrict(pc_55);
            temp_var_284 = temp_var_284.put(temp_var_286, temp_var_285);
            var_kvStore = var_kvStore.updateUnderGuard(pc_55, temp_var_284);
            
        }
        
        void 
        anonfun_15(
            Guard pc_56,
            EventBuffer effects,
            NamedTupleVS var_prepareReq
        ) {
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_56);
            
            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_56);
            
            PrimitiveVS<Boolean> var_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_56);
            
            PrimitiveVS<Integer> var_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_56);
            
            MapVS<Integer, NamedTupleVS> var_$tmp4 =
                new MapVS<Integer, NamedTupleVS>(Guard.constTrue()).restrict(pc_56);
            
            PrimitiveVS<String> var_$tmp5 =
                new PrimitiveVS<String>("").restrict(pc_56);
            
            PrimitiveVS<Integer> var_$tmp6 =
                new PrimitiveVS<Integer>(0).restrict(pc_56);
            
            NamedTupleVS var_$tmp7 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_56);
            
            NamedTupleVS var_$tmp8 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_56);
            
            PrimitiveVS<Boolean> var_$tmp9 =
                new PrimitiveVS<Boolean>(false).restrict(pc_56);
            
            PrimitiveVS<Machine> var_$tmp10 =
                new PrimitiveVS<Machine>().restrict(pc_56);
            
            PrimitiveVS<Machine> var_$tmp11 =
                new PrimitiveVS<Machine>().restrict(pc_56);
            
            PrimitiveVS<Event> var_$tmp12 =
                new PrimitiveVS<Event>(_null).restrict(pc_56);
            
            PrimitiveVS<Machine> var_$tmp13 =
                new PrimitiveVS<Machine>().restrict(pc_56);
            
            PrimitiveVS<Integer> var_$tmp14 =
                new PrimitiveVS<Integer>(0).restrict(pc_56);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp15 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_56);
            
            NamedTupleVS var_$tmp16 =
                new NamedTupleVS("participant", new PrimitiveVS<Machine>(), "transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_56);
            
            PrimitiveVS<Machine> var_$tmp17 =
                new PrimitiveVS<Machine>().restrict(pc_56);
            
            PrimitiveVS<Machine> var_$tmp18 =
                new PrimitiveVS<Machine>().restrict(pc_56);
            
            PrimitiveVS<Event> var_$tmp19 =
                new PrimitiveVS<Event>(_null).restrict(pc_56);
            
            PrimitiveVS<Machine> var_$tmp20 =
                new PrimitiveVS<Machine>().restrict(pc_56);
            
            PrimitiveVS<Integer> var_$tmp21 =
                new PrimitiveVS<Integer>(0).restrict(pc_56);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp22 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_56);
            
            NamedTupleVS var_$tmp23 =
                new NamedTupleVS("participant", new PrimitiveVS<Machine>(), "transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_56);
            
            PrimitiveVS<Integer> temp_var_287;
            temp_var_287 = (PrimitiveVS<Integer>)((var_prepareReq.restrict(pc_56)).getField("transId"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_56, temp_var_287);
            
            PrimitiveVS<Boolean> temp_var_288;
            temp_var_288 = var_pendingWriteTrans.restrict(pc_56).containsKey(var_$tmp0.restrict(pc_56));
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_56, temp_var_288);
            
            PrimitiveVS<Boolean> temp_var_289;
            temp_var_289 = (var_$tmp1.restrict(pc_56)).apply((temp_var_290) -> !temp_var_290);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_56, temp_var_289);
            
            PrimitiveVS<Integer> temp_var_291;
            temp_var_291 = (PrimitiveVS<Integer>)((var_prepareReq.restrict(pc_56)).getField("transId"));
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_56, temp_var_291);
            
            MapVS<Integer, NamedTupleVS> temp_var_292;
            temp_var_292 = var_pendingWriteTrans.restrict(pc_56);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_56, temp_var_292);
            
            PrimitiveVS<String> temp_var_293;
            temp_var_293 = new PrimitiveVS<String>(String.format("Duplicate transaction ids not allowed!, received transId: {0}, pending transactions: {1}", var_$tmp3.restrict(pc_56), var_$tmp4.restrict(pc_56))).restrict(pc_56);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_56, temp_var_293);
            
            Assert.progProp(!(var_$tmp2.restrict(pc_56)).getValues().contains(Boolean.FALSE), var_$tmp5.restrict(pc_56), scheduler, var_$tmp2.restrict(pc_56).getGuardFor(Boolean.FALSE));
            PrimitiveVS<Integer> temp_var_294;
            temp_var_294 = (PrimitiveVS<Integer>)((var_prepareReq.restrict(pc_56)).getField("transId"));
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_56, temp_var_294);
            
            NamedTupleVS temp_var_295;
            temp_var_295 = (NamedTupleVS)((var_prepareReq.restrict(pc_56)).getField("rec"));
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_56, temp_var_295);
            
            NamedTupleVS temp_var_296;
            temp_var_296 = var_$tmp7.restrict(pc_56);
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_56, temp_var_296);
            
            MapVS<Integer, NamedTupleVS> temp_var_297 = var_pendingWriteTrans.restrict(pc_56);    
            PrimitiveVS<Integer> temp_var_299 = var_$tmp6.restrict(pc_56);
            NamedTupleVS temp_var_298;
            temp_var_298 = var_$tmp8.restrict(pc_56);
            temp_var_297 = temp_var_297.put(temp_var_299, temp_var_298);
            var_pendingWriteTrans = var_pendingWriteTrans.updateUnderGuard(pc_56, temp_var_297);
            
            PrimitiveVS<Boolean> temp_var_300;
            temp_var_300 = scheduler.getNextBoolean(pc_56);
            var_$tmp9 = var_$tmp9.updateUnderGuard(pc_56, temp_var_300);
            
            PrimitiveVS<Boolean> temp_var_301 = var_$tmp9.restrict(pc_56);
            Guard pc_57 = BooleanVS.getTrueGuard(temp_var_301);
            Guard pc_58 = BooleanVS.getFalseGuard(temp_var_301);
            boolean jumpedOut_26 = false;
            boolean jumpedOut_27 = false;
            if (!pc_57.isFalse()) {
                // 'then' branch
                PrimitiveVS<Machine> temp_var_302;
                temp_var_302 = (PrimitiveVS<Machine>)((var_prepareReq.restrict(pc_57)).getField("coordinator"));
                var_$tmp10 = var_$tmp10.updateUnderGuard(pc_57, temp_var_302);
                
                PrimitiveVS<Machine> temp_var_303;
                temp_var_303 = var_$tmp10.restrict(pc_57);
                var_$tmp11 = var_$tmp11.updateUnderGuard(pc_57, temp_var_303);
                
                PrimitiveVS<Event> temp_var_304;
                temp_var_304 = new PrimitiveVS<Event>(ePrepareResp).restrict(pc_57);
                var_$tmp12 = var_$tmp12.updateUnderGuard(pc_57, temp_var_304);
                
                PrimitiveVS<Machine> temp_var_305;
                temp_var_305 = new PrimitiveVS<Machine>(this).restrict(pc_57);
                var_$tmp13 = var_$tmp13.updateUnderGuard(pc_57, temp_var_305);
                
                PrimitiveVS<Integer> temp_var_306;
                temp_var_306 = (PrimitiveVS<Integer>)((var_prepareReq.restrict(pc_57)).getField("transId"));
                var_$tmp14 = var_$tmp14.updateUnderGuard(pc_57, temp_var_306);
                
                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_307;
                temp_var_307 = new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_57);
                var_$tmp15 = var_$tmp15.updateUnderGuard(pc_57, temp_var_307);
                
                NamedTupleVS temp_var_308;
                temp_var_308 = new NamedTupleVS("participant", var_$tmp13.restrict(pc_57), "transId", var_$tmp14.restrict(pc_57), "status", var_$tmp15.restrict(pc_57));
                var_$tmp16 = var_$tmp16.updateUnderGuard(pc_57, temp_var_308);
                
                effects.send(pc_57, var_$tmp11.restrict(pc_57), var_$tmp12.restrict(pc_57), new UnionVS(var_$tmp16.restrict(pc_57)));
                
            }
            if (!pc_58.isFalse()) {
                // 'else' branch
                PrimitiveVS<Machine> temp_var_309;
                temp_var_309 = (PrimitiveVS<Machine>)((var_prepareReq.restrict(pc_58)).getField("coordinator"));
                var_$tmp17 = var_$tmp17.updateUnderGuard(pc_58, temp_var_309);
                
                PrimitiveVS<Machine> temp_var_310;
                temp_var_310 = var_$tmp17.restrict(pc_58);
                var_$tmp18 = var_$tmp18.updateUnderGuard(pc_58, temp_var_310);
                
                PrimitiveVS<Event> temp_var_311;
                temp_var_311 = new PrimitiveVS<Event>(ePrepareResp).restrict(pc_58);
                var_$tmp19 = var_$tmp19.updateUnderGuard(pc_58, temp_var_311);
                
                PrimitiveVS<Machine> temp_var_312;
                temp_var_312 = new PrimitiveVS<Machine>(this).restrict(pc_58);
                var_$tmp20 = var_$tmp20.updateUnderGuard(pc_58, temp_var_312);
                
                PrimitiveVS<Integer> temp_var_313;
                temp_var_313 = (PrimitiveVS<Integer>)((var_prepareReq.restrict(pc_58)).getField("transId"));
                var_$tmp21 = var_$tmp21.updateUnderGuard(pc_58, temp_var_313);
                
                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_314;
                temp_var_314 = new PrimitiveVS<Integer>(1 /* enum tTransStatus elem ERROR */).restrict(pc_58);
                var_$tmp22 = var_$tmp22.updateUnderGuard(pc_58, temp_var_314);
                
                NamedTupleVS temp_var_315;
                temp_var_315 = new NamedTupleVS("participant", var_$tmp20.restrict(pc_58), "transId", var_$tmp21.restrict(pc_58), "status", var_$tmp22.restrict(pc_58));
                var_$tmp23 = var_$tmp23.updateUnderGuard(pc_58, temp_var_315);
                
                effects.send(pc_58, var_$tmp18.restrict(pc_58), var_$tmp19.restrict(pc_58), new UnionVS(var_$tmp23.restrict(pc_58)));
                
            }
            if (jumpedOut_26 || jumpedOut_27) {
                pc_56 = pc_57.or(pc_58);
            }
            
        }
        
        void 
        anonfun_16(
            Guard pc_59,
            EventBuffer effects,
            NamedTupleVS var_req
        ) {
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_59);
            
            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_59);
            
            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_59);
            
            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_59);
            
            PrimitiveVS<Event> var_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_59);
            
            PrimitiveVS<Integer> var_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_59);
            
            PrimitiveVS<Integer> var_$tmp6 =
                new PrimitiveVS<Integer>(0).restrict(pc_59);
            
            PrimitiveVS<Integer> var_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_59);
            
            NamedTupleVS var_$tmp8 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_59);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp9 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_59);
            
            NamedTupleVS var_$tmp10 =
                new NamedTupleVS("rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_59);
            
            PrimitiveVS<Machine> var_$tmp11 =
                new PrimitiveVS<Machine>().restrict(pc_59);
            
            PrimitiveVS<Machine> var_$tmp12 =
                new PrimitiveVS<Machine>().restrict(pc_59);
            
            PrimitiveVS<Event> var_$tmp13 =
                new PrimitiveVS<Event>(_null).restrict(pc_59);
            
            NamedTupleVS var_$tmp14 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_59);
            
            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp15 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_59);
            
            NamedTupleVS var_$tmp16 =
                new NamedTupleVS("rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_59);
            
            PrimitiveVS<Integer> temp_var_316;
            temp_var_316 = (PrimitiveVS<Integer>)((var_req.restrict(pc_59)).getField("key"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_59, temp_var_316);
            
            PrimitiveVS<Boolean> temp_var_317;
            temp_var_317 = var_kvStore.restrict(pc_59).containsKey(var_$tmp0.restrict(pc_59));
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_59, temp_var_317);
            
            PrimitiveVS<Boolean> temp_var_318 = var_$tmp1.restrict(pc_59);
            Guard pc_60 = BooleanVS.getTrueGuard(temp_var_318);
            Guard pc_61 = BooleanVS.getFalseGuard(temp_var_318);
            boolean jumpedOut_28 = false;
            boolean jumpedOut_29 = false;
            if (!pc_60.isFalse()) {
                // 'then' branch
                PrimitiveVS<Machine> temp_var_319;
                temp_var_319 = (PrimitiveVS<Machine>)((var_req.restrict(pc_60)).getField("client"));
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_60, temp_var_319);
                
                PrimitiveVS<Machine> temp_var_320;
                temp_var_320 = var_$tmp2.restrict(pc_60);
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_60, temp_var_320);
                
                PrimitiveVS<Event> temp_var_321;
                temp_var_321 = new PrimitiveVS<Event>(eReadTransResp).restrict(pc_60);
                var_$tmp4 = var_$tmp4.updateUnderGuard(pc_60, temp_var_321);
                
                PrimitiveVS<Integer> temp_var_322;
                temp_var_322 = (PrimitiveVS<Integer>)((var_req.restrict(pc_60)).getField("key"));
                var_$tmp5 = var_$tmp5.updateUnderGuard(pc_60, temp_var_322);
                
                PrimitiveVS<Integer> temp_var_323;
                temp_var_323 = (PrimitiveVS<Integer>)((var_req.restrict(pc_60)).getField("key"));
                var_$tmp6 = var_$tmp6.updateUnderGuard(pc_60, temp_var_323);
                
                PrimitiveVS<Integer> temp_var_324;
                temp_var_324 = var_kvStore.restrict(pc_60).get(var_$tmp6.restrict(pc_60));
                var_$tmp7 = var_$tmp7.updateUnderGuard(pc_60, temp_var_324);
                
                NamedTupleVS temp_var_325;
                temp_var_325 = new NamedTupleVS("key", var_$tmp5.restrict(pc_60), "val", var_$tmp7.restrict(pc_60));
                var_$tmp8 = var_$tmp8.updateUnderGuard(pc_60, temp_var_325);
                
                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_326;
                temp_var_326 = new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_60);
                var_$tmp9 = var_$tmp9.updateUnderGuard(pc_60, temp_var_326);
                
                NamedTupleVS temp_var_327;
                temp_var_327 = new NamedTupleVS("rec", var_$tmp8.restrict(pc_60), "status", var_$tmp9.restrict(pc_60));
                var_$tmp10 = var_$tmp10.updateUnderGuard(pc_60, temp_var_327);
                
                effects.send(pc_60, var_$tmp3.restrict(pc_60), var_$tmp4.restrict(pc_60), new UnionVS(var_$tmp10.restrict(pc_60)));
                
            }
            if (!pc_61.isFalse()) {
                // 'else' branch
                PrimitiveVS<Machine> temp_var_328;
                temp_var_328 = (PrimitiveVS<Machine>)((var_req.restrict(pc_61)).getField("client"));
                var_$tmp11 = var_$tmp11.updateUnderGuard(pc_61, temp_var_328);
                
                PrimitiveVS<Machine> temp_var_329;
                temp_var_329 = var_$tmp11.restrict(pc_61);
                var_$tmp12 = var_$tmp12.updateUnderGuard(pc_61, temp_var_329);
                
                PrimitiveVS<Event> temp_var_330;
                temp_var_330 = new PrimitiveVS<Event>(eReadTransResp).restrict(pc_61);
                var_$tmp13 = var_$tmp13.updateUnderGuard(pc_61, temp_var_330);
                
                NamedTupleVS temp_var_331;
                temp_var_331 = new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_61);
                var_$tmp14 = var_$tmp14.updateUnderGuard(pc_61, temp_var_331);
                
                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_332;
                temp_var_332 = new PrimitiveVS<Integer>(1 /* enum tTransStatus elem ERROR */).restrict(pc_61);
                var_$tmp15 = var_$tmp15.updateUnderGuard(pc_61, temp_var_332);
                
                NamedTupleVS temp_var_333;
                temp_var_333 = new NamedTupleVS("rec", var_$tmp14.restrict(pc_61), "status", var_$tmp15.restrict(pc_61));
                var_$tmp16 = var_$tmp16.updateUnderGuard(pc_61, temp_var_333);
                
                effects.send(pc_61, var_$tmp12.restrict(pc_61), var_$tmp13.restrict(pc_61), new UnionVS(var_$tmp16.restrict(pc_61)));
                
            }
            if (jumpedOut_28 || jumpedOut_29) {
                pc_59 = pc_60.or(pc_61);
            }
            
        }
        
    }
    
    public static class Main extends Machine {
        
        static State Init = new State("Init") {
            @Override public void entry(Guard pc_62, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Main)machine).anonfun_17(pc_62, machine.sendBuffer);
            }
        };
        
        @Override
        public void reset() {
                super.reset();
        }
        
        public Main(int id) {
            super("Main", id, EventBufferSemantics.queue, Init, Init
                
            );
            Init.addHandlers();
        }
        
        void 
        anonfun_17(
            Guard pc_63,
            EventBuffer effects
        ) {
            PrimitiveVS<Machine> var_coord =
                new PrimitiveVS<Machine>().restrict(pc_63);
            
            ListVS<PrimitiveVS<Machine>> var_participants =
                new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_63);
            
            PrimitiveVS<Integer> var_i =
                new PrimitiveVS<Integer>(0).restrict(pc_63);
            
            PrimitiveVS<Boolean> var_$tmp0 =
                new PrimitiveVS<Boolean>(false).restrict(pc_63);
            
            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_63);
            
            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_63);
            
            PrimitiveVS<Integer> var_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_63);
            
            ListVS<PrimitiveVS<Machine>> var_$tmp4 =
                new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_63);
            
            PrimitiveVS<Machine> var_$tmp5 =
                new PrimitiveVS<Machine>().restrict(pc_63);
            
            PrimitiveVS<Machine> var_$tmp6 =
                new PrimitiveVS<Machine>().restrict(pc_63);
            
            PrimitiveVS<Integer> var_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_63);
            
            NamedTupleVS var_$tmp8 =
                new NamedTupleVS("coor", new PrimitiveVS<Machine>(), "n", new PrimitiveVS<Integer>(0)).restrict(pc_63);
            
            PrimitiveVS<Machine> var_$tmp9 =
                new PrimitiveVS<Machine>().restrict(pc_63);
            
            PrimitiveVS<Integer> var_$tmp10 =
                new PrimitiveVS<Integer>(0).restrict(pc_63);
            
            NamedTupleVS var_$tmp11 =
                new NamedTupleVS("coor", new PrimitiveVS<Machine>(), "n", new PrimitiveVS<Integer>(0)).restrict(pc_63);
            
            java.util.List<Guard> loop_exits_7 = new java.util.ArrayList<>();
            boolean loop_early_ret_7 = false;
            Guard pc_64 = pc_63;
            while (!pc_64.isFalse()) {
                PrimitiveVS<Boolean> temp_var_334;
                temp_var_334 = (var_i.restrict(pc_64)).apply(new PrimitiveVS<Integer>(2).restrict(pc_64), (temp_var_335, temp_var_336) -> temp_var_335 < temp_var_336);
                var_$tmp0 = var_$tmp0.updateUnderGuard(pc_64, temp_var_334);
                
                PrimitiveVS<Boolean> temp_var_337;
                temp_var_337 = var_$tmp0.restrict(pc_64);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_64, temp_var_337);
                
                PrimitiveVS<Boolean> temp_var_338 = var_$tmp1.restrict(pc_64);
                Guard pc_65 = BooleanVS.getTrueGuard(temp_var_338);
                Guard pc_66 = BooleanVS.getFalseGuard(temp_var_338);
                boolean jumpedOut_30 = false;
                boolean jumpedOut_31 = false;
                if (!pc_65.isFalse()) {
                    // 'then' branch
                }
                if (!pc_66.isFalse()) {
                    // 'else' branch
                    loop_exits_7.add(pc_66);
                    jumpedOut_31 = true;
                    pc_66 = Guard.constFalse();
                    
                }
                if (jumpedOut_30 || jumpedOut_31) {
                    pc_64 = pc_65.or(pc_66);
                }
                
                if (!pc_64.isFalse()) {
                    PrimitiveVS<Machine> temp_var_339;
                    temp_var_339 = effects.create(pc_64, scheduler, Participant.class, (i) -> new Participant(i));
                    var_$tmp2 = var_$tmp2.updateUnderGuard(pc_64, temp_var_339);
                    
                    ListVS<PrimitiveVS<Machine>> temp_var_340 = var_participants.restrict(pc_64);    
                    temp_var_340 = var_participants.restrict(pc_64).insert(var_i.restrict(pc_64), var_$tmp2.restrict(pc_64));
                    var_participants = var_participants.updateUnderGuard(pc_64, temp_var_340);
                    
                    PrimitiveVS<Integer> temp_var_341;
                    temp_var_341 = (var_i.restrict(pc_64)).apply(new PrimitiveVS<Integer>(1).restrict(pc_64), (temp_var_342, temp_var_343) -> temp_var_342 + temp_var_343);
                    var_$tmp3 = var_$tmp3.updateUnderGuard(pc_64, temp_var_341);
                    
                    PrimitiveVS<Integer> temp_var_344;
                    temp_var_344 = var_$tmp3.restrict(pc_64);
                    var_i = var_i.updateUnderGuard(pc_64, temp_var_344);
                    
                }
            }
            if (loop_early_ret_7) {
                pc_63 = Guard.orMany(loop_exits_7);
            }
            
            ListVS<PrimitiveVS<Machine>> temp_var_345;
            temp_var_345 = var_participants.restrict(pc_63);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_63, temp_var_345);
            
            PrimitiveVS<Machine> temp_var_346;
            temp_var_346 = effects.create(pc_63, scheduler, Coordinator.class, new UnionVS (var_$tmp4.restrict(pc_63)), (i) -> new Coordinator(i));
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_63, temp_var_346);
            
            PrimitiveVS<Machine> temp_var_347;
            temp_var_347 = var_$tmp5.restrict(pc_63);
            var_coord = var_coord.updateUnderGuard(pc_63, temp_var_347);
            
            PrimitiveVS<Machine> temp_var_348;
            temp_var_348 = var_coord.restrict(pc_63);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_63, temp_var_348);
            
            PrimitiveVS<Integer> temp_var_349;
            temp_var_349 = new PrimitiveVS<Integer>(1).restrict(pc_63);
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_63, temp_var_349);
            
            NamedTupleVS temp_var_350;
            temp_var_350 = new NamedTupleVS("coor", var_$tmp6.restrict(pc_63), "n", var_$tmp7.restrict(pc_63));
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_63, temp_var_350);
            
            effects.create(pc_63, scheduler, Client.class, new UnionVS (var_$tmp8.restrict(pc_63)), (i) -> new Client(i));
            
            PrimitiveVS<Machine> temp_var_351;
            temp_var_351 = var_coord.restrict(pc_63);
            var_$tmp9 = var_$tmp9.updateUnderGuard(pc_63, temp_var_351);
            
            PrimitiveVS<Integer> temp_var_352;
            temp_var_352 = new PrimitiveVS<Integer>(1).restrict(pc_63);
            var_$tmp10 = var_$tmp10.updateUnderGuard(pc_63, temp_var_352);
            
            NamedTupleVS temp_var_353;
            temp_var_353 = new NamedTupleVS("coor", var_$tmp9.restrict(pc_63), "n", var_$tmp10.restrict(pc_63));
            var_$tmp11 = var_$tmp11.updateUnderGuard(pc_63, temp_var_353);
            
            effects.create(pc_63, scheduler, Client.class, new UnionVS (var_$tmp11.restrict(pc_63)), (i) -> new Client(i));
            
        }
        
    }
    
    public static class Timer extends Machine {
        
        static State Init = new State("Init") {
            @Override public void entry(Guard pc_67, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Timer)machine).anonfun_18(pc_67, machine.sendBuffer, outcome, payload != null ? (PrimitiveVS<Machine>) ValueSummary.castFromAny(pc_67, new PrimitiveVS<Machine>().restrict(pc_67), payload) : new PrimitiveVS<Machine>().restrict(pc_67));
            }
        };
        static State WaitForTimerRequests = new State("WaitForTimerRequests") {
        };
        private PrimitiveVS<Machine> var_client = new PrimitiveVS<Machine>();
        
        @Override
        public void reset() {
                super.reset();
                var_client = new PrimitiveVS<Machine>();
        }
        
        public Timer(int id) {
            super("Timer", id, EventBufferSemantics.queue, Init, Init
                , WaitForTimerRequests
                
            );
            Init.addHandlers();
            WaitForTimerRequests.addHandlers(new EventHandler(eStartTimer) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Timer)machine).anonfun_19(pc, machine.sendBuffer);
                    }
                },
                new EventHandler(eCancelTimer) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Timer)machine).anonfun_20(pc, machine.sendBuffer);
                    }
                });
        }
        
        Guard 
        anonfun_18(
            Guard pc_68,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            PrimitiveVS<Machine> var__client
        ) {
            PrimitiveVS<Machine> temp_var_354;
            temp_var_354 = var__client.restrict(pc_68);
            var_client = var_client.updateUnderGuard(pc_68, temp_var_354);
            
            outcome.addGuardedGoto(pc_68, WaitForTimerRequests);
            pc_68 = Guard.constFalse();
            
            return pc_68;
        }
        
        void 
        anonfun_19(
            Guard pc_69,
            EventBuffer effects
        ) {
            PrimitiveVS<Boolean> var_$tmp0 =
                new PrimitiveVS<Boolean>(false).restrict(pc_69);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_69);
            
            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_69);
            
            PrimitiveVS<Boolean> temp_var_355;
            temp_var_355 = scheduler.getNextBoolean(pc_69);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_69, temp_var_355);
            
            PrimitiveVS<Boolean> temp_var_356 = var_$tmp0.restrict(pc_69);
            Guard pc_70 = BooleanVS.getTrueGuard(temp_var_356);
            Guard pc_71 = BooleanVS.getFalseGuard(temp_var_356);
            boolean jumpedOut_32 = false;
            boolean jumpedOut_33 = false;
            if (!pc_70.isFalse()) {
                // 'then' branch
                PrimitiveVS<Machine> temp_var_357;
                temp_var_357 = var_client.restrict(pc_70);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_70, temp_var_357);
                
                PrimitiveVS<Event> temp_var_358;
                temp_var_358 = new PrimitiveVS<Event>(eTimeOut).restrict(pc_70);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_70, temp_var_358);
                
                effects.send(pc_70, var_$tmp1.restrict(pc_70), var_$tmp2.restrict(pc_70), null);
                
            }
            if (!pc_71.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_32 || jumpedOut_33) {
                pc_69 = pc_70.or(pc_71);
            }
            
        }
        
        void 
        anonfun_20(
            Guard pc_72,
            EventBuffer effects
        ) {
        }
        
    }
    
    // Skipping TypeDef 'tRecord'

    // Skipping TypeDef 'tWriteTransReq'

    // Skipping TypeDef 'tWriteTransResp'

    // Skipping TypeDef 'tReadTransReq'

    // Skipping TypeDef 'tReadTransResp'

    // Skipping TypeDef 'tPrepareReq'

    // Skipping TypeDef 'tPrepareResp'

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
