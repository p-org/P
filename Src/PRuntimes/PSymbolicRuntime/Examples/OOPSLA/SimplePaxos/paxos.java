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

public class paxos implements Program {
    
    public static Scheduler scheduler;
    
    @Override
    public void setScheduler (Scheduler s) { this.scheduler = s; }
    
    
    
    // Skipping EnumElem 'EQKEY'

    // Skipping EnumElem 'EQVAL'

    // Skipping EnumElem 'NEQKEY'

    // Skipping EnumElem 'NEQVAL'

    // Skipping EnumElem 'DEFAULT'

    // Skipping PEnum 'tPreds'

    public static Event _null = new Event("_null");
    public static Event _halt = new Event("_halt");
    public static Event write = new Event("write");
    public static Event writeResp = new Event("writeResp");
    public static Event read = new Event("read");
    public static Event readResp = new Event("readResp");
    public static Event prepare = new Event("prepare");
    public static Event accept = new Event("accept");
    public static Event agree = new Event("agree");
    public static Event reject = new Event("reject");
    public static Event accepted = new Event("accepted");
    // Skipping Interface 'Main'

    // Skipping Interface 'AcceptorMachine'

    // Skipping Interface 'ProposerMachine'

    // Skipping Interface 'TestClient'

    public static class Main extends Machine {
        
        static State Init = new State("Init") {
            @Override public void entry(Guard pc_0, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((Main)machine).anonfun_0(pc_0, machine.sendBuffer);
            }
        };
        private PrimitiveVS<Integer> var_GC_NumOfAccptNodes = new PrimitiveVS<Integer>(0);
        private PrimitiveVS<Integer> var_GC_NumOfProposerNodes = new PrimitiveVS<Integer>(0);
        
        @Override
        public void reset() {
                super.reset();
                var_GC_NumOfAccptNodes = new PrimitiveVS<Integer>(0);
                var_GC_NumOfProposerNodes = new PrimitiveVS<Integer>(0);
        }
        
        @Override
        public List<ValueSummary> getLocalState() {
                List<ValueSummary> res = super.getLocalState();
                res.add(var_GC_NumOfAccptNodes);
                res.add(var_GC_NumOfProposerNodes);
                return res;
        }
        
        public Main(int id) {
            super("Main", id, EventBufferSemantics.queue, Init, Init
                
            );
            Init.addHandlers();
        }
        
        void 
        anonfun_0(
            Guard pc_1,
            EventBuffer effects
        ) {
            ListVS<PrimitiveVS<Machine>> var_proposers =
                new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_1);
            
            ListVS<PrimitiveVS<Machine>> var_acceptors =
                new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_1);
            
            PrimitiveVS<Machine> var_temp =
                new PrimitiveVS<Machine>().restrict(pc_1);
            
            PrimitiveVS<Integer> var_index =
                new PrimitiveVS<Integer>(0).restrict(pc_1);
            
