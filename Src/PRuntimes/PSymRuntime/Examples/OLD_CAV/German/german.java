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

public class german implements Program {

    public static Scheduler scheduler;

    @Override
    public void setScheduler (Scheduler s) { this.scheduler = s; }



    public static Event _null = new Event("_null");
    public static Event _halt = new Event("_halt");
    public static Event unit = new Event("unit");
    public static Event req_share = new Event("req_share");
    public static Event req_excl = new Event("req_excl");
    public static Event need_invalidate = new Event("need_invalidate");
    public static Event invalidate_ack = new Event("invalidate_ack");
    public static Event grant = new Event("grant");
    public static Event ask_share = new Event("ask_share");
    public static Event ask_excl = new Event("ask_excl");
    public static Event invalidate = new Event("invalidate");
    public static Event grant_excl = new Event("grant_excl");
    public static Event grant_share = new Event("grant_share");
    public static Event normal = new Event("normal");
    public static Event wait = new Event("wait");
    public static Event invalidate_sharers = new Event("invalidate_sharers");
    public static Event sharer_id = new Event("sharer_id");
    // Skipping Interface 'Main'

    // Skipping Interface 'Client'

    // Skipping Interface 'CPU'

    public static class Main extends Machine {

        static State init = new State("init") {
            @Override public void entry(Guard pc_0, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Main)machine).anonfun_0(pc_0, machine.sendBuffer, outcome);
            }
        };
        static State receiveState = new State("receiveState") {
            @Override public void entry(Guard pc_1, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Main)machine).anonfun_1(pc_1, machine.sendBuffer);
            }
        };
        static State ShareRequest = new State("ShareRequest") {
            @Override public void entry(Guard pc_2, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Main)machine).anonfun_2(pc_2, machine.sendBuffer, outcome, payload != null ? (PrimitiveVS<Machine>) ValueSummary.castFromAny(pc_2, new PrimitiveVS<Machine>().restrict(pc_2), payload) : new PrimitiveVS<Machine>().restrict(pc_2));
            }
        };
        static State ExclRequest = new State("ExclRequest") {
            @Override public void entry(Guard pc_3, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Main)machine).anonfun_3(pc_3, machine.sendBuffer, outcome, payload != null ? (PrimitiveVS<Machine>) ValueSummary.castFromAny(pc_3, new PrimitiveVS<Machine>().restrict(pc_3), payload) : new PrimitiveVS<Machine>().restrict(pc_3));
            }
        };
        static State ProcessReq = new State("ProcessReq") {
            @Override public void entry(Guard pc_4, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Main)machine).anonfun_4(pc_4, machine.sendBuffer, outcome);
            }
        };
        static State inv = new State("inv") {
            @Override public void entry(Guard pc_5, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Main)machine).anonfun_5(pc_5, machine.sendBuffer, outcome);
            }
        };
        static State grantAccess = new State("grantAccess") {
            @Override public void entry(Guard pc_6, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Main)machine).anonfun_6(pc_6, machine.sendBuffer, outcome);
            }
        };
        private PrimitiveVS<Machine> var_curr_client = new PrimitiveVS<Machine>();
        private TupleVS var_clients = new TupleVS(new PrimitiveVS<Machine>(), new PrimitiveVS<Machine>(), new PrimitiveVS<Machine>());
        private PrimitiveVS<Machine> var_curr_cpu = new PrimitiveVS<Machine>();
        private ListVS<PrimitiveVS<Machine>> var_sharer_list = new ListVS<PrimitiveVS<Machine>>(Guard.constTrue());
        private PrimitiveVS<Boolean> var_is_curr_req_excl = new PrimitiveVS<Boolean>(false);
        private PrimitiveVS<Boolean> var_is_excl_granted = new PrimitiveVS<Boolean>(false);
        private PrimitiveVS<Integer> var_i = new PrimitiveVS<Integer>(0);
        private PrimitiveVS<Integer> var_s = new PrimitiveVS<Integer>(0);
        private PrimitiveVS<Machine> var_temp = new PrimitiveVS<Machine>();

        @Override
        public void reset() {
                super.reset();
                var_curr_client = new PrimitiveVS<Machine>();
                var_clients = new TupleVS(new PrimitiveVS<Machine>(), new PrimitiveVS<Machine>(), new PrimitiveVS<Machine>());
                var_curr_cpu = new PrimitiveVS<Machine>();
                var_sharer_list = new ListVS<PrimitiveVS<Machine>>(Guard.constTrue());
                var_is_curr_req_excl = new PrimitiveVS<Boolean>(false);
                var_is_excl_granted = new PrimitiveVS<Boolean>(false);
                var_i = new PrimitiveVS<Integer>(0);
                var_s = new PrimitiveVS<Integer>(0);
                var_temp = new PrimitiveVS<Machine>();
        }

        public Main(int id) {
            super("Main", id, EventBufferSemantics.queue, init, init
                , receiveState
                , ShareRequest
                , ExclRequest
                , ProcessReq
                , inv
                , grantAccess

            );
            init.addHandlers(new GotoEventHandler(unit, receiveState));
            receiveState.addHandlers(new DeferEventHandler(invalidate_ack)
                ,
                new GotoEventHandler(req_share, ShareRequest),
                new GotoEventHandler(req_excl, ExclRequest));
            ShareRequest.addHandlers(new GotoEventHandler(unit, ProcessReq));
            ExclRequest.addHandlers(new GotoEventHandler(unit, ProcessReq));
            ProcessReq.addHandlers(new GotoEventHandler(need_invalidate, inv),
                new GotoEventHandler(grant, grantAccess));
            inv.addHandlers(new DeferEventHandler(req_share)
                ,
                new DeferEventHandler(req_excl)
                ,
                new EventHandler(invalidate_ack) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((Main)machine).rec_ack(pc, machine.sendBuffer, outcome);
                    }
                },
                new GotoEventHandler(grant, grantAccess));
            grantAccess.addHandlers(new GotoEventHandler(unit, receiveState));
        }

        Guard
        anonfun_0(
            Guard pc_7,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_7);

            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_7);

            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_7);

            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_7);

            PrimitiveVS<Boolean> var_$tmp4 =
                new PrimitiveVS<Boolean>(false).restrict(pc_7);

            PrimitiveVS<Machine> var_$tmp5 =
                new PrimitiveVS<Machine>().restrict(pc_7);

            PrimitiveVS<Machine> var_$tmp6 =
                new PrimitiveVS<Machine>().restrict(pc_7);

            PrimitiveVS<Boolean> var_$tmp7 =
                new PrimitiveVS<Boolean>(false).restrict(pc_7);

            PrimitiveVS<Machine> var_$tmp8 =
                new PrimitiveVS<Machine>().restrict(pc_7);

            TupleVS var_$tmp9 =
                new TupleVS(new PrimitiveVS<Machine>(), new PrimitiveVS<Machine>(), new PrimitiveVS<Machine>()).restrict(pc_7);

            PrimitiveVS<Machine> var_$tmp10 =
                new PrimitiveVS<Machine>().restrict(pc_7);

            PrimitiveVS<Integer> var_$tmp11 =
                new PrimitiveVS<Integer>(0).restrict(pc_7);

            PrimitiveVS<Boolean> var_$tmp12 =
                new PrimitiveVS<Boolean>(false).restrict(pc_7);

            PrimitiveVS<String> var_$tmp13 =
                new PrimitiveVS<String>("").restrict(pc_7);

            PrimitiveVS<Event> var_$tmp14 =
                new PrimitiveVS<Event>(_null).restrict(pc_7);

            PrimitiveVS<Machine> temp_var_0;
            temp_var_0 = new PrimitiveVS<Machine>(this).restrict(pc_7);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_7, temp_var_0);

            PrimitiveVS<Boolean> temp_var_1;
            temp_var_1 = new PrimitiveVS<Boolean>(false).restrict(pc_7);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_7, temp_var_1);

            PrimitiveVS<Machine> temp_var_2;
            temp_var_2 = effects.create(pc_7, scheduler, Client.class, new UnionVS (new TupleVS (var_$tmp0.restrict(pc_7), var_$tmp1.restrict(pc_7))), (i) -> new Client(i));
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_7, temp_var_2);

            PrimitiveVS<Machine> temp_var_3;
            temp_var_3 = var_$tmp2.restrict(pc_7);
            var_temp = var_temp.updateUnderGuard(pc_7, temp_var_3);

            TupleVS temp_var_4 = var_clients.restrict(pc_7);
            PrimitiveVS<Machine> temp_var_5;temp_var_5 = var_temp.restrict(pc_7);
            temp_var_4 = temp_var_4.setField(0,temp_var_5);
            var_clients = var_clients.updateUnderGuard(pc_7, temp_var_4);

            PrimitiveVS<Machine> temp_var_6;
            temp_var_6 = new PrimitiveVS<Machine>(this).restrict(pc_7);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_7, temp_var_6);

            PrimitiveVS<Boolean> temp_var_7;
            temp_var_7 = new PrimitiveVS<Boolean>(false).restrict(pc_7);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_7, temp_var_7);

            PrimitiveVS<Machine> temp_var_8;
            temp_var_8 = effects.create(pc_7, scheduler, Client.class, new UnionVS (new TupleVS (var_$tmp3.restrict(pc_7), var_$tmp4.restrict(pc_7))), (i) -> new Client(i));
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_7, temp_var_8);

            PrimitiveVS<Machine> temp_var_9;
            temp_var_9 = var_$tmp5.restrict(pc_7);
            var_temp = var_temp.updateUnderGuard(pc_7, temp_var_9);

            TupleVS temp_var_10 = var_clients.restrict(pc_7);
            PrimitiveVS<Machine> temp_var_11;temp_var_11 = var_temp.restrict(pc_7);
            temp_var_10 = temp_var_10.setField(1,temp_var_11);
            var_clients = var_clients.updateUnderGuard(pc_7, temp_var_10);

            PrimitiveVS<Machine> temp_var_12;
            temp_var_12 = new PrimitiveVS<Machine>(this).restrict(pc_7);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_7, temp_var_12);

            PrimitiveVS<Boolean> temp_var_13;
            temp_var_13 = new PrimitiveVS<Boolean>(false).restrict(pc_7);
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_7, temp_var_13);

            PrimitiveVS<Machine> temp_var_14;
            temp_var_14 = effects.create(pc_7, scheduler, Client.class, new UnionVS (new TupleVS (var_$tmp6.restrict(pc_7), var_$tmp7.restrict(pc_7))), (i) -> new Client(i));
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_7, temp_var_14);

            PrimitiveVS<Machine> temp_var_15;
            temp_var_15 = var_$tmp8.restrict(pc_7);
            var_temp = var_temp.updateUnderGuard(pc_7, temp_var_15);

            TupleVS temp_var_16 = var_clients.restrict(pc_7);
            PrimitiveVS<Machine> temp_var_17;temp_var_17 = var_temp.restrict(pc_7);
            temp_var_16 = temp_var_16.setField(2,temp_var_17);
            var_clients = var_clients.updateUnderGuard(pc_7, temp_var_16);

            TupleVS temp_var_18;
            temp_var_18 = var_clients.restrict(pc_7);
            var_$tmp9 = var_$tmp9.updateUnderGuard(pc_7, temp_var_18);

            PrimitiveVS<Machine> temp_var_19;
            temp_var_19 = effects.create(pc_7, scheduler, CPU.class, new UnionVS (var_$tmp9.restrict(pc_7)), (i) -> new CPU(i));
            var_$tmp10 = var_$tmp10.updateUnderGuard(pc_7, temp_var_19);

            PrimitiveVS<Machine> temp_var_20;
            temp_var_20 = var_$tmp10.restrict(pc_7);
            var_curr_cpu = var_curr_cpu.updateUnderGuard(pc_7, temp_var_20);

            PrimitiveVS<Integer> temp_var_21;
            temp_var_21 = var_sharer_list.restrict(pc_7).size();
            var_$tmp11 = var_$tmp11.updateUnderGuard(pc_7, temp_var_21);

            PrimitiveVS<Boolean> temp_var_22;
            temp_var_22 = var_$tmp11.restrict(pc_7).symbolicEquals(new PrimitiveVS<Integer>(0).restrict(pc_7), pc_7);
            var_$tmp12 = var_$tmp12.updateUnderGuard(pc_7, temp_var_22);

            PrimitiveVS<String> temp_var_23;
            temp_var_23 = new PrimitiveVS<String>(String.format("")).restrict(pc_7);
            var_$tmp13 = var_$tmp13.updateUnderGuard(pc_7, temp_var_23);

            Assert.progProp(!(var_$tmp12.restrict(pc_7)).getValues().contains(Boolean.FALSE), var_$tmp13.restrict(pc_7), scheduler, var_$tmp12.restrict(pc_7).getGuardFor(Boolean.FALSE));
            PrimitiveVS<Event> temp_var_24;
            temp_var_24 = new PrimitiveVS<Event>(unit).restrict(pc_7);
            var_$tmp14 = var_$tmp14.updateUnderGuard(pc_7, temp_var_24);

            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_7, var_$tmp14.restrict(pc_7));
            pc_7 = Guard.constFalse();

            return pc_7;
        }

        void
        anonfun_1(
            Guard pc_8,
            EventBuffer effects
        ) {
        }

        Guard
        anonfun_2(
            Guard pc_9,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            PrimitiveVS<Machine> var_payload
        ) {
            PrimitiveVS<Event> var_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_9);

            PrimitiveVS<Machine> temp_var_25;
            temp_var_25 = var_payload.restrict(pc_9);
            var_curr_client = var_curr_client.updateUnderGuard(pc_9, temp_var_25);

            PrimitiveVS<Boolean> temp_var_26;
            temp_var_26 = new PrimitiveVS<Boolean>(false).restrict(pc_9);
            var_is_curr_req_excl = var_is_curr_req_excl.updateUnderGuard(pc_9, temp_var_26);

            PrimitiveVS<Event> temp_var_27;
            temp_var_27 = new PrimitiveVS<Event>(unit).restrict(pc_9);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_9, temp_var_27);

            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_9, var_$tmp0.restrict(pc_9));
            pc_9 = Guard.constFalse();

            return pc_9;
        }

        Guard
        anonfun_3(
            Guard pc_10,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            PrimitiveVS<Machine> var_payload
        ) {
            PrimitiveVS<Event> var_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_10);

            PrimitiveVS<Machine> temp_var_28;
            temp_var_28 = var_payload.restrict(pc_10);
            var_curr_client = var_curr_client.updateUnderGuard(pc_10, temp_var_28);

            PrimitiveVS<Boolean> temp_var_29;
            temp_var_29 = new PrimitiveVS<Boolean>(true).restrict(pc_10);
            var_is_curr_req_excl = var_is_curr_req_excl.updateUnderGuard(pc_10, temp_var_29);

            PrimitiveVS<Event> temp_var_30;
            temp_var_30 = new PrimitiveVS<Event>(unit).restrict(pc_10);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_10, temp_var_30);

            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_10, var_$tmp0.restrict(pc_10));
            pc_10 = Guard.constFalse();

            return pc_10;
        }

        Guard
        anonfun_4(
            Guard pc_11,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveVS<Boolean> var_$tmp0 =
                new PrimitiveVS<Boolean>(false).restrict(pc_11);

            PrimitiveVS<Event> var_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_11);

            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_11);

            PrimitiveVS<Boolean> temp_var_31;
            temp_var_31 = var_is_curr_req_excl.restrict(pc_11);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_11, temp_var_31);

            PrimitiveVS<Boolean> temp_var_32 = var_$tmp0.restrict(pc_11);
            Guard pc_12 = BooleanVS.getTrueGuard(temp_var_32);
            Guard pc_13 = BooleanVS.getFalseGuard(temp_var_32);
            boolean jumpedOut_0 = false;
            boolean jumpedOut_1 = false;
            if (!pc_12.isFalse()) {
                // 'then' branch
            }
            if (!pc_13.isFalse()) {
                // 'else' branch
                PrimitiveVS<Boolean> temp_var_33;
                temp_var_33 = var_is_excl_granted.restrict(pc_13);
                var_$tmp0 = var_$tmp0.updateUnderGuard(pc_13, temp_var_33);

            }
            if (jumpedOut_0 || jumpedOut_1) {
                pc_11 = pc_12.or(pc_13);
            }

            PrimitiveVS<Boolean> temp_var_34 = var_$tmp0.restrict(pc_11);
            Guard pc_14 = BooleanVS.getTrueGuard(temp_var_34);
            Guard pc_15 = BooleanVS.getFalseGuard(temp_var_34);
            boolean jumpedOut_2 = false;
            boolean jumpedOut_3 = false;
            if (!pc_14.isFalse()) {
                // 'then' branch
                PrimitiveVS<Event> temp_var_35;
                temp_var_35 = new PrimitiveVS<Event>(need_invalidate).restrict(pc_14);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_14, temp_var_35);

                // NOTE (TODO): We currently perform no typechecking on the payload!
                outcome.raiseGuardedEvent(pc_14, var_$tmp1.restrict(pc_14));
                pc_14 = Guard.constFalse();
                jumpedOut_2 = true;

            }
            if (!pc_15.isFalse()) {
                // 'else' branch
                PrimitiveVS<Event> temp_var_36;
                temp_var_36 = new PrimitiveVS<Event>(grant).restrict(pc_15);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_15, temp_var_36);

                // NOTE (TODO): We currently perform no typechecking on the payload!
                outcome.raiseGuardedEvent(pc_15, var_$tmp2.restrict(pc_15));
                pc_15 = Guard.constFalse();
                jumpedOut_3 = true;

            }
            if (jumpedOut_2 || jumpedOut_3) {
                pc_11 = pc_14.or(pc_15);
            }

            return pc_11;
        }

        Guard
        anonfun_5(
            Guard pc_16,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_16);

            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_16);

            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_16);

            PrimitiveVS<Boolean> var_$tmp3 =
                new PrimitiveVS<Boolean>(false).restrict(pc_16);

            PrimitiveVS<Boolean> var_$tmp4 =
                new PrimitiveVS<Boolean>(false).restrict(pc_16);

            PrimitiveVS<Machine> var_$tmp5 =
                new PrimitiveVS<Machine>().restrict(pc_16);

            PrimitiveVS<Machine> var_$tmp6 =
                new PrimitiveVS<Machine>().restrict(pc_16);

            PrimitiveVS<Event> var_$tmp7 =
                new PrimitiveVS<Event>(_null).restrict(pc_16);

            PrimitiveVS<Integer> var_$tmp8 =
                new PrimitiveVS<Integer>(0).restrict(pc_16);

            PrimitiveVS<Integer> temp_var_37;
            temp_var_37 = new PrimitiveVS<Integer>(0).restrict(pc_16);
            var_i = var_i.updateUnderGuard(pc_16, temp_var_37);

            PrimitiveVS<Integer> temp_var_38;
            temp_var_38 = var_sharer_list.restrict(pc_16).size();
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_16, temp_var_38);

            PrimitiveVS<Integer> temp_var_39;
            temp_var_39 = var_$tmp0.restrict(pc_16);
            var_s = var_s.updateUnderGuard(pc_16, temp_var_39);

            PrimitiveVS<Boolean> temp_var_40;
            temp_var_40 = var_s.restrict(pc_16).symbolicEquals(new PrimitiveVS<Integer>(0).restrict(pc_16), pc_16);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_16, temp_var_40);

            PrimitiveVS<Boolean> temp_var_41 = var_$tmp1.restrict(pc_16);
            Guard pc_17 = BooleanVS.getTrueGuard(temp_var_41);
            Guard pc_18 = BooleanVS.getFalseGuard(temp_var_41);
            boolean jumpedOut_4 = false;
            boolean jumpedOut_5 = false;
            if (!pc_17.isFalse()) {
                // 'then' branch
                PrimitiveVS<Event> temp_var_42;
                temp_var_42 = new PrimitiveVS<Event>(grant).restrict(pc_17);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_17, temp_var_42);

                // NOTE (TODO): We currently perform no typechecking on the payload!
                outcome.raiseGuardedEvent(pc_17, var_$tmp2.restrict(pc_17));
                pc_17 = Guard.constFalse();
                jumpedOut_4 = true;

            }
            if (!pc_18.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_4 || jumpedOut_5) {
                pc_16 = pc_17.or(pc_18);
            }

            if (!pc_16.isFalse()) {
                java.util.List<Guard> loop_exits_0 = new java.util.ArrayList<>();
                boolean loop_early_ret_0 = false;
                Guard pc_19 = pc_16;
                while (!pc_19.isFalse()) {
                    PrimitiveVS<Boolean> temp_var_43;
                    temp_var_43 = (var_i.restrict(pc_19)).apply(var_s.restrict(pc_19), (temp_var_44, temp_var_45) -> temp_var_44 < temp_var_45);
                    var_$tmp3 = var_$tmp3.updateUnderGuard(pc_19, temp_var_43);

                    PrimitiveVS<Boolean> temp_var_46;
                    temp_var_46 = var_$tmp3.restrict(pc_19);
                    var_$tmp4 = var_$tmp4.updateUnderGuard(pc_19, temp_var_46);

                    PrimitiveVS<Boolean> temp_var_47 = var_$tmp4.restrict(pc_19);
                    Guard pc_20 = BooleanVS.getTrueGuard(temp_var_47);
                    Guard pc_21 = BooleanVS.getFalseGuard(temp_var_47);
                    boolean jumpedOut_6 = false;
                    boolean jumpedOut_7 = false;
                    if (!pc_20.isFalse()) {
                        // 'then' branch
                    }
                    if (!pc_21.isFalse()) {
                        // 'else' branch
                        loop_exits_0.add(pc_21);
                        jumpedOut_7 = true;
                        pc_21 = Guard.constFalse();

                    }
                    if (jumpedOut_6 || jumpedOut_7) {
                        pc_19 = pc_20.or(pc_21);
                    }

                    if (!pc_19.isFalse()) {
                        PrimitiveVS<Machine> temp_var_48;
                        temp_var_48 = var_sharer_list.restrict(pc_19).get(var_i.restrict(pc_19));
                        var_$tmp5 = var_$tmp5.updateUnderGuard(pc_19, temp_var_48);

                        PrimitiveVS<Machine> temp_var_49;
                        temp_var_49 = var_$tmp5.restrict(pc_19);
                        var_$tmp6 = var_$tmp6.updateUnderGuard(pc_19, temp_var_49);

                        PrimitiveVS<Event> temp_var_50;
                        temp_var_50 = new PrimitiveVS<Event>(invalidate).restrict(pc_19);
                        var_$tmp7 = var_$tmp7.updateUnderGuard(pc_19, temp_var_50);

                        effects.send(pc_19, var_$tmp6.restrict(pc_19), var_$tmp7.restrict(pc_19), null);

                        PrimitiveVS<Integer> temp_var_51;
                        temp_var_51 = (var_i.restrict(pc_19)).apply(new PrimitiveVS<Integer>(1).restrict(pc_19), (temp_var_52, temp_var_53) -> temp_var_52 + temp_var_53);
                        var_$tmp8 = var_$tmp8.updateUnderGuard(pc_19, temp_var_51);

                        PrimitiveVS<Integer> temp_var_54;
                        temp_var_54 = var_$tmp8.restrict(pc_19);
                        var_i = var_i.updateUnderGuard(pc_19, temp_var_54);

                    }
                }
                if (loop_early_ret_0) {
                    pc_16 = Guard.orMany(loop_exits_0);
                }

            }
            return pc_16;
        }

        Guard
        rec_ack(
            Guard pc_22,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_22);

            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_22);

            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_22);

            ListVS<PrimitiveVS<Machine>> temp_var_55 = var_sharer_list.restrict(pc_22);
            temp_var_55 = var_sharer_list.restrict(pc_22).removeAt(new PrimitiveVS<Integer>(0).restrict(pc_22));
            var_sharer_list = var_sharer_list.updateUnderGuard(pc_22, temp_var_55);

            PrimitiveVS<Integer> temp_var_56;
            temp_var_56 = var_sharer_list.restrict(pc_22).size();
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_22, temp_var_56);

            PrimitiveVS<Integer> temp_var_57;
            temp_var_57 = var_$tmp0.restrict(pc_22);
            var_s = var_s.updateUnderGuard(pc_22, temp_var_57);

            PrimitiveVS<Boolean> temp_var_58;
            temp_var_58 = var_s.restrict(pc_22).symbolicEquals(new PrimitiveVS<Integer>(0).restrict(pc_22), pc_22);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_22, temp_var_58);

            PrimitiveVS<Boolean> temp_var_59 = var_$tmp1.restrict(pc_22);
            Guard pc_23 = BooleanVS.getTrueGuard(temp_var_59);
            Guard pc_24 = BooleanVS.getFalseGuard(temp_var_59);
            boolean jumpedOut_8 = false;
            boolean jumpedOut_9 = false;
            if (!pc_23.isFalse()) {
                // 'then' branch
                PrimitiveVS<Event> temp_var_60;
                temp_var_60 = new PrimitiveVS<Event>(grant).restrict(pc_23);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_23, temp_var_60);

                // NOTE (TODO): We currently perform no typechecking on the payload!
                outcome.raiseGuardedEvent(pc_23, var_$tmp2.restrict(pc_23));
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
        anonfun_6(
            Guard pc_25,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_25);

            PrimitiveVS<Event> var_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_25);

            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_25);

            PrimitiveVS<Event> var_$tmp3 =
                new PrimitiveVS<Event>(_null).restrict(pc_25);

            PrimitiveVS<Machine> var_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_25);

            PrimitiveVS<Event> var_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_25);

            PrimitiveVS<Boolean> temp_var_61 = var_is_curr_req_excl.restrict(pc_25);
            Guard pc_26 = BooleanVS.getTrueGuard(temp_var_61);
            Guard pc_27 = BooleanVS.getFalseGuard(temp_var_61);
            boolean jumpedOut_10 = false;
            boolean jumpedOut_11 = false;
            if (!pc_26.isFalse()) {
                // 'then' branch
                PrimitiveVS<Boolean> temp_var_62;
                temp_var_62 = new PrimitiveVS<Boolean>(true).restrict(pc_26);
                var_is_excl_granted = var_is_excl_granted.updateUnderGuard(pc_26, temp_var_62);

                PrimitiveVS<Machine> temp_var_63;
                temp_var_63 = var_curr_client.restrict(pc_26);
                var_$tmp0 = var_$tmp0.updateUnderGuard(pc_26, temp_var_63);

                PrimitiveVS<Event> temp_var_64;
                temp_var_64 = new PrimitiveVS<Event>(grant_excl).restrict(pc_26);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_26, temp_var_64);

                effects.send(pc_26, var_$tmp0.restrict(pc_26), var_$tmp1.restrict(pc_26), null);

            }
            if (!pc_27.isFalse()) {
                // 'else' branch
                PrimitiveVS<Machine> temp_var_65;
                temp_var_65 = var_curr_client.restrict(pc_27);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_27, temp_var_65);

                PrimitiveVS<Event> temp_var_66;
                temp_var_66 = new PrimitiveVS<Event>(grant_share).restrict(pc_27);
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_27, temp_var_66);

                effects.send(pc_27, var_$tmp2.restrict(pc_27), var_$tmp3.restrict(pc_27), null);

            }
            if (jumpedOut_10 || jumpedOut_11) {
                pc_25 = pc_26.or(pc_27);
            }

            PrimitiveVS<Machine> temp_var_67;
            temp_var_67 = var_curr_client.restrict(pc_25);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_25, temp_var_67);

            ListVS<PrimitiveVS<Machine>> temp_var_68 = var_sharer_list.restrict(pc_25);
            temp_var_68 = var_sharer_list.restrict(pc_25).insert(new PrimitiveVS<Integer>(0).restrict(pc_25), var_$tmp4.restrict(pc_25));
            var_sharer_list = var_sharer_list.updateUnderGuard(pc_25, temp_var_68);

            PrimitiveVS<Event> temp_var_69;
            temp_var_69 = new PrimitiveVS<Event>(unit).restrict(pc_25);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_25, temp_var_69);

            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_25, var_$tmp5.restrict(pc_25));
            pc_25 = Guard.constFalse();

            return pc_25;
        }

    }

    public static class Client extends Machine {

        static State init = new State("init") {
            @Override public void entry(Guard pc_28, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Client)machine).anonfun_7(pc_28, machine.sendBuffer, outcome, payload != null ? (TupleVS) ValueSummary.castFromAny(pc_28, new TupleVS(new PrimitiveVS<Machine>(), new PrimitiveVS<Boolean>(false)).restrict(pc_28), payload) : new TupleVS(new PrimitiveVS<Machine>(), new PrimitiveVS<Boolean>(false)).restrict(pc_28));
            }
        };
        static State invalid = new State("invalid") {
            @Override public void entry(Guard pc_29, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Client)machine).anonfun_8(pc_29, machine.sendBuffer);
            }
        };
        static State asked_share = new State("asked_share") {
            @Override public void entry(Guard pc_30, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Client)machine).anonfun_9(pc_30, machine.sendBuffer, outcome);
            }
        };
        static State asked_excl = new State("asked_excl") {
            @Override public void entry(Guard pc_31, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Client)machine).anonfun_10(pc_31, machine.sendBuffer, outcome);
            }
        };
        static State invalid_wait = new State("invalid_wait") {
        };
        static State asked_ex2 = new State("asked_ex2") {
            @Override public void entry(Guard pc_32, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Client)machine).anonfun_11(pc_32, machine.sendBuffer, outcome);
            }
        };
        static State sharing = new State("sharing") {
            @Override public void entry(Guard pc_33, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Client)machine).anonfun_12(pc_33, machine.sendBuffer);
            }
        };
        static State sharing_wait = new State("sharing_wait") {
            @Override public void entry(Guard pc_34, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Client)machine).anonfun_13(pc_34, machine.sendBuffer);
            }
        };
        static State exclusive = new State("exclusive") {
            @Override public void entry(Guard pc_35, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Client)machine).anonfun_14(pc_35, machine.sendBuffer);
            }
        };
        static State invalidating = new State("invalidating") {
            @Override public void entry(Guard pc_36, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Client)machine).anonfun_15(pc_36, machine.sendBuffer, outcome);
            }
        };
        private PrimitiveVS<Machine> var_host = new PrimitiveVS<Machine>();
        private PrimitiveVS<Boolean> var_pending = new PrimitiveVS<Boolean>(false);

        @Override
        public void reset() {
                super.reset();
                var_host = new PrimitiveVS<Machine>();
                var_pending = new PrimitiveVS<Boolean>(false);
        }

        public Client(int id) {
            super("Client", id, EventBufferSemantics.queue, init, init
                , invalid
                , asked_share
                , asked_excl
                , invalid_wait
                , asked_ex2
                , sharing
                , sharing_wait
                , exclusive
                , invalidating

            );
            init.addHandlers(new GotoEventHandler(unit, invalid));
            invalid.addHandlers(new GotoEventHandler(ask_share, asked_share),
                new GotoEventHandler(ask_excl, asked_excl),
                new GotoEventHandler(invalidate, invalidating),
                new GotoEventHandler(grant_excl, exclusive),
                new GotoEventHandler(grant_share, sharing));
            asked_share.addHandlers(new GotoEventHandler(unit, invalid_wait));
            asked_excl.addHandlers(new GotoEventHandler(unit, invalid_wait));
            invalid_wait.addHandlers(new DeferEventHandler(ask_share)
                ,
                new DeferEventHandler(ask_excl)
                ,
                new GotoEventHandler(invalidate, invalidating),
                new GotoEventHandler(grant_excl, exclusive),
                new GotoEventHandler(grant_share, sharing));
            asked_ex2.addHandlers(new GotoEventHandler(unit, sharing_wait));
            sharing.addHandlers(new GotoEventHandler(invalidate, invalidating),
                new GotoEventHandler(grant_share, sharing),
                new GotoEventHandler(grant_excl, exclusive),
                new GotoEventHandler(ask_share, sharing),
                new GotoEventHandler(ask_excl, asked_ex2));
            sharing_wait.addHandlers(new DeferEventHandler(ask_share)
                ,
                new DeferEventHandler(ask_excl)
                ,
                new GotoEventHandler(invalidate, invalidating),
                new GotoEventHandler(grant_share, sharing_wait),
                new GotoEventHandler(grant_excl, exclusive));
            exclusive.addHandlers(new IgnoreEventHandler(ask_share),
                new IgnoreEventHandler(ask_excl),
                new GotoEventHandler(invalidate, invalidating),
                new GotoEventHandler(grant_share, sharing),
                new GotoEventHandler(grant_excl, exclusive));
            invalidating.addHandlers(new GotoEventHandler(wait, invalid_wait),
                new GotoEventHandler(normal, invalid));
        }

        Guard
        anonfun_7(
            Guard pc_37,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            TupleVS var_payload
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_37);

            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_37);

            PrimitiveVS<Boolean> var_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_37);

            PrimitiveVS<Boolean> var_$tmp3 =
                new PrimitiveVS<Boolean>(false).restrict(pc_37);

            PrimitiveVS<Event> var_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_37);

            PrimitiveVS<Machine> temp_var_70;
            temp_var_70 = (PrimitiveVS<Machine>)((var_payload.restrict(pc_37)).getField(0));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_37, temp_var_70);

            PrimitiveVS<Machine> temp_var_71;
            temp_var_71 = var_$tmp0.restrict(pc_37);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_37, temp_var_71);

            PrimitiveVS<Machine> temp_var_72;
            temp_var_72 = var_$tmp1.restrict(pc_37);
            var_host = var_host.updateUnderGuard(pc_37, temp_var_72);

            PrimitiveVS<Boolean> temp_var_73;
            temp_var_73 = (PrimitiveVS<Boolean>)((var_payload.restrict(pc_37)).getField(1));
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_37, temp_var_73);

            PrimitiveVS<Boolean> temp_var_74;
            temp_var_74 = var_$tmp2.restrict(pc_37);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_37, temp_var_74);

            PrimitiveVS<Boolean> temp_var_75;
            temp_var_75 = var_$tmp3.restrict(pc_37);
            var_pending = var_pending.updateUnderGuard(pc_37, temp_var_75);

            PrimitiveVS<Event> temp_var_76;
            temp_var_76 = new PrimitiveVS<Event>(unit).restrict(pc_37);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_37, temp_var_76);

            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_37, var_$tmp4.restrict(pc_37));
            pc_37 = Guard.constFalse();

            return pc_37;
        }

        void
        anonfun_8(
            Guard pc_38,
            EventBuffer effects
        ) {
        }

        Guard
        anonfun_9(
            Guard pc_39,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_39);

            PrimitiveVS<Event> var_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_39);

            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_39);

            PrimitiveVS<Event> var_$tmp3 =
                new PrimitiveVS<Event>(_null).restrict(pc_39);

            PrimitiveVS<Machine> temp_var_77;
            temp_var_77 = var_host.restrict(pc_39);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_39, temp_var_77);

            PrimitiveVS<Event> temp_var_78;
            temp_var_78 = new PrimitiveVS<Event>(req_share).restrict(pc_39);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_39, temp_var_78);

            PrimitiveVS<Machine> temp_var_79;
            temp_var_79 = new PrimitiveVS<Machine>(this).restrict(pc_39);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_39, temp_var_79);

            effects.send(pc_39, var_$tmp0.restrict(pc_39), var_$tmp1.restrict(pc_39), new UnionVS(var_$tmp2.restrict(pc_39)));

            PrimitiveVS<Boolean> temp_var_80;
            temp_var_80 = new PrimitiveVS<Boolean>(true).restrict(pc_39);
            var_pending = var_pending.updateUnderGuard(pc_39, temp_var_80);

            PrimitiveVS<Event> temp_var_81;
            temp_var_81 = new PrimitiveVS<Event>(unit).restrict(pc_39);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_39, temp_var_81);

            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_39, var_$tmp3.restrict(pc_39));
            pc_39 = Guard.constFalse();

            return pc_39;
        }

        Guard
        anonfun_10(
            Guard pc_40,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_40);

            PrimitiveVS<Event> var_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_40);

            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_40);

            PrimitiveVS<Event> var_$tmp3 =
                new PrimitiveVS<Event>(_null).restrict(pc_40);

            PrimitiveVS<Machine> temp_var_82;
            temp_var_82 = var_host.restrict(pc_40);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_40, temp_var_82);

            PrimitiveVS<Event> temp_var_83;
            temp_var_83 = new PrimitiveVS<Event>(req_excl).restrict(pc_40);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_40, temp_var_83);

            PrimitiveVS<Machine> temp_var_84;
            temp_var_84 = new PrimitiveVS<Machine>(this).restrict(pc_40);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_40, temp_var_84);

            effects.send(pc_40, var_$tmp0.restrict(pc_40), var_$tmp1.restrict(pc_40), new UnionVS(var_$tmp2.restrict(pc_40)));

            PrimitiveVS<Boolean> temp_var_85;
            temp_var_85 = new PrimitiveVS<Boolean>(true).restrict(pc_40);
            var_pending = var_pending.updateUnderGuard(pc_40, temp_var_85);

            PrimitiveVS<Event> temp_var_86;
            temp_var_86 = new PrimitiveVS<Event>(unit).restrict(pc_40);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_40, temp_var_86);

            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_40, var_$tmp3.restrict(pc_40));
            pc_40 = Guard.constFalse();

            return pc_40;
        }

        Guard
        anonfun_11(
            Guard pc_41,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_41);

            PrimitiveVS<Event> var_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_41);

            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_41);

            PrimitiveVS<Event> var_$tmp3 =
                new PrimitiveVS<Event>(_null).restrict(pc_41);

            PrimitiveVS<Machine> temp_var_87;
            temp_var_87 = var_host.restrict(pc_41);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_41, temp_var_87);

            PrimitiveVS<Event> temp_var_88;
            temp_var_88 = new PrimitiveVS<Event>(req_excl).restrict(pc_41);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_41, temp_var_88);

            PrimitiveVS<Machine> temp_var_89;
            temp_var_89 = new PrimitiveVS<Machine>(this).restrict(pc_41);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_41, temp_var_89);

            effects.send(pc_41, var_$tmp0.restrict(pc_41), var_$tmp1.restrict(pc_41), new UnionVS(var_$tmp2.restrict(pc_41)));

            PrimitiveVS<Boolean> temp_var_90;
            temp_var_90 = new PrimitiveVS<Boolean>(true).restrict(pc_41);
            var_pending = var_pending.updateUnderGuard(pc_41, temp_var_90);

            PrimitiveVS<Event> temp_var_91;
            temp_var_91 = new PrimitiveVS<Event>(unit).restrict(pc_41);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_41, temp_var_91);

            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_41, var_$tmp3.restrict(pc_41));
            pc_41 = Guard.constFalse();

            return pc_41;
        }

        void
        anonfun_12(
            Guard pc_42,
            EventBuffer effects
        ) {
            PrimitiveVS<Boolean> temp_var_92;
            temp_var_92 = new PrimitiveVS<Boolean>(false).restrict(pc_42);
            var_pending = var_pending.updateUnderGuard(pc_42, temp_var_92);

        }

        void
        anonfun_13(
            Guard pc_43,
            EventBuffer effects
        ) {
        }

        void
        anonfun_14(
            Guard pc_44,
            EventBuffer effects
        ) {
            PrimitiveVS<Boolean> temp_var_93;
            temp_var_93 = new PrimitiveVS<Boolean>(false).restrict(pc_44);
            var_pending = var_pending.updateUnderGuard(pc_44, temp_var_93);

        }

        Guard
        anonfun_15(
            Guard pc_45,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_45);

            PrimitiveVS<Event> var_$tmp1 =
                new PrimitiveVS<Event>(_null).restrict(pc_45);

            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_45);

            PrimitiveVS<Event> var_$tmp3 =
                new PrimitiveVS<Event>(_null).restrict(pc_45);

            PrimitiveVS<Machine> temp_var_94;
            temp_var_94 = var_host.restrict(pc_45);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_45, temp_var_94);

            PrimitiveVS<Event> temp_var_95;
            temp_var_95 = new PrimitiveVS<Event>(invalidate_ack).restrict(pc_45);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_45, temp_var_95);

            effects.send(pc_45, var_$tmp0.restrict(pc_45), var_$tmp1.restrict(pc_45), null);

            PrimitiveVS<Boolean> temp_var_96 = var_pending.restrict(pc_45);
            Guard pc_46 = BooleanVS.getTrueGuard(temp_var_96);
            Guard pc_47 = BooleanVS.getFalseGuard(temp_var_96);
            boolean jumpedOut_12 = false;
            boolean jumpedOut_13 = false;
            if (!pc_46.isFalse()) {
                // 'then' branch
                PrimitiveVS<Event> temp_var_97;
                temp_var_97 = new PrimitiveVS<Event>(wait).restrict(pc_46);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_46, temp_var_97);

                // NOTE (TODO): We currently perform no typechecking on the payload!
                outcome.raiseGuardedEvent(pc_46, var_$tmp2.restrict(pc_46));
                pc_46 = Guard.constFalse();
                jumpedOut_12 = true;

            }
            if (!pc_47.isFalse()) {
                // 'else' branch
                PrimitiveVS<Event> temp_var_98;
                temp_var_98 = new PrimitiveVS<Event>(normal).restrict(pc_47);
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_47, temp_var_98);

                // NOTE (TODO): We currently perform no typechecking on the payload!
                outcome.raiseGuardedEvent(pc_47, var_$tmp3.restrict(pc_47));
                pc_47 = Guard.constFalse();
                jumpedOut_13 = true;

            }
            if (jumpedOut_12 || jumpedOut_13) {
                pc_45 = pc_46.or(pc_47);
            }

            return pc_45;
        }

    }

    public static class CPU extends Machine {

        static State init = new State("init") {
            @Override public void entry(Guard pc_48, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((CPU)machine).anonfun_16(pc_48, machine.sendBuffer, outcome, payload != null ? (TupleVS) ValueSummary.castFromAny(pc_48, new TupleVS(new PrimitiveVS<Machine>(), new PrimitiveVS<Machine>(), new PrimitiveVS<Machine>()).restrict(pc_48), payload) : new TupleVS(new PrimitiveVS<Machine>(), new PrimitiveVS<Machine>(), new PrimitiveVS<Machine>()).restrict(pc_48));
            }
        };
        static State makeReq = new State("makeReq") {
            @Override public void entry(Guard pc_49, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((CPU)machine).anonfun_17(pc_49, machine.sendBuffer, outcome);
            }
        };
        private TupleVS var_cache = new TupleVS(new PrimitiveVS<Machine>(), new PrimitiveVS<Machine>(), new PrimitiveVS<Machine>());
        private PrimitiveVS<Integer> var_req_count = new PrimitiveVS<Integer>(0);

        @Override
        public void reset() {
                super.reset();
                var_cache = new TupleVS(new PrimitiveVS<Machine>(), new PrimitiveVS<Machine>(), new PrimitiveVS<Machine>());
                var_req_count = new PrimitiveVS<Integer>(0);
        }

        public CPU(int id) {
            super("CPU", id, EventBufferSemantics.queue, init, init
                , makeReq

            );
            init.addHandlers(new GotoEventHandler(unit, makeReq));
            makeReq.addHandlers(new GotoEventHandler(unit, makeReq));
        }

        Guard
        anonfun_16(
            Guard pc_50,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            TupleVS var_payload
        ) {
            PrimitiveVS<Event> var_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_50);

            TupleVS temp_var_99;
            temp_var_99 = var_payload.restrict(pc_50);
            var_cache = var_cache.updateUnderGuard(pc_50, temp_var_99);

            PrimitiveVS<Event> temp_var_100;
            temp_var_100 = new PrimitiveVS<Event>(unit).restrict(pc_50);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_50, temp_var_100);

            // NOTE (TODO): We currently perform no typechecking on the payload!
            outcome.raiseGuardedEvent(pc_50, var_$tmp0.restrict(pc_50));
            pc_50 = Guard.constFalse();

            return pc_50;
        }

        Guard
        anonfun_17(
            Guard pc_51,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveVS<Boolean> var_$tmp0 =
                new PrimitiveVS<Boolean>(false).restrict(pc_51);

            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_51);

            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_51);

            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_51);

            PrimitiveVS<Event> var_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_51);

            PrimitiveVS<Machine> var_$tmp5 =
                new PrimitiveVS<Machine>().restrict(pc_51);

            PrimitiveVS<Machine> var_$tmp6 =
                new PrimitiveVS<Machine>().restrict(pc_51);

            PrimitiveVS<Event> var_$tmp7 =
                new PrimitiveVS<Event>(_null).restrict(pc_51);

            PrimitiveVS<Boolean> var_$tmp8 =
                new PrimitiveVS<Boolean>(false).restrict(pc_51);

            PrimitiveVS<Boolean> var_$tmp9 =
                new PrimitiveVS<Boolean>(false).restrict(pc_51);

            PrimitiveVS<Machine> var_$tmp10 =
                new PrimitiveVS<Machine>().restrict(pc_51);

            PrimitiveVS<Machine> var_$tmp11 =
                new PrimitiveVS<Machine>().restrict(pc_51);

            PrimitiveVS<Event> var_$tmp12 =
                new PrimitiveVS<Event>(_null).restrict(pc_51);

            PrimitiveVS<Machine> var_$tmp13 =
                new PrimitiveVS<Machine>().restrict(pc_51);

            PrimitiveVS<Machine> var_$tmp14 =
                new PrimitiveVS<Machine>().restrict(pc_51);

            PrimitiveVS<Event> var_$tmp15 =
                new PrimitiveVS<Event>(_null).restrict(pc_51);

            PrimitiveVS<Boolean> var_$tmp16 =
                new PrimitiveVS<Boolean>(false).restrict(pc_51);

            PrimitiveVS<Machine> var_$tmp17 =
                new PrimitiveVS<Machine>().restrict(pc_51);

            PrimitiveVS<Machine> var_$tmp18 =
                new PrimitiveVS<Machine>().restrict(pc_51);

            PrimitiveVS<Event> var_$tmp19 =
                new PrimitiveVS<Event>(_null).restrict(pc_51);

            PrimitiveVS<Machine> var_$tmp20 =
                new PrimitiveVS<Machine>().restrict(pc_51);

            PrimitiveVS<Machine> var_$tmp21 =
                new PrimitiveVS<Machine>().restrict(pc_51);

            PrimitiveVS<Event> var_$tmp22 =
                new PrimitiveVS<Event>(_null).restrict(pc_51);

            PrimitiveVS<Boolean> var_$tmp23 =
                new PrimitiveVS<Boolean>(false).restrict(pc_51);

            PrimitiveVS<Integer> var_$tmp24 =
                new PrimitiveVS<Integer>(0).restrict(pc_51);

            PrimitiveVS<Event> var_$tmp25 =
                new PrimitiveVS<Event>(_null).restrict(pc_51);

            PrimitiveVS<Boolean> temp_var_101;
            temp_var_101 = scheduler.getNextBoolean(pc_51);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_51, temp_var_101);

            PrimitiveVS<Boolean> temp_var_102 = var_$tmp0.restrict(pc_51);
            Guard pc_52 = BooleanVS.getTrueGuard(temp_var_102);
            Guard pc_53 = BooleanVS.getFalseGuard(temp_var_102);
            boolean jumpedOut_14 = false;
            boolean jumpedOut_15 = false;
            if (!pc_52.isFalse()) {
                // 'then' branch
                PrimitiveVS<Boolean> temp_var_103;
                temp_var_103 = scheduler.getNextBoolean(pc_52);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_52, temp_var_103);

                PrimitiveVS<Boolean> temp_var_104 = var_$tmp1.restrict(pc_52);
                Guard pc_54 = BooleanVS.getTrueGuard(temp_var_104);
                Guard pc_55 = BooleanVS.getFalseGuard(temp_var_104);
                boolean jumpedOut_16 = false;
                boolean jumpedOut_17 = false;
                if (!pc_54.isFalse()) {
                    // 'then' branch
                    PrimitiveVS<Machine> temp_var_105;
                    temp_var_105 = (PrimitiveVS<Machine>)((var_cache.restrict(pc_54)).getField(0));
                    var_$tmp2 = var_$tmp2.updateUnderGuard(pc_54, temp_var_105);

                    PrimitiveVS<Machine> temp_var_106;
                    temp_var_106 = var_$tmp2.restrict(pc_54);
                    var_$tmp3 = var_$tmp3.updateUnderGuard(pc_54, temp_var_106);

                    PrimitiveVS<Event> temp_var_107;
                    temp_var_107 = new PrimitiveVS<Event>(ask_share).restrict(pc_54);
                    var_$tmp4 = var_$tmp4.updateUnderGuard(pc_54, temp_var_107);

                    effects.send(pc_54, var_$tmp3.restrict(pc_54), var_$tmp4.restrict(pc_54), null);

                }
                if (!pc_55.isFalse()) {
                    // 'else' branch
                    PrimitiveVS<Machine> temp_var_108;
                    temp_var_108 = (PrimitiveVS<Machine>)((var_cache.restrict(pc_55)).getField(0));
                    var_$tmp5 = var_$tmp5.updateUnderGuard(pc_55, temp_var_108);

                    PrimitiveVS<Machine> temp_var_109;
                    temp_var_109 = var_$tmp5.restrict(pc_55);
                    var_$tmp6 = var_$tmp6.updateUnderGuard(pc_55, temp_var_109);

                    PrimitiveVS<Event> temp_var_110;
                    temp_var_110 = new PrimitiveVS<Event>(ask_excl).restrict(pc_55);
                    var_$tmp7 = var_$tmp7.updateUnderGuard(pc_55, temp_var_110);

                    effects.send(pc_55, var_$tmp6.restrict(pc_55), var_$tmp7.restrict(pc_55), null);

                }
                if (jumpedOut_16 || jumpedOut_17) {
                    pc_52 = pc_54.or(pc_55);
                    jumpedOut_14 = true;
                }

            }
            if (!pc_53.isFalse()) {
                // 'else' branch
                PrimitiveVS<Boolean> temp_var_111;
                temp_var_111 = scheduler.getNextBoolean(pc_53);
                var_$tmp8 = var_$tmp8.updateUnderGuard(pc_53, temp_var_111);

                PrimitiveVS<Boolean> temp_var_112 = var_$tmp8.restrict(pc_53);
                Guard pc_56 = BooleanVS.getTrueGuard(temp_var_112);
                Guard pc_57 = BooleanVS.getFalseGuard(temp_var_112);
                boolean jumpedOut_18 = false;
                boolean jumpedOut_19 = false;
                if (!pc_56.isFalse()) {
                    // 'then' branch
                    PrimitiveVS<Boolean> temp_var_113;
                    temp_var_113 = scheduler.getNextBoolean(pc_56);
                    var_$tmp9 = var_$tmp9.updateUnderGuard(pc_56, temp_var_113);

                    PrimitiveVS<Boolean> temp_var_114 = var_$tmp9.restrict(pc_56);
                    Guard pc_58 = BooleanVS.getTrueGuard(temp_var_114);
                    Guard pc_59 = BooleanVS.getFalseGuard(temp_var_114);
                    boolean jumpedOut_20 = false;
                    boolean jumpedOut_21 = false;
                    if (!pc_58.isFalse()) {
                        // 'then' branch
                        PrimitiveVS<Machine> temp_var_115;
                        temp_var_115 = (PrimitiveVS<Machine>)((var_cache.restrict(pc_58)).getField(1));
                        var_$tmp10 = var_$tmp10.updateUnderGuard(pc_58, temp_var_115);

                        PrimitiveVS<Machine> temp_var_116;
                        temp_var_116 = var_$tmp10.restrict(pc_58);
                        var_$tmp11 = var_$tmp11.updateUnderGuard(pc_58, temp_var_116);

                        PrimitiveVS<Event> temp_var_117;
                        temp_var_117 = new PrimitiveVS<Event>(ask_share).restrict(pc_58);
                        var_$tmp12 = var_$tmp12.updateUnderGuard(pc_58, temp_var_117);

                        effects.send(pc_58, var_$tmp11.restrict(pc_58), var_$tmp12.restrict(pc_58), null);

                    }
                    if (!pc_59.isFalse()) {
                        // 'else' branch
                        PrimitiveVS<Machine> temp_var_118;
                        temp_var_118 = (PrimitiveVS<Machine>)((var_cache.restrict(pc_59)).getField(1));
                        var_$tmp13 = var_$tmp13.updateUnderGuard(pc_59, temp_var_118);

                        PrimitiveVS<Machine> temp_var_119;
                        temp_var_119 = var_$tmp13.restrict(pc_59);
                        var_$tmp14 = var_$tmp14.updateUnderGuard(pc_59, temp_var_119);

                        PrimitiveVS<Event> temp_var_120;
                        temp_var_120 = new PrimitiveVS<Event>(ask_excl).restrict(pc_59);
                        var_$tmp15 = var_$tmp15.updateUnderGuard(pc_59, temp_var_120);

                        effects.send(pc_59, var_$tmp14.restrict(pc_59), var_$tmp15.restrict(pc_59), null);

                    }
                    if (jumpedOut_20 || jumpedOut_21) {
                        pc_56 = pc_58.or(pc_59);
                        jumpedOut_18 = true;
                    }

                }
                if (!pc_57.isFalse()) {
                    // 'else' branch
                    PrimitiveVS<Boolean> temp_var_121;
                    temp_var_121 = scheduler.getNextBoolean(pc_57);
                    var_$tmp16 = var_$tmp16.updateUnderGuard(pc_57, temp_var_121);

                    PrimitiveVS<Boolean> temp_var_122 = var_$tmp16.restrict(pc_57);
                    Guard pc_60 = BooleanVS.getTrueGuard(temp_var_122);
                    Guard pc_61 = BooleanVS.getFalseGuard(temp_var_122);
                    boolean jumpedOut_22 = false;
                    boolean jumpedOut_23 = false;
                    if (!pc_60.isFalse()) {
                        // 'then' branch
                        PrimitiveVS<Machine> temp_var_123;
                        temp_var_123 = (PrimitiveVS<Machine>)((var_cache.restrict(pc_60)).getField(2));
                        var_$tmp17 = var_$tmp17.updateUnderGuard(pc_60, temp_var_123);

                        PrimitiveVS<Machine> temp_var_124;
                        temp_var_124 = var_$tmp17.restrict(pc_60);
                        var_$tmp18 = var_$tmp18.updateUnderGuard(pc_60, temp_var_124);

                        PrimitiveVS<Event> temp_var_125;
                        temp_var_125 = new PrimitiveVS<Event>(ask_share).restrict(pc_60);
                        var_$tmp19 = var_$tmp19.updateUnderGuard(pc_60, temp_var_125);

                        effects.send(pc_60, var_$tmp18.restrict(pc_60), var_$tmp19.restrict(pc_60), null);

                    }
                    if (!pc_61.isFalse()) {
                        // 'else' branch
                        PrimitiveVS<Machine> temp_var_126;
                        temp_var_126 = (PrimitiveVS<Machine>)((var_cache.restrict(pc_61)).getField(2));
                        var_$tmp20 = var_$tmp20.updateUnderGuard(pc_61, temp_var_126);

                        PrimitiveVS<Machine> temp_var_127;
                        temp_var_127 = var_$tmp20.restrict(pc_61);
                        var_$tmp21 = var_$tmp21.updateUnderGuard(pc_61, temp_var_127);

                        PrimitiveVS<Event> temp_var_128;
                        temp_var_128 = new PrimitiveVS<Event>(ask_excl).restrict(pc_61);
                        var_$tmp22 = var_$tmp22.updateUnderGuard(pc_61, temp_var_128);

                        effects.send(pc_61, var_$tmp21.restrict(pc_61), var_$tmp22.restrict(pc_61), null);

                    }
                    if (jumpedOut_22 || jumpedOut_23) {
                        pc_57 = pc_60.or(pc_61);
                        jumpedOut_19 = true;
                    }

                }
                if (jumpedOut_18 || jumpedOut_19) {
                    pc_53 = pc_56.or(pc_57);
                    jumpedOut_15 = true;
                }

            }
            if (jumpedOut_14 || jumpedOut_15) {
                pc_51 = pc_52.or(pc_53);
            }

            PrimitiveVS<Boolean> temp_var_129;
            temp_var_129 = (var_req_count.restrict(pc_51)).apply(new PrimitiveVS<Integer>(3).restrict(pc_51), (temp_var_130, temp_var_131) -> temp_var_130 < temp_var_131);
            var_$tmp23 = var_$tmp23.updateUnderGuard(pc_51, temp_var_129);

            PrimitiveVS<Boolean> temp_var_132 = var_$tmp23.restrict(pc_51);
            Guard pc_62 = BooleanVS.getTrueGuard(temp_var_132);
            Guard pc_63 = BooleanVS.getFalseGuard(temp_var_132);
            boolean jumpedOut_24 = false;
            boolean jumpedOut_25 = false;
            if (!pc_62.isFalse()) {
                // 'then' branch
                PrimitiveVS<Integer> temp_var_133;
                temp_var_133 = (var_req_count.restrict(pc_62)).apply(new PrimitiveVS<Integer>(1).restrict(pc_62), (temp_var_134, temp_var_135) -> temp_var_134 + temp_var_135);
                var_$tmp24 = var_$tmp24.updateUnderGuard(pc_62, temp_var_133);

                PrimitiveVS<Integer> temp_var_136;
                temp_var_136 = var_$tmp24.restrict(pc_62);
                var_req_count = var_req_count.updateUnderGuard(pc_62, temp_var_136);

                PrimitiveVS<Event> temp_var_137;
                temp_var_137 = new PrimitiveVS<Event>(unit).restrict(pc_62);
                var_$tmp25 = var_$tmp25.updateUnderGuard(pc_62, temp_var_137);

                // NOTE (TODO): We currently perform no typechecking on the payload!
                outcome.raiseGuardedEvent(pc_62, var_$tmp25.restrict(pc_62));
                pc_62 = Guard.constFalse();
                jumpedOut_24 = true;

            }
            if (!pc_63.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_24 || jumpedOut_25) {
                pc_51 = pc_62.or(pc_63);
            }

            if (!pc_51.isFalse()) {
            }
            return pc_51;
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
