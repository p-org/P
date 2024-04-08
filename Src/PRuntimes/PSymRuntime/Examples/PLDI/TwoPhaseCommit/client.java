package psymbolic;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import psymbolic.commandline.*;
import psymbolic.runtime.*;
import psymbolic.runtime.logger.*;
import psymbolic.runtime.machine.*;
import psymbolic.runtime.machine.buffer.*;
import psymbolic.runtime.machine.eventhandlers.*;
import psymbolic.runtime.scheduler.*;
import psymbolic.valuesummary.*;

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
    // Skipping Interface 'Client'

    // Skipping Interface 'Coordinator'

    // Skipping Interface 'Participant'

    // Skipping Interface 'Main'

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
            temp_var_6 = scheduler.getNextInteger(new PrimitiveVS<Integer>(2).restrict(pc_4), pc_4);
            var_local_0_$tmp0 = var_local_0_$tmp0.updateUnderGuard(pc_4, temp_var_6);

            PrimitiveVS<Integer> temp_var_7;
            temp_var_7 = scheduler.getNextInteger(new PrimitiveVS<Integer>(2).restrict(pc_4), pc_4);
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
            @Override public void entry(Guard pc_14, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Coordinator)machine).anonfun_6(pc_14, machine.sendBuffer, outcome);
            }
            @Override public void exit(Guard pc, Machine machine) {
                ((Coordinator)machine).anonfun_7(pc, machine.sendBuffer);
            }
        };
        private ListVS<PrimitiveVS<Machine>> var_participants = new ListVS<PrimitiveVS<Machine>>(Guard.constTrue());
        private NamedTupleVS var_pendingWrTrans = new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)));
        private PrimitiveVS<Integer> var_currTransId = new PrimitiveVS<Integer>(0);
        private PrimitiveVS<Integer> var_countPrepareResponses = new PrimitiveVS<Integer>(0);

        @Override
        public void reset() {
                super.reset();
                var_participants = new ListVS<PrimitiveVS<Machine>>(Guard.constTrue());
                var_pendingWrTrans = new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)));
                var_currTransId = new PrimitiveVS<Integer>(0);
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
                        ((Coordinator)machine).anonfun_8(pc, machine.sendBuffer, outcome, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0))), payload));
                    }
                },
                new EventHandler(eReadTransReq) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Coordinator)machine).anonfun_9(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PrimitiveVS<Integer>(0)), payload));
                    }
                },
                new IgnoreEventHandler(ePrepareResp));
            WaitForPrepareResponses.addHandlers(new DeferEventHandler(eWriteTransReq)
                ,
                new EventHandler(ePrepareResp) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Coordinator)machine).anonfun_10(pc, machine.sendBuffer, outcome, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("participant", new PrimitiveVS<Machine>(), "transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)), payload));
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
            Guard pc_15,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            ListVS<PrimitiveVS<Machine>> var_payload
        ) {
            PrimitiveVS<Integer> var_i =
                new PrimitiveVS<Integer>(0).restrict(pc_15);

            ListVS<PrimitiveVS<Machine>> temp_var_40;
            temp_var_40 = var_payload.restrict(pc_15);
            var_participants = var_participants.updateUnderGuard(pc_15, temp_var_40);

            PrimitiveVS<Integer> temp_var_41;
            temp_var_41 = new PrimitiveVS<Integer>(0).restrict(pc_15);
            var_i = var_i.updateUnderGuard(pc_15, temp_var_41);

            PrimitiveVS<Integer> temp_var_42;
            temp_var_42 = new PrimitiveVS<Integer>(0).restrict(pc_15);
            var_currTransId = var_currTransId.updateUnderGuard(pc_15, temp_var_42);

            outcome.addGuardedGoto(pc_15, WaitForTransactions);
            pc_15 = Guard.constFalse();

            return pc_15;
        }

        void
        anonfun_5(
            Guard pc_16,
            EventBuffer effects
        ) {
        }

        Guard
        anonfun_8(
            Guard pc_17,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            NamedTupleVS var_wTrans
        ) {
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_17);

            PrimitiveVS<Event> var_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_17);

            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_17);

            PrimitiveVS<Integer> var_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_17);

            NamedTupleVS var_$tmp4 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_17);

            NamedTupleVS var_$tmp5 =
                new NamedTupleVS("coordinator", new PrimitiveVS<Machine>(), "transId", new PrimitiveVS<Integer>(0), "rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0))).restrict(pc_17);

            PrimitiveVS<Event> var_inline_1_message =
                new PrimitiveVS<Event>(_null).restrict(pc_17);

            NamedTupleVS var_inline_1_payload =
                new NamedTupleVS("coordinator", new PrimitiveVS<Machine>(), "transId", new PrimitiveVS<Integer>(0), "rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0))).restrict(pc_17);

            PrimitiveVS<Integer> var_local_1_i =
                new PrimitiveVS<Integer>(0).restrict(pc_17);

            PrimitiveVS<Integer> var_local_1_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_17);

            PrimitiveVS<Boolean> var_local_1_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_17);

            PrimitiveVS<Boolean> var_local_1_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_17);

            PrimitiveVS<Machine> var_local_1_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_17);

            PrimitiveVS<Machine> var_local_1_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_17);

            PrimitiveVS<Event> var_local_1_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_17);

            UnionVS var_local_1_$tmp6 =
                new UnionVS().restrict(pc_17);

            PrimitiveVS<Integer> var_local_1_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_17);

            NamedTupleVS temp_var_43;
            temp_var_43 = var_wTrans.restrict(pc_17);
            var_pendingWrTrans = var_pendingWrTrans.updateUnderGuard(pc_17, temp_var_43);

            PrimitiveVS<Integer> temp_var_44;
            temp_var_44 = (var_currTransId.restrict(pc_17)).apply(new PrimitiveVS<Integer>(1).restrict(pc_17), (temp_var_45, temp_var_46) -> temp_var_45 + temp_var_46);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_17, temp_var_44);

            PrimitiveVS<Integer> temp_var_47;
            temp_var_47 = var_$tmp0.restrict(pc_17);
            var_currTransId = var_currTransId.updateUnderGuard(pc_17, temp_var_47);

            PrimitiveVS<Event> temp_var_48;
            temp_var_48 = new PrimitiveVS<Event>(ePrepareReq).restrict(pc_17);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_17, temp_var_48);

            PrimitiveVS<Machine> temp_var_49;
            temp_var_49 = new PrimitiveVS<Machine>(this).restrict(pc_17);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_17, temp_var_49);

            PrimitiveVS<Integer> temp_var_50;
            temp_var_50 = var_currTransId.restrict(pc_17);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_17, temp_var_50);

            NamedTupleVS temp_var_51;
            temp_var_51 = (NamedTupleVS)((var_wTrans.restrict(pc_17)).getField("rec"));
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_17, temp_var_51);

            NamedTupleVS temp_var_52;
            temp_var_52 = new NamedTupleVS("coordinator", var_$tmp2.restrict(pc_17), "transId", var_$tmp3.restrict(pc_17), "rec", var_$tmp4.restrict(pc_17));
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_17, temp_var_52);

            PrimitiveVS<Event> temp_var_53;
            temp_var_53 = var_$tmp1.restrict(pc_17);
            var_inline_1_message = var_inline_1_message.updateUnderGuard(pc_17, temp_var_53);

            NamedTupleVS temp_var_54;
            temp_var_54 = var_$tmp5.restrict(pc_17);
            var_inline_1_payload = var_inline_1_payload.updateUnderGuard(pc_17, temp_var_54);

            PrimitiveVS<Integer> temp_var_55;
            temp_var_55 = new PrimitiveVS<Integer>(0).restrict(pc_17);
            var_local_1_i = var_local_1_i.updateUnderGuard(pc_17, temp_var_55);

            java.util.List<Guard> loop_exits_0 = new java.util.ArrayList<>();
            boolean loop_early_ret_0 = false;
            Guard pc_18 = pc_17;
            while (!pc_18.isFalse()) {
                PrimitiveVS<Integer> temp_var_56;
                temp_var_56 = var_participants.restrict(pc_18).size();
                var_local_1_$tmp0 = var_local_1_$tmp0.updateUnderGuard(pc_18, temp_var_56);

                PrimitiveVS<Boolean> temp_var_57;
                temp_var_57 = (var_local_1_i.restrict(pc_18)).apply(var_local_1_$tmp0.restrict(pc_18), (temp_var_58, temp_var_59) -> temp_var_58 < temp_var_59);
                var_local_1_$tmp1 = var_local_1_$tmp1.updateUnderGuard(pc_18, temp_var_57);

                PrimitiveVS<Boolean> temp_var_60;
                temp_var_60 = var_local_1_$tmp1.restrict(pc_18);
                var_local_1_$tmp2 = var_local_1_$tmp2.updateUnderGuard(pc_18, temp_var_60);

                PrimitiveVS<Boolean> temp_var_61 = var_local_1_$tmp2.restrict(pc_18);
                Guard pc_19 = BooleanVS.getTrueGuard(temp_var_61);
                Guard pc_20 = BooleanVS.getFalseGuard(temp_var_61);
                boolean jumpedOut_6 = false;
                boolean jumpedOut_7 = false;
                if (!pc_19.isFalse()) {
                    // 'then' branch
                }
                if (!pc_20.isFalse()) {
                    // 'else' branch
                    loop_exits_0.add(pc_20);
                    jumpedOut_7 = true;
                    pc_20 = Guard.constFalse();

                }
                if (jumpedOut_6 || jumpedOut_7) {
                    pc_18 = pc_19.or(pc_20);
                }

                if (!pc_18.isFalse()) {
                    PrimitiveVS<Machine> temp_var_62;
                    temp_var_62 = var_participants.restrict(pc_18).get(var_local_1_i.restrict(pc_18));
                    var_local_1_$tmp3 = var_local_1_$tmp3.updateUnderGuard(pc_18, temp_var_62);

                    PrimitiveVS<Machine> temp_var_63;
                    temp_var_63 = var_local_1_$tmp3.restrict(pc_18);
                    var_local_1_$tmp4 = var_local_1_$tmp4.updateUnderGuard(pc_18, temp_var_63);

                    PrimitiveVS<Event> temp_var_64;
                    temp_var_64 = var_inline_1_message.restrict(pc_18);
                    var_local_1_$tmp5 = var_local_1_$tmp5.updateUnderGuard(pc_18, temp_var_64);

                    UnionVS temp_var_65;
                    temp_var_65 = ValueSummary.castToAny(pc_18, var_inline_1_payload.restrict(pc_18));
                    var_local_1_$tmp6 = var_local_1_$tmp6.updateUnderGuard(pc_18, temp_var_65);

                    effects.send(pc_18, var_local_1_$tmp4.restrict(pc_18), var_local_1_$tmp5.restrict(pc_18), new UnionVS(var_local_1_$tmp6.restrict(pc_18)));

                    PrimitiveVS<Integer> temp_var_66;
                    temp_var_66 = (var_local_1_i.restrict(pc_18)).apply(new PrimitiveVS<Integer>(1).restrict(pc_18), (temp_var_67, temp_var_68) -> temp_var_67 + temp_var_68);
                    var_local_1_$tmp7 = var_local_1_$tmp7.updateUnderGuard(pc_18, temp_var_66);

                    PrimitiveVS<Integer> temp_var_69;
                    temp_var_69 = var_local_1_$tmp7.restrict(pc_18);
                    var_local_1_i = var_local_1_i.updateUnderGuard(pc_18, temp_var_69);

                }
            }
            if (loop_early_ret_0) {
                pc_17 = Guard.orMany(loop_exits_0);
            }

            outcome.addGuardedGoto(pc_17, WaitForPrepareResponses);
            pc_17 = Guard.constFalse();

            return pc_17;
        }

        void
        anonfun_9(
            Guard pc_21,
            EventBuffer effects,
            NamedTupleVS var_rTrans
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_21);

            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_21);

            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_21);

            NamedTupleVS var_$tmp3 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PrimitiveVS<Integer>(0)).restrict(pc_21);

            PrimitiveVS<Machine> temp_var_70;
            temp_var_70 = (PrimitiveVS<Machine>) scheduler.getNextElement(var_participants.restrict(pc_21), pc_21);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_21, temp_var_70);

            PrimitiveVS<Machine> temp_var_71;
            temp_var_71 = var_$tmp0.restrict(pc_21);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_21, temp_var_71);

            PrimitiveVS<Event> temp_var_72;
            temp_var_72 = new PrimitiveVS<Event>(eReadTransReq).restrict(pc_21);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_21, temp_var_72);

            NamedTupleVS temp_var_73;
            temp_var_73 = var_rTrans.restrict(pc_21);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_21, temp_var_73);

            effects.send(pc_21, var_$tmp1.restrict(pc_21), var_$tmp2.restrict(pc_21), new UnionVS(var_$tmp3.restrict(pc_21)));

        }

        Guard
        anonfun_6(
            Guard pc_22,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveVS<Boolean> var_$tmp0 =
                new PrimitiveVS<Boolean>(false).restrict(pc_22);

            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp1 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_22);

            PrimitiveVS<Integer> /* enum tTransStatus */ var_inline_3_respStatus =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_22);

            PrimitiveVS<Event> var_local_3_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_22);

            PrimitiveVS<Integer> var_local_3_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_22);

            PrimitiveVS<Machine> var_local_3_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_22);

            PrimitiveVS<Machine> var_local_3_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_22);

            PrimitiveVS<Event> var_local_3_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_22);

            PrimitiveVS<Integer> var_local_3_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_22);

            PrimitiveVS<Integer> /* enum tTransStatus */ var_local_3_$tmp6 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_22);

            NamedTupleVS var_local_3_$tmp7 =
                new NamedTupleVS("transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_22);

            PrimitiveVS<Event> var_local_3_inline_2_message =
                new PrimitiveVS<Event>(_null).restrict(pc_22);

            PrimitiveVS<Integer> var_local_3_inline_2_payload =
                new PrimitiveVS<Integer>(0).restrict(pc_22);

            PrimitiveVS<Integer> var_local_3_local_2_i =
                new PrimitiveVS<Integer>(0).restrict(pc_22);

            PrimitiveVS<Integer> var_local_3_local_2_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_22);

            PrimitiveVS<Boolean> var_local_3_local_2_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_22);

            PrimitiveVS<Boolean> var_local_3_local_2_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_22);

            PrimitiveVS<Machine> var_local_3_local_2_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_22);

            PrimitiveVS<Machine> var_local_3_local_2_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_22);

            PrimitiveVS<Event> var_local_3_local_2_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_22);

            UnionVS var_local_3_local_2_$tmp6 =
                new UnionVS().restrict(pc_22);

            PrimitiveVS<Integer> var_local_3_local_2_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_22);

            PrimitiveVS<Boolean> temp_var_74;
            temp_var_74 = scheduler.getNextBoolean(pc_22);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_22, temp_var_74);

            PrimitiveVS<Boolean> temp_var_75 = var_$tmp0.restrict(pc_22);
            Guard pc_23 = BooleanVS.getTrueGuard(temp_var_75);
            Guard pc_24 = BooleanVS.getFalseGuard(temp_var_75);
            boolean jumpedOut_8 = false;
            boolean jumpedOut_9 = false;
            if (!pc_23.isFalse()) {
                // 'then' branch
                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_76;
                temp_var_76 = new PrimitiveVS<Integer>(2 /* enum tTransStatus elem TIMEOUT */).restrict(pc_23);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_23, temp_var_76);

                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_77;
                temp_var_77 = var_$tmp1.restrict(pc_23);
                var_inline_3_respStatus = var_inline_3_respStatus.updateUnderGuard(pc_23, temp_var_77);

                PrimitiveVS<Event> temp_var_78;
                temp_var_78 = new PrimitiveVS<Event>(eAbortTrans).restrict(pc_23);
                var_local_3_$tmp0 = var_local_3_$tmp0.updateUnderGuard(pc_23, temp_var_78);

                PrimitiveVS<Integer> temp_var_79;
                temp_var_79 = var_currTransId.restrict(pc_23);
                var_local_3_$tmp1 = var_local_3_$tmp1.updateUnderGuard(pc_23, temp_var_79);

                PrimitiveVS<Event> temp_var_80;
                temp_var_80 = var_local_3_$tmp0.restrict(pc_23);
                var_local_3_inline_2_message = var_local_3_inline_2_message.updateUnderGuard(pc_23, temp_var_80);

                PrimitiveVS<Integer> temp_var_81;
                temp_var_81 = var_local_3_$tmp1.restrict(pc_23);
                var_local_3_inline_2_payload = var_local_3_inline_2_payload.updateUnderGuard(pc_23, temp_var_81);

                PrimitiveVS<Integer> temp_var_82;
                temp_var_82 = new PrimitiveVS<Integer>(0).restrict(pc_23);
                var_local_3_local_2_i = var_local_3_local_2_i.updateUnderGuard(pc_23, temp_var_82);

                java.util.List<Guard> loop_exits_1 = new java.util.ArrayList<>();
                boolean loop_early_ret_1 = false;
                Guard pc_25 = pc_23;
                while (!pc_25.isFalse()) {
                    PrimitiveVS<Integer> temp_var_83;
                    temp_var_83 = var_participants.restrict(pc_25).size();
                    var_local_3_local_2_$tmp0 = var_local_3_local_2_$tmp0.updateUnderGuard(pc_25, temp_var_83);

                    PrimitiveVS<Boolean> temp_var_84;
                    temp_var_84 = (var_local_3_local_2_i.restrict(pc_25)).apply(var_local_3_local_2_$tmp0.restrict(pc_25), (temp_var_85, temp_var_86) -> temp_var_85 < temp_var_86);
                    var_local_3_local_2_$tmp1 = var_local_3_local_2_$tmp1.updateUnderGuard(pc_25, temp_var_84);

                    PrimitiveVS<Boolean> temp_var_87;
                    temp_var_87 = var_local_3_local_2_$tmp1.restrict(pc_25);
                    var_local_3_local_2_$tmp2 = var_local_3_local_2_$tmp2.updateUnderGuard(pc_25, temp_var_87);

                    PrimitiveVS<Boolean> temp_var_88 = var_local_3_local_2_$tmp2.restrict(pc_25);
                    Guard pc_26 = BooleanVS.getTrueGuard(temp_var_88);
                    Guard pc_27 = BooleanVS.getFalseGuard(temp_var_88);
                    boolean jumpedOut_10 = false;
                    boolean jumpedOut_11 = false;
                    if (!pc_26.isFalse()) {
                        // 'then' branch
                    }
                    if (!pc_27.isFalse()) {
                        // 'else' branch
                        loop_exits_1.add(pc_27);
                        jumpedOut_11 = true;
                        pc_27 = Guard.constFalse();

                    }
                    if (jumpedOut_10 || jumpedOut_11) {
                        pc_25 = pc_26.or(pc_27);
                    }

                    if (!pc_25.isFalse()) {
                        PrimitiveVS<Machine> temp_var_89;
                        temp_var_89 = var_participants.restrict(pc_25).get(var_local_3_local_2_i.restrict(pc_25));
                        var_local_3_local_2_$tmp3 = var_local_3_local_2_$tmp3.updateUnderGuard(pc_25, temp_var_89);

                        PrimitiveVS<Machine> temp_var_90;
                        temp_var_90 = var_local_3_local_2_$tmp3.restrict(pc_25);
                        var_local_3_local_2_$tmp4 = var_local_3_local_2_$tmp4.updateUnderGuard(pc_25, temp_var_90);

                        PrimitiveVS<Event> temp_var_91;
                        temp_var_91 = var_local_3_inline_2_message.restrict(pc_25);
                        var_local_3_local_2_$tmp5 = var_local_3_local_2_$tmp5.updateUnderGuard(pc_25, temp_var_91);

                        UnionVS temp_var_92;
                        temp_var_92 = ValueSummary.castToAny(pc_25, var_local_3_inline_2_payload.restrict(pc_25));
                        var_local_3_local_2_$tmp6 = var_local_3_local_2_$tmp6.updateUnderGuard(pc_25, temp_var_92);

                        effects.send(pc_25, var_local_3_local_2_$tmp4.restrict(pc_25), var_local_3_local_2_$tmp5.restrict(pc_25), new UnionVS(var_local_3_local_2_$tmp6.restrict(pc_25)));

                        PrimitiveVS<Integer> temp_var_93;
                        temp_var_93 = (var_local_3_local_2_i.restrict(pc_25)).apply(new PrimitiveVS<Integer>(1).restrict(pc_25), (temp_var_94, temp_var_95) -> temp_var_94 + temp_var_95);
                        var_local_3_local_2_$tmp7 = var_local_3_local_2_$tmp7.updateUnderGuard(pc_25, temp_var_93);

                        PrimitiveVS<Integer> temp_var_96;
                        temp_var_96 = var_local_3_local_2_$tmp7.restrict(pc_25);
                        var_local_3_local_2_i = var_local_3_local_2_i.updateUnderGuard(pc_25, temp_var_96);

                    }
                }
                if (loop_early_ret_1) {
                    pc_23 = Guard.orMany(loop_exits_1);
                    jumpedOut_8 = true;
                }

                PrimitiveVS<Machine> temp_var_97;
                temp_var_97 = (PrimitiveVS<Machine>)((var_pendingWrTrans.restrict(pc_23)).getField("client"));
                var_local_3_$tmp2 = var_local_3_$tmp2.updateUnderGuard(pc_23, temp_var_97);

                PrimitiveVS<Machine> temp_var_98;
                temp_var_98 = var_local_3_$tmp2.restrict(pc_23);
                var_local_3_$tmp3 = var_local_3_$tmp3.updateUnderGuard(pc_23, temp_var_98);

                PrimitiveVS<Event> temp_var_99;
                temp_var_99 = new PrimitiveVS<Event>(eWriteTransResp).restrict(pc_23);
                var_local_3_$tmp4 = var_local_3_$tmp4.updateUnderGuard(pc_23, temp_var_99);

                PrimitiveVS<Integer> temp_var_100;
                temp_var_100 = var_currTransId.restrict(pc_23);
                var_local_3_$tmp5 = var_local_3_$tmp5.updateUnderGuard(pc_23, temp_var_100);

                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_101;
                temp_var_101 = var_inline_3_respStatus.restrict(pc_23);
                var_local_3_$tmp6 = var_local_3_$tmp6.updateUnderGuard(pc_23, temp_var_101);

                NamedTupleVS temp_var_102;
                temp_var_102 = new NamedTupleVS("transId", var_local_3_$tmp5.restrict(pc_23), "status", var_local_3_$tmp6.restrict(pc_23));
                var_local_3_$tmp7 = var_local_3_$tmp7.updateUnderGuard(pc_23, temp_var_102);

                effects.send(pc_23, var_local_3_$tmp3.restrict(pc_23), var_local_3_$tmp4.restrict(pc_23), new UnionVS(var_local_3_$tmp7.restrict(pc_23)));

                outcome.addGuardedGoto(pc_23, WaitForTransactions);
                pc_23 = Guard.constFalse();
                jumpedOut_8 = true;

            }
            if (!pc_24.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_8 || jumpedOut_9) {
                pc_22 = pc_23.or(pc_24);
            }

            if (!pc_22.isFalse()) {
            }
            return pc_22;
        }

        Guard
        anonfun_10(
            Guard pc_28,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            NamedTupleVS var_resp
        ) {
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_28);

            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_28);

            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp2 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_28);

            PrimitiveVS<Boolean> var_$tmp3 =
                new PrimitiveVS<Boolean>(false).restrict(pc_28);

            PrimitiveVS<Integer> var_$tmp4 =
                new PrimitiveVS<Integer>(0).restrict(pc_28);

            PrimitiveVS<Integer> var_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_28);

            PrimitiveVS<Boolean> var_$tmp6 =
                new PrimitiveVS<Boolean>(false).restrict(pc_28);

            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp7 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_28);

            PrimitiveVS<Event> var_local_5_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_28);

            PrimitiveVS<Integer> var_local_5_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_28);

            PrimitiveVS<Machine> var_local_5_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_28);

            PrimitiveVS<Machine> var_local_5_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_28);

            PrimitiveVS<Event> var_local_5_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_28);

            PrimitiveVS<Integer> var_local_5_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_28);

            PrimitiveVS<Integer> /* enum tTransStatus */ var_local_5_$tmp6 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_28);

            NamedTupleVS var_local_5_$tmp7 =
                new NamedTupleVS("transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_28);

            PrimitiveVS<Event> var_local_5_inline_4_message =
                new PrimitiveVS<Event>(_null).restrict(pc_28);

            PrimitiveVS<Integer> var_local_5_inline_4_payload =
                new PrimitiveVS<Integer>(0).restrict(pc_28);

            PrimitiveVS<Integer> var_local_5_local_4_i =
                new PrimitiveVS<Integer>(0).restrict(pc_28);

            PrimitiveVS<Integer> var_local_5_local_4_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_28);

            PrimitiveVS<Boolean> var_local_5_local_4_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_28);

            PrimitiveVS<Boolean> var_local_5_local_4_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_28);

            PrimitiveVS<Machine> var_local_5_local_4_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_28);

            PrimitiveVS<Machine> var_local_5_local_4_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_28);

            PrimitiveVS<Event> var_local_5_local_4_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_28);

            UnionVS var_local_5_local_4_$tmp6 =
                new UnionVS().restrict(pc_28);

            PrimitiveVS<Integer> var_local_5_local_4_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_28);

            PrimitiveVS<Integer> /* enum tTransStatus */ var_inline_6_respStatus =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_28);

            PrimitiveVS<Event> var_local_6_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_28);

            PrimitiveVS<Integer> var_local_6_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_28);

            PrimitiveVS<Machine> var_local_6_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_28);

            PrimitiveVS<Machine> var_local_6_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_28);

            PrimitiveVS<Event> var_local_6_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_28);

            PrimitiveVS<Integer> var_local_6_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_28);

            PrimitiveVS<Integer> /* enum tTransStatus */ var_local_6_$tmp6 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_28);

            NamedTupleVS var_local_6_$tmp7 =
                new NamedTupleVS("transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_28);

            PrimitiveVS<Event> var_local_6_inline_2_message =
                new PrimitiveVS<Event>(_null).restrict(pc_28);

            PrimitiveVS<Integer> var_local_6_inline_2_payload =
                new PrimitiveVS<Integer>(0).restrict(pc_28);

            PrimitiveVS<Integer> var_local_6_local_2_i =
                new PrimitiveVS<Integer>(0).restrict(pc_28);

            PrimitiveVS<Integer> var_local_6_local_2_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_28);

            PrimitiveVS<Boolean> var_local_6_local_2_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_28);

            PrimitiveVS<Boolean> var_local_6_local_2_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_28);

            PrimitiveVS<Machine> var_local_6_local_2_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_28);

            PrimitiveVS<Machine> var_local_6_local_2_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_28);

            PrimitiveVS<Event> var_local_6_local_2_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_28);

            UnionVS var_local_6_local_2_$tmp6 =
                new UnionVS().restrict(pc_28);

            PrimitiveVS<Integer> var_local_6_local_2_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_28);

            PrimitiveVS<Integer> temp_var_103;
            temp_var_103 = (PrimitiveVS<Integer>)((var_resp.restrict(pc_28)).getField("transId"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_28, temp_var_103);

            PrimitiveVS<Boolean> temp_var_104;
            temp_var_104 = var_currTransId.restrict(pc_28).symbolicEquals(var_$tmp0.restrict(pc_28), pc_28);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_28, temp_var_104);

            PrimitiveVS<Boolean> temp_var_105 = var_$tmp1.restrict(pc_28);
            Guard pc_29 = BooleanVS.getTrueGuard(temp_var_105);
            Guard pc_30 = BooleanVS.getFalseGuard(temp_var_105);
            boolean jumpedOut_12 = false;
            boolean jumpedOut_13 = false;
            if (!pc_29.isFalse()) {
                // 'then' branch
                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_106;
                temp_var_106 = (PrimitiveVS<Integer> /* enum tTransStatus */)((var_resp.restrict(pc_29)).getField("status"));
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_29, temp_var_106);

                PrimitiveVS<Boolean> temp_var_107;
                temp_var_107 = var_$tmp2.restrict(pc_29).symbolicEquals(new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_29), pc_29);
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_29, temp_var_107);

                PrimitiveVS<Boolean> temp_var_108 = var_$tmp3.restrict(pc_29);
                Guard pc_31 = BooleanVS.getTrueGuard(temp_var_108);
                Guard pc_32 = BooleanVS.getFalseGuard(temp_var_108);
                boolean jumpedOut_14 = false;
                boolean jumpedOut_15 = false;
                if (!pc_31.isFalse()) {
                    // 'then' branch
                    PrimitiveVS<Integer> temp_var_109;
                    temp_var_109 = (var_countPrepareResponses.restrict(pc_31)).apply(new PrimitiveVS<Integer>(1).restrict(pc_31), (temp_var_110, temp_var_111) -> temp_var_110 + temp_var_111);
                    var_$tmp4 = var_$tmp4.updateUnderGuard(pc_31, temp_var_109);

                    PrimitiveVS<Integer> temp_var_112;
                    temp_var_112 = var_$tmp4.restrict(pc_31);
                    var_countPrepareResponses = var_countPrepareResponses.updateUnderGuard(pc_31, temp_var_112);

                    PrimitiveVS<Integer> temp_var_113;
                    temp_var_113 = var_participants.restrict(pc_31).size();
                    var_$tmp5 = var_$tmp5.updateUnderGuard(pc_31, temp_var_113);

                    PrimitiveVS<Boolean> temp_var_114;
                    temp_var_114 = var_countPrepareResponses.restrict(pc_31).symbolicEquals(var_$tmp5.restrict(pc_31), pc_31);
                    var_$tmp6 = var_$tmp6.updateUnderGuard(pc_31, temp_var_114);

                    PrimitiveVS<Boolean> temp_var_115 = var_$tmp6.restrict(pc_31);
                    Guard pc_33 = BooleanVS.getTrueGuard(temp_var_115);
                    Guard pc_34 = BooleanVS.getFalseGuard(temp_var_115);
                    boolean jumpedOut_16 = false;
                    boolean jumpedOut_17 = false;
                    if (!pc_33.isFalse()) {
                        // 'then' branch
                        PrimitiveVS<Event> temp_var_116;
                        temp_var_116 = new PrimitiveVS<Event>(eCommitTrans).restrict(pc_33);
                        var_local_5_$tmp0 = var_local_5_$tmp0.updateUnderGuard(pc_33, temp_var_116);

                        PrimitiveVS<Integer> temp_var_117;
                        temp_var_117 = var_currTransId.restrict(pc_33);
                        var_local_5_$tmp1 = var_local_5_$tmp1.updateUnderGuard(pc_33, temp_var_117);

                        PrimitiveVS<Event> temp_var_118;
                        temp_var_118 = var_local_5_$tmp0.restrict(pc_33);
                        var_local_5_inline_4_message = var_local_5_inline_4_message.updateUnderGuard(pc_33, temp_var_118);

                        PrimitiveVS<Integer> temp_var_119;
                        temp_var_119 = var_local_5_$tmp1.restrict(pc_33);
                        var_local_5_inline_4_payload = var_local_5_inline_4_payload.updateUnderGuard(pc_33, temp_var_119);

                        PrimitiveVS<Integer> temp_var_120;
                        temp_var_120 = new PrimitiveVS<Integer>(0).restrict(pc_33);
                        var_local_5_local_4_i = var_local_5_local_4_i.updateUnderGuard(pc_33, temp_var_120);

                        java.util.List<Guard> loop_exits_2 = new java.util.ArrayList<>();
                        boolean loop_early_ret_2 = false;
                        Guard pc_35 = pc_33;
                        while (!pc_35.isFalse()) {
                            PrimitiveVS<Integer> temp_var_121;
                            temp_var_121 = var_participants.restrict(pc_35).size();
                            var_local_5_local_4_$tmp0 = var_local_5_local_4_$tmp0.updateUnderGuard(pc_35, temp_var_121);

                            PrimitiveVS<Boolean> temp_var_122;
                            temp_var_122 = (var_local_5_local_4_i.restrict(pc_35)).apply(var_local_5_local_4_$tmp0.restrict(pc_35), (temp_var_123, temp_var_124) -> temp_var_123 < temp_var_124);
                            var_local_5_local_4_$tmp1 = var_local_5_local_4_$tmp1.updateUnderGuard(pc_35, temp_var_122);

                            PrimitiveVS<Boolean> temp_var_125;
                            temp_var_125 = var_local_5_local_4_$tmp1.restrict(pc_35);
                            var_local_5_local_4_$tmp2 = var_local_5_local_4_$tmp2.updateUnderGuard(pc_35, temp_var_125);

                            PrimitiveVS<Boolean> temp_var_126 = var_local_5_local_4_$tmp2.restrict(pc_35);
                            Guard pc_36 = BooleanVS.getTrueGuard(temp_var_126);
                            Guard pc_37 = BooleanVS.getFalseGuard(temp_var_126);
                            boolean jumpedOut_18 = false;
                            boolean jumpedOut_19 = false;
                            if (!pc_36.isFalse()) {
                                // 'then' branch
                            }
                            if (!pc_37.isFalse()) {
                                // 'else' branch
                                loop_exits_2.add(pc_37);
                                jumpedOut_19 = true;
                                pc_37 = Guard.constFalse();

                            }
                            if (jumpedOut_18 || jumpedOut_19) {
                                pc_35 = pc_36.or(pc_37);
                            }

                            if (!pc_35.isFalse()) {
                                PrimitiveVS<Machine> temp_var_127;
                                temp_var_127 = var_participants.restrict(pc_35).get(var_local_5_local_4_i.restrict(pc_35));
                                var_local_5_local_4_$tmp3 = var_local_5_local_4_$tmp3.updateUnderGuard(pc_35, temp_var_127);

                                PrimitiveVS<Machine> temp_var_128;
                                temp_var_128 = var_local_5_local_4_$tmp3.restrict(pc_35);
                                var_local_5_local_4_$tmp4 = var_local_5_local_4_$tmp4.updateUnderGuard(pc_35, temp_var_128);

                                PrimitiveVS<Event> temp_var_129;
                                temp_var_129 = var_local_5_inline_4_message.restrict(pc_35);
                                var_local_5_local_4_$tmp5 = var_local_5_local_4_$tmp5.updateUnderGuard(pc_35, temp_var_129);

                                UnionVS temp_var_130;
                                temp_var_130 = ValueSummary.castToAny(pc_35, var_local_5_inline_4_payload.restrict(pc_35));
                                var_local_5_local_4_$tmp6 = var_local_5_local_4_$tmp6.updateUnderGuard(pc_35, temp_var_130);

                                effects.send(pc_35, var_local_5_local_4_$tmp4.restrict(pc_35), var_local_5_local_4_$tmp5.restrict(pc_35), new UnionVS(var_local_5_local_4_$tmp6.restrict(pc_35)));

                                PrimitiveVS<Integer> temp_var_131;
                                temp_var_131 = (var_local_5_local_4_i.restrict(pc_35)).apply(new PrimitiveVS<Integer>(1).restrict(pc_35), (temp_var_132, temp_var_133) -> temp_var_132 + temp_var_133);
                                var_local_5_local_4_$tmp7 = var_local_5_local_4_$tmp7.updateUnderGuard(pc_35, temp_var_131);

                                PrimitiveVS<Integer> temp_var_134;
                                temp_var_134 = var_local_5_local_4_$tmp7.restrict(pc_35);
                                var_local_5_local_4_i = var_local_5_local_4_i.updateUnderGuard(pc_35, temp_var_134);

                            }
                        }
                        if (loop_early_ret_2) {
                            pc_33 = Guard.orMany(loop_exits_2);
                            jumpedOut_16 = true;
                        }

                        PrimitiveVS<Machine> temp_var_135;
                        temp_var_135 = (PrimitiveVS<Machine>)((var_pendingWrTrans.restrict(pc_33)).getField("client"));
                        var_local_5_$tmp2 = var_local_5_$tmp2.updateUnderGuard(pc_33, temp_var_135);

                        PrimitiveVS<Machine> temp_var_136;
                        temp_var_136 = var_local_5_$tmp2.restrict(pc_33);
                        var_local_5_$tmp3 = var_local_5_$tmp3.updateUnderGuard(pc_33, temp_var_136);

                        PrimitiveVS<Event> temp_var_137;
                        temp_var_137 = new PrimitiveVS<Event>(eWriteTransResp).restrict(pc_33);
                        var_local_5_$tmp4 = var_local_5_$tmp4.updateUnderGuard(pc_33, temp_var_137);

                        PrimitiveVS<Integer> temp_var_138;
                        temp_var_138 = var_currTransId.restrict(pc_33);
                        var_local_5_$tmp5 = var_local_5_$tmp5.updateUnderGuard(pc_33, temp_var_138);

                        PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_139;
                        temp_var_139 = new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_33);
                        var_local_5_$tmp6 = var_local_5_$tmp6.updateUnderGuard(pc_33, temp_var_139);

                        NamedTupleVS temp_var_140;
                        temp_var_140 = new NamedTupleVS("transId", var_local_5_$tmp5.restrict(pc_33), "status", var_local_5_$tmp6.restrict(pc_33));
                        var_local_5_$tmp7 = var_local_5_$tmp7.updateUnderGuard(pc_33, temp_var_140);

                        effects.send(pc_33, var_local_5_$tmp3.restrict(pc_33), var_local_5_$tmp4.restrict(pc_33), new UnionVS(var_local_5_$tmp7.restrict(pc_33)));

                        outcome.addGuardedGoto(pc_33, WaitForTransactions);
                        pc_33 = Guard.constFalse();
                        jumpedOut_16 = true;

                    }
                    if (!pc_34.isFalse()) {
                        // 'else' branch
                    }
                    if (jumpedOut_16 || jumpedOut_17) {
                        pc_31 = pc_33.or(pc_34);
                        jumpedOut_14 = true;
                    }

                    if (!pc_31.isFalse()) {
                    }
                }
                if (!pc_32.isFalse()) {
                    // 'else' branch
                    PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_141;
                    temp_var_141 = new PrimitiveVS<Integer>(1 /* enum tTransStatus elem ERROR */).restrict(pc_32);
                    var_$tmp7 = var_$tmp7.updateUnderGuard(pc_32, temp_var_141);

                    PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_142;
                    temp_var_142 = var_$tmp7.restrict(pc_32);
                    var_inline_6_respStatus = var_inline_6_respStatus.updateUnderGuard(pc_32, temp_var_142);

                    PrimitiveVS<Event> temp_var_143;
                    temp_var_143 = new PrimitiveVS<Event>(eAbortTrans).restrict(pc_32);
                    var_local_6_$tmp0 = var_local_6_$tmp0.updateUnderGuard(pc_32, temp_var_143);

                    PrimitiveVS<Integer> temp_var_144;
                    temp_var_144 = var_currTransId.restrict(pc_32);
                    var_local_6_$tmp1 = var_local_6_$tmp1.updateUnderGuard(pc_32, temp_var_144);

                    PrimitiveVS<Event> temp_var_145;
                    temp_var_145 = var_local_6_$tmp0.restrict(pc_32);
                    var_local_6_inline_2_message = var_local_6_inline_2_message.updateUnderGuard(pc_32, temp_var_145);

                    PrimitiveVS<Integer> temp_var_146;
                    temp_var_146 = var_local_6_$tmp1.restrict(pc_32);
                    var_local_6_inline_2_payload = var_local_6_inline_2_payload.updateUnderGuard(pc_32, temp_var_146);

                    PrimitiveVS<Integer> temp_var_147;
                    temp_var_147 = new PrimitiveVS<Integer>(0).restrict(pc_32);
                    var_local_6_local_2_i = var_local_6_local_2_i.updateUnderGuard(pc_32, temp_var_147);

                    java.util.List<Guard> loop_exits_3 = new java.util.ArrayList<>();
                    boolean loop_early_ret_3 = false;
                    Guard pc_38 = pc_32;
                    while (!pc_38.isFalse()) {
                        PrimitiveVS<Integer> temp_var_148;
                        temp_var_148 = var_participants.restrict(pc_38).size();
                        var_local_6_local_2_$tmp0 = var_local_6_local_2_$tmp0.updateUnderGuard(pc_38, temp_var_148);

                        PrimitiveVS<Boolean> temp_var_149;
                        temp_var_149 = (var_local_6_local_2_i.restrict(pc_38)).apply(var_local_6_local_2_$tmp0.restrict(pc_38), (temp_var_150, temp_var_151) -> temp_var_150 < temp_var_151);
                        var_local_6_local_2_$tmp1 = var_local_6_local_2_$tmp1.updateUnderGuard(pc_38, temp_var_149);

                        PrimitiveVS<Boolean> temp_var_152;
                        temp_var_152 = var_local_6_local_2_$tmp1.restrict(pc_38);
                        var_local_6_local_2_$tmp2 = var_local_6_local_2_$tmp2.updateUnderGuard(pc_38, temp_var_152);

                        PrimitiveVS<Boolean> temp_var_153 = var_local_6_local_2_$tmp2.restrict(pc_38);
                        Guard pc_39 = BooleanVS.getTrueGuard(temp_var_153);
                        Guard pc_40 = BooleanVS.getFalseGuard(temp_var_153);
                        boolean jumpedOut_20 = false;
                        boolean jumpedOut_21 = false;
                        if (!pc_39.isFalse()) {
                            // 'then' branch
                        }
                        if (!pc_40.isFalse()) {
                            // 'else' branch
                            loop_exits_3.add(pc_40);
                            jumpedOut_21 = true;
                            pc_40 = Guard.constFalse();

                        }
                        if (jumpedOut_20 || jumpedOut_21) {
                            pc_38 = pc_39.or(pc_40);
                        }

                        if (!pc_38.isFalse()) {
                            PrimitiveVS<Machine> temp_var_154;
                            temp_var_154 = var_participants.restrict(pc_38).get(var_local_6_local_2_i.restrict(pc_38));
                            var_local_6_local_2_$tmp3 = var_local_6_local_2_$tmp3.updateUnderGuard(pc_38, temp_var_154);

                            PrimitiveVS<Machine> temp_var_155;
                            temp_var_155 = var_local_6_local_2_$tmp3.restrict(pc_38);
                            var_local_6_local_2_$tmp4 = var_local_6_local_2_$tmp4.updateUnderGuard(pc_38, temp_var_155);

                            PrimitiveVS<Event> temp_var_156;
                            temp_var_156 = var_local_6_inline_2_message.restrict(pc_38);
                            var_local_6_local_2_$tmp5 = var_local_6_local_2_$tmp5.updateUnderGuard(pc_38, temp_var_156);

                            UnionVS temp_var_157;
                            temp_var_157 = ValueSummary.castToAny(pc_38, var_local_6_inline_2_payload.restrict(pc_38));
                            var_local_6_local_2_$tmp6 = var_local_6_local_2_$tmp6.updateUnderGuard(pc_38, temp_var_157);

                            effects.send(pc_38, var_local_6_local_2_$tmp4.restrict(pc_38), var_local_6_local_2_$tmp5.restrict(pc_38), new UnionVS(var_local_6_local_2_$tmp6.restrict(pc_38)));

                            PrimitiveVS<Integer> temp_var_158;
                            temp_var_158 = (var_local_6_local_2_i.restrict(pc_38)).apply(new PrimitiveVS<Integer>(1).restrict(pc_38), (temp_var_159, temp_var_160) -> temp_var_159 + temp_var_160);
                            var_local_6_local_2_$tmp7 = var_local_6_local_2_$tmp7.updateUnderGuard(pc_38, temp_var_158);

                            PrimitiveVS<Integer> temp_var_161;
                            temp_var_161 = var_local_6_local_2_$tmp7.restrict(pc_38);
                            var_local_6_local_2_i = var_local_6_local_2_i.updateUnderGuard(pc_38, temp_var_161);

                        }
                    }
                    if (loop_early_ret_3) {
                        pc_32 = Guard.orMany(loop_exits_3);
                        jumpedOut_15 = true;
                    }

                    PrimitiveVS<Machine> temp_var_162;
                    temp_var_162 = (PrimitiveVS<Machine>)((var_pendingWrTrans.restrict(pc_32)).getField("client"));
                    var_local_6_$tmp2 = var_local_6_$tmp2.updateUnderGuard(pc_32, temp_var_162);

                    PrimitiveVS<Machine> temp_var_163;
                    temp_var_163 = var_local_6_$tmp2.restrict(pc_32);
                    var_local_6_$tmp3 = var_local_6_$tmp3.updateUnderGuard(pc_32, temp_var_163);

                    PrimitiveVS<Event> temp_var_164;
                    temp_var_164 = new PrimitiveVS<Event>(eWriteTransResp).restrict(pc_32);
                    var_local_6_$tmp4 = var_local_6_$tmp4.updateUnderGuard(pc_32, temp_var_164);

                    PrimitiveVS<Integer> temp_var_165;
                    temp_var_165 = var_currTransId.restrict(pc_32);
                    var_local_6_$tmp5 = var_local_6_$tmp5.updateUnderGuard(pc_32, temp_var_165);

                    PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_166;
                    temp_var_166 = var_inline_6_respStatus.restrict(pc_32);
                    var_local_6_$tmp6 = var_local_6_$tmp6.updateUnderGuard(pc_32, temp_var_166);

                    NamedTupleVS temp_var_167;
                    temp_var_167 = new NamedTupleVS("transId", var_local_6_$tmp5.restrict(pc_32), "status", var_local_6_$tmp6.restrict(pc_32));
                    var_local_6_$tmp7 = var_local_6_$tmp7.updateUnderGuard(pc_32, temp_var_167);

                    effects.send(pc_32, var_local_6_$tmp3.restrict(pc_32), var_local_6_$tmp4.restrict(pc_32), new UnionVS(var_local_6_$tmp7.restrict(pc_32)));

                    outcome.addGuardedGoto(pc_32, WaitForTransactions);
                    pc_32 = Guard.constFalse();
                    jumpedOut_15 = true;

                }
                if (jumpedOut_14 || jumpedOut_15) {
                    pc_29 = pc_31.or(pc_32);
                    jumpedOut_12 = true;
                }

                if (!pc_29.isFalse()) {
                }
            }
            if (!pc_30.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_12 || jumpedOut_13) {
                pc_28 = pc_29.or(pc_30);
            }

            if (!pc_28.isFalse()) {
            }
            return pc_28;
        }

        void
        anonfun_11(
            Guard pc_41,
            EventBuffer effects,
            NamedTupleVS var_rTrans
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_41);

            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_41);

            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_41);

            NamedTupleVS var_$tmp3 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PrimitiveVS<Integer>(0)).restrict(pc_41);

            PrimitiveVS<Machine> temp_var_168;
            temp_var_168 = (PrimitiveVS<Machine>) scheduler.getNextElement(var_participants.restrict(pc_41), pc_41);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_41, temp_var_168);

            PrimitiveVS<Machine> temp_var_169;
            temp_var_169 = var_$tmp0.restrict(pc_41);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_41, temp_var_169);

            PrimitiveVS<Event> temp_var_170;
            temp_var_170 = new PrimitiveVS<Event>(eReadTransReq).restrict(pc_41);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_41, temp_var_170);

            NamedTupleVS temp_var_171;
            temp_var_171 = var_rTrans.restrict(pc_41);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_41, temp_var_171);

            effects.send(pc_41, var_$tmp1.restrict(pc_41), var_$tmp2.restrict(pc_41), new UnionVS(var_$tmp3.restrict(pc_41)));

        }

        void
        anonfun_7(
            Guard pc_42,
            EventBuffer effects
        ) {
            PrimitiveVS<Integer> temp_var_172;
            temp_var_172 = new PrimitiveVS<Integer>(0).restrict(pc_42);
            var_countPrepareResponses = var_countPrepareResponses.updateUnderGuard(pc_42, temp_var_172);

        }

        void
        DoGlobalAbort(
            Guard pc_43,
            EventBuffer effects,
            PrimitiveVS<Integer> /* enum tTransStatus */ var_respStatus
        ) {
            PrimitiveVS<Event> var_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_43);

            PrimitiveVS<Integer> var_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_43);

            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_43);

            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_43);

            PrimitiveVS<Event> var_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_43);

            PrimitiveVS<Integer> var_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_43);

            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp6 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_43);

            NamedTupleVS var_$tmp7 =
                new NamedTupleVS("transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_43);

            PrimitiveVS<Event> var_inline_2_message =
                new PrimitiveVS<Event>(_null).restrict(pc_43);

            PrimitiveVS<Integer> var_inline_2_payload =
                new PrimitiveVS<Integer>(0).restrict(pc_43);

            PrimitiveVS<Integer> var_local_2_i =
                new PrimitiveVS<Integer>(0).restrict(pc_43);

            PrimitiveVS<Integer> var_local_2_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_43);

            PrimitiveVS<Boolean> var_local_2_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_43);

            PrimitiveVS<Boolean> var_local_2_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_43);

            PrimitiveVS<Machine> var_local_2_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_43);

            PrimitiveVS<Machine> var_local_2_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_43);

            PrimitiveVS<Event> var_local_2_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_43);

            UnionVS var_local_2_$tmp6 =
                new UnionVS().restrict(pc_43);

            PrimitiveVS<Integer> var_local_2_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_43);

            PrimitiveVS<Event> temp_var_173;
            temp_var_173 = new PrimitiveVS<Event>(eAbortTrans).restrict(pc_43);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_43, temp_var_173);

            PrimitiveVS<Integer> temp_var_174;
            temp_var_174 = var_currTransId.restrict(pc_43);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_43, temp_var_174);

            PrimitiveVS<Event> temp_var_175;
            temp_var_175 = var_$tmp0.restrict(pc_43);
            var_inline_2_message = var_inline_2_message.updateUnderGuard(pc_43, temp_var_175);

            PrimitiveVS<Integer> temp_var_176;
            temp_var_176 = var_$tmp1.restrict(pc_43);
            var_inline_2_payload = var_inline_2_payload.updateUnderGuard(pc_43, temp_var_176);

            PrimitiveVS<Integer> temp_var_177;
            temp_var_177 = new PrimitiveVS<Integer>(0).restrict(pc_43);
            var_local_2_i = var_local_2_i.updateUnderGuard(pc_43, temp_var_177);

            java.util.List<Guard> loop_exits_4 = new java.util.ArrayList<>();
            boolean loop_early_ret_4 = false;
            Guard pc_44 = pc_43;
            while (!pc_44.isFalse()) {
                PrimitiveVS<Integer> temp_var_178;
                temp_var_178 = var_participants.restrict(pc_44).size();
                var_local_2_$tmp0 = var_local_2_$tmp0.updateUnderGuard(pc_44, temp_var_178);

                PrimitiveVS<Boolean> temp_var_179;
                temp_var_179 = (var_local_2_i.restrict(pc_44)).apply(var_local_2_$tmp0.restrict(pc_44), (temp_var_180, temp_var_181) -> temp_var_180 < temp_var_181);
                var_local_2_$tmp1 = var_local_2_$tmp1.updateUnderGuard(pc_44, temp_var_179);

                PrimitiveVS<Boolean> temp_var_182;
                temp_var_182 = var_local_2_$tmp1.restrict(pc_44);
                var_local_2_$tmp2 = var_local_2_$tmp2.updateUnderGuard(pc_44, temp_var_182);

                PrimitiveVS<Boolean> temp_var_183 = var_local_2_$tmp2.restrict(pc_44);
                Guard pc_45 = BooleanVS.getTrueGuard(temp_var_183);
                Guard pc_46 = BooleanVS.getFalseGuard(temp_var_183);
                boolean jumpedOut_22 = false;
                boolean jumpedOut_23 = false;
                if (!pc_45.isFalse()) {
                    // 'then' branch
                }
                if (!pc_46.isFalse()) {
                    // 'else' branch
                    loop_exits_4.add(pc_46);
                    jumpedOut_23 = true;
                    pc_46 = Guard.constFalse();

                }
                if (jumpedOut_22 || jumpedOut_23) {
                    pc_44 = pc_45.or(pc_46);
                }

                if (!pc_44.isFalse()) {
                    PrimitiveVS<Machine> temp_var_184;
                    temp_var_184 = var_participants.restrict(pc_44).get(var_local_2_i.restrict(pc_44));
                    var_local_2_$tmp3 = var_local_2_$tmp3.updateUnderGuard(pc_44, temp_var_184);

                    PrimitiveVS<Machine> temp_var_185;
                    temp_var_185 = var_local_2_$tmp3.restrict(pc_44);
                    var_local_2_$tmp4 = var_local_2_$tmp4.updateUnderGuard(pc_44, temp_var_185);

                    PrimitiveVS<Event> temp_var_186;
                    temp_var_186 = var_inline_2_message.restrict(pc_44);
                    var_local_2_$tmp5 = var_local_2_$tmp5.updateUnderGuard(pc_44, temp_var_186);

                    UnionVS temp_var_187;
                    temp_var_187 = ValueSummary.castToAny(pc_44, var_inline_2_payload.restrict(pc_44));
                    var_local_2_$tmp6 = var_local_2_$tmp6.updateUnderGuard(pc_44, temp_var_187);

                    effects.send(pc_44, var_local_2_$tmp4.restrict(pc_44), var_local_2_$tmp5.restrict(pc_44), new UnionVS(var_local_2_$tmp6.restrict(pc_44)));

                    PrimitiveVS<Integer> temp_var_188;
                    temp_var_188 = (var_local_2_i.restrict(pc_44)).apply(new PrimitiveVS<Integer>(1).restrict(pc_44), (temp_var_189, temp_var_190) -> temp_var_189 + temp_var_190);
                    var_local_2_$tmp7 = var_local_2_$tmp7.updateUnderGuard(pc_44, temp_var_188);

                    PrimitiveVS<Integer> temp_var_191;
                    temp_var_191 = var_local_2_$tmp7.restrict(pc_44);
                    var_local_2_i = var_local_2_i.updateUnderGuard(pc_44, temp_var_191);

                }
            }
            if (loop_early_ret_4) {
                pc_43 = Guard.orMany(loop_exits_4);
            }

            PrimitiveVS<Machine> temp_var_192;
            temp_var_192 = (PrimitiveVS<Machine>)((var_pendingWrTrans.restrict(pc_43)).getField("client"));
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_43, temp_var_192);

            PrimitiveVS<Machine> temp_var_193;
            temp_var_193 = var_$tmp2.restrict(pc_43);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_43, temp_var_193);

            PrimitiveVS<Event> temp_var_194;
            temp_var_194 = new PrimitiveVS<Event>(eWriteTransResp).restrict(pc_43);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_43, temp_var_194);

            PrimitiveVS<Integer> temp_var_195;
            temp_var_195 = var_currTransId.restrict(pc_43);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_43, temp_var_195);

            PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_196;
            temp_var_196 = var_respStatus.restrict(pc_43);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_43, temp_var_196);

            NamedTupleVS temp_var_197;
            temp_var_197 = new NamedTupleVS("transId", var_$tmp5.restrict(pc_43), "status", var_$tmp6.restrict(pc_43));
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_43, temp_var_197);

            effects.send(pc_43, var_$tmp3.restrict(pc_43), var_$tmp4.restrict(pc_43), new UnionVS(var_$tmp7.restrict(pc_43)));

        }

        void
        DoGlobalCommit(
            Guard pc_47,
            EventBuffer effects
        ) {
            PrimitiveVS<Event> var_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_47);

            PrimitiveVS<Integer> var_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_47);

            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_47);

            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_47);

            PrimitiveVS<Event> var_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_47);

            PrimitiveVS<Integer> var_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_47);

            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp6 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_47);

            NamedTupleVS var_$tmp7 =
                new NamedTupleVS("transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_47);

            PrimitiveVS<Event> var_inline_4_message =
                new PrimitiveVS<Event>(_null).restrict(pc_47);

            PrimitiveVS<Integer> var_inline_4_payload =
                new PrimitiveVS<Integer>(0).restrict(pc_47);

            PrimitiveVS<Integer> var_local_4_i =
                new PrimitiveVS<Integer>(0).restrict(pc_47);

            PrimitiveVS<Integer> var_local_4_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_47);

            PrimitiveVS<Boolean> var_local_4_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_47);

            PrimitiveVS<Boolean> var_local_4_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_47);

            PrimitiveVS<Machine> var_local_4_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_47);

            PrimitiveVS<Machine> var_local_4_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_47);

            PrimitiveVS<Event> var_local_4_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_47);

            UnionVS var_local_4_$tmp6 =
                new UnionVS().restrict(pc_47);

            PrimitiveVS<Integer> var_local_4_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_47);

            PrimitiveVS<Event> temp_var_198;
            temp_var_198 = new PrimitiveVS<Event>(eCommitTrans).restrict(pc_47);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_47, temp_var_198);

            PrimitiveVS<Integer> temp_var_199;
            temp_var_199 = var_currTransId.restrict(pc_47);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_47, temp_var_199);

            PrimitiveVS<Event> temp_var_200;
            temp_var_200 = var_$tmp0.restrict(pc_47);
            var_inline_4_message = var_inline_4_message.updateUnderGuard(pc_47, temp_var_200);

            PrimitiveVS<Integer> temp_var_201;
            temp_var_201 = var_$tmp1.restrict(pc_47);
            var_inline_4_payload = var_inline_4_payload.updateUnderGuard(pc_47, temp_var_201);

            PrimitiveVS<Integer> temp_var_202;
            temp_var_202 = new PrimitiveVS<Integer>(0).restrict(pc_47);
            var_local_4_i = var_local_4_i.updateUnderGuard(pc_47, temp_var_202);

            java.util.List<Guard> loop_exits_5 = new java.util.ArrayList<>();
            boolean loop_early_ret_5 = false;
            Guard pc_48 = pc_47;
            while (!pc_48.isFalse()) {
                PrimitiveVS<Integer> temp_var_203;
                temp_var_203 = var_participants.restrict(pc_48).size();
                var_local_4_$tmp0 = var_local_4_$tmp0.updateUnderGuard(pc_48, temp_var_203);

                PrimitiveVS<Boolean> temp_var_204;
                temp_var_204 = (var_local_4_i.restrict(pc_48)).apply(var_local_4_$tmp0.restrict(pc_48), (temp_var_205, temp_var_206) -> temp_var_205 < temp_var_206);
                var_local_4_$tmp1 = var_local_4_$tmp1.updateUnderGuard(pc_48, temp_var_204);

                PrimitiveVS<Boolean> temp_var_207;
                temp_var_207 = var_local_4_$tmp1.restrict(pc_48);
                var_local_4_$tmp2 = var_local_4_$tmp2.updateUnderGuard(pc_48, temp_var_207);

                PrimitiveVS<Boolean> temp_var_208 = var_local_4_$tmp2.restrict(pc_48);
                Guard pc_49 = BooleanVS.getTrueGuard(temp_var_208);
                Guard pc_50 = BooleanVS.getFalseGuard(temp_var_208);
                boolean jumpedOut_24 = false;
                boolean jumpedOut_25 = false;
                if (!pc_49.isFalse()) {
                    // 'then' branch
                }
                if (!pc_50.isFalse()) {
                    // 'else' branch
                    loop_exits_5.add(pc_50);
                    jumpedOut_25 = true;
                    pc_50 = Guard.constFalse();

                }
                if (jumpedOut_24 || jumpedOut_25) {
                    pc_48 = pc_49.or(pc_50);
                }

                if (!pc_48.isFalse()) {
                    PrimitiveVS<Machine> temp_var_209;
                    temp_var_209 = var_participants.restrict(pc_48).get(var_local_4_i.restrict(pc_48));
                    var_local_4_$tmp3 = var_local_4_$tmp3.updateUnderGuard(pc_48, temp_var_209);

                    PrimitiveVS<Machine> temp_var_210;
                    temp_var_210 = var_local_4_$tmp3.restrict(pc_48);
                    var_local_4_$tmp4 = var_local_4_$tmp4.updateUnderGuard(pc_48, temp_var_210);

                    PrimitiveVS<Event> temp_var_211;
                    temp_var_211 = var_inline_4_message.restrict(pc_48);
                    var_local_4_$tmp5 = var_local_4_$tmp5.updateUnderGuard(pc_48, temp_var_211);

                    UnionVS temp_var_212;
                    temp_var_212 = ValueSummary.castToAny(pc_48, var_inline_4_payload.restrict(pc_48));
                    var_local_4_$tmp6 = var_local_4_$tmp6.updateUnderGuard(pc_48, temp_var_212);

                    effects.send(pc_48, var_local_4_$tmp4.restrict(pc_48), var_local_4_$tmp5.restrict(pc_48), new UnionVS(var_local_4_$tmp6.restrict(pc_48)));

                    PrimitiveVS<Integer> temp_var_213;
                    temp_var_213 = (var_local_4_i.restrict(pc_48)).apply(new PrimitiveVS<Integer>(1).restrict(pc_48), (temp_var_214, temp_var_215) -> temp_var_214 + temp_var_215);
                    var_local_4_$tmp7 = var_local_4_$tmp7.updateUnderGuard(pc_48, temp_var_213);

                    PrimitiveVS<Integer> temp_var_216;
                    temp_var_216 = var_local_4_$tmp7.restrict(pc_48);
                    var_local_4_i = var_local_4_i.updateUnderGuard(pc_48, temp_var_216);

                }
            }
            if (loop_early_ret_5) {
                pc_47 = Guard.orMany(loop_exits_5);
            }

            PrimitiveVS<Machine> temp_var_217;
            temp_var_217 = (PrimitiveVS<Machine>)((var_pendingWrTrans.restrict(pc_47)).getField("client"));
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_47, temp_var_217);

            PrimitiveVS<Machine> temp_var_218;
            temp_var_218 = var_$tmp2.restrict(pc_47);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_47, temp_var_218);

            PrimitiveVS<Event> temp_var_219;
            temp_var_219 = new PrimitiveVS<Event>(eWriteTransResp).restrict(pc_47);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_47, temp_var_219);

            PrimitiveVS<Integer> temp_var_220;
            temp_var_220 = var_currTransId.restrict(pc_47);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_47, temp_var_220);

            PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_221;
            temp_var_221 = new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_47);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_47, temp_var_221);

            NamedTupleVS temp_var_222;
            temp_var_222 = new NamedTupleVS("transId", var_$tmp5.restrict(pc_47), "status", var_$tmp6.restrict(pc_47));
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_47, temp_var_222);

            effects.send(pc_47, var_$tmp3.restrict(pc_47), var_$tmp4.restrict(pc_47), new UnionVS(var_$tmp7.restrict(pc_47)));

        }

        void
        BroadcastToAllParticipants(
            Guard pc_51,
            EventBuffer effects,
            PrimitiveVS<Event> var_message,
            UnionVS var_payload
        ) {
            PrimitiveVS<Integer> var_i =
                new PrimitiveVS<Integer>(0).restrict(pc_51);

            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_51);

            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_51);

            PrimitiveVS<Boolean> var_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_51);

            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_51);

            PrimitiveVS<Machine> var_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_51);

            PrimitiveVS<Event> var_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_51);

            UnionVS var_$tmp6 =
                new UnionVS().restrict(pc_51);

            PrimitiveVS<Integer> var_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_51);

            PrimitiveVS<Integer> temp_var_223;
            temp_var_223 = new PrimitiveVS<Integer>(0).restrict(pc_51);
            var_i = var_i.updateUnderGuard(pc_51, temp_var_223);

            java.util.List<Guard> loop_exits_6 = new java.util.ArrayList<>();
            boolean loop_early_ret_6 = false;
            Guard pc_52 = pc_51;
            while (!pc_52.isFalse()) {
                PrimitiveVS<Integer> temp_var_224;
                temp_var_224 = var_participants.restrict(pc_52).size();
                var_$tmp0 = var_$tmp0.updateUnderGuard(pc_52, temp_var_224);

                PrimitiveVS<Boolean> temp_var_225;
                temp_var_225 = (var_i.restrict(pc_52)).apply(var_$tmp0.restrict(pc_52), (temp_var_226, temp_var_227) -> temp_var_226 < temp_var_227);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_52, temp_var_225);

                PrimitiveVS<Boolean> temp_var_228;
                temp_var_228 = var_$tmp1.restrict(pc_52);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_52, temp_var_228);

                PrimitiveVS<Boolean> temp_var_229 = var_$tmp2.restrict(pc_52);
                Guard pc_53 = BooleanVS.getTrueGuard(temp_var_229);
                Guard pc_54 = BooleanVS.getFalseGuard(temp_var_229);
                boolean jumpedOut_26 = false;
                boolean jumpedOut_27 = false;
                if (!pc_53.isFalse()) {
                    // 'then' branch
                }
                if (!pc_54.isFalse()) {
                    // 'else' branch
                    loop_exits_6.add(pc_54);
                    jumpedOut_27 = true;
                    pc_54 = Guard.constFalse();

                }
                if (jumpedOut_26 || jumpedOut_27) {
                    pc_52 = pc_53.or(pc_54);
                }

                if (!pc_52.isFalse()) {
                    PrimitiveVS<Machine> temp_var_230;
                    temp_var_230 = var_participants.restrict(pc_52).get(var_i.restrict(pc_52));
                    var_$tmp3 = var_$tmp3.updateUnderGuard(pc_52, temp_var_230);

                    PrimitiveVS<Machine> temp_var_231;
                    temp_var_231 = var_$tmp3.restrict(pc_52);
                    var_$tmp4 = var_$tmp4.updateUnderGuard(pc_52, temp_var_231);

                    PrimitiveVS<Event> temp_var_232;
                    temp_var_232 = var_message.restrict(pc_52);
                    var_$tmp5 = var_$tmp5.updateUnderGuard(pc_52, temp_var_232);

                    UnionVS temp_var_233;
                    temp_var_233 = ValueSummary.castToAny(pc_52, var_payload.restrict(pc_52));
                    var_$tmp6 = var_$tmp6.updateUnderGuard(pc_52, temp_var_233);

                    effects.send(pc_52, var_$tmp4.restrict(pc_52), var_$tmp5.restrict(pc_52), new UnionVS(var_$tmp6.restrict(pc_52)));

                    PrimitiveVS<Integer> temp_var_234;
                    temp_var_234 = (var_i.restrict(pc_52)).apply(new PrimitiveVS<Integer>(1).restrict(pc_52), (temp_var_235, temp_var_236) -> temp_var_235 + temp_var_236);
                    var_$tmp7 = var_$tmp7.updateUnderGuard(pc_52, temp_var_234);

                    PrimitiveVS<Integer> temp_var_237;
                    temp_var_237 = var_$tmp7.restrict(pc_52);
                    var_i = var_i.updateUnderGuard(pc_52, temp_var_237);

                }
            }
            if (loop_early_ret_6) {
                pc_51 = Guard.orMany(loop_exits_6);
            }

        }

    }

    public static class Participant extends Machine {

        static State Init = new State("Init") {
            @Override public void entry(Guard pc_55, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Participant)machine).anonfun_12(pc_55, machine.sendBuffer, outcome);
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
            Guard pc_56,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            outcome.addGuardedGoto(pc_56, WaitForRequests);
            pc_56 = Guard.constFalse();

            return pc_56;
        }

        void
        anonfun_13(
            Guard pc_57,
            EventBuffer effects,
            PrimitiveVS<Integer> var_transId
        ) {
            PrimitiveVS<Boolean> var_$tmp0 =
                new PrimitiveVS<Boolean>(false).restrict(pc_57);

            PrimitiveVS<Integer> var_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_57);

            MapVS<Integer, NamedTupleVS> var_$tmp2 =
                new MapVS<Integer, NamedTupleVS>(Guard.constTrue()).restrict(pc_57);

            PrimitiveVS<String> var_$tmp3 =
                new PrimitiveVS<String>("").restrict(pc_57);

            PrimitiveVS<Boolean> temp_var_238;
            temp_var_238 = var_pendingWriteTrans.restrict(pc_57).containsKey(var_transId.restrict(pc_57));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_57, temp_var_238);

            PrimitiveVS<Integer> temp_var_239;
            temp_var_239 = var_transId.restrict(pc_57);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_57, temp_var_239);

            MapVS<Integer, NamedTupleVS> temp_var_240;
            temp_var_240 = var_pendingWriteTrans.restrict(pc_57);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_57, temp_var_240);

            PrimitiveVS<String> temp_var_241;
            temp_var_241 = new PrimitiveVS<String>(String.format("Abort request for a non-pending transaction, transId: {0}, pendingTrans: {1}", var_$tmp1.restrict(pc_57), var_$tmp2.restrict(pc_57))).restrict(pc_57);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_57, temp_var_241);

            Assert.progProp(!(var_$tmp0.restrict(pc_57)).getValues().contains(Boolean.FALSE), var_$tmp3.restrict(pc_57), scheduler, var_$tmp0.restrict(pc_57).getGuardFor(Boolean.FALSE));
            MapVS<Integer, NamedTupleVS> temp_var_242 = var_pendingWriteTrans.restrict(pc_57);
            temp_var_242 = var_pendingWriteTrans.restrict(pc_57).remove(var_transId.restrict(pc_57));
            var_pendingWriteTrans = var_pendingWriteTrans.updateUnderGuard(pc_57, temp_var_242);

        }

        void
        anonfun_14(
            Guard pc_58,
            EventBuffer effects,
            PrimitiveVS<Integer> var_transId
        ) {
            NamedTupleVS var_transaction =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_58);

            PrimitiveVS<Boolean> var_$tmp0 =
                new PrimitiveVS<Boolean>(false).restrict(pc_58);

            PrimitiveVS<Integer> var_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_58);

            MapVS<Integer, NamedTupleVS> var_$tmp2 =
                new MapVS<Integer, NamedTupleVS>(Guard.constTrue()).restrict(pc_58);

            PrimitiveVS<String> var_$tmp3 =
                new PrimitiveVS<String>("").restrict(pc_58);

            NamedTupleVS var_$tmp4 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_58);

            NamedTupleVS var_$tmp5 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_58);

            PrimitiveVS<Integer> var_$tmp6 =
                new PrimitiveVS<Integer>(0).restrict(pc_58);

            PrimitiveVS<Integer> var_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_58);

            PrimitiveVS<Integer> var_$tmp8 =
                new PrimitiveVS<Integer>(0).restrict(pc_58);

            PrimitiveVS<Boolean> temp_var_243;
            temp_var_243 = var_pendingWriteTrans.restrict(pc_58).containsKey(var_transId.restrict(pc_58));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_58, temp_var_243);

            PrimitiveVS<Integer> temp_var_244;
            temp_var_244 = var_transId.restrict(pc_58);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_58, temp_var_244);

            MapVS<Integer, NamedTupleVS> temp_var_245;
            temp_var_245 = var_pendingWriteTrans.restrict(pc_58);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_58, temp_var_245);

            PrimitiveVS<String> temp_var_246;
            temp_var_246 = new PrimitiveVS<String>(String.format("Commit request for a non-pending transaction, transId: {0}, pendingTrans: {1}", var_$tmp1.restrict(pc_58), var_$tmp2.restrict(pc_58))).restrict(pc_58);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_58, temp_var_246);

            Assert.progProp(!(var_$tmp0.restrict(pc_58)).getValues().contains(Boolean.FALSE), var_$tmp3.restrict(pc_58), scheduler, var_$tmp0.restrict(pc_58).getGuardFor(Boolean.FALSE));
            NamedTupleVS temp_var_247;
            temp_var_247 = var_pendingWriteTrans.restrict(pc_58).get(var_transId.restrict(pc_58));
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_58, temp_var_247);

            NamedTupleVS temp_var_248;
            temp_var_248 = var_$tmp4.restrict(pc_58);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_58, temp_var_248);

            NamedTupleVS temp_var_249;
            temp_var_249 = var_$tmp5.restrict(pc_58);
            var_transaction = var_transaction.updateUnderGuard(pc_58, temp_var_249);

            PrimitiveVS<Integer> temp_var_250;
            temp_var_250 = (PrimitiveVS<Integer>)((var_transaction.restrict(pc_58)).getField("key"));
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_58, temp_var_250);

            PrimitiveVS<Integer> temp_var_251;
            temp_var_251 = (PrimitiveVS<Integer>)((var_transaction.restrict(pc_58)).getField("val"));
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_58, temp_var_251);

            PrimitiveVS<Integer> temp_var_252;
            temp_var_252 = var_$tmp7.restrict(pc_58);
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_58, temp_var_252);

            MapVS<Integer, PrimitiveVS<Integer>> temp_var_253 = var_kvStore.restrict(pc_58);
            PrimitiveVS<Integer> temp_var_255 = var_$tmp6.restrict(pc_58);
            PrimitiveVS<Integer> temp_var_254;
            temp_var_254 = var_$tmp8.restrict(pc_58);
            temp_var_253 = temp_var_253.put(temp_var_255, temp_var_254);
            var_kvStore = var_kvStore.updateUnderGuard(pc_58, temp_var_253);

        }

        void
        anonfun_15(
            Guard pc_59,
            EventBuffer effects,
            NamedTupleVS var_prepareReq
        ) {
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_59);

            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_59);

            PrimitiveVS<Boolean> var_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_59);

            PrimitiveVS<Integer> var_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_59);

            MapVS<Integer, NamedTupleVS> var_$tmp4 =
                new MapVS<Integer, NamedTupleVS>(Guard.constTrue()).restrict(pc_59);

            PrimitiveVS<String> var_$tmp5 =
                new PrimitiveVS<String>("").restrict(pc_59);

            PrimitiveVS<Integer> var_$tmp6 =
                new PrimitiveVS<Integer>(0).restrict(pc_59);

            NamedTupleVS var_$tmp7 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_59);

            NamedTupleVS var_$tmp8 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_59);

            PrimitiveVS<Boolean> var_$tmp9 =
                new PrimitiveVS<Boolean>(false).restrict(pc_59);

            PrimitiveVS<Machine> var_$tmp10 =
                new PrimitiveVS<Machine>().restrict(pc_59);

            PrimitiveVS<Machine> var_$tmp11 =
                new PrimitiveVS<Machine>().restrict(pc_59);

            PrimitiveVS<Event> var_$tmp12 =
                new PrimitiveVS<Event>(_null).restrict(pc_59);

            PrimitiveVS<Machine> var_$tmp13 =
                new PrimitiveVS<Machine>().restrict(pc_59);

            PrimitiveVS<Integer> var_$tmp14 =
                new PrimitiveVS<Integer>(0).restrict(pc_59);

            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp15 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_59);

            NamedTupleVS var_$tmp16 =
                new NamedTupleVS("participant", new PrimitiveVS<Machine>(), "transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_59);

            PrimitiveVS<Machine> var_$tmp17 =
                new PrimitiveVS<Machine>().restrict(pc_59);

            PrimitiveVS<Machine> var_$tmp18 =
                new PrimitiveVS<Machine>().restrict(pc_59);

            PrimitiveVS<Event> var_$tmp19 =
                new PrimitiveVS<Event>(_null).restrict(pc_59);

            PrimitiveVS<Machine> var_$tmp20 =
                new PrimitiveVS<Machine>().restrict(pc_59);

            PrimitiveVS<Integer> var_$tmp21 =
                new PrimitiveVS<Integer>(0).restrict(pc_59);

            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp22 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_59);

            NamedTupleVS var_$tmp23 =
                new NamedTupleVS("participant", new PrimitiveVS<Machine>(), "transId", new PrimitiveVS<Integer>(0), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_59);

            PrimitiveVS<Integer> temp_var_256;
            temp_var_256 = (PrimitiveVS<Integer>)((var_prepareReq.restrict(pc_59)).getField("transId"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_59, temp_var_256);

            PrimitiveVS<Boolean> temp_var_257;
            temp_var_257 = var_pendingWriteTrans.restrict(pc_59).containsKey(var_$tmp0.restrict(pc_59));
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_59, temp_var_257);

            PrimitiveVS<Boolean> temp_var_258;
            temp_var_258 = (var_$tmp1.restrict(pc_59)).apply((temp_var_259) -> !temp_var_259);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_59, temp_var_258);

            PrimitiveVS<Integer> temp_var_260;
            temp_var_260 = (PrimitiveVS<Integer>)((var_prepareReq.restrict(pc_59)).getField("transId"));
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_59, temp_var_260);

            MapVS<Integer, NamedTupleVS> temp_var_261;
            temp_var_261 = var_pendingWriteTrans.restrict(pc_59);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_59, temp_var_261);

            PrimitiveVS<String> temp_var_262;
            temp_var_262 = new PrimitiveVS<String>(String.format("Duplicate transaction ids not allowed!, received transId: {0}, pending transactions: {1}", var_$tmp3.restrict(pc_59), var_$tmp4.restrict(pc_59))).restrict(pc_59);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_59, temp_var_262);

            Assert.progProp(!(var_$tmp2.restrict(pc_59)).getValues().contains(Boolean.FALSE), var_$tmp5.restrict(pc_59), scheduler, var_$tmp2.restrict(pc_59).getGuardFor(Boolean.FALSE));
            PrimitiveVS<Integer> temp_var_263;
            temp_var_263 = (PrimitiveVS<Integer>)((var_prepareReq.restrict(pc_59)).getField("transId"));
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_59, temp_var_263);

            NamedTupleVS temp_var_264;
            temp_var_264 = (NamedTupleVS)((var_prepareReq.restrict(pc_59)).getField("rec"));
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_59, temp_var_264);

            NamedTupleVS temp_var_265;
            temp_var_265 = var_$tmp7.restrict(pc_59);
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_59, temp_var_265);

            MapVS<Integer, NamedTupleVS> temp_var_266 = var_pendingWriteTrans.restrict(pc_59);
            PrimitiveVS<Integer> temp_var_268 = var_$tmp6.restrict(pc_59);
            NamedTupleVS temp_var_267;
            temp_var_267 = var_$tmp8.restrict(pc_59);
            temp_var_266 = temp_var_266.put(temp_var_268, temp_var_267);
            var_pendingWriteTrans = var_pendingWriteTrans.updateUnderGuard(pc_59, temp_var_266);

            PrimitiveVS<Boolean> temp_var_269;
            temp_var_269 = scheduler.getNextBoolean(pc_59);
            var_$tmp9 = var_$tmp9.updateUnderGuard(pc_59, temp_var_269);

            PrimitiveVS<Boolean> temp_var_270 = var_$tmp9.restrict(pc_59);
            Guard pc_60 = BooleanVS.getTrueGuard(temp_var_270);
            Guard pc_61 = BooleanVS.getFalseGuard(temp_var_270);
            boolean jumpedOut_28 = false;
            boolean jumpedOut_29 = false;
            if (!pc_60.isFalse()) {
                // 'then' branch
                PrimitiveVS<Machine> temp_var_271;
                temp_var_271 = (PrimitiveVS<Machine>)((var_prepareReq.restrict(pc_60)).getField("coordinator"));
                var_$tmp10 = var_$tmp10.updateUnderGuard(pc_60, temp_var_271);

                PrimitiveVS<Machine> temp_var_272;
                temp_var_272 = var_$tmp10.restrict(pc_60);
                var_$tmp11 = var_$tmp11.updateUnderGuard(pc_60, temp_var_272);

                PrimitiveVS<Event> temp_var_273;
                temp_var_273 = new PrimitiveVS<Event>(ePrepareResp).restrict(pc_60);
                var_$tmp12 = var_$tmp12.updateUnderGuard(pc_60, temp_var_273);

                PrimitiveVS<Machine> temp_var_274;
                temp_var_274 = new PrimitiveVS<Machine>(this).restrict(pc_60);
                var_$tmp13 = var_$tmp13.updateUnderGuard(pc_60, temp_var_274);

                PrimitiveVS<Integer> temp_var_275;
                temp_var_275 = (PrimitiveVS<Integer>)((var_prepareReq.restrict(pc_60)).getField("transId"));
                var_$tmp14 = var_$tmp14.updateUnderGuard(pc_60, temp_var_275);

                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_276;
                temp_var_276 = new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_60);
                var_$tmp15 = var_$tmp15.updateUnderGuard(pc_60, temp_var_276);

                NamedTupleVS temp_var_277;
                temp_var_277 = new NamedTupleVS("participant", var_$tmp13.restrict(pc_60), "transId", var_$tmp14.restrict(pc_60), "status", var_$tmp15.restrict(pc_60));
                var_$tmp16 = var_$tmp16.updateUnderGuard(pc_60, temp_var_277);

                effects.send(pc_60, var_$tmp11.restrict(pc_60), var_$tmp12.restrict(pc_60), new UnionVS(var_$tmp16.restrict(pc_60)));

            }
            if (!pc_61.isFalse()) {
                // 'else' branch
                PrimitiveVS<Machine> temp_var_278;
                temp_var_278 = (PrimitiveVS<Machine>)((var_prepareReq.restrict(pc_61)).getField("coordinator"));
                var_$tmp17 = var_$tmp17.updateUnderGuard(pc_61, temp_var_278);

                PrimitiveVS<Machine> temp_var_279;
                temp_var_279 = var_$tmp17.restrict(pc_61);
                var_$tmp18 = var_$tmp18.updateUnderGuard(pc_61, temp_var_279);

                PrimitiveVS<Event> temp_var_280;
                temp_var_280 = new PrimitiveVS<Event>(ePrepareResp).restrict(pc_61);
                var_$tmp19 = var_$tmp19.updateUnderGuard(pc_61, temp_var_280);

                PrimitiveVS<Machine> temp_var_281;
                temp_var_281 = new PrimitiveVS<Machine>(this).restrict(pc_61);
                var_$tmp20 = var_$tmp20.updateUnderGuard(pc_61, temp_var_281);

                PrimitiveVS<Integer> temp_var_282;
                temp_var_282 = (PrimitiveVS<Integer>)((var_prepareReq.restrict(pc_61)).getField("transId"));
                var_$tmp21 = var_$tmp21.updateUnderGuard(pc_61, temp_var_282);

                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_283;
                temp_var_283 = new PrimitiveVS<Integer>(1 /* enum tTransStatus elem ERROR */).restrict(pc_61);
                var_$tmp22 = var_$tmp22.updateUnderGuard(pc_61, temp_var_283);

                NamedTupleVS temp_var_284;
                temp_var_284 = new NamedTupleVS("participant", var_$tmp20.restrict(pc_61), "transId", var_$tmp21.restrict(pc_61), "status", var_$tmp22.restrict(pc_61));
                var_$tmp23 = var_$tmp23.updateUnderGuard(pc_61, temp_var_284);

                effects.send(pc_61, var_$tmp18.restrict(pc_61), var_$tmp19.restrict(pc_61), new UnionVS(var_$tmp23.restrict(pc_61)));

            }
            if (jumpedOut_28 || jumpedOut_29) {
                pc_59 = pc_60.or(pc_61);
            }

        }

        void
        anonfun_16(
            Guard pc_62,
            EventBuffer effects,
            NamedTupleVS var_req
        ) {
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_62);

            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_62);

            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_62);

            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_62);

            PrimitiveVS<Event> var_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_62);

            PrimitiveVS<Integer> var_$tmp5 =
                new PrimitiveVS<Integer>(0).restrict(pc_62);

            PrimitiveVS<Integer> var_$tmp6 =
                new PrimitiveVS<Integer>(0).restrict(pc_62);

            PrimitiveVS<Integer> var_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_62);

            NamedTupleVS var_$tmp8 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_62);

            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp9 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_62);

            NamedTupleVS var_$tmp10 =
                new NamedTupleVS("rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_62);

            PrimitiveVS<Machine> var_$tmp11 =
                new PrimitiveVS<Machine>().restrict(pc_62);

            PrimitiveVS<Machine> var_$tmp12 =
                new PrimitiveVS<Machine>().restrict(pc_62);

            PrimitiveVS<Event> var_$tmp13 =
                new PrimitiveVS<Event>(_null).restrict(pc_62);

            NamedTupleVS var_$tmp14 =
                new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_62);

            PrimitiveVS<Integer> /* enum tTransStatus */ var_$tmp15 =
                new PrimitiveVS<Integer> /* enum tTransStatus */(0).restrict(pc_62);

            NamedTupleVS var_$tmp16 =
                new NamedTupleVS("rec", new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)), "status", new PrimitiveVS<Integer> /* enum tTransStatus */(0)).restrict(pc_62);

            PrimitiveVS<Integer> temp_var_285;
            temp_var_285 = (PrimitiveVS<Integer>)((var_req.restrict(pc_62)).getField("key"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_62, temp_var_285);

            PrimitiveVS<Boolean> temp_var_286;
            temp_var_286 = var_kvStore.restrict(pc_62).containsKey(var_$tmp0.restrict(pc_62));
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_62, temp_var_286);

            PrimitiveVS<Boolean> temp_var_287 = var_$tmp1.restrict(pc_62);
            Guard pc_63 = BooleanVS.getTrueGuard(temp_var_287);
            Guard pc_64 = BooleanVS.getFalseGuard(temp_var_287);
            boolean jumpedOut_30 = false;
            boolean jumpedOut_31 = false;
            if (!pc_63.isFalse()) {
                // 'then' branch
                PrimitiveVS<Machine> temp_var_288;
                temp_var_288 = (PrimitiveVS<Machine>)((var_req.restrict(pc_63)).getField("client"));
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_63, temp_var_288);

                PrimitiveVS<Machine> temp_var_289;
                temp_var_289 = var_$tmp2.restrict(pc_63);
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_63, temp_var_289);

                PrimitiveVS<Event> temp_var_290;
                temp_var_290 = new PrimitiveVS<Event>(eReadTransResp).restrict(pc_63);
                var_$tmp4 = var_$tmp4.updateUnderGuard(pc_63, temp_var_290);

                PrimitiveVS<Integer> temp_var_291;
                temp_var_291 = (PrimitiveVS<Integer>)((var_req.restrict(pc_63)).getField("key"));
                var_$tmp5 = var_$tmp5.updateUnderGuard(pc_63, temp_var_291);

                PrimitiveVS<Integer> temp_var_292;
                temp_var_292 = (PrimitiveVS<Integer>)((var_req.restrict(pc_63)).getField("key"));
                var_$tmp6 = var_$tmp6.updateUnderGuard(pc_63, temp_var_292);

                PrimitiveVS<Integer> temp_var_293;
                temp_var_293 = var_kvStore.restrict(pc_63).get(var_$tmp6.restrict(pc_63));
                var_$tmp7 = var_$tmp7.updateUnderGuard(pc_63, temp_var_293);

                NamedTupleVS temp_var_294;
                temp_var_294 = new NamedTupleVS("key", var_$tmp5.restrict(pc_63), "val", var_$tmp7.restrict(pc_63));
                var_$tmp8 = var_$tmp8.updateUnderGuard(pc_63, temp_var_294);

                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_295;
                temp_var_295 = new PrimitiveVS<Integer>(0 /* enum tTransStatus elem SUCCESS */).restrict(pc_63);
                var_$tmp9 = var_$tmp9.updateUnderGuard(pc_63, temp_var_295);

                NamedTupleVS temp_var_296;
                temp_var_296 = new NamedTupleVS("rec", var_$tmp8.restrict(pc_63), "status", var_$tmp9.restrict(pc_63));
                var_$tmp10 = var_$tmp10.updateUnderGuard(pc_63, temp_var_296);

                effects.send(pc_63, var_$tmp3.restrict(pc_63), var_$tmp4.restrict(pc_63), new UnionVS(var_$tmp10.restrict(pc_63)));

            }
            if (!pc_64.isFalse()) {
                // 'else' branch
                PrimitiveVS<Machine> temp_var_297;
                temp_var_297 = (PrimitiveVS<Machine>)((var_req.restrict(pc_64)).getField("client"));
                var_$tmp11 = var_$tmp11.updateUnderGuard(pc_64, temp_var_297);

                PrimitiveVS<Machine> temp_var_298;
                temp_var_298 = var_$tmp11.restrict(pc_64);
                var_$tmp12 = var_$tmp12.updateUnderGuard(pc_64, temp_var_298);

                PrimitiveVS<Event> temp_var_299;
                temp_var_299 = new PrimitiveVS<Event>(eReadTransResp).restrict(pc_64);
                var_$tmp13 = var_$tmp13.updateUnderGuard(pc_64, temp_var_299);

                NamedTupleVS temp_var_300;
                temp_var_300 = new NamedTupleVS("key", new PrimitiveVS<Integer>(0), "val", new PrimitiveVS<Integer>(0)).restrict(pc_64);
                var_$tmp14 = var_$tmp14.updateUnderGuard(pc_64, temp_var_300);

                PrimitiveVS<Integer> /* enum tTransStatus */ temp_var_301;
                temp_var_301 = new PrimitiveVS<Integer>(1 /* enum tTransStatus elem ERROR */).restrict(pc_64);
                var_$tmp15 = var_$tmp15.updateUnderGuard(pc_64, temp_var_301);

                NamedTupleVS temp_var_302;
                temp_var_302 = new NamedTupleVS("rec", var_$tmp14.restrict(pc_64), "status", var_$tmp15.restrict(pc_64));
                var_$tmp16 = var_$tmp16.updateUnderGuard(pc_64, temp_var_302);

                effects.send(pc_64, var_$tmp12.restrict(pc_64), var_$tmp13.restrict(pc_64), new UnionVS(var_$tmp16.restrict(pc_64)));

            }
            if (jumpedOut_30 || jumpedOut_31) {
                pc_62 = pc_63.or(pc_64);
            }

        }

    }

    public static class Main extends Machine {

        static State Init = new State("Init") {
            @Override public void entry(Guard pc_65, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Main)machine).anonfun_17(pc_65, machine.sendBuffer);
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
            Guard pc_66,
            EventBuffer effects
        ) {
            PrimitiveVS<Machine> var_coord =
                new PrimitiveVS<Machine>().restrict(pc_66);

            ListVS<PrimitiveVS<Machine>> var_participants =
                new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_66);

            PrimitiveVS<Integer> var_i =
                new PrimitiveVS<Integer>(0).restrict(pc_66);

            PrimitiveVS<Boolean> var_$tmp0 =
                new PrimitiveVS<Boolean>(false).restrict(pc_66);

            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_66);

            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_66);

            PrimitiveVS<Integer> var_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_66);

            ListVS<PrimitiveVS<Machine>> var_$tmp4 =
                new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_66);

            PrimitiveVS<Machine> var_$tmp5 =
                new PrimitiveVS<Machine>().restrict(pc_66);

            PrimitiveVS<Machine> var_$tmp6 =
                new PrimitiveVS<Machine>().restrict(pc_66);

            PrimitiveVS<Integer> var_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_66);

            NamedTupleVS var_$tmp8 =
                new NamedTupleVS("coor", new PrimitiveVS<Machine>(), "n", new PrimitiveVS<Integer>(0)).restrict(pc_66);

            PrimitiveVS<Machine> var_$tmp9 =
                new PrimitiveVS<Machine>().restrict(pc_66);

            PrimitiveVS<Integer> var_$tmp10 =
                new PrimitiveVS<Integer>(0).restrict(pc_66);

            NamedTupleVS var_$tmp11 =
                new NamedTupleVS("coor", new PrimitiveVS<Machine>(), "n", new PrimitiveVS<Integer>(0)).restrict(pc_66);

            java.util.List<Guard> loop_exits_7 = new java.util.ArrayList<>();
            boolean loop_early_ret_7 = false;
            Guard pc_67 = pc_66;
            while (!pc_67.isFalse()) {
                PrimitiveVS<Boolean> temp_var_303;
                temp_var_303 = (var_i.restrict(pc_67)).apply(new PrimitiveVS<Integer>(2).restrict(pc_67), (temp_var_304, temp_var_305) -> temp_var_304 < temp_var_305);
                var_$tmp0 = var_$tmp0.updateUnderGuard(pc_67, temp_var_303);

                PrimitiveVS<Boolean> temp_var_306;
                temp_var_306 = var_$tmp0.restrict(pc_67);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_67, temp_var_306);

                PrimitiveVS<Boolean> temp_var_307 = var_$tmp1.restrict(pc_67);
                Guard pc_68 = BooleanVS.getTrueGuard(temp_var_307);
                Guard pc_69 = BooleanVS.getFalseGuard(temp_var_307);
                boolean jumpedOut_32 = false;
                boolean jumpedOut_33 = false;
                if (!pc_68.isFalse()) {
                    // 'then' branch
                }
                if (!pc_69.isFalse()) {
                    // 'else' branch
                    loop_exits_7.add(pc_69);
                    jumpedOut_33 = true;
                    pc_69 = Guard.constFalse();

                }
                if (jumpedOut_32 || jumpedOut_33) {
                    pc_67 = pc_68.or(pc_69);
                }

                if (!pc_67.isFalse()) {
                    PrimitiveVS<Machine> temp_var_308;
                    temp_var_308 = effects.create(pc_67, scheduler, Participant.class, (i) -> new Participant(i));
                    var_$tmp2 = var_$tmp2.updateUnderGuard(pc_67, temp_var_308);

                    ListVS<PrimitiveVS<Machine>> temp_var_309 = var_participants.restrict(pc_67);
                    temp_var_309 = var_participants.restrict(pc_67).insert(var_i.restrict(pc_67), var_$tmp2.restrict(pc_67));
                    var_participants = var_participants.updateUnderGuard(pc_67, temp_var_309);

                    PrimitiveVS<Integer> temp_var_310;
                    temp_var_310 = (var_i.restrict(pc_67)).apply(new PrimitiveVS<Integer>(1).restrict(pc_67), (temp_var_311, temp_var_312) -> temp_var_311 + temp_var_312);
                    var_$tmp3 = var_$tmp3.updateUnderGuard(pc_67, temp_var_310);

                    PrimitiveVS<Integer> temp_var_313;
                    temp_var_313 = var_$tmp3.restrict(pc_67);
                    var_i = var_i.updateUnderGuard(pc_67, temp_var_313);

                }
            }
            if (loop_early_ret_7) {
                pc_66 = Guard.orMany(loop_exits_7);
            }

            ListVS<PrimitiveVS<Machine>> temp_var_314;
            temp_var_314 = var_participants.restrict(pc_66);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_66, temp_var_314);

            PrimitiveVS<Machine> temp_var_315;
            temp_var_315 = effects.create(pc_66, scheduler, Coordinator.class, new UnionVS (var_$tmp4.restrict(pc_66)), (i) -> new Coordinator(i));
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_66, temp_var_315);

            PrimitiveVS<Machine> temp_var_316;
            temp_var_316 = var_$tmp5.restrict(pc_66);
            var_coord = var_coord.updateUnderGuard(pc_66, temp_var_316);

            PrimitiveVS<Machine> temp_var_317;
            temp_var_317 = var_coord.restrict(pc_66);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_66, temp_var_317);

            PrimitiveVS<Integer> temp_var_318;
            temp_var_318 = new PrimitiveVS<Integer>(2).restrict(pc_66);
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_66, temp_var_318);

            NamedTupleVS temp_var_319;
            temp_var_319 = new NamedTupleVS("coor", var_$tmp6.restrict(pc_66), "n", var_$tmp7.restrict(pc_66));
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_66, temp_var_319);

            effects.create(pc_66, scheduler, Client.class, new UnionVS (var_$tmp8.restrict(pc_66)), (i) -> new Client(i));

            PrimitiveVS<Machine> temp_var_320;
            temp_var_320 = var_coord.restrict(pc_66);
            var_$tmp9 = var_$tmp9.updateUnderGuard(pc_66, temp_var_320);

            PrimitiveVS<Integer> temp_var_321;
            temp_var_321 = new PrimitiveVS<Integer>(2).restrict(pc_66);
            var_$tmp10 = var_$tmp10.updateUnderGuard(pc_66, temp_var_321);

            NamedTupleVS temp_var_322;
            temp_var_322 = new NamedTupleVS("coor", var_$tmp9.restrict(pc_66), "n", var_$tmp10.restrict(pc_66));
            var_$tmp11 = var_$tmp11.updateUnderGuard(pc_66, temp_var_322);

            effects.create(pc_66, scheduler, Client.class, new UnionVS (var_$tmp11.restrict(pc_66)), (i) -> new Client(i));

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