            PrimitiveVS<Boolean> var_$tmp0 =
                new PrimitiveVS<Boolean>(false).restrict(pc_1);
            
            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_1);
            
            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_1);
            
            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_1);
            
            PrimitiveVS<Integer> var_$tmp4 =
                new PrimitiveVS<Integer>(0).restrict(pc_1);
            
            PrimitiveVS<Boolean> var_$tmp5 =
                new PrimitiveVS<Boolean>(false).restrict(pc_1);
            
            PrimitiveVS<Boolean> var_$tmp6 =
                new PrimitiveVS<Boolean>(false).restrict(pc_1);
            
            ListVS<PrimitiveVS<Machine>> var_$tmp7 =
                new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_1);
            
            PrimitiveVS<Integer> var_$tmp8 =
                new PrimitiveVS<Integer>(0).restrict(pc_1);
            
            PrimitiveVS<Machine> var_$tmp9 =
                new PrimitiveVS<Machine>().restrict(pc_1);
            
            PrimitiveVS<Machine> var_$tmp10 =
                new PrimitiveVS<Machine>().restrict(pc_1);
            
            PrimitiveVS<Integer> var_$tmp11 =
                new PrimitiveVS<Integer>(0).restrict(pc_1);
            
            ListVS<PrimitiveVS<Machine>> var_$tmp12 =
                new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_1);
            
            PrimitiveVS<Integer> temp_var_0;
            temp_var_0 = new PrimitiveVS<Integer>(1).restrict(pc_1);
            var_GC_NumOfAccptNodes = var_GC_NumOfAccptNodes.updateUnderGuard(pc_1, temp_var_0);
            
            PrimitiveVS<Integer> temp_var_1;
            temp_var_1 = new PrimitiveVS<Integer>(1).restrict(pc_1);
            var_GC_NumOfProposerNodes = var_GC_NumOfProposerNodes.updateUnderGuard(pc_1, temp_var_1);
            
            PrimitiveVS<Integer> temp_var_2;
            temp_var_2 = new PrimitiveVS<Integer>(0).restrict(pc_1);
            var_index = var_index.updateUnderGuard(pc_1, temp_var_2);
            
            java.util.List<Guard> loop_exits_0 = new java.util.ArrayList<>();
            boolean loop_early_ret_0 = false;
            Guard pc_2 = pc_1;
            while (!pc_2.isFalse()) {
                PrimitiveVS<Boolean> temp_var_3;
                temp_var_3 = (var_index.restrict(pc_2)).apply(var_GC_NumOfAccptNodes.restrict(pc_2), (temp_var_4, temp_var_5) -> temp_var_4 < temp_var_5);
                var_$tmp0 = var_$tmp0.updateUnderGuard(pc_2, temp_var_3);
                
                PrimitiveVS<Boolean> temp_var_6;
                temp_var_6 = var_$tmp0.restrict(pc_2);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_2, temp_var_6);
                
                PrimitiveVS<Boolean> temp_var_7 = var_$tmp1.restrict(pc_2);
                Guard pc_3 = BooleanVS.getTrueGuard(temp_var_7);
                Guard pc_4 = BooleanVS.getFalseGuard(temp_var_7);
                boolean jumpedOut_0 = false;
                boolean jumpedOut_1 = false;
                if (!pc_3.isFalse()) {
                    // 'then' branch
                }
                if (!pc_4.isFalse()) {
                    // 'else' branch
                    loop_exits_0.add(pc_4);
                    jumpedOut_1 = true;
                    pc_4 = Guard.constFalse();
                    
                }
                if (jumpedOut_0 || jumpedOut_1) {
                    pc_2 = pc_3.or(pc_4);
                }
                
                if (!pc_2.isFalse()) {
                    PrimitiveVS<Machine> temp_var_8;
                    temp_var_8 = effects.create(pc_2, scheduler, AcceptorMachine.class, (i) -> new AcceptorMachine(i));
                    var_$tmp2 = var_$tmp2.updateUnderGuard(pc_2, temp_var_8);
                    
                    PrimitiveVS<Machine> temp_var_9;
                    temp_var_9 = var_$tmp2.restrict(pc_2);
                    var_temp = var_temp.updateUnderGuard(pc_2, temp_var_9);
                    
                    PrimitiveVS<Machine> temp_var_10;
                    temp_var_10 = var_temp.restrict(pc_2);
                    var_$tmp3 = var_$tmp3.updateUnderGuard(pc_2, temp_var_10);
                    
                    ListVS<PrimitiveVS<Machine>> temp_var_11 = var_acceptors.restrict(pc_2);    
                    temp_var_11 = var_acceptors.restrict(pc_2).insert(var_index.restrict(pc_2), var_$tmp3.restrict(pc_2));
                    var_acceptors = var_acceptors.updateUnderGuard(pc_2, temp_var_11);
                    
                    PrimitiveVS<Integer> temp_var_12;
                    temp_var_12 = (var_index.restrict(pc_2)).apply(new PrimitiveVS<Integer>(1).restrict(pc_2), (temp_var_13, temp_var_14) -> temp_var_13 + temp_var_14);
                    var_$tmp4 = var_$tmp4.updateUnderGuard(pc_2, temp_var_12);
                    
                    PrimitiveVS<Integer> temp_var_15;
                    temp_var_15 = var_$tmp4.restrict(pc_2);
                    var_index = var_index.updateUnderGuard(pc_2, temp_var_15);
                    
                }
            }
            if (loop_early_ret_0) {
                pc_1 = Guard.orMany(loop_exits_0);
            }
            
            PrimitiveVS<Integer> temp_var_16;
            temp_var_16 = new PrimitiveVS<Integer>(0).restrict(pc_1);
            var_index = var_index.updateUnderGuard(pc_1, temp_var_16);
            
            java.util.List<Guard> loop_exits_1 = new java.util.ArrayList<>();
            boolean loop_early_ret_1 = false;
            Guard pc_5 = pc_1;
            while (!pc_5.isFalse()) {
                PrimitiveVS<Boolean> temp_var_17;
                temp_var_17 = (var_index.restrict(pc_5)).apply(var_GC_NumOfProposerNodes.restrict(pc_5), (temp_var_18, temp_var_19) -> temp_var_18 < temp_var_19);
                var_$tmp5 = var_$tmp5.updateUnderGuard(pc_5, temp_var_17);
                
                PrimitiveVS<Boolean> temp_var_20;
                temp_var_20 = var_$tmp5.restrict(pc_5);
                var_$tmp6 = var_$tmp6.updateUnderGuard(pc_5, temp_var_20);
                
                PrimitiveVS<Boolean> temp_var_21 = var_$tmp6.restrict(pc_5);
                Guard pc_6 = BooleanVS.getTrueGuard(temp_var_21);
                Guard pc_7 = BooleanVS.getFalseGuard(temp_var_21);
                boolean jumpedOut_2 = false;
                boolean jumpedOut_3 = false;
                if (!pc_6.isFalse()) {
                    // 'then' branch
                }
                if (!pc_7.isFalse()) {
                    // 'else' branch
                    loop_exits_1.add(pc_7);
                    jumpedOut_3 = true;
                    pc_7 = Guard.constFalse();
                    
                }
                if (jumpedOut_2 || jumpedOut_3) {
                    pc_5 = pc_6.or(pc_7);
                }
                
                if (!pc_5.isFalse()) {
                    ListVS<PrimitiveVS<Machine>> temp_var_22;
                    temp_var_22 = var_acceptors.restrict(pc_5);
                    var_$tmp7 = var_$tmp7.updateUnderGuard(pc_5, temp_var_22);
                    
                    PrimitiveVS<Integer> temp_var_23;
                    temp_var_23 = (var_index.restrict(pc_5)).apply(new PrimitiveVS<Integer>(1).restrict(pc_5), (temp_var_24, temp_var_25) -> temp_var_24 + temp_var_25);
                    var_$tmp8 = var_$tmp8.updateUnderGuard(pc_5, temp_var_23);
                    
                    PrimitiveVS<Machine> temp_var_26;
                    temp_var_26 = effects.create(pc_5, scheduler, ProposerMachine.class, new UnionVS (new TupleVS (var_$tmp7.restrict(pc_5), var_$tmp8.restrict(pc_5))), (i) -> new ProposerMachine(i));
                    var_$tmp9 = var_$tmp9.updateUnderGuard(pc_5, temp_var_26);
                    
                    PrimitiveVS<Machine> temp_var_27;
                    temp_var_27 = var_$tmp9.restrict(pc_5);
                    var_temp = var_temp.updateUnderGuard(pc_5, temp_var_27);
                    
                    PrimitiveVS<Machine> temp_var_28;
                    temp_var_28 = var_temp.restrict(pc_5);
                    var_$tmp10 = var_$tmp10.updateUnderGuard(pc_5, temp_var_28);
                    
                    ListVS<PrimitiveVS<Machine>> temp_var_29 = var_proposers.restrict(pc_5);    
                    temp_var_29 = var_proposers.restrict(pc_5).insert(var_index.restrict(pc_5), var_$tmp10.restrict(pc_5));
                    var_proposers = var_proposers.updateUnderGuard(pc_5, temp_var_29);
                    
                    PrimitiveVS<Integer> temp_var_30;
                    temp_var_30 = (var_index.restrict(pc_5)).apply(new PrimitiveVS<Integer>(1).restrict(pc_5), (temp_var_31, temp_var_32) -> temp_var_31 + temp_var_32);
                    var_$tmp11 = var_$tmp11.updateUnderGuard(pc_5, temp_var_30);
                    
                    PrimitiveVS<Integer> temp_var_33;
                    temp_var_33 = var_$tmp11.restrict(pc_5);
                    var_index = var_index.updateUnderGuard(pc_5, temp_var_33);
                    
                }
            }
            if (loop_early_ret_1) {
                pc_1 = Guard.orMany(loop_exits_1);
            }
            
            ListVS<PrimitiveVS<Machine>> temp_var_34;
            temp_var_34 = var_proposers.restrict(pc_1);
            var_$tmp12 = var_$tmp12.updateUnderGuard(pc_1, temp_var_34);
            
            effects.create(pc_1, scheduler, TestClient.class, new UnionVS (var_$tmp12.restrict(pc_1)), (i) -> new TestClient(i));
            
        }
        
    }
    
    public static class AcceptorMachine extends Machine {
        
        static State Init = new State("Init") {
            @Override public void entry(Guard pc_8, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((AcceptorMachine)machine).anonfun_1(pc_8, machine.sendBuffer, outcome);
            }
        };
        static State WaitForRequests = new State("WaitForRequests") {
        };
        private NamedTupleVS var_lastRecvProposal = new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()));
        private PrimitiveVS<Integer> var_GC_NumOfAccptNodes = new PrimitiveVS<Integer>(0);
        private PrimitiveVS<Integer> var_GC_NumOfProposerNodes = new PrimitiveVS<Integer>(0);
        private MapVS<String, PredVS<String> /* pred enum tPreds */> var_store = new MapVS<String, PredVS<String> /* pred enum tPreds */>(Guard.constTrue());
        
        @Override
        public void reset() {
                super.reset();
                var_lastRecvProposal = new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()));
                var_GC_NumOfAccptNodes = new PrimitiveVS<Integer>(0);
                var_GC_NumOfProposerNodes = new PrimitiveVS<Integer>(0);
                var_store = new MapVS<String, PredVS<String> /* pred enum tPreds */>(Guard.constTrue());
        }
        
        @Override
        public List<ValueSummary> getLocalState() {
                List<ValueSummary> res = super.getLocalState();
                res.add(var_lastRecvProposal);
                res.add(var_GC_NumOfAccptNodes);
                res.add(var_GC_NumOfProposerNodes);
                res.add(var_store);
                return res;
        }
        
        public AcceptorMachine(int id) {
            super("AcceptorMachine", id, EventBufferSemantics.queue, Init, Init
                , WaitForRequests
                
            );
            Init.addHandlers();
            WaitForRequests.addHandlers(new EventHandler(read) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((AcceptorMachine)machine).anonfun_2(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PredVS<String> /* pred enum tPreds */()), payload));
                    }
                },
                new EventHandler(prepare) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((AcceptorMachine)machine).anonfun_3(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("proposer", new PrimitiveVS<Machine>(), "proposal", new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()))), payload));
                    }
                },
                new EventHandler(accept) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((AcceptorMachine)machine).anonfun_4(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("proposer", new PrimitiveVS<Machine>(), "proposal", new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()))), payload));
                    }
                });
        }
        
        Guard 
        anonfun_1(
            Guard pc_9,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            NamedTupleVS var_$tmp0 =
                new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_9);
            
            NamedTupleVS temp_var_35;
            temp_var_35 = new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_9);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_9, temp_var_35);
            
            NamedTupleVS temp_var_36;
            temp_var_36 = var_$tmp0.restrict(pc_9);
            var_lastRecvProposal = var_lastRecvProposal.updateUnderGuard(pc_9, temp_var_36);
            
            outcome.addGuardedGoto(pc_9, WaitForRequests);
            pc_9 = Guard.constFalse();
            
            return pc_9;
        }
        
        void 
        anonfun_2(
            Guard pc_10,
            EventBuffer effects,
            NamedTupleVS var_req
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_10);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_10);
            
            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_10);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp3 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_10);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp4 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_10);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp5 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_10);
            
            NamedTupleVS var_$tmp6 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_10);
            
            PrimitiveVS<Machine> temp_var_37;
            temp_var_37 = (PrimitiveVS<Machine>)((var_req.restrict(pc_10)).getField("client"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_10, temp_var_37);
            
            PrimitiveVS<Machine> temp_var_38;
            temp_var_38 = var_$tmp0.restrict(pc_10);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_10, temp_var_38);
            
            PrimitiveVS<Event> temp_var_39;
            temp_var_39 = new PrimitiveVS<Event>(readResp).restrict(pc_10);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_10, temp_var_39);
            
            PredVS<String> /* pred enum tPreds */ temp_var_40;
            temp_var_40 = (PredVS<String> /* pred enum tPreds */)((var_req.restrict(pc_10)).getField("key"));
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_10, temp_var_40);
            
            PredVS<String> /* pred enum tPreds */ temp_var_41;
            temp_var_41 = (PredVS<String> /* pred enum tPreds */)((var_req.restrict(pc_10)).getField("key"));
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_10, temp_var_41);
            
            PredVS<String> /* pred enum tPreds */ temp_var_42;
            temp_var_42 = var_store.restrict(pc_10).get(var_$tmp4.restrict(pc_10));
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_10, temp_var_42);
            
            NamedTupleVS temp_var_43;
            temp_var_43 = new NamedTupleVS("key", var_$tmp3.restrict(pc_10), "val", var_$tmp5.restrict(pc_10));
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_10, temp_var_43);
            
            effects.send(pc_10, var_$tmp1.restrict(pc_10), var_$tmp2.restrict(pc_10), new UnionVS(var_$tmp6.restrict(pc_10)));
            
        }
        
        void 
        anonfun_3(
            Guard pc_11,
            EventBuffer effects,
            NamedTupleVS var_payload
        ) {
            NamedTupleVS var_$tmp0 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_11);
            
            NamedTupleVS var_$tmp1 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_11);
            
            PrimitiveVS<Boolean> var_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_11);
            
            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_11);
            
            PrimitiveVS<Machine> var_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_11);
            
            PrimitiveVS<Event> var_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_11);
            
            NamedTupleVS var_$tmp6 =
                new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_11);
            
            NamedTupleVS var_$tmp7 =
                new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_11);
            
            NamedTupleVS var_$tmp8 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_11);
            
            NamedTupleVS var_$tmp9 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_11);
            
            PrimitiveVS<Boolean> var_$tmp10 =
                new PrimitiveVS<Boolean>(false).restrict(pc_11);
            
            PrimitiveVS<Machine> var_$tmp11 =
                new PrimitiveVS<Machine>().restrict(pc_11);
            
            PrimitiveVS<Machine> var_$tmp12 =
                new PrimitiveVS<Machine>().restrict(pc_11);
            
            PrimitiveVS<Event> var_$tmp13 =
                new PrimitiveVS<Event>(_null).restrict(pc_11);
            
            NamedTupleVS var_$tmp14 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_11);
            
            PrimitiveVS<Machine> var_$tmp15 =
                new PrimitiveVS<Machine>().restrict(pc_11);
            
            PrimitiveVS<Machine> var_$tmp16 =
                new PrimitiveVS<Machine>().restrict(pc_11);
            
            PrimitiveVS<Event> var_$tmp17 =
                new PrimitiveVS<Event>(_null).restrict(pc_11);
            
            NamedTupleVS var_$tmp18 =
                new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_11);
            
            NamedTupleVS var_$tmp19 =
                new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_11);
            
            NamedTupleVS var_$tmp20 =
                new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_11);
            
            NamedTupleVS var_inline_0_id1 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_11);
            
            NamedTupleVS var_inline_0_id2 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_11);
            
            PrimitiveVS<Integer> var_local_0_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_11);
            
            PrimitiveVS<Integer> var_local_0_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_11);
            
            PrimitiveVS<Boolean> var_local_0_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_11);
            
            PrimitiveVS<Integer> var_local_0_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_11);
            
            PrimitiveVS<Integer> var_local_0_$tmp4 =
                new PrimitiveVS<Integer>(0).restrict(pc_11);
            
            PrimitiveVS<Boolean> var_local_0_$tmp5 =
                new PrimitiveVS<Boolean>(false).restrict(pc_11);
            
            PrimitiveVS<Integer> var_local_0_$tmp6 =
                new PrimitiveVS<Integer>(0).restrict(pc_11);
            
            PrimitiveVS<Integer> var_local_0_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_11);
            
            PrimitiveVS<Boolean> var_local_0_$tmp8 =
                new PrimitiveVS<Boolean>(false).restrict(pc_11);
            
            NamedTupleVS temp_var_44;
            temp_var_44 = (NamedTupleVS)((var_lastRecvProposal.restrict(pc_11)).getField("value"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_11, temp_var_44);
            
            NamedTupleVS temp_var_45;
            temp_var_45 = new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_11);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_11, temp_var_45);
            
            PrimitiveVS<Boolean> temp_var_46;
            temp_var_46 = var_$tmp0.restrict(pc_11).symbolicEquals(var_$tmp1.restrict(pc_11), pc_11);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_11, temp_var_46);
            
            PrimitiveVS<Boolean> temp_var_47 = var_$tmp2.restrict(pc_11);
            Guard pc_12 = BooleanVS.getTrueGuard(temp_var_47);
            Guard pc_13 = BooleanVS.getFalseGuard(temp_var_47);
            boolean jumpedOut_4 = false;
            boolean jumpedOut_5 = false;
            if (!pc_12.isFalse()) {
                // 'then' branch
                PrimitiveVS<Machine> temp_var_48;
                temp_var_48 = (PrimitiveVS<Machine>)((var_payload.restrict(pc_12)).getField("proposer"));
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_12, temp_var_48);
                
                PrimitiveVS<Machine> temp_var_49;
                temp_var_49 = var_$tmp3.restrict(pc_12);
                var_$tmp4 = var_$tmp4.updateUnderGuard(pc_12, temp_var_49);
                
                PrimitiveVS<Event> temp_var_50;
                temp_var_50 = new PrimitiveVS<Event>(agree).restrict(pc_12);
                var_$tmp5 = var_$tmp5.updateUnderGuard(pc_12, temp_var_50);
                
                NamedTupleVS temp_var_51;
                temp_var_51 = new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_12);
                var_$tmp6 = var_$tmp6.updateUnderGuard(pc_12, temp_var_51);
                
                effects.send(pc_12, var_$tmp4.restrict(pc_12), var_$tmp5.restrict(pc_12), new UnionVS(var_$tmp6.restrict(pc_12)));
                
            }
            if (!pc_13.isFalse()) {
                // 'else' branch
                NamedTupleVS temp_var_52;
                temp_var_52 = (NamedTupleVS)((var_payload.restrict(pc_13)).getField("proposal"));
                var_$tmp7 = var_$tmp7.updateUnderGuard(pc_13, temp_var_52);
                
                NamedTupleVS temp_var_53;
                temp_var_53 = (NamedTupleVS)((var_$tmp7.restrict(pc_13)).getField("pid"));
                var_$tmp8 = var_$tmp8.updateUnderGuard(pc_13, temp_var_53);
                
                NamedTupleVS temp_var_54;
                temp_var_54 = (NamedTupleVS)((var_lastRecvProposal.restrict(pc_13)).getField("pid"));
                var_$tmp9 = var_$tmp9.updateUnderGuard(pc_13, temp_var_54);
                
                NamedTupleVS temp_var_55;
                temp_var_55 = var_$tmp8.restrict(pc_13);
                var_inline_0_id1 = var_inline_0_id1.updateUnderGuard(pc_13, temp_var_55);
                
                NamedTupleVS temp_var_56;
                temp_var_56 = var_$tmp9.restrict(pc_13);
                var_inline_0_id2 = var_inline_0_id2.updateUnderGuard(pc_13, temp_var_56);
                
                PrimitiveVS<Integer> temp_var_57;
                temp_var_57 = (PrimitiveVS<Integer>)((var_inline_0_id1.restrict(pc_13)).getField("round"));
                var_local_0_$tmp0 = var_local_0_$tmp0.updateUnderGuard(pc_13, temp_var_57);
                
                PrimitiveVS<Integer> temp_var_58;
                temp_var_58 = (PrimitiveVS<Integer>)((var_inline_0_id2.restrict(pc_13)).getField("round"));
                var_local_0_$tmp1 = var_local_0_$tmp1.updateUnderGuard(pc_13, temp_var_58);
                
                PrimitiveVS<Boolean> temp_var_59;
                temp_var_59 = (var_local_0_$tmp0.restrict(pc_13)).apply(var_local_0_$tmp1.restrict(pc_13), (temp_var_60, temp_var_61) -> temp_var_60 < temp_var_61);
                var_local_0_$tmp2 = var_local_0_$tmp2.updateUnderGuard(pc_13, temp_var_59);
                
                PrimitiveVS<Boolean> temp_var_62 = var_local_0_$tmp2.restrict(pc_13);
                Guard pc_14 = BooleanVS.getTrueGuard(temp_var_62);
                Guard pc_15 = BooleanVS.getFalseGuard(temp_var_62);
                boolean jumpedOut_6 = false;
                boolean jumpedOut_7 = false;
                if (!pc_14.isFalse()) {
                    // 'then' branch
                    PrimitiveVS<Boolean> temp_var_63;
                    temp_var_63 = new PrimitiveVS<Boolean>(true).restrict(pc_14);
                    var_$tmp10 = var_$tmp10.updateUnderGuard(pc_14, temp_var_63);
                    
                }
                if (!pc_15.isFalse()) {
                    // 'else' branch
                    PrimitiveVS<Integer> temp_var_64;
                    temp_var_64 = (PrimitiveVS<Integer>)((var_inline_0_id1.restrict(pc_15)).getField("round"));
                    var_local_0_$tmp3 = var_local_0_$tmp3.updateUnderGuard(pc_15, temp_var_64);
                    
                    PrimitiveVS<Integer> temp_var_65;
                    temp_var_65 = (PrimitiveVS<Integer>)((var_inline_0_id2.restrict(pc_15)).getField("round"));
                    var_local_0_$tmp4 = var_local_0_$tmp4.updateUnderGuard(pc_15, temp_var_65);
                    
                    PrimitiveVS<Boolean> temp_var_66;
                    temp_var_66 = var_local_0_$tmp3.restrict(pc_15).symbolicEquals(var_local_0_$tmp4.restrict(pc_15), pc_15);
                    var_local_0_$tmp5 = var_local_0_$tmp5.updateUnderGuard(pc_15, temp_var_66);
                    
                    PrimitiveVS<Boolean> temp_var_67 = var_local_0_$tmp5.restrict(pc_15);
                    Guard pc_16 = BooleanVS.getTrueGuard(temp_var_67);
                    Guard pc_17 = BooleanVS.getFalseGuard(temp_var_67);
                    boolean jumpedOut_8 = false;
                    boolean jumpedOut_9 = false;
                    if (!pc_16.isFalse()) {
                        // 'then' branch
                        PrimitiveVS<Integer> temp_var_68;
                        temp_var_68 = (PrimitiveVS<Integer>)((var_inline_0_id1.restrict(pc_16)).getField("serverid"));
                        var_local_0_$tmp6 = var_local_0_$tmp6.updateUnderGuard(pc_16, temp_var_68);
                        
                        PrimitiveVS<Integer> temp_var_69;
                        temp_var_69 = (PrimitiveVS<Integer>)((var_inline_0_id2.restrict(pc_16)).getField("serverid"));
                        var_local_0_$tmp7 = var_local_0_$tmp7.updateUnderGuard(pc_16, temp_var_69);
                        
                        PrimitiveVS<Boolean> temp_var_70;
                        temp_var_70 = (var_local_0_$tmp6.restrict(pc_16)).apply(var_local_0_$tmp7.restrict(pc_16), (temp_var_71, temp_var_72) -> temp_var_71 < temp_var_72);
                        var_local_0_$tmp8 = var_local_0_$tmp8.updateUnderGuard(pc_16, temp_var_70);
                        
                        PrimitiveVS<Boolean> temp_var_73 = var_local_0_$tmp8.restrict(pc_16);
                        Guard pc_18 = BooleanVS.getTrueGuard(temp_var_73);
                        Guard pc_19 = BooleanVS.getFalseGuard(temp_var_73);
                        boolean jumpedOut_10 = false;
                        boolean jumpedOut_11 = false;
                        if (!pc_18.isFalse()) {
                            // 'then' branch
                            PrimitiveVS<Boolean> temp_var_74;
                            temp_var_74 = new PrimitiveVS<Boolean>(true).restrict(pc_18);
                            var_$tmp10 = var_$tmp10.updateUnderGuard(pc_18, temp_var_74);
                            
                        }
                        if (!pc_19.isFalse()) {
                            // 'else' branch
                            PrimitiveVS<Boolean> temp_var_75;
                            temp_var_75 = new PrimitiveVS<Boolean>(false).restrict(pc_19);
                            var_$tmp10 = var_$tmp10.updateUnderGuard(pc_19, temp_var_75);
                            
                        }
                        if (jumpedOut_10 || jumpedOut_11) {
                            pc_16 = pc_18.or(pc_19);
                            jumpedOut_8 = true;
                        }
                        
                    }
                    if (!pc_17.isFalse()) {
                        // 'else' branch
                        PrimitiveVS<Boolean> temp_var_76;
                        temp_var_76 = new PrimitiveVS<Boolean>(false).restrict(pc_17);
                        var_$tmp10 = var_$tmp10.updateUnderGuard(pc_17, temp_var_76);
                        
                    }
                    if (jumpedOut_8 || jumpedOut_9) {
                        pc_15 = pc_16.or(pc_17);
                        jumpedOut_7 = true;
                    }
                    
                }
                if (jumpedOut_6 || jumpedOut_7) {
                    pc_13 = pc_14.or(pc_15);
                    jumpedOut_5 = true;
                }
                
                PrimitiveVS<Boolean> temp_var_77 = var_$tmp10.restrict(pc_13);
                Guard pc_20 = BooleanVS.getTrueGuard(temp_var_77);
                Guard pc_21 = BooleanVS.getFalseGuard(temp_var_77);
                boolean jumpedOut_12 = false;
                boolean jumpedOut_13 = false;
                if (!pc_20.isFalse()) {
                    // 'then' branch
                    PrimitiveVS<Machine> temp_var_78;
                    temp_var_78 = (PrimitiveVS<Machine>)((var_payload.restrict(pc_20)).getField("proposer"));
                    var_$tmp11 = var_$tmp11.updateUnderGuard(pc_20, temp_var_78);
                    
                    PrimitiveVS<Machine> temp_var_79;
                    temp_var_79 = var_$tmp11.restrict(pc_20);
                    var_$tmp12 = var_$tmp12.updateUnderGuard(pc_20, temp_var_79);
                    
                    PrimitiveVS<Event> temp_var_80;
                    temp_var_80 = new PrimitiveVS<Event>(reject).restrict(pc_20);
                    var_$tmp13 = var_$tmp13.updateUnderGuard(pc_20, temp_var_80);
                    
                    NamedTupleVS temp_var_81;
                    temp_var_81 = (NamedTupleVS)((var_lastRecvProposal.restrict(pc_20)).getField("pid"));
                    var_$tmp14 = var_$tmp14.updateUnderGuard(pc_20, temp_var_81);
                    
                    effects.send(pc_20, var_$tmp12.restrict(pc_20), var_$tmp13.restrict(pc_20), new UnionVS(var_$tmp14.restrict(pc_20)));
                    
                }
                if (!pc_21.isFalse()) {
                    // 'else' branch
                    PrimitiveVS<Machine> temp_var_82;
                    temp_var_82 = (PrimitiveVS<Machine>)((var_payload.restrict(pc_21)).getField("proposer"));
                    var_$tmp15 = var_$tmp15.updateUnderGuard(pc_21, temp_var_82);
                    
                    PrimitiveVS<Machine> temp_var_83;
                    temp_var_83 = var_$tmp15.restrict(pc_21);
                    var_$tmp16 = var_$tmp16.updateUnderGuard(pc_21, temp_var_83);
                    
                    PrimitiveVS<Event> temp_var_84;
                    temp_var_84 = new PrimitiveVS<Event>(agree).restrict(pc_21);
                    var_$tmp17 = var_$tmp17.updateUnderGuard(pc_21, temp_var_84);
                    
                    NamedTupleVS temp_var_85;
                    temp_var_85 = var_lastRecvProposal.restrict(pc_21);
                    var_$tmp18 = var_$tmp18.updateUnderGuard(pc_21, temp_var_85);
                    
                    effects.send(pc_21, var_$tmp16.restrict(pc_21), var_$tmp17.restrict(pc_21), new UnionVS(var_$tmp18.restrict(pc_21)));
                    
                    NamedTupleVS temp_var_86;
                    temp_var_86 = (NamedTupleVS)((var_payload.restrict(pc_21)).getField("proposal"));
                    var_$tmp19 = var_$tmp19.updateUnderGuard(pc_21, temp_var_86);
                    
                    NamedTupleVS temp_var_87;
                    temp_var_87 = var_$tmp19.restrict(pc_21);
                    var_$tmp20 = var_$tmp20.updateUnderGuard(pc_21, temp_var_87);
                    
                    NamedTupleVS temp_var_88;
                    temp_var_88 = var_$tmp20.restrict(pc_21);
                    var_lastRecvProposal = var_lastRecvProposal.updateUnderGuard(pc_21, temp_var_88);
                    
                }
                if (jumpedOut_12 || jumpedOut_13) {
                    pc_13 = pc_20.or(pc_21);
                    jumpedOut_5 = true;
                }
                
            }
            if (jumpedOut_4 || jumpedOut_5) {
                pc_11 = pc_12.or(pc_13);
            }
            
        }
        
        void 
        anonfun_4(
            Guard pc_22,
            EventBuffer effects,
            NamedTupleVS var_payload
        ) {
            NamedTupleVS var_$tmp0 =
                new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_22);
            
            NamedTupleVS var_$tmp1 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_22);
            
            NamedTupleVS var_$tmp2 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_22);
            
            PrimitiveVS<Boolean> var_$tmp3 =
                new PrimitiveVS<Boolean>(false).restrict(pc_22);
            
            PrimitiveVS<Boolean> var_$tmp4 =
                new PrimitiveVS<Boolean>(false).restrict(pc_22);
            
            PrimitiveVS<Machine> var_$tmp5 =
                new PrimitiveVS<Machine>().restrict(pc_22);
            
            PrimitiveVS<Machine> var_$tmp6 =
                new PrimitiveVS<Machine>().restrict(pc_22);
            
            PrimitiveVS<Event> var_$tmp7 =
                new PrimitiveVS<Event>(_null).restrict(pc_22);
            
            NamedTupleVS var_$tmp8 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_22);
            
            NamedTupleVS var_$tmp9 =
                new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_22);
            
            NamedTupleVS var_$tmp10 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_22);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp11 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_22);
            
            NamedTupleVS var_$tmp12 =
                new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_22);
            
            NamedTupleVS var_$tmp13 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_22);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp14 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_22);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp15 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_22);
            
            NamedTupleVS var_$tmp16 =
                new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_22);
            
            NamedTupleVS var_$tmp17 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_22);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp18 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_22);
            
            NamedTupleVS var_$tmp19 =
                new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_22);
            
            NamedTupleVS var_$tmp20 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_22);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp21 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_22);
            
            PrimitiveVS<String> var_$tmp22 =
                new PrimitiveVS<String>("").restrict(pc_22);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp23 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_22);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp24 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_22);
            
            PrimitiveVS<String> var_$tmp25 =
                new PrimitiveVS<String>("").restrict(pc_22);
            
            PrimitiveVS<Machine> var_$tmp26 =
                new PrimitiveVS<Machine>().restrict(pc_22);
            
            PrimitiveVS<Machine> var_$tmp27 =
                new PrimitiveVS<Machine>().restrict(pc_22);
            
            PrimitiveVS<Event> var_$tmp28 =
                new PrimitiveVS<Event>(_null).restrict(pc_22);
            
            NamedTupleVS var_$tmp29 =
                new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_22);
            
            NamedTupleVS var_inline_1_id1 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_22);
            
            NamedTupleVS var_inline_1_id2 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_22);
            
            PrimitiveVS<Integer> var_local_1_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_22);
            
            PrimitiveVS<Integer> var_local_1_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_22);
            
            PrimitiveVS<Boolean> var_local_1_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_22);
            
            PrimitiveVS<Integer> var_local_1_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_22);
            
            PrimitiveVS<Integer> var_local_1_$tmp4 =
                new PrimitiveVS<Integer>(0).restrict(pc_22);
            
            PrimitiveVS<Boolean> var_local_1_$tmp5 =
                new PrimitiveVS<Boolean>(false).restrict(pc_22);
            
            PrimitiveVS<Boolean> var_local_1_$tmp6 =
                new PrimitiveVS<Boolean>(false).restrict(pc_22);
            
            NamedTupleVS temp_var_89;
            temp_var_89 = (NamedTupleVS)((var_payload.restrict(pc_22)).getField("proposal"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_22, temp_var_89);
            
            NamedTupleVS temp_var_90;
            temp_var_90 = (NamedTupleVS)((var_$tmp0.restrict(pc_22)).getField("pid"));
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_22, temp_var_90);
            
            NamedTupleVS temp_var_91;
            temp_var_91 = (NamedTupleVS)((var_lastRecvProposal.restrict(pc_22)).getField("pid"));
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_22, temp_var_91);
            
            NamedTupleVS temp_var_92;
            temp_var_92 = var_$tmp1.restrict(pc_22);
            var_inline_1_id1 = var_inline_1_id1.updateUnderGuard(pc_22, temp_var_92);
            
            NamedTupleVS temp_var_93;
            temp_var_93 = var_$tmp2.restrict(pc_22);
            var_inline_1_id2 = var_inline_1_id2.updateUnderGuard(pc_22, temp_var_93);
            
            PrimitiveVS<Integer> temp_var_94;
            temp_var_94 = (PrimitiveVS<Integer>)((var_inline_1_id1.restrict(pc_22)).getField("serverid"));
            var_local_1_$tmp0 = var_local_1_$tmp0.updateUnderGuard(pc_22, temp_var_94);
            
            PrimitiveVS<Integer> temp_var_95;
            temp_var_95 = (PrimitiveVS<Integer>)((var_inline_1_id2.restrict(pc_22)).getField("serverid"));
            var_local_1_$tmp1 = var_local_1_$tmp1.updateUnderGuard(pc_22, temp_var_95);
            
            PrimitiveVS<Boolean> temp_var_96;
            temp_var_96 = var_local_1_$tmp0.restrict(pc_22).symbolicEquals(var_local_1_$tmp1.restrict(pc_22), pc_22);
            var_local_1_$tmp2 = var_local_1_$tmp2.updateUnderGuard(pc_22, temp_var_96);
            
            PrimitiveVS<Boolean> temp_var_97;
            temp_var_97 = var_local_1_$tmp2.restrict(pc_22);
            var_local_1_$tmp6 = var_local_1_$tmp6.updateUnderGuard(pc_22, temp_var_97);
            
            PrimitiveVS<Boolean> temp_var_98 = var_local_1_$tmp6.restrict(pc_22);
            Guard pc_23 = BooleanVS.getTrueGuard(temp_var_98);
            Guard pc_24 = BooleanVS.getFalseGuard(temp_var_98);
            boolean jumpedOut_14 = false;
            boolean jumpedOut_15 = false;
            if (!pc_23.isFalse()) {
                // 'then' branch
                PrimitiveVS<Integer> temp_var_99;
                temp_var_99 = (PrimitiveVS<Integer>)((var_inline_1_id1.restrict(pc_23)).getField("round"));
                var_local_1_$tmp3 = var_local_1_$tmp3.updateUnderGuard(pc_23, temp_var_99);
                
                PrimitiveVS<Integer> temp_var_100;
                temp_var_100 = (PrimitiveVS<Integer>)((var_inline_1_id2.restrict(pc_23)).getField("round"));
                var_local_1_$tmp4 = var_local_1_$tmp4.updateUnderGuard(pc_23, temp_var_100);
                
                PrimitiveVS<Boolean> temp_var_101;
                temp_var_101 = var_local_1_$tmp3.restrict(pc_23).symbolicEquals(var_local_1_$tmp4.restrict(pc_23), pc_23);
                var_local_1_$tmp5 = var_local_1_$tmp5.updateUnderGuard(pc_23, temp_var_101);
                
                PrimitiveVS<Boolean> temp_var_102;
                temp_var_102 = var_local_1_$tmp5.restrict(pc_23);
                var_local_1_$tmp6 = var_local_1_$tmp6.updateUnderGuard(pc_23, temp_var_102);
                
            }
            if (jumpedOut_14 || jumpedOut_15) {
                pc_22 = pc_23.or(pc_24);
            }
            
            PrimitiveVS<Boolean> temp_var_103 = var_local_1_$tmp6.restrict(pc_22);
            Guard pc_25 = BooleanVS.getTrueGuard(temp_var_103);
            Guard pc_26 = BooleanVS.getFalseGuard(temp_var_103);
            boolean jumpedOut_16 = false;
            boolean jumpedOut_17 = false;
            if (!pc_25.isFalse()) {
                // 'then' branch
                PrimitiveVS<Boolean> temp_var_104;
                temp_var_104 = new PrimitiveVS<Boolean>(true).restrict(pc_25);
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_25, temp_var_104);
                
            }
            if (!pc_26.isFalse()) {
                // 'else' branch
                PrimitiveVS<Boolean> temp_var_105;
                temp_var_105 = new PrimitiveVS<Boolean>(false).restrict(pc_26);
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_26, temp_var_105);
                
            }
            if (jumpedOut_16 || jumpedOut_17) {
                pc_22 = pc_25.or(pc_26);
            }
            
            PrimitiveVS<Boolean> temp_var_106;
            temp_var_106 = (var_$tmp3.restrict(pc_22)).apply((temp_var_107) -> !temp_var_107);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_22, temp_var_106);
            
            PrimitiveVS<Boolean> temp_var_108 = var_$tmp4.restrict(pc_22);
            Guard pc_27 = BooleanVS.getTrueGuard(temp_var_108);
            Guard pc_28 = BooleanVS.getFalseGuard(temp_var_108);
            boolean jumpedOut_18 = false;
            boolean jumpedOut_19 = false;
            if (!pc_27.isFalse()) {
                // 'then' branch
                PrimitiveVS<Machine> temp_var_109;
                temp_var_109 = (PrimitiveVS<Machine>)((var_payload.restrict(pc_27)).getField("proposer"));
                var_$tmp5 = var_$tmp5.updateUnderGuard(pc_27, temp_var_109);
                
                PrimitiveVS<Machine> temp_var_110;
                temp_var_110 = var_$tmp5.restrict(pc_27);
                var_$tmp6 = var_$tmp6.updateUnderGuard(pc_27, temp_var_110);
                
                PrimitiveVS<Event> temp_var_111;
                temp_var_111 = new PrimitiveVS<Event>(reject).restrict(pc_27);
                var_$tmp7 = var_$tmp7.updateUnderGuard(pc_27, temp_var_111);
                
                NamedTupleVS temp_var_112;
                temp_var_112 = (NamedTupleVS)((var_lastRecvProposal.restrict(pc_27)).getField("pid"));
                var_$tmp8 = var_$tmp8.updateUnderGuard(pc_27, temp_var_112);
                
                effects.send(pc_27, var_$tmp6.restrict(pc_27), var_$tmp7.restrict(pc_27), new UnionVS(var_$tmp8.restrict(pc_27)));
                
            }
            if (!pc_28.isFalse()) {
                // 'else' branch
                NamedTupleVS temp_var_113;
                temp_var_113 = (NamedTupleVS)((var_payload.restrict(pc_28)).getField("proposal"));
                var_$tmp9 = var_$tmp9.updateUnderGuard(pc_28, temp_var_113);
                
                NamedTupleVS temp_var_114;
                temp_var_114 = (NamedTupleVS)((var_$tmp9.restrict(pc_28)).getField("value"));
                var_$tmp10 = var_$tmp10.updateUnderGuard(pc_28, temp_var_114);
                
                PredVS<String> /* pred enum tPreds */ temp_var_115;
                temp_var_115 = (PredVS<String> /* pred enum tPreds */)((var_$tmp10.restrict(pc_28)).getField("key"));
                var_$tmp11 = var_$tmp11.updateUnderGuard(pc_28, temp_var_115);
                
                NamedTupleVS temp_var_116;
                temp_var_116 = (NamedTupleVS)((var_payload.restrict(pc_28)).getField("proposal"));
                var_$tmp12 = var_$tmp12.updateUnderGuard(pc_28, temp_var_116);
                
                NamedTupleVS temp_var_117;
                temp_var_117 = (NamedTupleVS)((var_$tmp12.restrict(pc_28)).getField("value"));
                var_$tmp13 = var_$tmp13.updateUnderGuard(pc_28, temp_var_117);
                
                PredVS<String> /* pred enum tPreds */ temp_var_118;
                temp_var_118 = (PredVS<String> /* pred enum tPreds */)((var_$tmp13.restrict(pc_28)).getField("val"));
                var_$tmp14 = var_$tmp14.updateUnderGuard(pc_28, temp_var_118);
                
                PredVS<String> /* pred enum tPreds */ temp_var_119;
                temp_var_119 = var_$tmp14.restrict(pc_28);
                var_$tmp15 = var_$tmp15.updateUnderGuard(pc_28, temp_var_119);
                
                MapVS<String, PredVS<String> /* pred enum tPreds */> temp_var_120 = var_store.restrict(pc_28);    
                PredVS<String> /* pred enum tPreds */ temp_var_122 = var_$tmp11.restrict(pc_28);
                PredVS<String> /* pred enum tPreds */ temp_var_121;
                temp_var_121 = var_$tmp15.restrict(pc_28);
                temp_var_120 = temp_var_120.put(temp_var_122, temp_var_121);
                var_store = var_store.updateUnderGuard(pc_28, temp_var_120);
                
                NamedTupleVS temp_var_123;
                temp_var_123 = (NamedTupleVS)((var_payload.restrict(pc_28)).getField("proposal"));
                var_$tmp16 = var_$tmp16.updateUnderGuard(pc_28, temp_var_123);
                
                NamedTupleVS temp_var_124;
                temp_var_124 = (NamedTupleVS)((var_$tmp16.restrict(pc_28)).getField("value"));
                var_$tmp17 = var_$tmp17.updateUnderGuard(pc_28, temp_var_124);
                
                PredVS<String> /* pred enum tPreds */ temp_var_125;
                temp_var_125 = (PredVS<String> /* pred enum tPreds */)((var_$tmp17.restrict(pc_28)).getField("key"));
                var_$tmp18 = var_$tmp18.updateUnderGuard(pc_28, temp_var_125);
                
                NamedTupleVS temp_var_126;
                temp_var_126 = (NamedTupleVS)((var_payload.restrict(pc_28)).getField("proposal"));
                var_$tmp19 = var_$tmp19.updateUnderGuard(pc_28, temp_var_126);
                
                NamedTupleVS temp_var_127;
                temp_var_127 = (NamedTupleVS)((var_$tmp19.restrict(pc_28)).getField("value"));
                var_$tmp20 = var_$tmp20.updateUnderGuard(pc_28, temp_var_127);
                
                PredVS<String> /* pred enum tPreds */ temp_var_128;
                temp_var_128 = (PredVS<String> /* pred enum tPreds */)((var_$tmp20.restrict(pc_28)).getField("val"));
                var_$tmp21 = var_$tmp21.updateUnderGuard(pc_28, temp_var_128);
                
                PrimitiveVS<String> temp_var_129;
                temp_var_129 = new PrimitiveVS<String>(String.format("wrote key, value {0} %s, {1} %s", var_$tmp18.restrict(pc_28), var_$tmp21.restrict(pc_28))).restrict(pc_28);
                var_$tmp22 = var_$tmp22.updateUnderGuard(pc_28, temp_var_129);
                
                System.out.println((var_$tmp22.restrict(pc_28)).toString());
                PredVS<String> /* pred enum tPreds */ temp_var_130;
                temp_var_130 = new PredVS<String>("EQKEY").restrict(pc_28);
                var_$tmp23 = var_$tmp23.updateUnderGuard(pc_28, temp_var_130);
                
                PredVS<String> /* pred enum tPreds */ temp_var_131;
                temp_var_131 = var_store.restrict(pc_28).get(new PredVS<String>("EQKEY").restrict(pc_28));
                var_$tmp24 = var_$tmp24.updateUnderGuard(pc_28, temp_var_131);
                
                PrimitiveVS<String> temp_var_132;
                temp_var_132 = new PrimitiveVS<String>(String.format("key, value {0} %s, {1} %s", var_$tmp23.restrict(pc_28), var_$tmp24.restrict(pc_28))).restrict(pc_28);
                var_$tmp25 = var_$tmp25.updateUnderGuard(pc_28, temp_var_132);
                
                System.out.println((var_$tmp25.restrict(pc_28)).toString());
                PrimitiveVS<Machine> temp_var_133;
                temp_var_133 = (PrimitiveVS<Machine>)((var_payload.restrict(pc_28)).getField("proposer"));
                var_$tmp26 = var_$tmp26.updateUnderGuard(pc_28, temp_var_133);
                
                PrimitiveVS<Machine> temp_var_134;
                temp_var_134 = var_$tmp26.restrict(pc_28);
                var_$tmp27 = var_$tmp27.updateUnderGuard(pc_28, temp_var_134);
                
                PrimitiveVS<Event> temp_var_135;
                temp_var_135 = new PrimitiveVS<Event>(accepted).restrict(pc_28);
                var_$tmp28 = var_$tmp28.updateUnderGuard(pc_28, temp_var_135);
                
                NamedTupleVS temp_var_136;
                temp_var_136 = (NamedTupleVS)((var_payload.restrict(pc_28)).getField("proposal"));
                var_$tmp29 = var_$tmp29.updateUnderGuard(pc_28, temp_var_136);
                
                effects.send(pc_28, var_$tmp27.restrict(pc_28), var_$tmp28.restrict(pc_28), new UnionVS(var_$tmp29.restrict(pc_28)));
                
            }
            if (jumpedOut_18 || jumpedOut_19) {
                pc_22 = pc_27.or(pc_28);
            }
            
        }
        
    }
    
    public static class ProposerMachine extends Machine {
        
        static State Init = new State("Init") {
            @Override public void entry(Guard pc_29, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((ProposerMachine)machine).anonfun_5(pc_29, machine.sendBuffer, outcome, payload != null ? (TupleVS) ValueSummary.castFromAny(pc_29, new TupleVS(new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()), new PrimitiveVS<Integer>(0)).restrict(pc_29), payload) : new TupleVS(new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()), new PrimitiveVS<Integer>(0)).restrict(pc_29));
            }
        };
        static State WaitForClient = new State("WaitForClient") {
        };
        static State ProposerPhaseOne = new State("ProposerPhaseOne") {
            @Override public void entry(Guard pc_30, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((ProposerMachine)machine).anonfun_6(pc_30, machine.sendBuffer);
            }
        };
        static State ProposerPhaseTwo = new State("ProposerPhaseTwo") {
            @Override public void entry(Guard pc_31, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((ProposerMachine)machine).anonfun_7(pc_31, machine.sendBuffer);
            }
        };
        private ListVS<PrimitiveVS<Machine>> var_acceptors = new ListVS<PrimitiveVS<Machine>>(Guard.constTrue());
        private PrimitiveVS<Integer> var_majority = new PrimitiveVS<Integer>(0);
        private PrimitiveVS<Integer> var_serverid = new PrimitiveVS<Integer>(0);
        private NamedTupleVS var_proposeValue = new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */());
        private NamedTupleVS var_nextProposalId = new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0));
        private PrimitiveVS<Integer> var_GC_NumOfAccptNodes = new PrimitiveVS<Integer>(0);
        private PrimitiveVS<Integer> var_GC_NumOfProposerNodes = new PrimitiveVS<Integer>(0);
        private PrimitiveVS<Machine> var_client = new PrimitiveVS<Machine>();
        private PrimitiveVS<Integer> var_numOfAgreeRecv = new PrimitiveVS<Integer>(0);
        private PrimitiveVS<Integer> var_numOfAcceptRecv = new PrimitiveVS<Integer>(0);
        private NamedTupleVS var_promisedAgree = new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()));
        
        @Override
        public void reset() {
                super.reset();
                var_acceptors = new ListVS<PrimitiveVS<Machine>>(Guard.constTrue());
                var_majority = new PrimitiveVS<Integer>(0);
                var_serverid = new PrimitiveVS<Integer>(0);
                var_proposeValue = new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */());
                var_nextProposalId = new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0));
                var_GC_NumOfAccptNodes = new PrimitiveVS<Integer>(0);
                var_GC_NumOfProposerNodes = new PrimitiveVS<Integer>(0);
                var_client = new PrimitiveVS<Machine>();
                var_numOfAgreeRecv = new PrimitiveVS<Integer>(0);
                var_numOfAcceptRecv = new PrimitiveVS<Integer>(0);
                var_promisedAgree = new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()));
        }
        
        @Override
        public List<ValueSummary> getLocalState() {
                List<ValueSummary> res = super.getLocalState();
                res.add(var_acceptors);
                res.add(var_majority);
                res.add(var_serverid);
                res.add(var_proposeValue);
                res.add(var_nextProposalId);
                res.add(var_GC_NumOfAccptNodes);
                res.add(var_GC_NumOfProposerNodes);
                res.add(var_client);
                res.add(var_numOfAgreeRecv);
                res.add(var_numOfAcceptRecv);
                res.add(var_promisedAgree);
                return res;
        }
        
        public ProposerMachine(int id) {
            super("ProposerMachine", id, EventBufferSemantics.queue, Init, Init
                , WaitForClient
                , ProposerPhaseOne
                , ProposerPhaseTwo
                
            );
            Init.addHandlers();
            WaitForClient.addHandlers(new EventHandler(write) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((ProposerMachine)machine).anonfun_8(pc, machine.sendBuffer, outcome, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())), payload));
                    }
                },
                new EventHandler(read) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((ProposerMachine)machine).anonfun_9(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PredVS<String> /* pred enum tPreds */()), payload));
                    }
                });
            ProposerPhaseOne.addHandlers(new DeferEventHandler(write)
                ,
                new IgnoreEventHandler(accepted),
                new EventHandler(agree) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((ProposerMachine)machine).anonfun_10(pc, machine.sendBuffer, outcome, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())), payload));
                    }
                },
                new EventHandler(reject) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((ProposerMachine)machine).anonfun_11(pc, machine.sendBuffer, outcome, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), payload));
                    }
                },
                new EventHandler(read) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((ProposerMachine)machine).anonfun_12(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PredVS<String> /* pred enum tPreds */()), payload));
                    }
                });
            ProposerPhaseTwo.addHandlers(new IgnoreEventHandler(agree),
                new DeferEventHandler(write)
                ,
                new DeferEventHandler(read)
                ,
                new EventHandler(reject) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((ProposerMachine)machine).anonfun_13(pc, machine.sendBuffer, outcome, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), payload));
                    }
                },
                new EventHandler(accepted) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((ProposerMachine)machine).anonfun_14(pc, machine.sendBuffer, outcome, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())), payload));
                    }
                });
        }
        
        Guard 
        anonfun_5(
            Guard pc_32,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            TupleVS var_payload
        ) {
            ListVS<PrimitiveVS<Machine>> var_$tmp0 =
                new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_32);
            
            ListVS<PrimitiveVS<Machine>> var_$tmp1 =
                new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_32);
            
            PrimitiveVS<Integer> var_$tmp2 =
                new PrimitiveVS<Integer>(0).restrict(pc_32);
            
            PrimitiveVS<Integer> var_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_32);
            
            PrimitiveVS<Integer> temp_var_137;
            temp_var_137 = new PrimitiveVS<Integer>(1).restrict(pc_32);
            var_GC_NumOfAccptNodes = var_GC_NumOfAccptNodes.updateUnderGuard(pc_32, temp_var_137);
            
            PrimitiveVS<Integer> temp_var_138;
            temp_var_138 = new PrimitiveVS<Integer>(1).restrict(pc_32);
            var_GC_NumOfProposerNodes = var_GC_NumOfProposerNodes.updateUnderGuard(pc_32, temp_var_138);
            
            ListVS<PrimitiveVS<Machine>> temp_var_139;
            temp_var_139 = (ListVS<PrimitiveVS<Machine>>)((var_payload.restrict(pc_32)).getField(0));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_32, temp_var_139);
            
            ListVS<PrimitiveVS<Machine>> temp_var_140;
            temp_var_140 = var_$tmp0.restrict(pc_32);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_32, temp_var_140);
            
            ListVS<PrimitiveVS<Machine>> temp_var_141;
            temp_var_141 = var_$tmp1.restrict(pc_32);
            var_acceptors = var_acceptors.updateUnderGuard(pc_32, temp_var_141);
            
            PrimitiveVS<Integer> temp_var_142;
            temp_var_142 = (PrimitiveVS<Integer>)((var_payload.restrict(pc_32)).getField(1));
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_32, temp_var_142);
            
            PrimitiveVS<Integer> temp_var_143;
            temp_var_143 = var_$tmp2.restrict(pc_32);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_32, temp_var_143);
            
            PrimitiveVS<Integer> temp_var_144;
            temp_var_144 = var_$tmp3.restrict(pc_32);
            var_serverid = var_serverid.updateUnderGuard(pc_32, temp_var_144);
            
            outcome.addGuardedGoto(pc_32, WaitForClient);
            pc_32 = Guard.constFalse();
            
            return pc_32;
        }
        
        Guard 
        anonfun_8(
            Guard pc_33,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            NamedTupleVS var_req
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_33);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_33);
            
            NamedTupleVS var_$tmp2 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_33);
            
            NamedTupleVS var_$tmp3 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_33);
            
            NamedTupleVS var_$tmp4 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_33);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp5 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_33);
            
            NamedTupleVS var_$tmp6 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_33);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp7 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_33);
            
            PrimitiveVS<String> var_$tmp8 =
                new PrimitiveVS<String>("").restrict(pc_33);
            
            PrimitiveVS<Integer> var_$tmp9 =
                new PrimitiveVS<Integer>(0).restrict(pc_33);
            
            PrimitiveVS<Integer> var_$tmp10 =
                new PrimitiveVS<Integer>(0).restrict(pc_33);
            
            NamedTupleVS var_$tmp11 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_33);
            
            PrimitiveVS<Integer> var_$tmp12 =
                new PrimitiveVS<Integer>(0).restrict(pc_33);
            
            PrimitiveVS<Integer> var_$tmp13 =
                new PrimitiveVS<Integer>(0).restrict(pc_33);
            
            PrimitiveVS<Machine> temp_var_145;
            temp_var_145 = (PrimitiveVS<Machine>)((var_req.restrict(pc_33)).getField("client"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_33, temp_var_145);
            
            PrimitiveVS<Machine> temp_var_146;
            temp_var_146 = var_$tmp0.restrict(pc_33);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_33, temp_var_146);
            
            PrimitiveVS<Machine> temp_var_147;
            temp_var_147 = var_$tmp1.restrict(pc_33);
            var_client = var_client.updateUnderGuard(pc_33, temp_var_147);
            
            NamedTupleVS temp_var_148;
            temp_var_148 = (NamedTupleVS)((var_req.restrict(pc_33)).getField("rec"));
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_33, temp_var_148);
            
            NamedTupleVS temp_var_149;
            temp_var_149 = var_$tmp2.restrict(pc_33);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_33, temp_var_149);
            
            NamedTupleVS temp_var_150;
            temp_var_150 = var_$tmp3.restrict(pc_33);
            var_proposeValue = var_proposeValue.updateUnderGuard(pc_33, temp_var_150);
            
            NamedTupleVS temp_var_151;
            temp_var_151 = (NamedTupleVS)((var_req.restrict(pc_33)).getField("rec"));
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_33, temp_var_151);
            
            PredVS<String> /* pred enum tPreds */ temp_var_152;
            temp_var_152 = (PredVS<String> /* pred enum tPreds */)((var_$tmp4.restrict(pc_33)).getField("key"));
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_33, temp_var_152);
            
            NamedTupleVS temp_var_153;
            temp_var_153 = (NamedTupleVS)((var_req.restrict(pc_33)).getField("rec"));
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_33, temp_var_153);
            
            PredVS<String> /* pred enum tPreds */ temp_var_154;
            temp_var_154 = (PredVS<String> /* pred enum tPreds */)((var_$tmp6.restrict(pc_33)).getField("val"));
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_33, temp_var_154);
            
            PrimitiveVS<String> temp_var_155;
            temp_var_155 = new PrimitiveVS<String>(String.format("proposed write key {0} %s val {1} %s (before)", var_$tmp5.restrict(pc_33), var_$tmp7.restrict(pc_33))).restrict(pc_33);
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_33, temp_var_155);
            
            System.out.println((var_$tmp8.restrict(pc_33)).toString());
            PrimitiveVS<Integer> temp_var_156;
            temp_var_156 = var_serverid.restrict(pc_33);
            var_$tmp9 = var_$tmp9.updateUnderGuard(pc_33, temp_var_156);
            
            PrimitiveVS<Integer> temp_var_157;
            temp_var_157 = new PrimitiveVS<Integer>(1).restrict(pc_33);
            var_$tmp10 = var_$tmp10.updateUnderGuard(pc_33, temp_var_157);
            
            NamedTupleVS temp_var_158;
            temp_var_158 = new NamedTupleVS("serverid", var_$tmp9.restrict(pc_33), "round", var_$tmp10.restrict(pc_33));
            var_$tmp11 = var_$tmp11.updateUnderGuard(pc_33, temp_var_158);
            
            NamedTupleVS temp_var_159;
            temp_var_159 = var_$tmp11.restrict(pc_33);
            var_nextProposalId = var_nextProposalId.updateUnderGuard(pc_33, temp_var_159);
            
            PrimitiveVS<Integer> temp_var_160;
            temp_var_160 = (var_GC_NumOfAccptNodes.restrict(pc_33)).apply(new PrimitiveVS<Integer>(2).restrict(pc_33), (temp_var_161, temp_var_162) -> temp_var_161 / temp_var_162);
            var_$tmp12 = var_$tmp12.updateUnderGuard(pc_33, temp_var_160);
            
            PrimitiveVS<Integer> temp_var_163;
            temp_var_163 = (var_$tmp12.restrict(pc_33)).apply(new PrimitiveVS<Integer>(1).restrict(pc_33), (temp_var_164, temp_var_165) -> temp_var_164 + temp_var_165);
            var_$tmp13 = var_$tmp13.updateUnderGuard(pc_33, temp_var_163);
            
            PrimitiveVS<Integer> temp_var_166;
            temp_var_166 = var_$tmp13.restrict(pc_33);
            var_majority = var_majority.updateUnderGuard(pc_33, temp_var_166);
            
            outcome.addGuardedGoto(pc_33, ProposerPhaseOne);
            pc_33 = Guard.constFalse();
            
            return pc_33;
        }
        
        void 
        anonfun_9(
            Guard pc_34,
            EventBuffer effects,
            NamedTupleVS var_req
        ) {
            PrimitiveVS<Machine> var_acceptor =
                new PrimitiveVS<Machine>().restrict(pc_34);
            
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_34);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_34);
            
            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_34);
            
            NamedTupleVS var_$tmp3 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PredVS<String> /* pred enum tPreds */()).restrict(pc_34);
            
            PrimitiveVS<Machine> temp_var_167;
            temp_var_167 = (PrimitiveVS<Machine>) scheduler.getNextElement(var_acceptors.restrict(pc_34), pc_34);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_34, temp_var_167);
            
            PrimitiveVS<Machine> temp_var_168;
            temp_var_168 = var_$tmp0.restrict(pc_34);
            var_acceptor = var_acceptor.updateUnderGuard(pc_34, temp_var_168);
            
            PrimitiveVS<Machine> temp_var_169;
            temp_var_169 = var_acceptor.restrict(pc_34);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_34, temp_var_169);
            
            PrimitiveVS<Event> temp_var_170;
            temp_var_170 = new PrimitiveVS<Event>(read).restrict(pc_34);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_34, temp_var_170);
            
            NamedTupleVS temp_var_171;
            temp_var_171 = var_req.restrict(pc_34);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_34, temp_var_171);
            
            effects.send(pc_34, var_$tmp1.restrict(pc_34), var_$tmp2.restrict(pc_34), new UnionVS(var_$tmp3.restrict(pc_34)));
            
        }
        
        void 
        SendToAllAcceptors(
            Guard pc_35,
            EventBuffer effects,
            PrimitiveVS<Event> var_e,
            UnionVS var_v
        ) {
            PrimitiveVS<Integer> var_index =
                new PrimitiveVS<Integer>(0).restrict(pc_35);
            
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_35);
            
            PrimitiveVS<Boolean> var_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_35);
            
            PrimitiveVS<Boolean> var_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_35);
            
            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_35);
            
            PrimitiveVS<Machine> var_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_35);
            
            PrimitiveVS<Event> var_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_35);
            
            UnionVS var_$tmp6 =
                new UnionVS().restrict(pc_35);
            
            PrimitiveVS<Integer> var_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_35);
            
            PrimitiveVS<Integer> temp_var_172;
            temp_var_172 = new PrimitiveVS<Integer>(0).restrict(pc_35);
            var_index = var_index.updateUnderGuard(pc_35, temp_var_172);
            
            java.util.List<Guard> loop_exits_2 = new java.util.ArrayList<>();
            boolean loop_early_ret_2 = false;
            Guard pc_36 = pc_35;
            while (!pc_36.isFalse()) {
                PrimitiveVS<Integer> temp_var_173;
                temp_var_173 = var_acceptors.restrict(pc_36).size();
                var_$tmp0 = var_$tmp0.updateUnderGuard(pc_36, temp_var_173);
                
                PrimitiveVS<Boolean> temp_var_174;
                temp_var_174 = (var_index.restrict(pc_36)).apply(var_$tmp0.restrict(pc_36), (temp_var_175, temp_var_176) -> temp_var_175 < temp_var_176);
                var_$tmp1 = var_$tmp1.updateUnderGuard(pc_36, temp_var_174);
                
                PrimitiveVS<Boolean> temp_var_177;
                temp_var_177 = var_$tmp1.restrict(pc_36);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_36, temp_var_177);
                
                PrimitiveVS<Boolean> temp_var_178 = var_$tmp2.restrict(pc_36);
                Guard pc_37 = BooleanVS.getTrueGuard(temp_var_178);
                Guard pc_38 = BooleanVS.getFalseGuard(temp_var_178);
                boolean jumpedOut_20 = false;
                boolean jumpedOut_21 = false;
                if (!pc_37.isFalse()) {
                    // 'then' branch
                }
                if (!pc_38.isFalse()) {
                    // 'else' branch
                    loop_exits_2.add(pc_38);
                    jumpedOut_21 = true;
                    pc_38 = Guard.constFalse();
                    
                }
                if (jumpedOut_20 || jumpedOut_21) {
                    pc_36 = pc_37.or(pc_38);
                }
                
                if (!pc_36.isFalse()) {
                    PrimitiveVS<Machine> temp_var_179;
                    temp_var_179 = var_acceptors.restrict(pc_36).get(var_index.restrict(pc_36));
                    var_$tmp3 = var_$tmp3.updateUnderGuard(pc_36, temp_var_179);
                    
                    PrimitiveVS<Machine> temp_var_180;
                    temp_var_180 = var_$tmp3.restrict(pc_36);
                    var_$tmp4 = var_$tmp4.updateUnderGuard(pc_36, temp_var_180);
                    
                    PrimitiveVS<Event> temp_var_181;
                    temp_var_181 = var_e.restrict(pc_36);
                    var_$tmp5 = var_$tmp5.updateUnderGuard(pc_36, temp_var_181);
                    
                    UnionVS temp_var_182;
                    temp_var_182 = ValueSummary.castToAny(pc_36, var_v.restrict(pc_36));
                    var_$tmp6 = var_$tmp6.updateUnderGuard(pc_36, temp_var_182);
                    
                    effects.send(pc_36, var_$tmp4.restrict(pc_36), var_$tmp5.restrict(pc_36), new UnionVS(var_$tmp6.restrict(pc_36)));
                    
                    PrimitiveVS<Integer> temp_var_183;
                    temp_var_183 = (var_index.restrict(pc_36)).apply(new PrimitiveVS<Integer>(1).restrict(pc_36), (temp_var_184, temp_var_185) -> temp_var_184 + temp_var_185);
                    var_$tmp7 = var_$tmp7.updateUnderGuard(pc_36, temp_var_183);
                    
                    PrimitiveVS<Integer> temp_var_186;
                    temp_var_186 = var_$tmp7.restrict(pc_36);
                    var_index = var_index.updateUnderGuard(pc_36, temp_var_186);
                    
                }
            }
            if (loop_early_ret_2) {
                pc_35 = Guard.orMany(loop_exits_2);
            }
            
        }
        
        void 
        anonfun_6(
            Guard pc_39,
            EventBuffer effects
        ) {
            PrimitiveVS<Event> var_$tmp0 =
                new PrimitiveVS<Event>(_null).restrict(pc_39);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_39);
            
            NamedTupleVS var_$tmp2 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_39);
            
            NamedTupleVS var_$tmp3 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_39);
            
            NamedTupleVS var_$tmp4 =
                new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_39);
            
            NamedTupleVS var_$tmp5 =
                new NamedTupleVS("proposer", new PrimitiveVS<Machine>(), "proposal", new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()))).restrict(pc_39);
            
            PrimitiveVS<Event> var_inline_2_e =
                new PrimitiveVS<Event>(_null).restrict(pc_39);
            
            NamedTupleVS var_inline_2_v =
                new NamedTupleVS("proposer", new PrimitiveVS<Machine>(), "proposal", new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()))).restrict(pc_39);
            
            PrimitiveVS<Integer> var_local_2_index =
                new PrimitiveVS<Integer>(0).restrict(pc_39);
            
            PrimitiveVS<Integer> var_local_2_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_39);
            
            PrimitiveVS<Boolean> var_local_2_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_39);
            
            PrimitiveVS<Boolean> var_local_2_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_39);
            
            PrimitiveVS<Machine> var_local_2_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_39);
            
            PrimitiveVS<Machine> var_local_2_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_39);
            
            PrimitiveVS<Event> var_local_2_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_39);
            
            UnionVS var_local_2_$tmp6 =
                new UnionVS().restrict(pc_39);
            
            PrimitiveVS<Integer> var_local_2_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_39);
            
            PrimitiveVS<Integer> temp_var_187;
            temp_var_187 = new PrimitiveVS<Integer>(0).restrict(pc_39);
            var_numOfAgreeRecv = var_numOfAgreeRecv.updateUnderGuard(pc_39, temp_var_187);
            
            PrimitiveVS<Event> temp_var_188;
            temp_var_188 = new PrimitiveVS<Event>(prepare).restrict(pc_39);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_39, temp_var_188);
            
            PrimitiveVS<Machine> temp_var_189;
            temp_var_189 = new PrimitiveVS<Machine>(this).restrict(pc_39);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_39, temp_var_189);
            
            NamedTupleVS temp_var_190;
            temp_var_190 = var_nextProposalId.restrict(pc_39);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_39, temp_var_190);
            
            NamedTupleVS temp_var_191;
            temp_var_191 = var_proposeValue.restrict(pc_39);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_39, temp_var_191);
            
            NamedTupleVS temp_var_192;
            temp_var_192 = new NamedTupleVS("pid", var_$tmp2.restrict(pc_39), "value", var_$tmp3.restrict(pc_39));
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_39, temp_var_192);
            
            NamedTupleVS temp_var_193;
            temp_var_193 = new NamedTupleVS("proposer", var_$tmp1.restrict(pc_39), "proposal", var_$tmp4.restrict(pc_39));
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_39, temp_var_193);
            
            PrimitiveVS<Event> temp_var_194;
            temp_var_194 = var_$tmp0.restrict(pc_39);
            var_inline_2_e = var_inline_2_e.updateUnderGuard(pc_39, temp_var_194);
            
            NamedTupleVS temp_var_195;
            temp_var_195 = var_$tmp5.restrict(pc_39);
            var_inline_2_v = var_inline_2_v.updateUnderGuard(pc_39, temp_var_195);
            
            PrimitiveVS<Integer> temp_var_196;
            temp_var_196 = new PrimitiveVS<Integer>(0).restrict(pc_39);
            var_local_2_index = var_local_2_index.updateUnderGuard(pc_39, temp_var_196);
            
            java.util.List<Guard> loop_exits_3 = new java.util.ArrayList<>();
            boolean loop_early_ret_3 = false;
            Guard pc_40 = pc_39;
            while (!pc_40.isFalse()) {
                PrimitiveVS<Integer> temp_var_197;
                temp_var_197 = var_acceptors.restrict(pc_40).size();
                var_local_2_$tmp0 = var_local_2_$tmp0.updateUnderGuard(pc_40, temp_var_197);
                
                PrimitiveVS<Boolean> temp_var_198;
                temp_var_198 = (var_local_2_index.restrict(pc_40)).apply(var_local_2_$tmp0.restrict(pc_40), (temp_var_199, temp_var_200) -> temp_var_199 < temp_var_200);
                var_local_2_$tmp1 = var_local_2_$tmp1.updateUnderGuard(pc_40, temp_var_198);
                
                PrimitiveVS<Boolean> temp_var_201;
                temp_var_201 = var_local_2_$tmp1.restrict(pc_40);
                var_local_2_$tmp2 = var_local_2_$tmp2.updateUnderGuard(pc_40, temp_var_201);
                
                PrimitiveVS<Boolean> temp_var_202 = var_local_2_$tmp2.restrict(pc_40);
                Guard pc_41 = BooleanVS.getTrueGuard(temp_var_202);
                Guard pc_42 = BooleanVS.getFalseGuard(temp_var_202);
                boolean jumpedOut_22 = false;
                boolean jumpedOut_23 = false;
                if (!pc_41.isFalse()) {
                    // 'then' branch
                }
                if (!pc_42.isFalse()) {
                    // 'else' branch
                    loop_exits_3.add(pc_42);
                    jumpedOut_23 = true;
                    pc_42 = Guard.constFalse();
                    
                }
                if (jumpedOut_22 || jumpedOut_23) {
                    pc_40 = pc_41.or(pc_42);
                }
                
                if (!pc_40.isFalse()) {
                    PrimitiveVS<Machine> temp_var_203;
                    temp_var_203 = var_acceptors.restrict(pc_40).get(var_local_2_index.restrict(pc_40));
                    var_local_2_$tmp3 = var_local_2_$tmp3.updateUnderGuard(pc_40, temp_var_203);
                    
                    PrimitiveVS<Machine> temp_var_204;
                    temp_var_204 = var_local_2_$tmp3.restrict(pc_40);
                    var_local_2_$tmp4 = var_local_2_$tmp4.updateUnderGuard(pc_40, temp_var_204);
                    
                    PrimitiveVS<Event> temp_var_205;
                    temp_var_205 = var_inline_2_e.restrict(pc_40);
                    var_local_2_$tmp5 = var_local_2_$tmp5.updateUnderGuard(pc_40, temp_var_205);
                    
                    UnionVS temp_var_206;
                    temp_var_206 = ValueSummary.castToAny(pc_40, var_inline_2_v.restrict(pc_40));
                    var_local_2_$tmp6 = var_local_2_$tmp6.updateUnderGuard(pc_40, temp_var_206);
                    
                    effects.send(pc_40, var_local_2_$tmp4.restrict(pc_40), var_local_2_$tmp5.restrict(pc_40), new UnionVS(var_local_2_$tmp6.restrict(pc_40)));
                    
                    PrimitiveVS<Integer> temp_var_207;
                    temp_var_207 = (var_local_2_index.restrict(pc_40)).apply(new PrimitiveVS<Integer>(1).restrict(pc_40), (temp_var_208, temp_var_209) -> temp_var_208 + temp_var_209);
                    var_local_2_$tmp7 = var_local_2_$tmp7.updateUnderGuard(pc_40, temp_var_207);
                    
                    PrimitiveVS<Integer> temp_var_210;
                    temp_var_210 = var_local_2_$tmp7.restrict(pc_40);
                    var_local_2_index = var_local_2_index.updateUnderGuard(pc_40, temp_var_210);
                    
                }
            }
            if (loop_early_ret_3) {
                pc_39 = Guard.orMany(loop_exits_3);
            }
            
        }
        
        Guard 
        anonfun_10(
            Guard pc_43,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            NamedTupleVS var_payload
        ) {
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_43);
            
            NamedTupleVS var_$tmp1 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_43);
            
            PrimitiveVS<Integer> var_$tmp2 =
                new PrimitiveVS<Integer>(0).restrict(pc_43);
            
            NamedTupleVS var_$tmp3 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_43);
            
            PrimitiveVS<Integer> var_$tmp4 =
                new PrimitiveVS<Integer>(0).restrict(pc_43);
            
            NamedTupleVS var_$tmp5 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_43);
            
            PrimitiveVS<Integer> var_$tmp6 =
                new PrimitiveVS<Integer>(0).restrict(pc_43);
            
            NamedTupleVS var_$tmp7 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_43);
            
            PrimitiveVS<Integer> var_$tmp8 =
                new PrimitiveVS<Integer>(0).restrict(pc_43);
            
            PrimitiveVS<String> var_$tmp9 =
                new PrimitiveVS<String>("").restrict(pc_43);
            
            NamedTupleVS var_$tmp10 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_43);
            
            NamedTupleVS var_$tmp11 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_43);
            
            PrimitiveVS<Boolean> var_$tmp12 =
                new PrimitiveVS<Boolean>(false).restrict(pc_43);
            
            PrimitiveVS<String> var_$tmp13 =
                new PrimitiveVS<String>("").restrict(pc_43);
            
            NamedTupleVS var_$tmp14 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_43);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp15 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_43);
            
            NamedTupleVS var_$tmp16 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_43);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp17 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_43);
            
            PrimitiveVS<String> var_$tmp18 =
                new PrimitiveVS<String>("").restrict(pc_43);
            
            NamedTupleVS var_$tmp19 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_43);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp20 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_43);
            
            NamedTupleVS var_$tmp21 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_43);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp22 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_43);
            
            PrimitiveVS<String> var_$tmp23 =
                new PrimitiveVS<String>("").restrict(pc_43);
            
            PrimitiveVS<Boolean> var_$tmp24 =
                new PrimitiveVS<Boolean>(false).restrict(pc_43);
            
            NamedTupleVS var_inline_3_id1 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_43);
            
            NamedTupleVS var_inline_3_id2 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_43);
            
            PrimitiveVS<Integer> var_local_3_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_43);
            
            PrimitiveVS<Integer> var_local_3_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_43);
            
            PrimitiveVS<Boolean> var_local_3_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_43);
            
            PrimitiveVS<Integer> var_local_3_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_43);
            
            PrimitiveVS<Integer> var_local_3_$tmp4 =
                new PrimitiveVS<Integer>(0).restrict(pc_43);
            
            PrimitiveVS<Boolean> var_local_3_$tmp5 =
                new PrimitiveVS<Boolean>(false).restrict(pc_43);
            
            PrimitiveVS<Integer> var_local_3_$tmp6 =
                new PrimitiveVS<Integer>(0).restrict(pc_43);
            
            PrimitiveVS<Integer> var_local_3_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_43);
            
            PrimitiveVS<Boolean> var_local_3_$tmp8 =
                new PrimitiveVS<Boolean>(false).restrict(pc_43);
            
            PrimitiveVS<Integer> temp_var_211;
            temp_var_211 = (var_numOfAgreeRecv.restrict(pc_43)).apply(new PrimitiveVS<Integer>(1).restrict(pc_43), (temp_var_212, temp_var_213) -> temp_var_212 + temp_var_213);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_43, temp_var_211);
            
            PrimitiveVS<Integer> temp_var_214;
            temp_var_214 = var_$tmp0.restrict(pc_43);
            var_numOfAgreeRecv = var_numOfAgreeRecv.updateUnderGuard(pc_43, temp_var_214);
            
            NamedTupleVS temp_var_215;
            temp_var_215 = (NamedTupleVS)((var_promisedAgree.restrict(pc_43)).getField("pid"));
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_43, temp_var_215);
            
            PrimitiveVS<Integer> temp_var_216;
            temp_var_216 = (PrimitiveVS<Integer>)((var_$tmp1.restrict(pc_43)).getField("serverid"));
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_43, temp_var_216);
            
            NamedTupleVS temp_var_217;
            temp_var_217 = (NamedTupleVS)((var_promisedAgree.restrict(pc_43)).getField("pid"));
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_43, temp_var_217);
            
            PrimitiveVS<Integer> temp_var_218;
            temp_var_218 = (PrimitiveVS<Integer>)((var_$tmp3.restrict(pc_43)).getField("round"));
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_43, temp_var_218);
            
            NamedTupleVS temp_var_219;
            temp_var_219 = (NamedTupleVS)((var_payload.restrict(pc_43)).getField("pid"));
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_43, temp_var_219);
            
            PrimitiveVS<Integer> temp_var_220;
            temp_var_220 = (PrimitiveVS<Integer>)((var_$tmp5.restrict(pc_43)).getField("serverid"));
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_43, temp_var_220);
            
            NamedTupleVS temp_var_221;
            temp_var_221 = (NamedTupleVS)((var_payload.restrict(pc_43)).getField("pid"));
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_43, temp_var_221);
            
            PrimitiveVS<Integer> temp_var_222;
            temp_var_222 = (PrimitiveVS<Integer>)((var_$tmp7.restrict(pc_43)).getField("round"));
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_43, temp_var_222);
            
            PrimitiveVS<String> temp_var_223;
            temp_var_223 = new PrimitiveVS<String>(String.format("promised agree pid {0} %s {1} %s, payload pid {2} %s {3} %s", var_$tmp2.restrict(pc_43), var_$tmp4.restrict(pc_43), var_$tmp6.restrict(pc_43), var_$tmp8.restrict(pc_43))).restrict(pc_43);
            var_$tmp9 = var_$tmp9.updateUnderGuard(pc_43, temp_var_223);
            
            System.out.println((var_$tmp9.restrict(pc_43)).toString());
            NamedTupleVS temp_var_224;
            temp_var_224 = (NamedTupleVS)((var_promisedAgree.restrict(pc_43)).getField("pid"));
            var_$tmp10 = var_$tmp10.updateUnderGuard(pc_43, temp_var_224);
            
            NamedTupleVS temp_var_225;
            temp_var_225 = (NamedTupleVS)((var_payload.restrict(pc_43)).getField("pid"));
            var_$tmp11 = var_$tmp11.updateUnderGuard(pc_43, temp_var_225);
            
            NamedTupleVS temp_var_226;
            temp_var_226 = var_$tmp10.restrict(pc_43);
            var_inline_3_id1 = var_inline_3_id1.updateUnderGuard(pc_43, temp_var_226);
            
            NamedTupleVS temp_var_227;
            temp_var_227 = var_$tmp11.restrict(pc_43);
            var_inline_3_id2 = var_inline_3_id2.updateUnderGuard(pc_43, temp_var_227);
            
            PrimitiveVS<Integer> temp_var_228;
            temp_var_228 = (PrimitiveVS<Integer>)((var_inline_3_id1.restrict(pc_43)).getField("round"));
            var_local_3_$tmp0 = var_local_3_$tmp0.updateUnderGuard(pc_43, temp_var_228);
            
            PrimitiveVS<Integer> temp_var_229;
            temp_var_229 = (PrimitiveVS<Integer>)((var_inline_3_id2.restrict(pc_43)).getField("round"));
            var_local_3_$tmp1 = var_local_3_$tmp1.updateUnderGuard(pc_43, temp_var_229);
            
            PrimitiveVS<Boolean> temp_var_230;
            temp_var_230 = (var_local_3_$tmp0.restrict(pc_43)).apply(var_local_3_$tmp1.restrict(pc_43), (temp_var_231, temp_var_232) -> temp_var_231 < temp_var_232);
            var_local_3_$tmp2 = var_local_3_$tmp2.updateUnderGuard(pc_43, temp_var_230);
            
            PrimitiveVS<Boolean> temp_var_233 = var_local_3_$tmp2.restrict(pc_43);
            Guard pc_44 = BooleanVS.getTrueGuard(temp_var_233);
            Guard pc_45 = BooleanVS.getFalseGuard(temp_var_233);
            boolean jumpedOut_24 = false;
            boolean jumpedOut_25 = false;
            if (!pc_44.isFalse()) {
                // 'then' branch
                PrimitiveVS<Boolean> temp_var_234;
                temp_var_234 = new PrimitiveVS<Boolean>(true).restrict(pc_44);
                var_$tmp12 = var_$tmp12.updateUnderGuard(pc_44, temp_var_234);
                
            }
            if (!pc_45.isFalse()) {
                // 'else' branch
                PrimitiveVS<Integer> temp_var_235;
                temp_var_235 = (PrimitiveVS<Integer>)((var_inline_3_id1.restrict(pc_45)).getField("round"));
                var_local_3_$tmp3 = var_local_3_$tmp3.updateUnderGuard(pc_45, temp_var_235);
                
                PrimitiveVS<Integer> temp_var_236;
                temp_var_236 = (PrimitiveVS<Integer>)((var_inline_3_id2.restrict(pc_45)).getField("round"));
                var_local_3_$tmp4 = var_local_3_$tmp4.updateUnderGuard(pc_45, temp_var_236);
                
                PrimitiveVS<Boolean> temp_var_237;
                temp_var_237 = var_local_3_$tmp3.restrict(pc_45).symbolicEquals(var_local_3_$tmp4.restrict(pc_45), pc_45);
                var_local_3_$tmp5 = var_local_3_$tmp5.updateUnderGuard(pc_45, temp_var_237);
                
                PrimitiveVS<Boolean> temp_var_238 = var_local_3_$tmp5.restrict(pc_45);
                Guard pc_46 = BooleanVS.getTrueGuard(temp_var_238);
                Guard pc_47 = BooleanVS.getFalseGuard(temp_var_238);
                boolean jumpedOut_26 = false;
                boolean jumpedOut_27 = false;
                if (!pc_46.isFalse()) {
                    // 'then' branch
                    PrimitiveVS<Integer> temp_var_239;
                    temp_var_239 = (PrimitiveVS<Integer>)((var_inline_3_id1.restrict(pc_46)).getField("serverid"));
                    var_local_3_$tmp6 = var_local_3_$tmp6.updateUnderGuard(pc_46, temp_var_239);
                    
                    PrimitiveVS<Integer> temp_var_240;
                    temp_var_240 = (PrimitiveVS<Integer>)((var_inline_3_id2.restrict(pc_46)).getField("serverid"));
                    var_local_3_$tmp7 = var_local_3_$tmp7.updateUnderGuard(pc_46, temp_var_240);
                    
                    PrimitiveVS<Boolean> temp_var_241;
                    temp_var_241 = (var_local_3_$tmp6.restrict(pc_46)).apply(var_local_3_$tmp7.restrict(pc_46), (temp_var_242, temp_var_243) -> temp_var_242 < temp_var_243);
                    var_local_3_$tmp8 = var_local_3_$tmp8.updateUnderGuard(pc_46, temp_var_241);
                    
                    PrimitiveVS<Boolean> temp_var_244 = var_local_3_$tmp8.restrict(pc_46);
                    Guard pc_48 = BooleanVS.getTrueGuard(temp_var_244);
                    Guard pc_49 = BooleanVS.getFalseGuard(temp_var_244);
                    boolean jumpedOut_28 = false;
                    boolean jumpedOut_29 = false;
                    if (!pc_48.isFalse()) {
                        // 'then' branch
                        PrimitiveVS<Boolean> temp_var_245;
                        temp_var_245 = new PrimitiveVS<Boolean>(true).restrict(pc_48);
                        var_$tmp12 = var_$tmp12.updateUnderGuard(pc_48, temp_var_245);
                        
                    }
                    if (!pc_49.isFalse()) {
                        // 'else' branch
                        PrimitiveVS<Boolean> temp_var_246;
                        temp_var_246 = new PrimitiveVS<Boolean>(false).restrict(pc_49);
                        var_$tmp12 = var_$tmp12.updateUnderGuard(pc_49, temp_var_246);
                        
                    }
                    if (jumpedOut_28 || jumpedOut_29) {
                        pc_46 = pc_48.or(pc_49);
                        jumpedOut_26 = true;
                    }
                    
                }
                if (!pc_47.isFalse()) {
                    // 'else' branch
                    PrimitiveVS<Boolean> temp_var_247;
                    temp_var_247 = new PrimitiveVS<Boolean>(false).restrict(pc_47);
                    var_$tmp12 = var_$tmp12.updateUnderGuard(pc_47, temp_var_247);
                    
                }
                if (jumpedOut_26 || jumpedOut_27) {
                    pc_45 = pc_46.or(pc_47);
                    jumpedOut_25 = true;
                }
                
            }
            if (jumpedOut_24 || jumpedOut_25) {
                pc_43 = pc_44.or(pc_45);
            }
            
            PrimitiveVS<Boolean> temp_var_248 = var_$tmp12.restrict(pc_43);
            Guard pc_50 = BooleanVS.getTrueGuard(temp_var_248);
            Guard pc_51 = BooleanVS.getFalseGuard(temp_var_248);
            boolean jumpedOut_30 = false;
            boolean jumpedOut_31 = false;
            if (!pc_50.isFalse()) {
                // 'then' branch
                PrimitiveVS<String> temp_var_249;
                temp_var_249 = new PrimitiveVS<String>(String.format("proposal less than")).restrict(pc_50);
                var_$tmp13 = var_$tmp13.updateUnderGuard(pc_50, temp_var_249);
                
                System.out.println((var_$tmp13.restrict(pc_50)).toString());
                NamedTupleVS temp_var_250;
                temp_var_250 = (NamedTupleVS)((var_promisedAgree.restrict(pc_50)).getField("value"));
                var_$tmp14 = var_$tmp14.updateUnderGuard(pc_50, temp_var_250);
                
                PredVS<String> /* pred enum tPreds */ temp_var_251;
                temp_var_251 = (PredVS<String> /* pred enum tPreds */)((var_$tmp14.restrict(pc_50)).getField("key"));
                var_$tmp15 = var_$tmp15.updateUnderGuard(pc_50, temp_var_251);
                
                NamedTupleVS temp_var_252;
                temp_var_252 = (NamedTupleVS)((var_promisedAgree.restrict(pc_50)).getField("value"));
                var_$tmp16 = var_$tmp16.updateUnderGuard(pc_50, temp_var_252);
                
                PredVS<String> /* pred enum tPreds */ temp_var_253;
                temp_var_253 = (PredVS<String> /* pred enum tPreds */)((var_$tmp16.restrict(pc_50)).getField("val"));
                var_$tmp17 = var_$tmp17.updateUnderGuard(pc_50, temp_var_253);
                
                PrimitiveVS<String> temp_var_254;
                temp_var_254 = new PrimitiveVS<String>(String.format("promised agree pld key {0} %s val {1} %s (before)", var_$tmp15.restrict(pc_50), var_$tmp17.restrict(pc_50))).restrict(pc_50);
                var_$tmp18 = var_$tmp18.updateUnderGuard(pc_50, temp_var_254);
                
                System.out.println((var_$tmp18.restrict(pc_50)).toString());
                NamedTupleVS temp_var_255;
                temp_var_255 = var_payload.restrict(pc_50);
                var_promisedAgree = var_promisedAgree.updateUnderGuard(pc_50, temp_var_255);
                
                NamedTupleVS temp_var_256;
                temp_var_256 = (NamedTupleVS)((var_promisedAgree.restrict(pc_50)).getField("value"));
                var_$tmp19 = var_$tmp19.updateUnderGuard(pc_50, temp_var_256);
                
                PredVS<String> /* pred enum tPreds */ temp_var_257;
                temp_var_257 = (PredVS<String> /* pred enum tPreds */)((var_$tmp19.restrict(pc_50)).getField("key"));
                var_$tmp20 = var_$tmp20.updateUnderGuard(pc_50, temp_var_257);
                
                NamedTupleVS temp_var_258;
                temp_var_258 = (NamedTupleVS)((var_promisedAgree.restrict(pc_50)).getField("value"));
                var_$tmp21 = var_$tmp21.updateUnderGuard(pc_50, temp_var_258);
                
                PredVS<String> /* pred enum tPreds */ temp_var_259;
                temp_var_259 = (PredVS<String> /* pred enum tPreds */)((var_$tmp21.restrict(pc_50)).getField("val"));
                var_$tmp22 = var_$tmp22.updateUnderGuard(pc_50, temp_var_259);
                
                PrimitiveVS<String> temp_var_260;
                temp_var_260 = new PrimitiveVS<String>(String.format("promised agree pld key {0} %s val {1} %s (after)", var_$tmp20.restrict(pc_50), var_$tmp22.restrict(pc_50))).restrict(pc_50);
                var_$tmp23 = var_$tmp23.updateUnderGuard(pc_50, temp_var_260);
                
                System.out.println((var_$tmp23.restrict(pc_50)).toString());
            }
            if (!pc_51.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_30 || jumpedOut_31) {
                pc_43 = pc_50.or(pc_51);
            }
            
            PrimitiveVS<Boolean> temp_var_261;
            temp_var_261 = var_numOfAgreeRecv.restrict(pc_43).symbolicEquals(var_majority.restrict(pc_43), pc_43);
            var_$tmp24 = var_$tmp24.updateUnderGuard(pc_43, temp_var_261);
            
            PrimitiveVS<Boolean> temp_var_262 = var_$tmp24.restrict(pc_43);
            Guard pc_52 = BooleanVS.getTrueGuard(temp_var_262);
            Guard pc_53 = BooleanVS.getFalseGuard(temp_var_262);
            boolean jumpedOut_32 = false;
            boolean jumpedOut_33 = false;
            if (!pc_52.isFalse()) {
                // 'then' branch
                outcome.addGuardedGoto(pc_52, ProposerPhaseTwo);
                pc_52 = Guard.constFalse();
                jumpedOut_32 = true;
                
            }
            if (!pc_53.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_32 || jumpedOut_33) {
                pc_43 = pc_52.or(pc_53);
            }
            
            if (!pc_43.isFalse()) {
            }
            return pc_43;
        }
        
        Guard 
        anonfun_11(
            Guard pc_54,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            NamedTupleVS var_payload
        ) {
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_54);
            
            PrimitiveVS<Integer> var_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_54);
            
            PrimitiveVS<Boolean> var_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_54);
            
            PrimitiveVS<Integer> var_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_54);
            
            PrimitiveVS<Integer> var_$tmp4 =
                new PrimitiveVS<Integer>(0).restrict(pc_54);
            
            PrimitiveVS<Integer> temp_var_263;
            temp_var_263 = (PrimitiveVS<Integer>)((var_nextProposalId.restrict(pc_54)).getField("round"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_54, temp_var_263);
            
            PrimitiveVS<Integer> temp_var_264;
            temp_var_264 = (PrimitiveVS<Integer>)((var_payload.restrict(pc_54)).getField("round"));
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_54, temp_var_264);
            
            PrimitiveVS<Boolean> temp_var_265;
            temp_var_265 = (var_$tmp0.restrict(pc_54)).apply(var_$tmp1.restrict(pc_54), (temp_var_266, temp_var_267) -> temp_var_266 <= temp_var_267);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_54, temp_var_265);
            
            PrimitiveVS<Boolean> temp_var_268 = var_$tmp2.restrict(pc_54);
            Guard pc_55 = BooleanVS.getTrueGuard(temp_var_268);
            Guard pc_56 = BooleanVS.getFalseGuard(temp_var_268);
            boolean jumpedOut_34 = false;
            boolean jumpedOut_35 = false;
            if (!pc_55.isFalse()) {
                // 'then' branch
                PrimitiveVS<Integer> temp_var_269;
                temp_var_269 = (PrimitiveVS<Integer>)((var_payload.restrict(pc_55)).getField("round"));
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_55, temp_var_269);
                
                PrimitiveVS<Integer> temp_var_270;
                temp_var_270 = (var_$tmp3.restrict(pc_55)).apply(new PrimitiveVS<Integer>(1).restrict(pc_55), (temp_var_271, temp_var_272) -> temp_var_271 + temp_var_272);
                var_$tmp4 = var_$tmp4.updateUnderGuard(pc_55, temp_var_270);
                
                NamedTupleVS temp_var_273 = var_nextProposalId.restrict(pc_55);    
                PrimitiveVS<Integer> temp_var_274;
                temp_var_274 = var_$tmp4.restrict(pc_55);
                temp_var_273 = temp_var_273.setField("round", temp_var_274);
                var_nextProposalId = var_nextProposalId.updateUnderGuard(pc_55, temp_var_273);
                
            }
            if (!pc_56.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_34 || jumpedOut_35) {
                pc_54 = pc_55.or(pc_56);
            }
            
            outcome.addGuardedGoto(pc_54, ProposerPhaseOne);
            pc_54 = Guard.constFalse();
            
            return pc_54;
        }
        
        void 
        anonfun_12(
            Guard pc_57,
            EventBuffer effects,
            NamedTupleVS var_req
        ) {
            PrimitiveVS<Machine> var_acceptor =
                new PrimitiveVS<Machine>().restrict(pc_57);
            
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_57);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_57);
            
            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_57);
            
            NamedTupleVS var_$tmp3 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PredVS<String> /* pred enum tPreds */()).restrict(pc_57);
            
            PrimitiveVS<Machine> temp_var_275;
            temp_var_275 = (PrimitiveVS<Machine>) scheduler.getNextElement(var_acceptors.restrict(pc_57), pc_57);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_57, temp_var_275);
            
            PrimitiveVS<Machine> temp_var_276;
            temp_var_276 = var_$tmp0.restrict(pc_57);
            var_acceptor = var_acceptor.updateUnderGuard(pc_57, temp_var_276);
            
            PrimitiveVS<Machine> temp_var_277;
            temp_var_277 = var_acceptor.restrict(pc_57);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_57, temp_var_277);
            
            PrimitiveVS<Event> temp_var_278;
            temp_var_278 = new PrimitiveVS<Event>(read).restrict(pc_57);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_57, temp_var_278);
            
            NamedTupleVS temp_var_279;
            temp_var_279 = var_req.restrict(pc_57);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_57, temp_var_279);
            
            effects.send(pc_57, var_$tmp1.restrict(pc_57), var_$tmp2.restrict(pc_57), new UnionVS(var_$tmp3.restrict(pc_57)));
            
        }
        
        NamedTupleVS 
        GetValueToBeProposed(
            Guard pc_58,
            EventBuffer effects
        ) {
            NamedTupleVS var_$tmp0 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_58);
            
            NamedTupleVS var_$tmp1 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_58);
            
            PrimitiveVS<Boolean> var_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_58);
            
            NamedTupleVS var_$tmp3 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_58);
            
            NamedTupleVS retval = new NamedTupleVS(new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_58));
            NamedTupleVS temp_var_280;
            temp_var_280 = (NamedTupleVS)((var_promisedAgree.restrict(pc_58)).getField("value"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_58, temp_var_280);
            
            NamedTupleVS temp_var_281;
            temp_var_281 = new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_58);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_58, temp_var_281);
            
            PrimitiveVS<Boolean> temp_var_282;
            temp_var_282 = var_$tmp0.restrict(pc_58).symbolicEquals(var_$tmp1.restrict(pc_58), pc_58);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_58, temp_var_282);
            
            PrimitiveVS<Boolean> temp_var_283 = var_$tmp2.restrict(pc_58);
            Guard pc_59 = BooleanVS.getTrueGuard(temp_var_283);
            Guard pc_60 = BooleanVS.getFalseGuard(temp_var_283);
            boolean jumpedOut_36 = false;
            boolean jumpedOut_37 = false;
            if (!pc_59.isFalse()) {
                // 'then' branch
                retval = retval.updateUnderGuard(pc_59, var_proposeValue.restrict(pc_59));
                pc_59 = Guard.constFalse();
                jumpedOut_36 = true;
                
            }
            if (!pc_60.isFalse()) {
                // 'else' branch
                NamedTupleVS temp_var_284;
                temp_var_284 = (NamedTupleVS)((var_promisedAgree.restrict(pc_60)).getField("value"));
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_60, temp_var_284);
                
                retval = retval.updateUnderGuard(pc_60, var_$tmp3.restrict(pc_60));
                pc_60 = Guard.constFalse();
                jumpedOut_37 = true;
                
            }
            if (jumpedOut_36 || jumpedOut_37) {
                pc_58 = pc_59.or(pc_60);
            }
            
            return retval;
        }
        
        void 
        anonfun_7(
            Guard pc_61,
            EventBuffer effects
        ) {
            NamedTupleVS var_$tmp0 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_61);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp1 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_61);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp2 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_61);
            
            PrimitiveVS<String> var_$tmp3 =
                new PrimitiveVS<String>("").restrict(pc_61);
            
            PrimitiveVS<Event> var_$tmp4 =
                new PrimitiveVS<Event>(_null).restrict(pc_61);
            
            PrimitiveVS<Machine> var_$tmp5 =
                new PrimitiveVS<Machine>().restrict(pc_61);
            
            NamedTupleVS var_$tmp6 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_61);
            
            NamedTupleVS var_$tmp7 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_61);
            
            NamedTupleVS var_$tmp8 =
                new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_61);
            
            NamedTupleVS var_$tmp9 =
                new NamedTupleVS("proposer", new PrimitiveVS<Machine>(), "proposal", new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()))).restrict(pc_61);
            
            NamedTupleVS var_local_4_$tmp0 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_61);
            
            NamedTupleVS var_local_4_$tmp1 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_61);
            
            PrimitiveVS<Boolean> var_local_4_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_61);
            
            NamedTupleVS var_local_4_$tmp3 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_61);
            
            PrimitiveVS<Event> var_inline_5_e =
                new PrimitiveVS<Event>(_null).restrict(pc_61);
            
            NamedTupleVS var_inline_5_v =
                new NamedTupleVS("proposer", new PrimitiveVS<Machine>(), "proposal", new NamedTupleVS("pid", new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)), "value", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()))).restrict(pc_61);
            
            PrimitiveVS<Integer> var_local_5_index =
                new PrimitiveVS<Integer>(0).restrict(pc_61);
            
            PrimitiveVS<Integer> var_local_5_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_61);
            
            PrimitiveVS<Boolean> var_local_5_$tmp1 =
                new PrimitiveVS<Boolean>(false).restrict(pc_61);
            
            PrimitiveVS<Boolean> var_local_5_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_61);
            
            PrimitiveVS<Machine> var_local_5_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_61);
            
            PrimitiveVS<Machine> var_local_5_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_61);
            
            PrimitiveVS<Event> var_local_5_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_61);
            
            UnionVS var_local_5_$tmp6 =
                new UnionVS().restrict(pc_61);
            
            PrimitiveVS<Integer> var_local_5_$tmp7 =
                new PrimitiveVS<Integer>(0).restrict(pc_61);
            
            PrimitiveVS<Integer> temp_var_285;
            temp_var_285 = new PrimitiveVS<Integer>(0).restrict(pc_61);
            var_numOfAcceptRecv = var_numOfAcceptRecv.updateUnderGuard(pc_61, temp_var_285);
            
            NamedTupleVS temp_var_286;
            temp_var_286 = (NamedTupleVS)((var_promisedAgree.restrict(pc_61)).getField("value"));
            var_local_4_$tmp0 = var_local_4_$tmp0.updateUnderGuard(pc_61, temp_var_286);
            
            NamedTupleVS temp_var_287;
            temp_var_287 = new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_61);
            var_local_4_$tmp1 = var_local_4_$tmp1.updateUnderGuard(pc_61, temp_var_287);
            
            PrimitiveVS<Boolean> temp_var_288;
            temp_var_288 = var_local_4_$tmp0.restrict(pc_61).symbolicEquals(var_local_4_$tmp1.restrict(pc_61), pc_61);
            var_local_4_$tmp2 = var_local_4_$tmp2.updateUnderGuard(pc_61, temp_var_288);
            
            PrimitiveVS<Boolean> temp_var_289 = var_local_4_$tmp2.restrict(pc_61);
            Guard pc_62 = BooleanVS.getTrueGuard(temp_var_289);
            Guard pc_63 = BooleanVS.getFalseGuard(temp_var_289);
            boolean jumpedOut_38 = false;
            boolean jumpedOut_39 = false;
            if (!pc_62.isFalse()) {
                // 'then' branch
                NamedTupleVS temp_var_290;
                temp_var_290 = var_proposeValue.restrict(pc_62);
                var_$tmp0 = var_$tmp0.updateUnderGuard(pc_62, temp_var_290);
                
            }
            if (!pc_63.isFalse()) {
                // 'else' branch
                NamedTupleVS temp_var_291;
                temp_var_291 = (NamedTupleVS)((var_promisedAgree.restrict(pc_63)).getField("value"));
                var_local_4_$tmp3 = var_local_4_$tmp3.updateUnderGuard(pc_63, temp_var_291);
                
                NamedTupleVS temp_var_292;
                temp_var_292 = var_local_4_$tmp3.restrict(pc_63);
                var_$tmp0 = var_$tmp0.updateUnderGuard(pc_63, temp_var_292);
                
            }
            if (jumpedOut_38 || jumpedOut_39) {
                pc_61 = pc_62.or(pc_63);
            }
            
            NamedTupleVS temp_var_293;
            temp_var_293 = var_$tmp0.restrict(pc_61);
            var_proposeValue = var_proposeValue.updateUnderGuard(pc_61, temp_var_293);
            
            PredVS<String> /* pred enum tPreds */ temp_var_294;
            temp_var_294 = (PredVS<String> /* pred enum tPreds */)((var_proposeValue.restrict(pc_61)).getField("key"));
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_61, temp_var_294);
            
            PredVS<String> /* pred enum tPreds */ temp_var_295;
            temp_var_295 = (PredVS<String> /* pred enum tPreds */)((var_proposeValue.restrict(pc_61)).getField("val"));
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_61, temp_var_295);
            
            PrimitiveVS<String> temp_var_296;
            temp_var_296 = new PrimitiveVS<String>(String.format("got value to be proposed key, value {0} %s, {1} %s", var_$tmp1.restrict(pc_61), var_$tmp2.restrict(pc_61))).restrict(pc_61);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_61, temp_var_296);
            
            System.out.println((var_$tmp3.restrict(pc_61)).toString());
            PrimitiveVS<Event> temp_var_297;
            temp_var_297 = new PrimitiveVS<Event>(accept).restrict(pc_61);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_61, temp_var_297);
            
            PrimitiveVS<Machine> temp_var_298;
            temp_var_298 = new PrimitiveVS<Machine>(this).restrict(pc_61);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_61, temp_var_298);
            
            NamedTupleVS temp_var_299;
            temp_var_299 = var_nextProposalId.restrict(pc_61);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_61, temp_var_299);
            
            NamedTupleVS temp_var_300;
            temp_var_300 = var_proposeValue.restrict(pc_61);
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_61, temp_var_300);
            
            NamedTupleVS temp_var_301;
            temp_var_301 = new NamedTupleVS("pid", var_$tmp6.restrict(pc_61), "value", var_$tmp7.restrict(pc_61));
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_61, temp_var_301);
            
            NamedTupleVS temp_var_302;
            temp_var_302 = new NamedTupleVS("proposer", var_$tmp5.restrict(pc_61), "proposal", var_$tmp8.restrict(pc_61));
            var_$tmp9 = var_$tmp9.updateUnderGuard(pc_61, temp_var_302);
            
            PrimitiveVS<Event> temp_var_303;
            temp_var_303 = var_$tmp4.restrict(pc_61);
            var_inline_5_e = var_inline_5_e.updateUnderGuard(pc_61, temp_var_303);
            
            NamedTupleVS temp_var_304;
            temp_var_304 = var_$tmp9.restrict(pc_61);
            var_inline_5_v = var_inline_5_v.updateUnderGuard(pc_61, temp_var_304);
            
            PrimitiveVS<Integer> temp_var_305;
            temp_var_305 = new PrimitiveVS<Integer>(0).restrict(pc_61);
            var_local_5_index = var_local_5_index.updateUnderGuard(pc_61, temp_var_305);
            
            java.util.List<Guard> loop_exits_4 = new java.util.ArrayList<>();
            boolean loop_early_ret_4 = false;
            Guard pc_64 = pc_61;
            while (!pc_64.isFalse()) {
                PrimitiveVS<Integer> temp_var_306;
                temp_var_306 = var_acceptors.restrict(pc_64).size();
                var_local_5_$tmp0 = var_local_5_$tmp0.updateUnderGuard(pc_64, temp_var_306);
                
                PrimitiveVS<Boolean> temp_var_307;
                temp_var_307 = (var_local_5_index.restrict(pc_64)).apply(var_local_5_$tmp0.restrict(pc_64), (temp_var_308, temp_var_309) -> temp_var_308 < temp_var_309);
                var_local_5_$tmp1 = var_local_5_$tmp1.updateUnderGuard(pc_64, temp_var_307);
                
                PrimitiveVS<Boolean> temp_var_310;
                temp_var_310 = var_local_5_$tmp1.restrict(pc_64);
                var_local_5_$tmp2 = var_local_5_$tmp2.updateUnderGuard(pc_64, temp_var_310);
                
                PrimitiveVS<Boolean> temp_var_311 = var_local_5_$tmp2.restrict(pc_64);
                Guard pc_65 = BooleanVS.getTrueGuard(temp_var_311);
                Guard pc_66 = BooleanVS.getFalseGuard(temp_var_311);
                boolean jumpedOut_40 = false;
                boolean jumpedOut_41 = false;
                if (!pc_65.isFalse()) {
                    // 'then' branch
                }
                if (!pc_66.isFalse()) {
                    // 'else' branch
                    loop_exits_4.add(pc_66);
                    jumpedOut_41 = true;
                    pc_66 = Guard.constFalse();
                    
                }
                if (jumpedOut_40 || jumpedOut_41) {
                    pc_64 = pc_65.or(pc_66);
                }
                
                if (!pc_64.isFalse()) {
                    PrimitiveVS<Machine> temp_var_312;
                    temp_var_312 = var_acceptors.restrict(pc_64).get(var_local_5_index.restrict(pc_64));
                    var_local_5_$tmp3 = var_local_5_$tmp3.updateUnderGuard(pc_64, temp_var_312);
                    
                    PrimitiveVS<Machine> temp_var_313;
                    temp_var_313 = var_local_5_$tmp3.restrict(pc_64);
                    var_local_5_$tmp4 = var_local_5_$tmp4.updateUnderGuard(pc_64, temp_var_313);
                    
                    PrimitiveVS<Event> temp_var_314;
                    temp_var_314 = var_inline_5_e.restrict(pc_64);
                    var_local_5_$tmp5 = var_local_5_$tmp5.updateUnderGuard(pc_64, temp_var_314);
                    
                    UnionVS temp_var_315;
                    temp_var_315 = ValueSummary.castToAny(pc_64, var_inline_5_v.restrict(pc_64));
                    var_local_5_$tmp6 = var_local_5_$tmp6.updateUnderGuard(pc_64, temp_var_315);
                    
                    effects.send(pc_64, var_local_5_$tmp4.restrict(pc_64), var_local_5_$tmp5.restrict(pc_64), new UnionVS(var_local_5_$tmp6.restrict(pc_64)));
                    
                    PrimitiveVS<Integer> temp_var_316;
                    temp_var_316 = (var_local_5_index.restrict(pc_64)).apply(new PrimitiveVS<Integer>(1).restrict(pc_64), (temp_var_317, temp_var_318) -> temp_var_317 + temp_var_318);
                    var_local_5_$tmp7 = var_local_5_$tmp7.updateUnderGuard(pc_64, temp_var_316);
                    
                    PrimitiveVS<Integer> temp_var_319;
                    temp_var_319 = var_local_5_$tmp7.restrict(pc_64);
                    var_local_5_index = var_local_5_index.updateUnderGuard(pc_64, temp_var_319);
                    
                }
            }
            if (loop_early_ret_4) {
                pc_61 = Guard.orMany(loop_exits_4);
            }
            
        }
        
        Guard 
        anonfun_13(
            Guard pc_67,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            NamedTupleVS var_payload
        ) {
            PrimitiveVS<Integer> var_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_67);
            
            PrimitiveVS<Integer> var_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_67);
            
            PrimitiveVS<Boolean> var_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_67);
            
            PrimitiveVS<Integer> var_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_67);
            
            PrimitiveVS<Integer> var_$tmp4 =
                new PrimitiveVS<Integer>(0).restrict(pc_67);
            
            PrimitiveVS<Integer> temp_var_320;
            temp_var_320 = (PrimitiveVS<Integer>)((var_nextProposalId.restrict(pc_67)).getField("round"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_67, temp_var_320);
            
            PrimitiveVS<Integer> temp_var_321;
            temp_var_321 = (PrimitiveVS<Integer>)((var_payload.restrict(pc_67)).getField("round"));
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_67, temp_var_321);
            
            PrimitiveVS<Boolean> temp_var_322;
            temp_var_322 = (var_$tmp0.restrict(pc_67)).apply(var_$tmp1.restrict(pc_67), (temp_var_323, temp_var_324) -> temp_var_323 <= temp_var_324);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_67, temp_var_322);
            
            PrimitiveVS<Boolean> temp_var_325 = var_$tmp2.restrict(pc_67);
            Guard pc_68 = BooleanVS.getTrueGuard(temp_var_325);
            Guard pc_69 = BooleanVS.getFalseGuard(temp_var_325);
            boolean jumpedOut_42 = false;
            boolean jumpedOut_43 = false;
            if (!pc_68.isFalse()) {
                // 'then' branch
                PrimitiveVS<Integer> temp_var_326;
                temp_var_326 = (PrimitiveVS<Integer>)((var_payload.restrict(pc_68)).getField("round"));
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_68, temp_var_326);
                
                PrimitiveVS<Integer> temp_var_327;
                temp_var_327 = var_$tmp3.restrict(pc_68);
                var_$tmp4 = var_$tmp4.updateUnderGuard(pc_68, temp_var_327);
                
                NamedTupleVS temp_var_328 = var_nextProposalId.restrict(pc_68);    
                PrimitiveVS<Integer> temp_var_329;
                temp_var_329 = var_$tmp4.restrict(pc_68);
                temp_var_328 = temp_var_328.setField("round", temp_var_329);
                var_nextProposalId = var_nextProposalId.updateUnderGuard(pc_68, temp_var_328);
                
            }
            if (!pc_69.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_42 || jumpedOut_43) {
                pc_67 = pc_68.or(pc_69);
            }
            
            outcome.addGuardedGoto(pc_67, ProposerPhaseOne);
            pc_67 = Guard.constFalse();
            
            return pc_67;
        }
        
        Guard 
        anonfun_14(
            Guard pc_70,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            NamedTupleVS var_payload
        ) {
            NamedTupleVS var_$tmp0 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_70);
            
            NamedTupleVS var_$tmp1 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_70);
            
            PrimitiveVS<Boolean> var_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_70);
            
            PrimitiveVS<Integer> var_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_70);
            
            PrimitiveVS<Boolean> var_$tmp4 =
                new PrimitiveVS<Boolean>(false).restrict(pc_70);
            
            PrimitiveVS<Machine> var_$tmp5 =
                new PrimitiveVS<Machine>().restrict(pc_70);
            
            PrimitiveVS<Event> var_$tmp6 =
                new PrimitiveVS<Event>(_null).restrict(pc_70);
            
            NamedTupleVS var_inline_6_id1 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_70);
            
            NamedTupleVS var_inline_6_id2 =
                new NamedTupleVS("serverid", new PrimitiveVS<Integer>(0), "round", new PrimitiveVS<Integer>(0)).restrict(pc_70);
            
            PrimitiveVS<Integer> var_local_6_$tmp0 =
                new PrimitiveVS<Integer>(0).restrict(pc_70);
            
            PrimitiveVS<Integer> var_local_6_$tmp1 =
                new PrimitiveVS<Integer>(0).restrict(pc_70);
            
            PrimitiveVS<Boolean> var_local_6_$tmp2 =
                new PrimitiveVS<Boolean>(false).restrict(pc_70);
            
            PrimitiveVS<Integer> var_local_6_$tmp3 =
                new PrimitiveVS<Integer>(0).restrict(pc_70);
            
            PrimitiveVS<Integer> var_local_6_$tmp4 =
                new PrimitiveVS<Integer>(0).restrict(pc_70);
            
            PrimitiveVS<Boolean> var_local_6_$tmp5 =
                new PrimitiveVS<Boolean>(false).restrict(pc_70);
            
            PrimitiveVS<Boolean> var_local_6_$tmp6 =
                new PrimitiveVS<Boolean>(false).restrict(pc_70);
            
            NamedTupleVS temp_var_330;
            temp_var_330 = (NamedTupleVS)((var_payload.restrict(pc_70)).getField("pid"));
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_70, temp_var_330);
            
            NamedTupleVS temp_var_331;
            temp_var_331 = var_nextProposalId.restrict(pc_70);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_70, temp_var_331);
            
            NamedTupleVS temp_var_332;
            temp_var_332 = var_$tmp0.restrict(pc_70);
            var_inline_6_id1 = var_inline_6_id1.updateUnderGuard(pc_70, temp_var_332);
            
            NamedTupleVS temp_var_333;
            temp_var_333 = var_$tmp1.restrict(pc_70);
            var_inline_6_id2 = var_inline_6_id2.updateUnderGuard(pc_70, temp_var_333);
            
            PrimitiveVS<Integer> temp_var_334;
            temp_var_334 = (PrimitiveVS<Integer>)((var_inline_6_id1.restrict(pc_70)).getField("serverid"));
            var_local_6_$tmp0 = var_local_6_$tmp0.updateUnderGuard(pc_70, temp_var_334);
            
            PrimitiveVS<Integer> temp_var_335;
            temp_var_335 = (PrimitiveVS<Integer>)((var_inline_6_id2.restrict(pc_70)).getField("serverid"));
            var_local_6_$tmp1 = var_local_6_$tmp1.updateUnderGuard(pc_70, temp_var_335);
            
            PrimitiveVS<Boolean> temp_var_336;
            temp_var_336 = var_local_6_$tmp0.restrict(pc_70).symbolicEquals(var_local_6_$tmp1.restrict(pc_70), pc_70);
            var_local_6_$tmp2 = var_local_6_$tmp2.updateUnderGuard(pc_70, temp_var_336);
            
            PrimitiveVS<Boolean> temp_var_337;
            temp_var_337 = var_local_6_$tmp2.restrict(pc_70);
            var_local_6_$tmp6 = var_local_6_$tmp6.updateUnderGuard(pc_70, temp_var_337);
            
            PrimitiveVS<Boolean> temp_var_338 = var_local_6_$tmp6.restrict(pc_70);
            Guard pc_71 = BooleanVS.getTrueGuard(temp_var_338);
            Guard pc_72 = BooleanVS.getFalseGuard(temp_var_338);
            boolean jumpedOut_44 = false;
            boolean jumpedOut_45 = false;
            if (!pc_71.isFalse()) {
                // 'then' branch
                PrimitiveVS<Integer> temp_var_339;
                temp_var_339 = (PrimitiveVS<Integer>)((var_inline_6_id1.restrict(pc_71)).getField("round"));
                var_local_6_$tmp3 = var_local_6_$tmp3.updateUnderGuard(pc_71, temp_var_339);
                
                PrimitiveVS<Integer> temp_var_340;
                temp_var_340 = (PrimitiveVS<Integer>)((var_inline_6_id2.restrict(pc_71)).getField("round"));
                var_local_6_$tmp4 = var_local_6_$tmp4.updateUnderGuard(pc_71, temp_var_340);
                
                PrimitiveVS<Boolean> temp_var_341;
                temp_var_341 = var_local_6_$tmp3.restrict(pc_71).symbolicEquals(var_local_6_$tmp4.restrict(pc_71), pc_71);
                var_local_6_$tmp5 = var_local_6_$tmp5.updateUnderGuard(pc_71, temp_var_341);
                
                PrimitiveVS<Boolean> temp_var_342;
                temp_var_342 = var_local_6_$tmp5.restrict(pc_71);
                var_local_6_$tmp6 = var_local_6_$tmp6.updateUnderGuard(pc_71, temp_var_342);
                
            }
            if (jumpedOut_44 || jumpedOut_45) {
                pc_70 = pc_71.or(pc_72);
            }
            
            PrimitiveVS<Boolean> temp_var_343 = var_local_6_$tmp6.restrict(pc_70);
            Guard pc_73 = BooleanVS.getTrueGuard(temp_var_343);
            Guard pc_74 = BooleanVS.getFalseGuard(temp_var_343);
            boolean jumpedOut_46 = false;
            boolean jumpedOut_47 = false;
            if (!pc_73.isFalse()) {
                // 'then' branch
                PrimitiveVS<Boolean> temp_var_344;
                temp_var_344 = new PrimitiveVS<Boolean>(true).restrict(pc_73);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_73, temp_var_344);
                
            }
            if (!pc_74.isFalse()) {
                // 'else' branch
                PrimitiveVS<Boolean> temp_var_345;
                temp_var_345 = new PrimitiveVS<Boolean>(false).restrict(pc_74);
                var_$tmp2 = var_$tmp2.updateUnderGuard(pc_74, temp_var_345);
                
            }
            if (jumpedOut_46 || jumpedOut_47) {
                pc_70 = pc_73.or(pc_74);
            }
            
            PrimitiveVS<Boolean> temp_var_346 = var_$tmp2.restrict(pc_70);
            Guard pc_75 = BooleanVS.getTrueGuard(temp_var_346);
            Guard pc_76 = BooleanVS.getFalseGuard(temp_var_346);
            boolean jumpedOut_48 = false;
            boolean jumpedOut_49 = false;
            if (!pc_75.isFalse()) {
                // 'then' branch
                PrimitiveVS<Integer> temp_var_347;
                temp_var_347 = (var_numOfAcceptRecv.restrict(pc_75)).apply(new PrimitiveVS<Integer>(1).restrict(pc_75), (temp_var_348, temp_var_349) -> temp_var_348 + temp_var_349);
                var_$tmp3 = var_$tmp3.updateUnderGuard(pc_75, temp_var_347);
                
                PrimitiveVS<Integer> temp_var_350;
                temp_var_350 = var_$tmp3.restrict(pc_75);
                var_numOfAcceptRecv = var_numOfAcceptRecv.updateUnderGuard(pc_75, temp_var_350);
                
            }
            if (!pc_76.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_48 || jumpedOut_49) {
                pc_70 = pc_75.or(pc_76);
            }
            
            PrimitiveVS<Boolean> temp_var_351;
            temp_var_351 = var_numOfAcceptRecv.restrict(pc_70).symbolicEquals(var_majority.restrict(pc_70), pc_70);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_70, temp_var_351);
            
            PrimitiveVS<Boolean> temp_var_352 = var_$tmp4.restrict(pc_70);
            Guard pc_77 = BooleanVS.getTrueGuard(temp_var_352);
            Guard pc_78 = BooleanVS.getFalseGuard(temp_var_352);
            boolean jumpedOut_50 = false;
            boolean jumpedOut_51 = false;
            if (!pc_77.isFalse()) {
                // 'then' branch
                PrimitiveVS<Machine> temp_var_353;
                temp_var_353 = var_client.restrict(pc_77);
                var_$tmp5 = var_$tmp5.updateUnderGuard(pc_77, temp_var_353);
                
                PrimitiveVS<Event> temp_var_354;
                temp_var_354 = new PrimitiveVS<Event>(writeResp).restrict(pc_77);
                var_$tmp6 = var_$tmp6.updateUnderGuard(pc_77, temp_var_354);
                
                effects.send(pc_77, var_$tmp5.restrict(pc_77), var_$tmp6.restrict(pc_77), null);
                
                outcome.addGuardedGoto(pc_77, WaitForClient);
                pc_77 = Guard.constFalse();
                jumpedOut_50 = true;
                
            }
            if (!pc_78.isFalse()) {
                // 'else' branch
            }
            if (jumpedOut_50 || jumpedOut_51) {
                pc_70 = pc_77.or(pc_78);
            }
            
            if (!pc_70.isFalse()) {
            }
            return pc_70;
        }
        
    }
    
    public static class TestClient extends Machine {
        
        static State Init = new State("Init") {
            @Override public void entry(Guard pc_79, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((TestClient)machine).anonfun_15(pc_79, machine.sendBuffer, outcome, payload != null ? (ListVS<PrimitiveVS<Machine>>) ValueSummary.castFromAny(pc_79, new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_79), payload) : new ListVS<PrimitiveVS<Machine>>(Guard.constTrue()).restrict(pc_79));
            }
        };
        static State ChoosePre = new State("ChoosePre") {
            @Override public void entry(Guard pc_80, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((TestClient)machine).anonfun_16(pc_80, machine.sendBuffer, outcome);
            }
        };
        static State SendPreWrites = new State("SendPreWrites") {
            @Override public void entry(Guard pc_81, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((TestClient)machine).anonfun_17(pc_81, machine.sendBuffer);
            }
        };
        static State SendSelectWrite = new State("SendSelectWrite") {
            @Override public void entry(Guard pc_82, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {
                ((TestClient)machine).anonfun_18(pc_82, machine.sendBuffer, outcome);
            }
        };
        static State SendPost = new State("SendPost") {
        };
        private ListVS<PrimitiveVS<Machine>> var_proposers = new ListVS<PrimitiveVS<Machine>>(Guard.constTrue());
        private NamedTupleVS var_currTransaction = new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */());
        private PrimitiveVS<Machine> var_proposer = new PrimitiveVS<Machine>();
        
        @Override
        public void reset() {
                super.reset();
                var_proposers = new ListVS<PrimitiveVS<Machine>>(Guard.constTrue());
                var_currTransaction = new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */());
                var_proposer = new PrimitiveVS<Machine>();
        }
        
        @Override
        public List<ValueSummary> getLocalState() {
                List<ValueSummary> res = super.getLocalState();
                res.add(var_proposers);
                res.add(var_currTransaction);
                res.add(var_proposer);
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
            SendPreWrites.addHandlers(new GotoEventHandler(writeResp, SendSelectWrite));
            SendSelectWrite.addHandlers();
            SendPost.addHandlers(new EventHandler(writeResp) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((TestClient)machine).anonfun_19(pc, machine.sendBuffer);
                    }
                },
                new EventHandler(readResp) {
                    @Override public void handleEvent(Guard pc, Machine machine, UnionVS payload, EventHandlerReturnReason outcome) {
                        ((TestClient)machine).anonfun_20(pc, machine.sendBuffer, (NamedTupleVS) ValueSummary.castFromAny(pc, new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()), payload));
                    }
                });
        }
        
        Guard 
        anonfun_15(
            Guard pc_83,
            EventBuffer effects,
            EventHandlerReturnReason outcome,
            ListVS<PrimitiveVS<Machine>> var_payload
        ) {
            ListVS<PrimitiveVS<Machine>> temp_var_355;
            temp_var_355 = var_payload.restrict(pc_83);
            var_proposers = var_proposers.updateUnderGuard(pc_83, temp_var_355);
            
            outcome.addGuardedGoto(pc_83, SendPreWrites);
            pc_83 = Guard.constFalse();
            
            return pc_83;
        }
        
        Guard 
        anonfun_16(
            Guard pc_84,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveVS<Boolean> var_$tmp0 =
                new PrimitiveVS<Boolean>(false).restrict(pc_84);
            
            PrimitiveVS<Boolean> temp_var_356;
            temp_var_356 = scheduler.getNextBoolean(pc_84);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_84, temp_var_356);
            
            PrimitiveVS<Boolean> temp_var_357 = var_$tmp0.restrict(pc_84);
            Guard pc_85 = BooleanVS.getTrueGuard(temp_var_357);
            Guard pc_86 = BooleanVS.getFalseGuard(temp_var_357);
            boolean jumpedOut_52 = false;
            boolean jumpedOut_53 = false;
            if (!pc_85.isFalse()) {
                // 'then' branch
                outcome.addGuardedGoto(pc_85, SendPreWrites);
                pc_85 = Guard.constFalse();
                jumpedOut_52 = true;
                
            }
            if (!pc_86.isFalse()) {
                // 'else' branch
                outcome.addGuardedGoto(pc_86, SendSelectWrite);
                pc_86 = Guard.constFalse();
                jumpedOut_53 = true;
                
            }
            if (jumpedOut_52 || jumpedOut_53) {
                pc_84 = pc_85.or(pc_86);
            }
            
            return pc_84;
        }
        
        void 
        anonfun_17(
            Guard pc_87,
            EventBuffer effects
        ) {
            NamedTupleVS var_$tmp0 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_87);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_87);
            
            PrimitiveVS<Machine> var_$tmp2 =
                new PrimitiveVS<Machine>().restrict(pc_87);
            
            PrimitiveVS<Event> var_$tmp3 =
                new PrimitiveVS<Event>(_null).restrict(pc_87);
            
            PrimitiveVS<Machine> var_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_87);
            
            NamedTupleVS var_$tmp5 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_87);
            
            NamedTupleVS var_$tmp6 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_87);
            
            ListVS<PredVS<String> /* pred enum tPreds */> var_local_7_keyChoices =
                new ListVS<PredVS<String> /* pred enum tPreds */>(Guard.constTrue()).restrict(pc_87);
            
            ListVS<PredVS<String> /* pred enum tPreds */> var_local_7_valChoices =
                new ListVS<PredVS<String> /* pred enum tPreds */>(Guard.constTrue()).restrict(pc_87);
            
            PredVS<String> /* pred enum tPreds */ var_local_7_$tmp0 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_87);
            
            PredVS<String> /* pred enum tPreds */ var_local_7_$tmp1 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_87);
            
            PredVS<String> /* pred enum tPreds */ var_local_7_$tmp2 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_87);
            
            PredVS<String> /* pred enum tPreds */ var_local_7_$tmp3 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_87);
            
            PredVS<String> /* pred enum tPreds */ var_local_7_$tmp4 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_87);
            
            PredVS<String> /* pred enum tPreds */ var_local_7_$tmp5 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_87);
            
            NamedTupleVS var_local_7_$tmp6 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_87);
            
            PredVS<String> /* pred enum tPreds */ temp_var_358;
            temp_var_358 = new PredVS<String>("EQKEY").restrict(pc_87);
            var_local_7_$tmp0 = var_local_7_$tmp0.updateUnderGuard(pc_87, temp_var_358);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_359 = var_local_7_keyChoices.restrict(pc_87);    
            temp_var_359 = var_local_7_keyChoices.restrict(pc_87).insert(new PrimitiveVS<Integer>(0).restrict(pc_87), var_local_7_$tmp0.restrict(pc_87));
            var_local_7_keyChoices = var_local_7_keyChoices.updateUnderGuard(pc_87, temp_var_359);
            
            PredVS<String> /* pred enum tPreds */ temp_var_360;
            temp_var_360 = new PredVS<String>("NEQKEY").restrict(pc_87);
            var_local_7_$tmp1 = var_local_7_$tmp1.updateUnderGuard(pc_87, temp_var_360);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_361 = var_local_7_keyChoices.restrict(pc_87);    
            temp_var_361 = var_local_7_keyChoices.restrict(pc_87).insert(new PrimitiveVS<Integer>(1).restrict(pc_87), var_local_7_$tmp1.restrict(pc_87));
            var_local_7_keyChoices = var_local_7_keyChoices.updateUnderGuard(pc_87, temp_var_361);
            
            PredVS<String> /* pred enum tPreds */ temp_var_362;
            temp_var_362 = new PredVS<String>("EQVAL").restrict(pc_87);
            var_local_7_$tmp2 = var_local_7_$tmp2.updateUnderGuard(pc_87, temp_var_362);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_363 = var_local_7_valChoices.restrict(pc_87);    
            temp_var_363 = var_local_7_valChoices.restrict(pc_87).insert(new PrimitiveVS<Integer>(0).restrict(pc_87), var_local_7_$tmp2.restrict(pc_87));
            var_local_7_valChoices = var_local_7_valChoices.updateUnderGuard(pc_87, temp_var_363);
            
            PredVS<String> /* pred enum tPreds */ temp_var_364;
            temp_var_364 = new PredVS<String>("NEQVAL").restrict(pc_87);
            var_local_7_$tmp3 = var_local_7_$tmp3.updateUnderGuard(pc_87, temp_var_364);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_365 = var_local_7_valChoices.restrict(pc_87);    
            temp_var_365 = var_local_7_valChoices.restrict(pc_87).insert(new PrimitiveVS<Integer>(1).restrict(pc_87), var_local_7_$tmp3.restrict(pc_87));
            var_local_7_valChoices = var_local_7_valChoices.updateUnderGuard(pc_87, temp_var_365);
            
            PredVS<String> /* pred enum tPreds */ temp_var_366;
            temp_var_366 = new PredVS<String> /* pred enum tPreds */(var_local_7_keyChoices.restrict(pc_87), pc_87);
            var_local_7_$tmp4 = var_local_7_$tmp4.updateUnderGuard(pc_87, temp_var_366);
            
            PredVS<String> /* pred enum tPreds */ temp_var_367;
            temp_var_367 = new PredVS<String> /* pred enum tPreds */(var_local_7_valChoices.restrict(pc_87), pc_87);
            var_local_7_$tmp5 = var_local_7_$tmp5.updateUnderGuard(pc_87, temp_var_367);
            
            NamedTupleVS temp_var_368;
            temp_var_368 = new NamedTupleVS("key", var_local_7_$tmp4.restrict(pc_87), "val", var_local_7_$tmp5.restrict(pc_87));
            var_local_7_$tmp6 = var_local_7_$tmp6.updateUnderGuard(pc_87, temp_var_368);
            
            NamedTupleVS temp_var_369;
            temp_var_369 = var_local_7_$tmp6.restrict(pc_87);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_87, temp_var_369);
            
            NamedTupleVS temp_var_370;
            temp_var_370 = var_$tmp0.restrict(pc_87);
            var_currTransaction = var_currTransaction.updateUnderGuard(pc_87, temp_var_370);
            
            PrimitiveVS<Machine> temp_var_371;
            temp_var_371 = (PrimitiveVS<Machine>) scheduler.getNextElement(var_proposers.restrict(pc_87), pc_87);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_87, temp_var_371);
            
            PrimitiveVS<Machine> temp_var_372;
            temp_var_372 = var_$tmp1.restrict(pc_87);
            var_proposer = var_proposer.updateUnderGuard(pc_87, temp_var_372);
            
            PrimitiveVS<Machine> temp_var_373;
            temp_var_373 = var_proposer.restrict(pc_87);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_87, temp_var_373);
            
            PrimitiveVS<Event> temp_var_374;
            temp_var_374 = new PrimitiveVS<Event>(write).restrict(pc_87);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_87, temp_var_374);
            
            PrimitiveVS<Machine> temp_var_375;
            temp_var_375 = new PrimitiveVS<Machine>(this).restrict(pc_87);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_87, temp_var_375);
            
            NamedTupleVS temp_var_376;
            temp_var_376 = var_currTransaction.restrict(pc_87);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_87, temp_var_376);
            
            NamedTupleVS temp_var_377;
            temp_var_377 = new NamedTupleVS("client", var_$tmp4.restrict(pc_87), "rec", var_$tmp5.restrict(pc_87));
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_87, temp_var_377);
            
            effects.send(pc_87, var_$tmp2.restrict(pc_87), var_$tmp3.restrict(pc_87), new UnionVS(var_$tmp6.restrict(pc_87)));
            
        }
        
        Guard 
        anonfun_18(
            Guard pc_88,
            EventBuffer effects,
            EventHandlerReturnReason outcome
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_88);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp1 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_88);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp2 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_88);
            
            NamedTupleVS var_$tmp3 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_88);
            
            PrimitiveVS<Machine> var_$tmp4 =
                new PrimitiveVS<Machine>().restrict(pc_88);
            
            PrimitiveVS<Event> var_$tmp5 =
                new PrimitiveVS<Event>(_null).restrict(pc_88);
            
            PrimitiveVS<Machine> var_$tmp6 =
                new PrimitiveVS<Machine>().restrict(pc_88);
            
            NamedTupleVS var_$tmp7 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_88);
            
            NamedTupleVS var_$tmp8 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_88);
            
            PrimitiveVS<Machine> temp_var_378;
            temp_var_378 = (PrimitiveVS<Machine>) scheduler.getNextElement(var_proposers.restrict(pc_88), pc_88);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_88, temp_var_378);
            
            PrimitiveVS<Machine> temp_var_379;
            temp_var_379 = var_$tmp0.restrict(pc_88);
            var_proposer = var_proposer.updateUnderGuard(pc_88, temp_var_379);
            
            PredVS<String> /* pred enum tPreds */ temp_var_380;
            temp_var_380 = new PredVS<String>("EQKEY").restrict(pc_88);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_88, temp_var_380);
            
            PredVS<String> /* pred enum tPreds */ temp_var_381;
            temp_var_381 = new PredVS<String>("EQVAL").restrict(pc_88);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_88, temp_var_381);
            
            NamedTupleVS temp_var_382;
            temp_var_382 = new NamedTupleVS("key", var_$tmp1.restrict(pc_88), "val", var_$tmp2.restrict(pc_88));
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_88, temp_var_382);
            
            NamedTupleVS temp_var_383;
            temp_var_383 = var_$tmp3.restrict(pc_88);
            var_currTransaction = var_currTransaction.updateUnderGuard(pc_88, temp_var_383);
            
            PrimitiveVS<Machine> temp_var_384;
            temp_var_384 = var_proposer.restrict(pc_88);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_88, temp_var_384);
            
            PrimitiveVS<Event> temp_var_385;
            temp_var_385 = new PrimitiveVS<Event>(write).restrict(pc_88);
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_88, temp_var_385);
            
            PrimitiveVS<Machine> temp_var_386;
            temp_var_386 = new PrimitiveVS<Machine>(this).restrict(pc_88);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_88, temp_var_386);
            
            NamedTupleVS temp_var_387;
            temp_var_387 = var_currTransaction.restrict(pc_88);
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_88, temp_var_387);
            
            NamedTupleVS temp_var_388;
            temp_var_388 = new NamedTupleVS("client", var_$tmp6.restrict(pc_88), "rec", var_$tmp7.restrict(pc_88));
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_88, temp_var_388);
            
            effects.send(pc_88, var_$tmp4.restrict(pc_88), var_$tmp5.restrict(pc_88), new UnionVS(var_$tmp8.restrict(pc_88)));
            
            outcome.addGuardedGoto(pc_88, SendPost);
            pc_88 = Guard.constFalse();
            
            return pc_88;
        }
        
        void 
        anonfun_19(
            Guard pc_89,
            EventBuffer effects
        ) {
            PrimitiveVS<Machine> var_$tmp0 =
                new PrimitiveVS<Machine>().restrict(pc_89);
            
            PrimitiveVS<Machine> var_$tmp1 =
                new PrimitiveVS<Machine>().restrict(pc_89);
            
            PrimitiveVS<Event> var_$tmp2 =
                new PrimitiveVS<Event>(_null).restrict(pc_89);
            
            PrimitiveVS<Machine> var_$tmp3 =
                new PrimitiveVS<Machine>().restrict(pc_89);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp4 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_89);
            
            NamedTupleVS var_$tmp5 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "key", new PredVS<String> /* pred enum tPreds */()).restrict(pc_89);
            
            PrimitiveVS<Machine> temp_var_389;
            temp_var_389 = (PrimitiveVS<Machine>) scheduler.getNextElement(var_proposers.restrict(pc_89), pc_89);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_89, temp_var_389);
            
            PrimitiveVS<Machine> temp_var_390;
            temp_var_390 = var_$tmp0.restrict(pc_89);
            var_proposer = var_proposer.updateUnderGuard(pc_89, temp_var_390);
            
            PrimitiveVS<Machine> temp_var_391;
            temp_var_391 = var_proposer.restrict(pc_89);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_89, temp_var_391);
            
            PrimitiveVS<Event> temp_var_392;
            temp_var_392 = new PrimitiveVS<Event>(read).restrict(pc_89);
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_89, temp_var_392);
            
            PrimitiveVS<Machine> temp_var_393;
            temp_var_393 = new PrimitiveVS<Machine>(this).restrict(pc_89);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_89, temp_var_393);
            
            PredVS<String> /* pred enum tPreds */ temp_var_394;
            temp_var_394 = new PredVS<String>("EQKEY").restrict(pc_89);
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_89, temp_var_394);
            
            NamedTupleVS temp_var_395;
            temp_var_395 = new NamedTupleVS("client", var_$tmp3.restrict(pc_89), "key", var_$tmp4.restrict(pc_89));
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_89, temp_var_395);
            
            effects.send(pc_89, var_$tmp1.restrict(pc_89), var_$tmp2.restrict(pc_89), new UnionVS(var_$tmp5.restrict(pc_89)));
            
        }
        
        void 
        anonfun_20(
            Guard pc_90,
            EventBuffer effects,
            NamedTupleVS var_resp
        ) {
            ListVS<PredVS<String> /* pred enum tPreds */> var_choices =
                new ListVS<PredVS<String> /* pred enum tPreds */>(Guard.constTrue()).restrict(pc_90);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp0 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_90);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp1 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_90);
            
            NamedTupleVS var_$tmp2 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_90);
            
            PrimitiveVS<Boolean> var_$tmp3 =
                new PrimitiveVS<Boolean>(false).restrict(pc_90);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp4 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_90);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp5 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_90);
            
            PrimitiveVS<String> var_$tmp6 =
                new PrimitiveVS<String>("").restrict(pc_90);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp7 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_90);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp8 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_90);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp9 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_90);
            
            PredVS<String> /* pred enum tPreds */ var_$tmp10 =
                new PredVS<String> /* pred enum tPreds */().restrict(pc_90);
            
            NamedTupleVS var_$tmp11 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_90);
            
            PrimitiveVS<Machine> var_$tmp12 =
                new PrimitiveVS<Machine>().restrict(pc_90);
            
            PrimitiveVS<Machine> var_$tmp13 =
                new PrimitiveVS<Machine>().restrict(pc_90);
            
            PrimitiveVS<Event> var_$tmp14 =
                new PrimitiveVS<Event>(_null).restrict(pc_90);
            
            PrimitiveVS<Machine> var_$tmp15 =
                new PrimitiveVS<Machine>().restrict(pc_90);
            
            NamedTupleVS var_$tmp16 =
                new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */()).restrict(pc_90);
            
            NamedTupleVS var_$tmp17 =
                new NamedTupleVS("client", new PrimitiveVS<Machine>(), "rec", new NamedTupleVS("key", new PredVS<String> /* pred enum tPreds */(), "val", new PredVS<String> /* pred enum tPreds */())).restrict(pc_90);
            
            PredVS<String> /* pred enum tPreds */ temp_var_396;
            temp_var_396 = new PredVS<String>("EQKEY").restrict(pc_90);
            var_$tmp0 = var_$tmp0.updateUnderGuard(pc_90, temp_var_396);
            
            PredVS<String> /* pred enum tPreds */ temp_var_397;
            temp_var_397 = new PredVS<String>("EQVAL").restrict(pc_90);
            var_$tmp1 = var_$tmp1.updateUnderGuard(pc_90, temp_var_397);
            
            NamedTupleVS temp_var_398;
            temp_var_398 = new NamedTupleVS("key", var_$tmp0.restrict(pc_90), "val", var_$tmp1.restrict(pc_90));
            var_$tmp2 = var_$tmp2.updateUnderGuard(pc_90, temp_var_398);
            
            PrimitiveVS<Boolean> temp_var_399;
            temp_var_399 = var_resp.restrict(pc_90).symbolicEquals(var_$tmp2.restrict(pc_90), pc_90);
            var_$tmp3 = var_$tmp3.updateUnderGuard(pc_90, temp_var_399);
            
            PredVS<String> /* pred enum tPreds */ temp_var_400;
            temp_var_400 = (PredVS<String> /* pred enum tPreds */)((var_resp.restrict(pc_90)).getField("key"));
            var_$tmp4 = var_$tmp4.updateUnderGuard(pc_90, temp_var_400);
            
            PredVS<String> /* pred enum tPreds */ temp_var_401;
            temp_var_401 = (PredVS<String> /* pred enum tPreds */)((var_resp.restrict(pc_90)).getField("val"));
            var_$tmp5 = var_$tmp5.updateUnderGuard(pc_90, temp_var_401);
            
            PrimitiveVS<String> temp_var_402;
            temp_var_402 = new PrimitiveVS<String>(String.format("value not equal {0} %s, {1} %s", var_$tmp4.restrict(pc_90), var_$tmp5.restrict(pc_90))).restrict(pc_90);
            var_$tmp6 = var_$tmp6.updateUnderGuard(pc_90, temp_var_402);
            
            Assert.progProp(!(var_$tmp3.restrict(pc_90)).getValues().contains(Boolean.FALSE), var_$tmp6.restrict(pc_90), scheduler, var_$tmp3.restrict(pc_90).getGuardFor(Boolean.FALSE));
            PredVS<String> /* pred enum tPreds */ temp_var_403;
            temp_var_403 = new PredVS<String>("EQVAL").restrict(pc_90);
            var_$tmp7 = var_$tmp7.updateUnderGuard(pc_90, temp_var_403);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_404 = var_choices.restrict(pc_90);    
            temp_var_404 = var_choices.restrict(pc_90).insert(new PrimitiveVS<Integer>(0).restrict(pc_90), var_$tmp7.restrict(pc_90));
            var_choices = var_choices.updateUnderGuard(pc_90, temp_var_404);
            
            PredVS<String> /* pred enum tPreds */ temp_var_405;
            temp_var_405 = new PredVS<String>("NEQVAL").restrict(pc_90);
            var_$tmp8 = var_$tmp8.updateUnderGuard(pc_90, temp_var_405);
            
            ListVS<PredVS<String> /* pred enum tPreds */> temp_var_406 = var_choices.restrict(pc_90);    
            temp_var_406 = var_choices.restrict(pc_90).insert(new PrimitiveVS<Integer>(1).restrict(pc_90), var_$tmp8.restrict(pc_90));
            var_choices = var_choices.updateUnderGuard(pc_90, temp_var_406);
            
            PredVS<String> /* pred enum tPreds */ temp_var_407;
            temp_var_407 = new PredVS<String>("NEQKEY").restrict(pc_90);
            var_$tmp9 = var_$tmp9.updateUnderGuard(pc_90, temp_var_407);
            
            PredVS<String> /* pred enum tPreds */ temp_var_408;
            temp_var_408 = new PredVS<String> /* pred enum tPreds */(var_choices.restrict(pc_90), pc_90);
            var_$tmp10 = var_$tmp10.updateUnderGuard(pc_90, temp_var_408);
            
            NamedTupleVS temp_var_409;
            temp_var_409 = new NamedTupleVS("key", var_$tmp9.restrict(pc_90), "val", var_$tmp10.restrict(pc_90));
            var_$tmp11 = var_$tmp11.updateUnderGuard(pc_90, temp_var_409);
            
            NamedTupleVS temp_var_410;
            temp_var_410 = var_$tmp11.restrict(pc_90);
            var_currTransaction = var_currTransaction.updateUnderGuard(pc_90, temp_var_410);
            
            PrimitiveVS<Machine> temp_var_411;
            temp_var_411 = (PrimitiveVS<Machine>) scheduler.getNextElement(var_proposers.restrict(pc_90), pc_90);
            var_$tmp12 = var_$tmp12.updateUnderGuard(pc_90, temp_var_411);
            
            PrimitiveVS<Machine> temp_var_412;
            temp_var_412 = var_$tmp12.restrict(pc_90);
            var_proposer = var_proposer.updateUnderGuard(pc_90, temp_var_412);
            
            PrimitiveVS<Machine> temp_var_413;
            temp_var_413 = var_proposer.restrict(pc_90);
            var_$tmp13 = var_$tmp13.updateUnderGuard(pc_90, temp_var_413);
            
            PrimitiveVS<Event> temp_var_414;
            temp_var_414 = new PrimitiveVS<Event>(write).restrict(pc_90);
            var_$tmp14 = var_$tmp14.updateUnderGuard(pc_90, temp_var_414);
            
            PrimitiveVS<Machine> temp_var_415;
            temp_var_415 = new PrimitiveVS<Machine>(this).restrict(pc_90);
            var_$tmp15 = var_$tmp15.updateUnderGuard(pc_90, temp_var_415);
            
            NamedTupleVS temp_var_416;
            temp_var_416 = var_currTransaction.restrict(pc_90);
            var_$tmp16 = var_$tmp16.updateUnderGuard(pc_90, temp_var_416);
            
            NamedTupleVS temp_var_417;
            temp_var_417 = new NamedTupleVS("client", var_$tmp15.restrict(pc_90), "rec", var_$tmp16.restrict(pc_90));
            var_$tmp17 = var_$tmp17.updateUnderGuard(pc_90, temp_var_417);
            
            effects.send(pc_90, var_$tmp13.restrict(pc_90), var_$tmp14.restrict(pc_90), new UnionVS(var_$tmp17.restrict(pc_90)));
            
        }
        
    }
    
    // Skipping TypeDef 'tRecord'

    // Skipping TypeDef 'writeRequest'

    // Skipping TypeDef 'readRequest'

    // Skipping TypeDef 'ProposalIdType'

    // Skipping TypeDef 'ProposalType'

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
