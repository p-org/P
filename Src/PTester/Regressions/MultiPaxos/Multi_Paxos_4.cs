#pragma warning disable CS0162, CS0164, CS0168, CS0649
namespace P.Program
{
    using P.Runtime;
    using System;
    using System.Collections.Generic;

    public partial class Application : StateImpl
    {
        public partial class Events
        {
            public static PrtEventValue accepted;
            public static PrtEventValue local;
            public static PrtEventValue success;
            public static PrtEventValue allNodes;
            public static PrtEventValue goPropose;
            public static PrtEventValue chosen;
            public static PrtEventValue update;
            public static PrtEventValue announce_valueChosen;
            public static PrtEventValue announce_valueProposed;
            public static PrtEventValue announce_client_sent;
            public static PrtEventValue announce_proposer_sent;
            public static PrtEventValue announce_proposer_chosen;
            public static PrtEventValue Ping;
            public static PrtEventValue newLeader;
            public static PrtEventValue timeout;
            public static PrtEventValue startTimer;
            public static PrtEventValue cancelTimer;
            public static PrtEventValue cancelTimerSuccess;
            public static PrtEventValue response;
            public static PrtEventValue prepare;
            public static PrtEventValue accept;
            public static PrtEventValue agree;
            public static PrtEventValue reject;
            public static void Events_Multi_Paxos_4()
            {
                accepted = new PrtEventValue(new PrtEvent("accepted", Types.type_7_134078708, 6, true));
                local = new PrtEventValue(new PrtEvent("local", Types.type_28_134078708, PrtEvent.DefaultMaxInstances, false));
                success = new PrtEventValue(new PrtEvent("success", Types.type_28_134078708, PrtEvent.DefaultMaxInstances, false));
                allNodes = new PrtEventValue(new PrtEvent("allNodes", Types.type_13_134078708, PrtEvent.DefaultMaxInstances, false));
                goPropose = new PrtEventValue(new PrtEvent("goPropose", Types.type_28_134078708, PrtEvent.DefaultMaxInstances, false));
                chosen = new PrtEventValue(new PrtEvent("chosen", Types.type_7_134078708, PrtEvent.DefaultMaxInstances, false));
                update = new PrtEventValue(new PrtEvent("update", Types.type_14_134078708, PrtEvent.DefaultMaxInstances, false));
                announce_valueChosen = new PrtEventValue(new PrtEvent("announce_valueChosen", Types.type_0_134078708, PrtEvent.DefaultMaxInstances, false));
                announce_valueProposed = new PrtEventValue(new PrtEvent("announce_valueProposed", Types.type_0_134078708, PrtEvent.DefaultMaxInstances, false));
                announce_client_sent = new PrtEventValue(new PrtEvent("announce_client_sent", Types.type_2_134078708, PrtEvent.DefaultMaxInstances, false));
                announce_proposer_sent = new PrtEventValue(new PrtEvent("announce_proposer_sent", Types.type_2_134078708, PrtEvent.DefaultMaxInstances, false));
                announce_proposer_chosen = new PrtEventValue(new PrtEvent("announce_proposer_chosen", Types.type_2_134078708, PrtEvent.DefaultMaxInstances, false));
                Ping = new PrtEventValue(new PrtEvent("Ping", Types.type_16_134078708, 4, true));
                newLeader = new PrtEventValue(new PrtEvent("newLeader", Types.type_16_134078708, PrtEvent.DefaultMaxInstances, false));
                timeout = new PrtEventValue(new PrtEvent("timeout", Types.type_12_134078708, PrtEvent.DefaultMaxInstances, false));
                startTimer = new PrtEventValue(new PrtEvent("startTimer", Types.type_28_134078708, PrtEvent.DefaultMaxInstances, false));
                cancelTimer = new PrtEventValue(new PrtEvent("cancelTimer", Types.type_28_134078708, PrtEvent.DefaultMaxInstances, false));
                cancelTimerSuccess = new PrtEventValue(new PrtEvent("cancelTimerSuccess", Types.type_28_134078708, PrtEvent.DefaultMaxInstances, false));
                response = new PrtEventValue(new PrtEvent("response", Types.type_28_134078708, PrtEvent.DefaultMaxInstances, false));
                prepare = new PrtEventValue(new PrtEvent("prepare", Types.type_4_134078708, 3, true));
                accept = new PrtEventValue(new PrtEvent("accept", Types.type_0_134078708, 3, true));
                agree = new PrtEventValue(new PrtEvent("agree", Types.type_7_134078708, 6, true));
                reject = new PrtEventValue(new PrtEvent("reject", Types.type_10_134078708, 6, true));
            }
        }

        public partial class Types
        {
            public static PrtType type_1_134078708;
            public static PrtType type_2_134078708;
            public static PrtType type_3_134078708;
            public static PrtType type_0_134078708;
            public static PrtType type_4_134078708;
            public static PrtType type_6_134078708;
            public static PrtType type_5_134078708;
            public static PrtType type_7_134078708;
            public static PrtType type_9_134078708;
            public static PrtType type_8_134078708;
            public static PrtType type_10_134078708;
            public static PrtType type_11_134078708;
            public static PrtType type_12_134078708;
            public static PrtType type_13_134078708;
            public static PrtType type_14_134078708;
            public static PrtType type_15_134078708;
            public static PrtType type_16_134078708;
            public static PrtType type_17_134078708;
            public static PrtType type_19_134078708;
            public static PrtType type_18_134078708;
            public static PrtType type_20_134078708;
            public static PrtType type_21_134078708;
            public static PrtType type_22_134078708;
            public static PrtType type_23_134078708;
            public static PrtType type_24_134078708;
            public static PrtType type_25_134078708;
            public static PrtType type_26_134078708;
            public static PrtType type_27_134078708;
            public static PrtType type_28_134078708;
            static public void Types_Multi_Paxos_4()
            {
                Types.type_1_134078708 = new PrtMachineType();
                Types.type_2_134078708 = new PrtIntType();
                Types.type_3_134078708 = new PrtNamedTupleType(new object[]{"round", Types.type_2_134078708, "servermachine", Types.type_2_134078708});
                Types.type_0_134078708 = new PrtNamedTupleType(new object[]{"proposer", Types.type_1_134078708, "slot", Types.type_2_134078708, "proposal", Types.type_3_134078708, "value", Types.type_2_134078708});
                Types.type_4_134078708 = new PrtNamedTupleType(new object[]{"proposer", Types.type_1_134078708, "slot", Types.type_2_134078708, "proposal", Types.type_3_134078708});
                Types.type_6_134078708 = new PrtNamedTupleType(new object[]{"proposal", Types.type_3_134078708, "value", Types.type_2_134078708});
                Types.type_5_134078708 = new PrtMapType(Types.type_2_134078708, Types.type_6_134078708);
                Types.type_7_134078708 = new PrtNamedTupleType(new object[]{"slot", Types.type_2_134078708, "proposal", Types.type_3_134078708, "value", Types.type_2_134078708});
                Types.type_9_134078708 = new PrtSeqType(Types.type_1_134078708);
                Types.type_8_134078708 = new PrtNamedTupleType(new object[]{"servers", Types.type_9_134078708, "parentServer", Types.type_1_134078708, "rank", Types.type_2_134078708});
                Types.type_10_134078708 = new PrtNamedTupleType(new object[]{"slot", Types.type_2_134078708, "proposal", Types.type_3_134078708});
                Types.type_11_134078708 = new PrtTupleType(new PrtType[]{Types.type_1_134078708, Types.type_2_134078708});
                Types.type_12_134078708 = new PrtNamedTupleType(new object[]{"mymachine", Types.type_1_134078708});
                Types.type_13_134078708 = new PrtNamedTupleType(new object[]{"nodes", Types.type_9_134078708});
                Types.type_14_134078708 = new PrtNamedTupleType(new object[]{"seqmachine", Types.type_2_134078708, "command", Types.type_2_134078708});
                Types.type_15_134078708 = new PrtMapType(Types.type_2_134078708, Types.type_2_134078708);
                Types.type_16_134078708 = new PrtNamedTupleType(new object[]{"rank", Types.type_2_134078708, "server", Types.type_1_134078708});
                Types.type_17_134078708 = new PrtNamedTupleType(new object[]{"rank", Types.type_2_134078708});
                Types.type_19_134078708 = new PrtInterfaceType("Interface");
                Types.type_18_134078708 = new PrtNamedTupleType(new object[]{"proposer", Types.type_19_134078708, "slot", Types.type_2_134078708, "proposal", Types.type_3_134078708});
                Types.type_20_134078708 = new PrtNamedTupleType(new object[]{"servers", Types.type_9_134078708, "parentServer", Types.type_19_134078708, "rank", Types.type_2_134078708});
                Types.type_21_134078708 = new PrtNamedTupleType(new object[]{"proposer", Types.type_19_134078708, "slot", Types.type_2_134078708, "proposal", Types.type_3_134078708, "value", Types.type_2_134078708});
                Types.type_22_134078708 = new PrtTupleType(new PrtType[]{Types.type_19_134078708, Types.type_2_134078708});
                Types.type_23_134078708 = new PrtTupleType(new PrtType[]{Types.type_2_134078708, Types.type_1_134078708});
                Types.type_24_134078708 = new PrtNamedTupleType(new object[]{"rank", Types.type_2_134078708, "server", Types.type_19_134078708});
                Types.type_25_134078708 = new PrtAnyType();
                Types.type_26_134078708 = new PrtEventType();
                Types.type_27_134078708 = new PrtBoolType();
                Types.type_28_134078708 = new PrtNullType();
            }
        }

        public static PrtImplMachine CreateMachine_PaxosNode(StateImpl application, PrtValue payload)
        {
            var machine = new PaxosNode(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).Trace("<CreateLog> Created Machine PaxosNode-{0}", (machine).instanceNumber);
            (((machine).self).permissions).Add(Events.accepted);
            (((machine).self).permissions).Add(Events.reject);
            (((machine).self).permissions).Add(Events.agree);
            (((machine).self).permissions).Add(Events.accept);
            (((machine).self).permissions).Add(Events.prepare);
            (((machine).self).permissions).Add(Events.halt);
            (((machine).self).permissions).Add(Events.response);
            (((machine).self).permissions).Add(Events.cancelTimerSuccess);
            (((machine).self).permissions).Add(Events.cancelTimer);
            (((machine).self).permissions).Add(Events.startTimer);
            (((machine).self).permissions).Add(Events.timeout);
            (((machine).self).permissions).Add(Events.newLeader);
            (((machine).self).permissions).Add(Events.Ping);
            (((machine).self).permissions).Add(Events.announce_proposer_chosen);
            (((machine).self).permissions).Add(Events.announce_proposer_sent);
            (((machine).self).permissions).Add(Events.announce_client_sent);
            (((machine).self).permissions).Add(Events.announce_valueProposed);
            (((machine).self).permissions).Add(Events.announce_valueChosen);
            (((machine).self).permissions).Add(Events.update);
            (((machine).self).permissions).Add(Events.chosen);
            (((machine).self).permissions).Add(Events.goPropose);
            (((machine).self).permissions).Add(Events.allNodes);
            (((machine).self).permissions).Add(Events.success);
            (((machine).self).permissions).Add(Events.local);
            ((machine).sends).Add(Events.response);
            ((machine).sends).Add(Events.cancelTimerSuccess);
            ((machine).sends).Add(Events.cancelTimer);
            ((machine).sends).Add(Events.startTimer);
            ((machine).sends).Add(Events.timeout);
            ((machine).sends).Add(Events.newLeader);
            ((machine).sends).Add(Events.Ping);
            ((machine).sends).Add(Events.announce_proposer_chosen);
            ((machine).sends).Add(Events.announce_proposer_sent);
            ((machine).sends).Add(Events.announce_client_sent);
            ((machine).sends).Add(Events.announce_valueProposed);
            ((machine).sends).Add(Events.announce_valueChosen);
            ((machine).sends).Add(Events.update);
            ((machine).sends).Add(Events.chosen);
            ((machine).sends).Add(Events.goPropose);
            ((machine).sends).Add(Events.allNodes);
            ((machine).sends).Add(Events.success);
            ((machine).sends).Add(Events.local);
            ((machine).sends).Add(Events.accepted);
            ((machine).sends).Add(Events.reject);
            ((machine).sends).Add(Events.agree);
            ((machine).sends).Add(Events.accept);
            ((machine).sends).Add(Events.prepare);
            ((machine).sends).Add(Events.halt);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtImplMachine CreateMachine_Client(StateImpl application, PrtValue payload)
        {
            var machine = new Client(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).Trace("<CreateLog> Created Machine Client-{0}", (machine).instanceNumber);
            (((machine).self).permissions).Add(Events.response);
            (((machine).self).permissions).Add(Events.cancelTimerSuccess);
            (((machine).self).permissions).Add(Events.cancelTimer);
            (((machine).self).permissions).Add(Events.startTimer);
            (((machine).self).permissions).Add(Events.timeout);
            (((machine).self).permissions).Add(Events.newLeader);
            (((machine).self).permissions).Add(Events.Ping);
            (((machine).self).permissions).Add(Events.announce_proposer_chosen);
            (((machine).self).permissions).Add(Events.announce_proposer_sent);
            (((machine).self).permissions).Add(Events.announce_client_sent);
            (((machine).self).permissions).Add(Events.announce_valueProposed);
            (((machine).self).permissions).Add(Events.announce_valueChosen);
            (((machine).self).permissions).Add(Events.update);
            (((machine).self).permissions).Add(Events.chosen);
            (((machine).self).permissions).Add(Events.goPropose);
            (((machine).self).permissions).Add(Events.allNodes);
            (((machine).self).permissions).Add(Events.success);
            (((machine).self).permissions).Add(Events.local);
            (((machine).self).permissions).Add(Events.accepted);
            (((machine).self).permissions).Add(Events.reject);
            (((machine).self).permissions).Add(Events.agree);
            (((machine).self).permissions).Add(Events.accept);
            (((machine).self).permissions).Add(Events.prepare);
            (((machine).self).permissions).Add(Events.halt);
            ((machine).sends).Add(Events.response);
            ((machine).sends).Add(Events.cancelTimerSuccess);
            ((machine).sends).Add(Events.cancelTimer);
            ((machine).sends).Add(Events.startTimer);
            ((machine).sends).Add(Events.timeout);
            ((machine).sends).Add(Events.newLeader);
            ((machine).sends).Add(Events.Ping);
            ((machine).sends).Add(Events.announce_proposer_chosen);
            ((machine).sends).Add(Events.announce_proposer_sent);
            ((machine).sends).Add(Events.announce_client_sent);
            ((machine).sends).Add(Events.announce_valueProposed);
            ((machine).sends).Add(Events.announce_valueChosen);
            ((machine).sends).Add(Events.update);
            ((machine).sends).Add(Events.chosen);
            ((machine).sends).Add(Events.goPropose);
            ((machine).sends).Add(Events.allNodes);
            ((machine).sends).Add(Events.success);
            ((machine).sends).Add(Events.local);
            ((machine).sends).Add(Events.accepted);
            ((machine).sends).Add(Events.reject);
            ((machine).sends).Add(Events.agree);
            ((machine).sends).Add(Events.accept);
            ((machine).sends).Add(Events.prepare);
            ((machine).sends).Add(Events.halt);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtImplMachine CreateMachine_Main(StateImpl application, PrtValue payload)
        {
            var machine = new Main(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).Trace("<CreateLog> Created Machine Main-{0}", (machine).instanceNumber);
            (((machine).self).permissions).Add(Events.response);
            (((machine).self).permissions).Add(Events.cancelTimerSuccess);
            (((machine).self).permissions).Add(Events.cancelTimer);
            (((machine).self).permissions).Add(Events.startTimer);
            (((machine).self).permissions).Add(Events.timeout);
            (((machine).self).permissions).Add(Events.newLeader);
            (((machine).self).permissions).Add(Events.Ping);
            (((machine).self).permissions).Add(Events.announce_proposer_chosen);
            (((machine).self).permissions).Add(Events.announce_proposer_sent);
            (((machine).self).permissions).Add(Events.announce_client_sent);
            (((machine).self).permissions).Add(Events.announce_valueProposed);
            (((machine).self).permissions).Add(Events.announce_valueChosen);
            (((machine).self).permissions).Add(Events.update);
            (((machine).self).permissions).Add(Events.chosen);
            (((machine).self).permissions).Add(Events.goPropose);
            (((machine).self).permissions).Add(Events.allNodes);
            (((machine).self).permissions).Add(Events.success);
            (((machine).self).permissions).Add(Events.local);
            (((machine).self).permissions).Add(Events.accepted);
            (((machine).self).permissions).Add(Events.reject);
            (((machine).self).permissions).Add(Events.agree);
            (((machine).self).permissions).Add(Events.accept);
            (((machine).self).permissions).Add(Events.prepare);
            (((machine).self).permissions).Add(Events.halt);
            ((machine).sends).Add(Events.response);
            ((machine).sends).Add(Events.cancelTimerSuccess);
            ((machine).sends).Add(Events.cancelTimer);
            ((machine).sends).Add(Events.startTimer);
            ((machine).sends).Add(Events.timeout);
            ((machine).sends).Add(Events.newLeader);
            ((machine).sends).Add(Events.Ping);
            ((machine).sends).Add(Events.announce_proposer_chosen);
            ((machine).sends).Add(Events.announce_proposer_sent);
            ((machine).sends).Add(Events.announce_client_sent);
            ((machine).sends).Add(Events.announce_valueProposed);
            ((machine).sends).Add(Events.announce_valueChosen);
            ((machine).sends).Add(Events.update);
            ((machine).sends).Add(Events.chosen);
            ((machine).sends).Add(Events.goPropose);
            ((machine).sends).Add(Events.allNodes);
            ((machine).sends).Add(Events.success);
            ((machine).sends).Add(Events.local);
            ((machine).sends).Add(Events.accepted);
            ((machine).sends).Add(Events.reject);
            ((machine).sends).Add(Events.agree);
            ((machine).sends).Add(Events.accept);
            ((machine).sends).Add(Events.prepare);
            ((machine).sends).Add(Events.halt);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtImplMachine CreateMachine_Timer(StateImpl application, PrtValue payload)
        {
            var machine = new Timer(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).Trace("<CreateLog> Created Machine Timer-{0}", (machine).instanceNumber);
            (((machine).self).permissions).Add(Events.goPropose);
            (((machine).self).permissions).Add(Events.allNodes);
            (((machine).self).permissions).Add(Events.success);
            (((machine).self).permissions).Add(Events.local);
            (((machine).self).permissions).Add(Events.accepted);
            (((machine).self).permissions).Add(Events.reject);
            (((machine).self).permissions).Add(Events.agree);
            (((machine).self).permissions).Add(Events.accept);
            (((machine).self).permissions).Add(Events.prepare);
            (((machine).self).permissions).Add(Events.halt);
            (((machine).self).permissions).Add(Events.response);
            (((machine).self).permissions).Add(Events.cancelTimerSuccess);
            (((machine).self).permissions).Add(Events.cancelTimer);
            (((machine).self).permissions).Add(Events.startTimer);
            (((machine).self).permissions).Add(Events.timeout);
            (((machine).self).permissions).Add(Events.newLeader);
            (((machine).self).permissions).Add(Events.Ping);
            (((machine).self).permissions).Add(Events.announce_proposer_chosen);
            (((machine).self).permissions).Add(Events.announce_proposer_sent);
            (((machine).self).permissions).Add(Events.announce_client_sent);
            (((machine).self).permissions).Add(Events.announce_valueProposed);
            (((machine).self).permissions).Add(Events.announce_valueChosen);
            (((machine).self).permissions).Add(Events.update);
            (((machine).self).permissions).Add(Events.chosen);
            ((machine).sends).Add(Events.response);
            ((machine).sends).Add(Events.cancelTimerSuccess);
            ((machine).sends).Add(Events.cancelTimer);
            ((machine).sends).Add(Events.startTimer);
            ((machine).sends).Add(Events.timeout);
            ((machine).sends).Add(Events.newLeader);
            ((machine).sends).Add(Events.Ping);
            ((machine).sends).Add(Events.announce_proposer_chosen);
            ((machine).sends).Add(Events.announce_proposer_sent);
            ((machine).sends).Add(Events.announce_client_sent);
            ((machine).sends).Add(Events.announce_valueProposed);
            ((machine).sends).Add(Events.announce_valueChosen);
            ((machine).sends).Add(Events.update);
            ((machine).sends).Add(Events.chosen);
            ((machine).sends).Add(Events.goPropose);
            ((machine).sends).Add(Events.allNodes);
            ((machine).sends).Add(Events.success);
            ((machine).sends).Add(Events.local);
            ((machine).sends).Add(Events.accepted);
            ((machine).sends).Add(Events.reject);
            ((machine).sends).Add(Events.agree);
            ((machine).sends).Add(Events.accept);
            ((machine).sends).Add(Events.prepare);
            ((machine).sends).Add(Events.halt);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtImplMachine CreateMachine_LeaderElection(StateImpl application, PrtValue payload)
        {
            var machine = new LeaderElection(application, PrtImplMachine.DefaultMaxBufferSize, false);
            (application).Trace("<CreateLog> Created Machine LeaderElection-{0}", (machine).instanceNumber);
            (((machine).self).permissions).Add(Events.response);
            (((machine).self).permissions).Add(Events.cancelTimerSuccess);
            (((machine).self).permissions).Add(Events.cancelTimer);
            (((machine).self).permissions).Add(Events.startTimer);
            (((machine).self).permissions).Add(Events.timeout);
            (((machine).self).permissions).Add(Events.newLeader);
            (((machine).self).permissions).Add(Events.Ping);
            (((machine).self).permissions).Add(Events.announce_proposer_chosen);
            (((machine).self).permissions).Add(Events.announce_proposer_sent);
            (((machine).self).permissions).Add(Events.announce_client_sent);
            (((machine).self).permissions).Add(Events.announce_valueProposed);
            (((machine).self).permissions).Add(Events.announce_valueChosen);
            (((machine).self).permissions).Add(Events.update);
            (((machine).self).permissions).Add(Events.chosen);
            (((machine).self).permissions).Add(Events.goPropose);
            (((machine).self).permissions).Add(Events.allNodes);
            (((machine).self).permissions).Add(Events.success);
            (((machine).self).permissions).Add(Events.local);
            (((machine).self).permissions).Add(Events.accepted);
            (((machine).self).permissions).Add(Events.reject);
            (((machine).self).permissions).Add(Events.agree);
            (((machine).self).permissions).Add(Events.accept);
            (((machine).self).permissions).Add(Events.prepare);
            (((machine).self).permissions).Add(Events.halt);
            ((machine).sends).Add(Events.response);
            ((machine).sends).Add(Events.cancelTimerSuccess);
            ((machine).sends).Add(Events.cancelTimer);
            ((machine).sends).Add(Events.startTimer);
            ((machine).sends).Add(Events.timeout);
            ((machine).sends).Add(Events.newLeader);
            ((machine).sends).Add(Events.Ping);
            ((machine).sends).Add(Events.announce_proposer_chosen);
            ((machine).sends).Add(Events.announce_proposer_sent);
            ((machine).sends).Add(Events.announce_client_sent);
            ((machine).sends).Add(Events.announce_valueProposed);
            ((machine).sends).Add(Events.announce_valueChosen);
            ((machine).sends).Add(Events.update);
            ((machine).sends).Add(Events.chosen);
            ((machine).sends).Add(Events.goPropose);
            ((machine).sends).Add(Events.allNodes);
            ((machine).sends).Add(Events.success);
            ((machine).sends).Add(Events.local);
            ((machine).sends).Add(Events.accepted);
            ((machine).sends).Add(Events.reject);
            ((machine).sends).Add(Events.agree);
            ((machine).sends).Add(Events.accept);
            ((machine).sends).Add(Events.prepare);
            ((machine).sends).Add(Events.halt);
            (machine).currentPayload = payload;
            return machine;
        }

        public static PrtSpecMachine CreateSpecMachine_ValmachineityCheck(StateImpl application)
        {
            var machine = new ValmachineityCheck(application);
            (application).Trace("<CreateLog> Created spec Machine ValmachineityCheck");
            ((machine).observes).Add(Events.announce_proposer_chosen);
            ((machine).observes).Add(Events.announce_proposer_sent);
            ((machine).observes).Add(Events.announce_client_sent);
            return machine;
        }

        public static PrtSpecMachine CreateSpecMachine_BasicPaxosInvariant_P2b(StateImpl application)
        {
            var machine = new BasicPaxosInvariant_P2b(application);
            (application).Trace("<CreateLog> Created spec Machine BasicPaxosInvariant_P2b");
            ((machine).observes).Add(Events.announce_valueProposed);
            ((machine).observes).Add(Events.announce_valueChosen);
            return machine;
        }

        public class PaxosNode : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return PaxosNode_Init;
                }
            }

            public PrtValue lastExecutedSlot
            {
                get
                {
                    return fields[0];
                }

                set
                {
                    fields[0] = value;
                }
            }

            public PrtValue learnerSlots
            {
                get
                {
                    return fields[1];
                }

                set
                {
                    fields[1] = value;
                }
            }

            public PrtValue receivedMess_2
            {
                get
                {
                    return fields[2];
                }

                set
                {
                    fields[2] = value;
                }
            }

            public PrtValue acceptorSlots
            {
                get
                {
                    return fields[3];
                }

                set
                {
                    fields[3] = value;
                }
            }

            public PrtValue currCommitOperation
            {
                get
                {
                    return fields[4];
                }

                set
                {
                    fields[4] = value;
                }
            }

            public PrtValue nextSlotForProposer
            {
                get
                {
                    return fields[5];
                }

                set
                {
                    fields[5] = value;
                }
            }

            public PrtValue receivedMess_1
            {
                get
                {
                    return fields[6];
                }

                set
                {
                    fields[6] = value;
                }
            }

            public PrtValue timer
            {
                get
                {
                    return fields[7];
                }

                set
                {
                    fields[7] = value;
                }
            }

            public PrtValue returnVal
            {
                get
                {
                    return fields[8];
                }

                set
                {
                    fields[8] = value;
                }
            }

            public PrtValue tempVal
            {
                get
                {
                    return fields[9];
                }

                set
                {
                    fields[9] = value;
                }
            }

            public PrtValue countAgree
            {
                get
                {
                    return fields[10];
                }

                set
                {
                    fields[10] = value;
                }
            }

            public PrtValue countAccept
            {
                get
                {
                    return fields[11];
                }

                set
                {
                    fields[11] = value;
                }
            }

            public PrtValue maxRound
            {
                get
                {
                    return fields[12];
                }

                set
                {
                    fields[12] = value;
                }
            }

            public PrtValue iter
            {
                get
                {
                    return fields[13];
                }

                set
                {
                    fields[13] = value;
                }
            }

            public PrtValue receivedAgree
            {
                get
                {
                    return fields[14];
                }

                set
                {
                    fields[14] = value;
                }
            }

            public PrtValue nextProposal
            {
                get
                {
                    return fields[15];
                }

                set
                {
                    fields[15] = value;
                }
            }

            public PrtValue myRank
            {
                get
                {
                    return fields[16];
                }

                set
                {
                    fields[16] = value;
                }
            }

            public PrtValue roundNum
            {
                get
                {
                    return fields[17];
                }

                set
                {
                    fields[17] = value;
                }
            }

            public PrtValue majority
            {
                get
                {
                    return fields[18];
                }

                set
                {
                    fields[18] = value;
                }
            }

            public PrtValue proposeVal
            {
                get
                {
                    return fields[19];
                }

                set
                {
                    fields[19] = value;
                }
            }

            public PrtValue commitValue
            {
                get
                {
                    return fields[20];
                }

                set
                {
                    fields[20] = value;
                }
            }

            public PrtValue acceptors
            {
                get
                {
                    return fields[21];
                }

                set
                {
                    fields[21] = value;
                }
            }

            public PrtValue leaderElectionService
            {
                get
                {
                    return fields[22];
                }

                set
                {
                    fields[22] = value;
                }
            }

            public PrtValue currentLeader
            {
                get
                {
                    return fields[23];
                }

                set
                {
                    fields[23] = value;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new PaxosNode();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "PaxosNode";
                }
            }

            public PaxosNode(): base ()
            {
            }

            public PaxosNode(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_5_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_0_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_5_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_27_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_7_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_1_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_27_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_6_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_3_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_9_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_1_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_16_134078708));
            }

            public class ignore_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class ignore_StackFrame : PrtFunStackFrame
                {
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new ignore_StackFrame(this, locals, retLoc);
                }
            }

            public static ignore_Class ignore = new ignore_Class();
            public class RunReplicatedMachine_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class RunReplicatedMachine_StackFrame : PrtFunStackFrame
                {
                    public RunReplicatedMachine_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public RunReplicatedMachine_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    RunReplicatedMachine_loop_start_0:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(true))).bl)
                        goto RunReplicatedMachine_loop_end_0;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtMapValue)((parent).learnerSlots)).Contains(new PrtIntValue(((PrtIntValue)((parent).lastExecutedSlot)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt))))).bl)
                        goto RunReplicatedMachine_if_0_else;
                    (parent).lastExecutedSlot = (new PrtIntValue(((PrtIntValue)((parent).lastExecutedSlot)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    goto RunReplicatedMachine_if_0_end;
                    RunReplicatedMachine_if_0_else:
                        ;
                    (parent).PrtFunContReturn((currFun).locals);
                    return;
                    RunReplicatedMachine_if_0_end:
                        ;
                    goto RunReplicatedMachine_loop_start_0;
                    RunReplicatedMachine_loop_end_0:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new RunReplicatedMachine_StackFrame(this, locals, retLoc);
                }
            }

            public static RunReplicatedMachine_Class RunReplicatedMachine = new RunReplicatedMachine_Class();
            public class getHighestProposedValue_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class getHighestProposedValue_StackFrame : PrtFunStackFrame
                {
                    public getHighestProposedValue_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public getHighestProposedValue_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!((PrtBoolValue)(new PrtBoolValue(!((((PrtTupleValue)((parent).receivedAgree)).fieldValues[1]).Clone()).Equals(new PrtIntValue(-((PrtIntValue)(new PrtIntValue(1))).nt))))).bl)
                        goto getHighestProposedValue_if_0_else;
                    (parent).currCommitOperation = (new PrtBoolValue(false)).Clone();
                    (parent).PrtFunContReturnVal((((PrtTupleValue)((parent).receivedAgree)).fieldValues[1]).Clone(), (currFun).locals);
                    return;
                    goto getHighestProposedValue_if_0_end;
                    getHighestProposedValue_if_0_else:
                        ;
                    (parent).currCommitOperation = (new PrtBoolValue(true)).Clone();
                    (parent).PrtFunContReturnVal((parent).commitValue, (currFun).locals);
                    return;
                    getHighestProposedValue_if_0_end:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new getHighestProposedValue_StackFrame(this, locals, retLoc);
                }
            }

            public static getHighestProposedValue_Class getHighestProposedValue = new getHighestProposedValue_Class();
            public class CountAccepted_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class CountAccepted_StackFrame : PrtFunStackFrame
                {
                    public CountAccepted_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public CountAccepted_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue receivedMess_1
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto CountAccepted_1;
                    }

                    if (!((PrtBoolValue)(new PrtBoolValue(((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone()).Equals((parent).nextSlotForProposer)))).bl)
                        goto CountAccepted_if_2_else;
                    (parent).returnVal = ((equal).ExecuteToCompletion(application, parent, (((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone(), (parent).nextProposal)).Clone();
                    if (!((PrtBoolValue)((parent).returnVal)).bl)
                        goto CountAccepted_if_0_else;
                    (parent).countAccept = (new PrtIntValue(((PrtIntValue)((parent).countAccept)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    goto CountAccepted_if_0_end;
                    CountAccepted_if_0_else:
                        ;
                    CountAccepted_if_0_end:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).countAccept).Equals((parent).majority)))).bl)
                        goto CountAccepted_if_1_else;
                    (application).Announce((PrtEventValue)(Events.announce_valueChosen), new PrtNamedTupleValue(Types.type_21_134078708, parent.self, (parent).nextSlotForProposer, (parent).nextProposal, (parent).proposeVal), parent);
                    (((PrtMachineValue)((parent).timer)).mach).PrtEnqueueEvent((PrtEventValue)(Events.cancelTimer), Events.@null, parent, (PrtMachineValue)((parent).timer));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    CountAccepted_1:
                        ;
                    (application).Announce((PrtEventValue)(Events.announce_proposer_chosen), (parent).proposeVal, parent);
                    (parent).nextSlotForProposer = (new PrtIntValue(((PrtIntValue)((parent).nextSlotForProposer)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    if (!!(Events.chosen).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (242, 5): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine PaxosNode-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.chosen)).evt).name);
                    (parent).currentTrigger = Events.chosen;
                    (parent).currentPayload = (currFun).locals[0];
                    (parent).PrtFunContRaise();
                    return;
                    goto CountAccepted_if_1_end;
                    CountAccepted_if_1_else:
                        ;
                    CountAccepted_if_1_end:
                        ;
                    goto CountAccepted_if_2_end;
                    CountAccepted_if_2_else:
                        ;
                    CountAccepted_if_2_end:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new CountAccepted_StackFrame(this, locals, retLoc);
                }
            }

            public static CountAccepted_Class CountAccepted = new CountAccepted_Class();
            public class BroadCastAcceptors_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class BroadCastAcceptors_StackFrame : PrtFunStackFrame
                {
                    public BroadCastAcceptors_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public BroadCastAcceptors_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue mess
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }

                    public PrtValue pay
                    {
                        get
                        {
                            return locals[1];
                        }

                        set
                        {
                            locals[1] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto BroadCastAcceptors_1;
                    }

                    (parent).iter = (new PrtIntValue(0)).Clone();
                    BroadCastAcceptors_loop_start_0:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((parent).iter)).nt < ((PrtIntValue)(new PrtIntValue(((parent).acceptors).Size()))).nt))).bl)
                        goto BroadCastAcceptors_loop_end_0;
                    (((PrtMachineValue)((((PrtSeqValue)((parent).acceptors)).Lookup((parent).iter)).Clone())).mach).PrtEnqueueEvent((PrtEventValue)((currFun).locals[0]), (currFun).locals[1], parent, (PrtMachineValue)((((PrtSeqValue)((parent).acceptors)).Lookup((parent).iter)).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    BroadCastAcceptors_1:
                        ;
                    (parent).iter = (new PrtIntValue(((PrtIntValue)((parent).iter)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    goto BroadCastAcceptors_loop_start_0;
                    BroadCastAcceptors_loop_end_0:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new BroadCastAcceptors_StackFrame(this, locals, retLoc);
                }
            }

            public static BroadCastAcceptors_Class BroadCastAcceptors = new BroadCastAcceptors_Class();
            public class lessThan_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class lessThan_StackFrame : PrtFunStackFrame
                {
                    public lessThan_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public lessThan_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue p1
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }

                    public PrtValue p2
                    {
                        get
                        {
                            return locals[1];
                        }

                        set
                        {
                            locals[1] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone())).nt < ((PrtIntValue)((((PrtTupleValue)((currFun).locals[1])).fieldValues[0]).Clone())).nt))).bl)
                        goto lessThan_if_2_else;
                    (parent).PrtFunContReturnVal(new PrtBoolValue(true), (currFun).locals);
                    return;
                    goto lessThan_if_2_end;
                    lessThan_if_2_else:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone()).Equals((((PrtTupleValue)((currFun).locals[1])).fieldValues[0]).Clone())))).bl)
                        goto lessThan_if_1_else;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).nt < ((PrtIntValue)((((PrtTupleValue)((currFun).locals[1])).fieldValues[1]).Clone())).nt))).bl)
                        goto lessThan_if_0_else;
                    (parent).PrtFunContReturnVal(new PrtBoolValue(true), (currFun).locals);
                    return;
                    goto lessThan_if_0_end;
                    lessThan_if_0_else:
                        ;
                    (parent).PrtFunContReturnVal(new PrtBoolValue(false), (currFun).locals);
                    return;
                    lessThan_if_0_end:
                        ;
                    goto lessThan_if_1_end;
                    lessThan_if_1_else:
                        ;
                    (parent).PrtFunContReturnVal(new PrtBoolValue(false), (currFun).locals);
                    return;
                    lessThan_if_1_end:
                        ;
                    lessThan_if_2_end:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new lessThan_StackFrame(this, locals, retLoc);
                }
            }

            public static lessThan_Class lessThan = new lessThan_Class();
            public class equal_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class equal_StackFrame : PrtFunStackFrame
                {
                    public equal_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public equal_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue p1
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }

                    public PrtValue p2
                    {
                        get
                        {
                            return locals[1];
                        }

                        set
                        {
                            locals[1] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtBoolValue)(new PrtBoolValue(((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone()).Equals((((PrtTupleValue)((currFun).locals[1])).fieldValues[0]).Clone())))).bl && ((PrtBoolValue)(new PrtBoolValue(((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone()).Equals((((PrtTupleValue)((currFun).locals[1])).fieldValues[1]).Clone())))).bl))).bl)
                        goto equal_if_0_else;
                    (parent).PrtFunContReturnVal(new PrtBoolValue(true), (currFun).locals);
                    return;
                    goto equal_if_0_end;
                    equal_if_0_else:
                        ;
                    (parent).PrtFunContReturnVal(new PrtBoolValue(false), (currFun).locals);
                    return;
                    equal_if_0_end:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new equal_StackFrame(this, locals, retLoc);
                }
            }

            public static equal_Class equal = new equal_Class();
            public class GetNextProposal_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class GetNextProposal_StackFrame : PrtFunStackFrame
                {
                    public GetNextProposal_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public GetNextProposal_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue maxRound
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    (parent).PrtFunContReturnVal(new PrtNamedTupleValue(Types.type_3_134078708, new PrtIntValue(((PrtIntValue)((currFun).locals[0])).nt + ((PrtIntValue)(new PrtIntValue(1))).nt), (parent).myRank), (currFun).locals);
                    return;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new GetNextProposal_StackFrame(this, locals, retLoc);
                }
            }

            public static GetNextProposal_Class GetNextProposal = new GetNextProposal_Class();
            public class acceptfun_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class acceptfun_StackFrame : PrtFunStackFrame
                {
                    public acceptfun_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public acceptfun_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue receivedMess_2
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto acceptfun_1;
                        case 2:
                            goto acceptfun_2;
                    }

                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtMapValue)((parent).acceptorSlots)).Contains((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())))).bl)
                        goto acceptfun_if_1_else;
                    (parent).returnVal = ((equal).ExecuteToCompletion(application, parent, (((PrtTupleValue)((currFun).locals[0])).fieldValues[2]).Clone(), (((PrtTupleValue)((((PrtMapValue)((parent).acceptorSlots)).Lookup((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).Clone())).fieldValues[0]).Clone())).Clone();
                    if (!((PrtBoolValue)(new PrtBoolValue(!((PrtBoolValue)((parent).returnVal)).bl))).bl)
                        goto acceptfun_if_0_else;
                    (((PrtMachineValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.reject), new PrtNamedTupleValue(Types.type_10_134078708, (((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone(), (((PrtTupleValue)((((PrtMapValue)((parent).acceptorSlots)).Lookup((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).Clone())).fieldValues[0]).Clone()), parent, (PrtMachineValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    acceptfun_1:
                        ;
                    goto acceptfun_if_0_end;
                    acceptfun_if_0_else:
                        ;
                    ((PrtMapValue)((parent).acceptorSlots)).Update((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone(), (new PrtNamedTupleValue(Types.type_6_134078708, (((PrtTupleValue)((currFun).locals[0])).fieldValues[2]).Clone(), (((PrtTupleValue)((currFun).locals[0])).fieldValues[3]).Clone())).Clone());
                    (((PrtMachineValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.accepted), new PrtNamedTupleValue(Types.type_7_134078708, (((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone(), (((PrtTupleValue)((currFun).locals[0])).fieldValues[2]).Clone(), (((PrtTupleValue)((currFun).locals[0])).fieldValues[3]).Clone()), parent, (PrtMachineValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 2);
                    return;
                    acceptfun_2:
                        ;
                    acceptfun_if_0_end:
                        ;
                    goto acceptfun_if_1_end;
                    acceptfun_if_1_else:
                        ;
                    acceptfun_if_1_end:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new acceptfun_StackFrame(this, locals, retLoc);
                }
            }

            public static acceptfun_Class acceptfun = new acceptfun_Class();
            public class preparefun_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class preparefun_StackFrame : PrtFunStackFrame
                {
                    public preparefun_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public preparefun_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue receivedMess_2
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto preparefun_1;
                        case 2:
                            goto preparefun_2;
                        case 3:
                            goto preparefun_3;
                    }

                    if (!((PrtBoolValue)(new PrtBoolValue(!((PrtBoolValue)(new PrtBoolValue(((PrtMapValue)((parent).acceptorSlots)).Contains((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())))).bl))).bl)
                        goto preparefun_if_0_else;
                    (((PrtMachineValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.agree), new PrtNamedTupleValue(Types.type_7_134078708, (((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone(), new PrtNamedTupleValue(Types.type_3_134078708, new PrtIntValue(-((PrtIntValue)(new PrtIntValue(1))).nt), new PrtIntValue(-((PrtIntValue)(new PrtIntValue(1))).nt)), new PrtIntValue(-((PrtIntValue)(new PrtIntValue(1))).nt)), parent, (PrtMachineValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    preparefun_1:
                        ;
                    ((PrtMapValue)((parent).acceptorSlots)).Update((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone(), (new PrtNamedTupleValue(Types.type_6_134078708, (((PrtTupleValue)((currFun).locals[0])).fieldValues[2]).Clone(), new PrtIntValue(-((PrtIntValue)(new PrtIntValue(1))).nt))).Clone());
                    (parent).PrtFunContReturn((currFun).locals);
                    return;
                    goto preparefun_if_0_end;
                    preparefun_if_0_else:
                        ;
                    preparefun_if_0_end:
                        ;
                    (parent).returnVal = ((lessThan).ExecuteToCompletion(application, parent, (((PrtTupleValue)((currFun).locals[0])).fieldValues[2]).Clone(), (((PrtTupleValue)((((PrtMapValue)((parent).acceptorSlots)).Lookup((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).Clone())).fieldValues[0]).Clone())).Clone();
                    if (!((PrtBoolValue)((parent).returnVal)).bl)
                        goto preparefun_if_1_else;
                    (((PrtMachineValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.reject), new PrtNamedTupleValue(Types.type_10_134078708, (((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone(), (((PrtTupleValue)((((PrtMapValue)((parent).acceptorSlots)).Lookup((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).Clone())).fieldValues[0]).Clone()), parent, (PrtMachineValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 2);
                    return;
                    preparefun_2:
                        ;
                    goto preparefun_if_1_end;
                    preparefun_if_1_else:
                        ;
                    (((PrtMachineValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.agree), new PrtNamedTupleValue(Types.type_7_134078708, (((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone(), (((PrtTupleValue)((((PrtMapValue)((parent).acceptorSlots)).Lookup((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).Clone())).fieldValues[0]).Clone(), (((PrtTupleValue)((((PrtMapValue)((parent).acceptorSlots)).Lookup((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).Clone())).fieldValues[1]).Clone()), parent, (PrtMachineValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 3);
                    return;
                    preparefun_3:
                        ;
                    ((PrtMapValue)((parent).acceptorSlots)).Update((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone(), (new PrtNamedTupleValue(Types.type_6_134078708, (((PrtTupleValue)((currFun).locals[0])).fieldValues[2]).Clone(), new PrtIntValue(-((PrtIntValue)(new PrtIntValue(1))).nt))).Clone());
                    preparefun_if_1_end:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new preparefun_StackFrame(this, locals, retLoc);
                }
            }

            public static preparefun_Class preparefun = new preparefun_Class();
            public class CheckIfLeader_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class CheckIfLeader_StackFrame : PrtFunStackFrame
                {
                    public CheckIfLeader_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public CheckIfLeader_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto CheckIfLeader_1;
                    }

                    if (!((PrtBoolValue)(new PrtBoolValue(((((PrtTupleValue)((parent).currentLeader)).fieldValues[0]).Clone()).Equals((parent).myRank)))).bl)
                        goto CheckIfLeader_if_0_else;
                    (parent).commitValue = ((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone()).Clone();
                    (parent).proposeVal = ((parent).commitValue).Clone();
                    if (!!(Events.goPropose).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (79, 4): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine PaxosNode-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.goPropose)).evt).name);
                    (parent).currentTrigger = Events.goPropose;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    goto CheckIfLeader_if_0_end;
                    CheckIfLeader_if_0_else:
                        ;
                    (((PrtMachineValue)((((PrtTupleValue)((parent).currentLeader)).fieldValues[1]).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.update), (currFun).locals[0], parent, (PrtMachineValue)((((PrtTupleValue)((parent).currentLeader)).fieldValues[1]).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    CheckIfLeader_1:
                        ;
                    CheckIfLeader_if_0_end:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new CheckIfLeader_StackFrame(this, locals, retLoc);
                }
            }

            public static CheckIfLeader_Class CheckIfLeader = new CheckIfLeader_Class();
            public class UpdateAcceptors_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class UpdateAcceptors_StackFrame : PrtFunStackFrame
                {
                    public UpdateAcceptors_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public UpdateAcceptors_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto UpdateAcceptors_1;
                    }

                    (parent).acceptors = ((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone()).Clone();
                    (parent).majority = (new PrtIntValue(((PrtIntValue)(new PrtIntValue(((PrtIntValue)(new PrtIntValue(((parent).acceptors).Size()))).nt / ((PrtIntValue)(new PrtIntValue(2))).nt))).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).majority).Equals(new PrtIntValue(2))))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (67, 3): Assert failed");
                    (parent).leaderElectionService = (application).CreateInterfaceOrMachine((parent).renamedName, "LeaderElection", new PrtNamedTupleValue(Types.type_20_134078708, (parent).acceptors, parent.self, (parent).myRank));
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 1);
                    return;
                    UpdateAcceptors_1:
                        ;
                    if (!!(Events.local).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (71, 3): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine PaxosNode-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.local)).evt).name);
                    (parent).currentTrigger = Events.local;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new UpdateAcceptors_StackFrame(this, locals, retLoc);
                }
            }

            public static UpdateAcceptors_Class UpdateAcceptors = new UpdateAcceptors_Class();
            public class AnonFun0_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun0_StackFrame : PrtFunStackFrame
                {
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun0_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun0_Class AnonFun0 = new AnonFun0_Class();
            public class AnonFun1_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun1_StackFrame : PrtFunStackFrame
                {
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun1_1;
                        case 2:
                            goto AnonFun1_2;
                    }

                    (parent).countAgree = (new PrtIntValue(0)).Clone();
                    (parent).nextProposal = ((GetNextProposal).ExecuteToCompletion(application, parent, (parent).maxRound)).Clone();
                    (parent).receivedAgree = (new PrtNamedTupleValue(Types.type_6_134078708, new PrtNamedTupleValue(Types.type_3_134078708, new PrtIntValue(-((PrtIntValue)(new PrtIntValue(1))).nt), new PrtIntValue(-((PrtIntValue)(new PrtIntValue(1))).nt)), new PrtIntValue(-((PrtIntValue)(new PrtIntValue(1))).nt))).Clone();
                    (parent).PrtPushFunStackFrame(BroadCastAcceptors, (BroadCastAcceptors).CreateLocals(Events.prepare, new PrtNamedTupleValue(Types.type_18_134078708, parent.self, (parent).nextSlotForProposer, new PrtNamedTupleValue(Types.type_3_134078708, (((PrtTupleValue)((parent).nextProposal)).fieldValues[0]).Clone(), (parent).myRank))));
                    AnonFun1_1:
                        ;
                    (BroadCastAcceptors).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 1);
                        return;
                    }

                    (application).Announce((PrtEventValue)(Events.announce_proposer_sent), (parent).proposeVal, parent);
                    (((PrtMachineValue)((parent).timer)).mach).PrtEnqueueEvent((PrtEventValue)(Events.startTimer), Events.@null, parent, (PrtMachineValue)((parent).timer));
                    (parent).PrtFunContSend(this, (currFun).locals, 2);
                    return;
                    AnonFun1_2:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun1_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun1_Class AnonFun1 = new AnonFun1_Class();
            public class AnonFun2_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun2_StackFrame : PrtFunStackFrame
                {
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun2_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
            public class AnonFun3_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun3_StackFrame : PrtFunStackFrame
                {
                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_0
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun3_1;
                    }

                    (((PrtMachineValue)((parent).timer)).mach).PrtEnqueueEvent((PrtEventValue)(Events.cancelTimer), Events.@null, parent, (PrtMachineValue)((parent).timer));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun3_1:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun3_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun3_Class AnonFun3 = new AnonFun3_Class();
            public class AnonFun4_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun4_StackFrame : PrtFunStackFrame
                {
                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun4_1;
                    }

                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((((PrtTupleValue)((parent).nextProposal)).fieldValues[0]).Clone())).nt <= ((PrtIntValue)((((PrtTupleValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).fieldValues[0]).Clone())).nt))).bl)
                        goto AnonFun4_if_0_else;
                    (parent).maxRound = ((((PrtTupleValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).fieldValues[0]).Clone()).Clone();
                    goto AnonFun4_if_0_end;
                    AnonFun4_if_0_else:
                        ;
                    AnonFun4_if_0_end:
                        ;
                    (((PrtMachineValue)((parent).timer)).mach).PrtEnqueueEvent((PrtEventValue)(Events.cancelTimer), Events.@null, parent, (PrtMachineValue)((parent).timer));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun4_1:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun4_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun4_Class AnonFun4 = new AnonFun4_Class();
            public class AnonFun5_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun5_StackFrame : PrtFunStackFrame
                {
                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun5_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun5_Class AnonFun5 = new AnonFun5_Class();
            public class AnonFun6_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun6_StackFrame : PrtFunStackFrame
                {
                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun6_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun6_Class AnonFun6 = new AnonFun6_Class();
            public class AnonFun7_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun7_StackFrame : PrtFunStackFrame
                {
                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun7_1;
                    }

                    (parent).myRank = ((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone()).Clone();
                    (parent).currentLeader = (new PrtNamedTupleValue(Types.type_24_134078708, (parent).myRank, parent.self)).Clone();
                    (parent).roundNum = (new PrtIntValue(0)).Clone();
                    (parent).maxRound = (new PrtIntValue(0)).Clone();
                    (parent).timer = (application).CreateInterfaceOrMachine((parent).renamedName, "Timer", new PrtTupleValue(parent.self, new PrtIntValue(10)));
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 1);
                    return;
                    AnonFun7_1:
                        ;
                    (parent).lastExecutedSlot = (new PrtIntValue(-((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    (parent).nextSlotForProposer = (new PrtIntValue(0)).Clone();
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun7_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun7_Class AnonFun7 = new AnonFun7_Class();
            public class AnonFun8_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun8_StackFrame : PrtFunStackFrame
                {
                    public AnonFun8_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun8_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun8_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun8_Class AnonFun8 = new AnonFun8_Class();
            public class AnonFun9_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun9_StackFrame : PrtFunStackFrame
                {
                    public AnonFun9_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun9_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun9_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun9_Class AnonFun9 = new AnonFun9_Class();
            public class AnonFun10_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun10_StackFrame : PrtFunStackFrame
                {
                    public AnonFun10_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun10_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue receivedMess_1
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun10_1;
                    }

                    ((PrtMapValue)((parent).learnerSlots)).Update((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone(), (new PrtNamedTupleValue(Types.type_6_134078708, (((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone(), (((PrtTupleValue)((currFun).locals[0])).fieldValues[2]).Clone())).Clone());
                    (parent).PrtPushFunStackFrame(RunReplicatedMachine, (RunReplicatedMachine).CreateLocals());
                    AnonFun10_1:
                        ;
                    (RunReplicatedMachine).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 1);
                        return;
                    }

                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtBoolValue)((parent).currCommitOperation)).bl && ((PrtBoolValue)(new PrtBoolValue(((parent).commitValue).Equals((((PrtTupleValue)((currFun).locals[0])).fieldValues[2]).Clone())))).bl))).bl)
                        goto AnonFun10_if_0_else;
                    (parent).currentTrigger = Events.@null;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContPop();
                    return;
                    goto AnonFun10_if_0_end;
                    AnonFun10_if_0_else:
                        ;
                    (parent).proposeVal = ((parent).commitValue).Clone();
                    if (!!(Events.goPropose).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (319, 5): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine PaxosNode-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.goPropose)).evt).name);
                    (parent).currentTrigger = Events.goPropose;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    AnonFun10_if_0_end:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun10_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun10_Class AnonFun10 = new AnonFun10_Class();
            public class AnonFun11_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun11_StackFrame : PrtFunStackFrame
                {
                    public AnonFun11_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun11_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun11_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun11_Class AnonFun11 = new AnonFun11_Class();
            public class AnonFun12_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun12_StackFrame : PrtFunStackFrame
                {
                    public AnonFun12_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun12_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_1
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun12_1;
                        case 2:
                            goto AnonFun12_2;
                        case 3:
                            goto AnonFun12_3;
                    }

                    (parent).countAccept = (new PrtIntValue(0)).Clone();
                    (parent).PrtPushFunStackFrame(getHighestProposedValue, (getHighestProposedValue).CreateLocals());
                    AnonFun12_1:
                        ;
                    (getHighestProposedValue).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                        (parent).proposeVal = ((parent).continuation).retVal;
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 1);
                        return;
                    }

                    (application).Announce((PrtEventValue)(Events.announce_valueProposed), new PrtNamedTupleValue(Types.type_21_134078708, parent.self, (parent).nextSlotForProposer, (parent).nextProposal, (parent).proposeVal), parent);
                    (application).Announce((PrtEventValue)(Events.announce_proposer_sent), (parent).proposeVal, parent);
                    (parent).PrtPushFunStackFrame(BroadCastAcceptors, (BroadCastAcceptors).CreateLocals(Events.accept, new PrtNamedTupleValue(Types.type_21_134078708, parent.self, (parent).nextSlotForProposer, (parent).nextProposal, (parent).proposeVal)));
                    AnonFun12_2:
                        ;
                    (BroadCastAcceptors).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 2);
                        return;
                    }

                    (((PrtMachineValue)((parent).timer)).mach).PrtEnqueueEvent((PrtEventValue)(Events.startTimer), Events.@null, parent, (PrtMachineValue)((parent).timer));
                    (parent).PrtFunContSend(this, (currFun).locals, 3);
                    return;
                    AnonFun12_3:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun12_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun12_Class AnonFun12 = new AnonFun12_Class();
            public class AnonFun13_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun13_StackFrame : PrtFunStackFrame
                {
                    public AnonFun13_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun13_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun13_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun13_Class AnonFun13 = new AnonFun13_Class();
            public class AnonFun14_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun14_StackFrame : PrtFunStackFrame
                {
                    public AnonFun14_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun14_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun14_1;
                    }

                    (parent).PrtPushFunStackFrame(CountAccepted, (CountAccepted).CreateLocals((currFun).locals[0]));
                    AnonFun14_1:
                        ;
                    (CountAccepted).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 1);
                        return;
                    }

                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun14_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun14_Class AnonFun14 = new AnonFun14_Class();
            public class AnonFun15_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun15_StackFrame : PrtFunStackFrame
                {
                    public AnonFun15_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun15_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue receivedMess
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!((PrtBoolValue)(new PrtBoolValue(((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone()).Equals((parent).nextSlotForProposer)))).bl)
                        goto AnonFun15_if_2_else;
                    (parent).countAgree = (new PrtIntValue(((PrtIntValue)((parent).countAgree)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    (parent).returnVal = ((lessThan).ExecuteToCompletion(application, parent, (((PrtTupleValue)((parent).receivedAgree)).fieldValues[0]).Clone(), (((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).Clone();
                    if (!((PrtBoolValue)((parent).returnVal)).bl)
                        goto AnonFun15_if_0_else;
                    ((PrtTupleValue)((parent).receivedAgree)).Update(0, ((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone()).Clone());
                    ((PrtTupleValue)((parent).receivedAgree)).Update(1, ((((PrtTupleValue)((currFun).locals[0])).fieldValues[2]).Clone()).Clone());
                    goto AnonFun15_if_0_end;
                    AnonFun15_if_0_else:
                        ;
                    AnonFun15_if_0_end:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).countAgree).Equals((parent).majority)))).bl)
                        goto AnonFun15_if_1_else;
                    if (!!(Events.success).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (209, 6): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine PaxosNode-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.success)).evt).name);
                    (parent).currentTrigger = Events.success;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    goto AnonFun15_if_1_end;
                    AnonFun15_if_1_else:
                        ;
                    AnonFun15_if_1_end:
                        ;
                    goto AnonFun15_if_2_end;
                    AnonFun15_if_2_else:
                        ;
                    AnonFun15_if_2_end:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun15_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun15_Class AnonFun15 = new AnonFun15_Class();
            public class AnonFun16_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun16_StackFrame : PrtFunStackFrame
                {
                    public AnonFun16_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun16_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    (parent).currentLeader = ((currFun).locals[0]).Clone();
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun16_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun16_Class AnonFun16 = new AnonFun16_Class();
            public class AnonFun17_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun17_StackFrame : PrtFunStackFrame
                {
                    public AnonFun17_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun17_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun17_1;
                    }

                    (((PrtMachineValue)((parent).leaderElectionService)).mach).PrtEnqueueEvent((PrtEventValue)(Events.Ping), (currFun).locals[0], parent, (PrtMachineValue)((parent).leaderElectionService));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun17_1:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun17_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun17_Class AnonFun17 = new AnonFun17_Class();
            public class AnonFun18_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun18_StackFrame : PrtFunStackFrame
                {
                    public AnonFun18_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun18_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun18_1;
                    }

                    (parent).PrtPushFunStackFrame(acceptfun, (acceptfun).CreateLocals((currFun).locals[0]));
                    AnonFun18_1:
                        ;
                    (acceptfun).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 1);
                        return;
                    }

                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun18_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun18_Class AnonFun18 = new AnonFun18_Class();
            public class AnonFun19_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun19_StackFrame : PrtFunStackFrame
                {
                    public AnonFun19_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun19_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun19_1;
                    }

                    (parent).PrtPushFunStackFrame(preparefun, (preparefun).CreateLocals((currFun).locals[0]));
                    AnonFun19_1:
                        ;
                    (preparefun).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 1);
                        return;
                    }

                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun19_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun19_Class AnonFun19 = new AnonFun19_Class();
            public class AnonFun20_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun20_StackFrame : PrtFunStackFrame
                {
                    public AnonFun20_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun20_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun20_1;
                    }

                    (parent).PrtPushFunStackFrame(CheckIfLeader, (CheckIfLeader).CreateLocals((currFun).locals[0]));
                    AnonFun20_1:
                        ;
                    (CheckIfLeader).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 1);
                        return;
                    }

                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun20_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun20_Class AnonFun20 = new AnonFun20_Class();
            public class AnonFun21_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun21_StackFrame : PrtFunStackFrame
                {
                    public AnonFun21_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun21_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun21_1;
                    }

                    (parent).PrtPushFunStackFrame(UpdateAcceptors, (UpdateAcceptors).CreateLocals((currFun).locals[0]));
                    AnonFun21_1:
                        ;
                    (UpdateAcceptors).Execute(application, parent);
                    if (((parent).continuation).reason == PrtContinuationReason.Return)
                    {
                    }
                    else
                    {
                        (parent).PrtPushFunStackFrame((currFun).fun, (currFun).locals, 1);
                        return;
                    }

                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun21_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun21_Class AnonFun21 = new AnonFun21_Class();
            public class AnonFun22_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun22_StackFrame : PrtFunStackFrame
                {
                    public AnonFun22_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun22_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun22_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun22_Class AnonFun22 = new AnonFun22_Class();
            public class AnonFun23_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun23_StackFrame : PrtFunStackFrame
                {
                    public AnonFun23_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun23_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun23_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun23_Class AnonFun23 = new AnonFun23_Class();
            public class AnonFun24_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun24_StackFrame : PrtFunStackFrame
                {
                    public AnonFun24_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun24_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun24_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun24_Class AnonFun24 = new AnonFun24_Class();
            public class AnonFun25_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun25_StackFrame : PrtFunStackFrame
                {
                    public AnonFun25_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun25_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun25_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun25_Class AnonFun25 = new AnonFun25_Class();
            public class AnonFun26_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun26_StackFrame : PrtFunStackFrame
                {
                    public AnonFun26_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun26_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun26_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun26_Class AnonFun26 = new AnonFun26_Class();
            public class AnonFun27_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun27_StackFrame : PrtFunStackFrame
                {
                    public AnonFun27_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun27_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    PaxosNode parent = (PaxosNode)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun27_1;
                    }

                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((((PrtTupleValue)((parent).nextProposal)).fieldValues[0]).Clone())).nt <= ((PrtIntValue)((((PrtTupleValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).fieldValues[0]).Clone())).nt))).bl)
                        goto AnonFun27_if_0_else;
                    (parent).maxRound = ((((PrtTupleValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).fieldValues[0]).Clone()).Clone();
                    goto AnonFun27_if_0_end;
                    AnonFun27_if_0_else:
                        ;
                    AnonFun27_if_0_end:
                        ;
                    (((PrtMachineValue)((parent).timer)).mach).PrtEnqueueEvent((PrtEventValue)(Events.cancelTimer), Events.@null, parent, (PrtMachineValue)((parent).timer));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun27_1:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun27_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun27_Class AnonFun27 = new AnonFun27_Class();
            public class PaxosNode_ProposeValuePhase1_Class : PrtState
            {
                public PaxosNode_ProposeValuePhase1_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static PaxosNode_ProposeValuePhase1_Class PaxosNode_ProposeValuePhase1;
            public class PaxosNode_PerformOperation_Class : PrtState
            {
                public PaxosNode_PerformOperation_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static PaxosNode_PerformOperation_Class PaxosNode_PerformOperation;
            public class PaxosNode_Init_Class : PrtState
            {
                public PaxosNode_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static PaxosNode_Init_Class PaxosNode_Init;
            public class PaxosNode_RunLearner_Class : PrtState
            {
                public PaxosNode_RunLearner_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static PaxosNode_RunLearner_Class PaxosNode_RunLearner;
            public class PaxosNode_ProposeValuePhase2_Class : PrtState
            {
                public PaxosNode_ProposeValuePhase2_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static PaxosNode_ProposeValuePhase2_Class PaxosNode_ProposeValuePhase2;
            static PaxosNode()
            {
                PaxosNode_ProposeValuePhase1 = new PaxosNode_ProposeValuePhase1_Class("PaxosNode_ProposeValuePhase1", AnonFun1, AnonFun2, false, StateTemperature.Warm);
                PaxosNode_PerformOperation = new PaxosNode_PerformOperation_Class("PaxosNode_PerformOperation", AnonFun5, AnonFun6, false, StateTemperature.Warm);
                PaxosNode_Init = new PaxosNode_Init_Class("PaxosNode_Init", AnonFun7, AnonFun8, false, StateTemperature.Warm);
                PaxosNode_RunLearner = new PaxosNode_RunLearner_Class("PaxosNode_RunLearner", AnonFun10, AnonFun11, false, StateTemperature.Warm);
                PaxosNode_ProposeValuePhase2 = new PaxosNode_ProposeValuePhase2_Class("PaxosNode_ProposeValuePhase2", AnonFun12, AnonFun13, false, StateTemperature.Warm);
                PaxosNode_ProposeValuePhase1.dos.Add(Events.agree, AnonFun15);
                PaxosNode_ProposeValuePhase1.dos.Add(Events.accepted, PrtFun.IgnoreFun);
                PrtTransition transition_1 = new PrtTransition(AnonFun3, PaxosNode_ProposeValuePhase2, false);
                PaxosNode_ProposeValuePhase1.transitions.Add(Events.success, transition_1);
                PrtTransition transition_2 = new PrtTransition(AnonFun4, PaxosNode_ProposeValuePhase1, false);
                PaxosNode_ProposeValuePhase1.transitions.Add(Events.reject, transition_2);
                PrtTransition transition_3 = new PrtTransition(AnonFun0, PaxosNode_ProposeValuePhase1, false);
                PaxosNode_ProposeValuePhase1.transitions.Add(Events.timeout, transition_3);
                PaxosNode_PerformOperation.dos.Add(Events.newLeader, AnonFun16);
                PaxosNode_PerformOperation.dos.Add(Events.Ping, AnonFun17);
                PaxosNode_PerformOperation.dos.Add(Events.accept, AnonFun18);
                PaxosNode_PerformOperation.dos.Add(Events.prepare, AnonFun19);
                PaxosNode_PerformOperation.dos.Add(Events.update, AnonFun20);
                PaxosNode_PerformOperation.dos.Add(Events.timeout, PrtFun.IgnoreFun);
                PaxosNode_PerformOperation.dos.Add(Events.accepted, PrtFun.IgnoreFun);
                PaxosNode_PerformOperation.dos.Add(Events.agree, PrtFun.IgnoreFun);
                PrtTransition transition_4 = new PrtTransition(PrtFun.IgnoreFun, PaxosNode_RunLearner, true);
                PaxosNode_PerformOperation.transitions.Add(Events.chosen, transition_4);
                PrtTransition transition_5 = new PrtTransition(PrtFun.IgnoreFun, PaxosNode_ProposeValuePhase1, true);
                PaxosNode_PerformOperation.transitions.Add(Events.goPropose, transition_5);
                PaxosNode_Init.dos.Add(Events.allNodes, AnonFun21);
                PaxosNode_Init.deferredSet.Add(Events.Ping);
                PrtTransition transition_6 = new PrtTransition(AnonFun9, PaxosNode_PerformOperation, false);
                PaxosNode_Init.transitions.Add(Events.local, transition_6);
                PaxosNode_RunLearner.dos.Add(Events.accepted, PrtFun.IgnoreFun);
                PaxosNode_RunLearner.dos.Add(Events.agree, PrtFun.IgnoreFun);
                PaxosNode_RunLearner.dos.Add(Events.accept, PrtFun.IgnoreFun);
                PaxosNode_RunLearner.dos.Add(Events.reject, PrtFun.IgnoreFun);
                PaxosNode_RunLearner.dos.Add(Events.prepare, PrtFun.IgnoreFun);
                PaxosNode_RunLearner.dos.Add(Events.timeout, PrtFun.IgnoreFun);
                PaxosNode_RunLearner.deferredSet.Add(Events.newLeader);
                PaxosNode_ProposeValuePhase2.dos.Add(Events.accepted, AnonFun14);
                PaxosNode_ProposeValuePhase2.dos.Add(Events.agree, PrtFun.IgnoreFun);
                PrtTransition transition_7 = new PrtTransition(AnonFun26, PaxosNode_ProposeValuePhase1, false);
                PaxosNode_ProposeValuePhase2.transitions.Add(Events.timeout, transition_7);
                PrtTransition transition_8 = new PrtTransition(AnonFun27, PaxosNode_ProposeValuePhase1, false);
                PaxosNode_ProposeValuePhase2.transitions.Add(Events.reject, transition_8);
            }
        }

        public class Client : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return Client_Init;
                }
            }

            public PrtValue servers
            {
                get
                {
                    return fields[0];
                }

                set
                {
                    fields[0] = value;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new Client();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "Client";
                }
            }

            public Client(): base ()
            {
            }

            public Client(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_9_134078708));
            }

            public class ignore_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class ignore_StackFrame : PrtFunStackFrame
                {
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Client parent = (Client)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new ignore_StackFrame(this, locals, retLoc);
                }
            }

            public static ignore_Class ignore = new ignore_Class();
            public class AnonFun0_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun0_StackFrame : PrtFunStackFrame
                {
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Client parent = (Client)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun0_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun0_Class AnonFun0 = new AnonFun0_Class();
            public class AnonFun1_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun1_StackFrame : PrtFunStackFrame
                {
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Client parent = (Client)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun1_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun1_Class AnonFun1 = new AnonFun1_Class();
            public class AnonFun2_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun2_StackFrame : PrtFunStackFrame
                {
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Client parent = (Client)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun2_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
            public class AnonFun3_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun3_StackFrame : PrtFunStackFrame
                {
                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_9
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Client parent = (Client)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun3_1;
                        case 2:
                            goto AnonFun3_2;
                        case 3:
                            goto AnonFun3_3;
                    }

                    (application).Announce((PrtEventValue)(Events.announce_client_sent), new PrtIntValue(2), parent);
                    (parent).PrtFunContNondet(this, (currFun).locals, 3);
                    return;
                    AnonFun3_3:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).continuation).ReturnAndResetNondet()))).bl)
                        goto AnonFun3_if_0_else;
                    (((PrtMachineValue)((((PrtSeqValue)((parent).servers)).Lookup(new PrtIntValue(0))).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.update), new PrtNamedTupleValue(Types.type_14_134078708, new PrtIntValue(0), new PrtIntValue(2)), parent, (PrtMachineValue)((((PrtSeqValue)((parent).servers)).Lookup(new PrtIntValue(0))).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun3_1:
                        ;
                    goto AnonFun3_if_0_end;
                    AnonFun3_if_0_else:
                        ;
                    (((PrtMachineValue)((((PrtSeqValue)((parent).servers)).Lookup(new PrtIntValue(((PrtIntValue)(new PrtIntValue(((parent).servers).Size()))).nt - ((PrtIntValue)(new PrtIntValue(1))).nt))).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.update), new PrtNamedTupleValue(Types.type_14_134078708, new PrtIntValue(0), new PrtIntValue(2)), parent, (PrtMachineValue)((((PrtSeqValue)((parent).servers)).Lookup(new PrtIntValue(((PrtIntValue)(new PrtIntValue(((parent).servers).Size()))).nt - ((PrtIntValue)(new PrtIntValue(1))).nt))).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 2);
                    return;
                    AnonFun3_2:
                        ;
                    AnonFun3_if_0_end:
                        ;
                    if (!!(Events.response).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (571, 4): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine Client-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.response)).evt).name);
                    (parent).currentTrigger = Events.response;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun3_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun3_Class AnonFun3 = new AnonFun3_Class();
            public class AnonFun4_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun4_StackFrame : PrtFunStackFrame
                {
                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Client parent = (Client)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun4_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun4_Class AnonFun4 = new AnonFun4_Class();
            public class AnonFun5_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun5_StackFrame : PrtFunStackFrame
                {
                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Client parent = (Client)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun5_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun5_Class AnonFun5 = new AnonFun5_Class();
            public class AnonFun6_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun6_StackFrame : PrtFunStackFrame
                {
                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_8
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Client parent = (Client)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun6_1;
                        case 2:
                            goto AnonFun6_2;
                        case 3:
                            goto AnonFun6_3;
                    }

                    (application).Announce((PrtEventValue)(Events.announce_client_sent), new PrtIntValue(1), parent);
                    (parent).PrtFunContNondet(this, (currFun).locals, 3);
                    return;
                    AnonFun6_3:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).continuation).ReturnAndResetNondet()))).bl)
                        goto AnonFun6_if_0_else;
                    (((PrtMachineValue)((((PrtSeqValue)((parent).servers)).Lookup(new PrtIntValue(0))).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.update), new PrtNamedTupleValue(Types.type_14_134078708, new PrtIntValue(0), new PrtIntValue(1)), parent, (PrtMachineValue)((((PrtSeqValue)((parent).servers)).Lookup(new PrtIntValue(0))).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun6_1:
                        ;
                    goto AnonFun6_if_0_end;
                    AnonFun6_if_0_else:
                        ;
                    (((PrtMachineValue)((((PrtSeqValue)((parent).servers)).Lookup(new PrtIntValue(((PrtIntValue)(new PrtIntValue(((parent).servers).Size()))).nt - ((PrtIntValue)(new PrtIntValue(1))).nt))).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.update), new PrtNamedTupleValue(Types.type_14_134078708, new PrtIntValue(0), new PrtIntValue(1)), parent, (PrtMachineValue)((((PrtSeqValue)((parent).servers)).Lookup(new PrtIntValue(((PrtIntValue)(new PrtIntValue(((parent).servers).Size()))).nt - ((PrtIntValue)(new PrtIntValue(1))).nt))).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 2);
                    return;
                    AnonFun6_2:
                        ;
                    AnonFun6_if_0_end:
                        ;
                    if (!!(Events.response).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (557, 4): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine Client-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.response)).evt).name);
                    (parent).currentTrigger = Events.response;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun6_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun6_Class AnonFun6 = new AnonFun6_Class();
            public class AnonFun7_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun7_StackFrame : PrtFunStackFrame
                {
                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Client parent = (Client)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun7_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun7_Class AnonFun7 = new AnonFun7_Class();
            public class AnonFun8_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun8_StackFrame : PrtFunStackFrame
                {
                    public AnonFun8_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun8_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Client parent = (Client)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun8_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun8_Class AnonFun8 = new AnonFun8_Class();
            public class AnonFun9_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun9_StackFrame : PrtFunStackFrame
                {
                    public AnonFun9_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun9_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Client parent = (Client)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    (parent).servers = ((currFun).locals[0]).Clone();
                    if (!!(Events.local).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (543, 4): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine Client-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.local)).evt).name);
                    (parent).currentTrigger = Events.local;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun9_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun9_Class AnonFun9 = new AnonFun9_Class();
            public class AnonFun10_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun10_StackFrame : PrtFunStackFrame
                {
                    public AnonFun10_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun10_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Client parent = (Client)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun10_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun10_Class AnonFun10 = new AnonFun10_Class();
            public class AnonFun11_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun11_StackFrame : PrtFunStackFrame
                {
                    public AnonFun11_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun11_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Client parent = (Client)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun11_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun11_Class AnonFun11 = new AnonFun11_Class();
            public class AnonFun12_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun12_StackFrame : PrtFunStackFrame
                {
                    public AnonFun12_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun12_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Client parent = (Client)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun12_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun12_Class AnonFun12 = new AnonFun12_Class();
            public class AnonFun13_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun13_StackFrame : PrtFunStackFrame
                {
                    public AnonFun13_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun13_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Client parent = (Client)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun13_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun13_Class AnonFun13 = new AnonFun13_Class();
            public class Client_PumpRequestTwo_Class : PrtState
            {
                public Client_PumpRequestTwo_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Client_PumpRequestTwo_Class Client_PumpRequestTwo;
            public class Client_PumpRequestOne_Class : PrtState
            {
                public Client_PumpRequestOne_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Client_PumpRequestOne_Class Client_PumpRequestOne;
            public class Client_Init_Class : PrtState
            {
                public Client_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Client_Init_Class Client_Init;
            public class Client_Done_Class : PrtState
            {
                public Client_Done_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Client_Done_Class Client_Done;
            static Client()
            {
                Client_PumpRequestTwo = new Client_PumpRequestTwo_Class("Client_PumpRequestTwo", AnonFun3, AnonFun4, false, StateTemperature.Warm);
                Client_PumpRequestOne = new Client_PumpRequestOne_Class("Client_PumpRequestOne", AnonFun6, AnonFun7, false, StateTemperature.Warm);
                Client_Init = new Client_Init_Class("Client_Init", AnonFun9, AnonFun10, false, StateTemperature.Warm);
                Client_Done = new Client_Done_Class("Client_Done", AnonFun12, AnonFun13, false, StateTemperature.Warm);
                PrtTransition transition_1 = new PrtTransition(AnonFun5, Client_Done, false);
                Client_PumpRequestTwo.transitions.Add(Events.response, transition_1);
                PrtTransition transition_2 = new PrtTransition(AnonFun8, Client_PumpRequestTwo, false);
                Client_PumpRequestOne.transitions.Add(Events.response, transition_2);
                PrtTransition transition_3 = new PrtTransition(AnonFun11, Client_PumpRequestOne, false);
                Client_Init.transitions.Add(Events.local, transition_3);
            }
        }

        public class Main : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return Main_Init;
                }
            }

            public PrtValue iter
            {
                get
                {
                    return fields[0];
                }

                set
                {
                    fields[0] = value;
                }
            }

            public PrtValue temp
            {
                get
                {
                    return fields[1];
                }

                set
                {
                    fields[1] = value;
                }
            }

            public PrtValue paxosnodes
            {
                get
                {
                    return fields[2];
                }

                set
                {
                    fields[2] = value;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new Main();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "Main";
                }
            }

            public Main(): base ()
            {
            }

            public Main(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_1_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_9_134078708));
            }

            public class ignore_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class ignore_StackFrame : PrtFunStackFrame
                {
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Main parent = (Main)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new ignore_StackFrame(this, locals, retLoc);
                }
            }

            public static ignore_Class ignore = new ignore_Class();
            public class AnonFun0_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun0_StackFrame : PrtFunStackFrame
                {
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Main parent = (Main)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun0_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun0_Class AnonFun0 = new AnonFun0_Class();
            public class AnonFun1_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun1_StackFrame : PrtFunStackFrame
                {
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_7
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Main parent = (Main)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun1_1;
                        case 2:
                            goto AnonFun1_2;
                        case 3:
                            goto AnonFun1_3;
                        case 4:
                            goto AnonFun1_4;
                        case 5:
                            goto AnonFun1_5;
                    }

                    (parent).temp = (application).CreateInterfaceOrMachine((parent).renamedName, "PaxosNode", new PrtNamedTupleValue(Types.type_17_134078708, new PrtIntValue(3)));
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 1);
                    return;
                    AnonFun1_1:
                        ;
                    ((PrtSeqValue)((parent).paxosnodes)).Insert(((PrtTupleValue)(new PrtTupleValue(new PrtIntValue(0), (parent).temp))).fieldValues[0], ((PrtTupleValue)(new PrtTupleValue(new PrtIntValue(0), (parent).temp))).fieldValues[1]);
                    (parent).temp = (application).CreateInterfaceOrMachine((parent).renamedName, "PaxosNode", new PrtNamedTupleValue(Types.type_17_134078708, new PrtIntValue(2)));
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 2);
                    return;
                    AnonFun1_2:
                        ;
                    ((PrtSeqValue)((parent).paxosnodes)).Insert(((PrtTupleValue)(new PrtTupleValue(new PrtIntValue(0), (parent).temp))).fieldValues[0], ((PrtTupleValue)(new PrtTupleValue(new PrtIntValue(0), (parent).temp))).fieldValues[1]);
                    (parent).temp = (application).CreateInterfaceOrMachine((parent).renamedName, "PaxosNode", new PrtNamedTupleValue(Types.type_17_134078708, new PrtIntValue(1)));
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 3);
                    return;
                    AnonFun1_3:
                        ;
                    ((PrtSeqValue)((parent).paxosnodes)).Insert(((PrtTupleValue)(new PrtTupleValue(new PrtIntValue(0), (parent).temp))).fieldValues[0], ((PrtTupleValue)(new PrtTupleValue(new PrtIntValue(0), (parent).temp))).fieldValues[1]);
                    (parent).iter = (new PrtIntValue(0)).Clone();
                    AnonFun1_loop_start_0:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((parent).iter)).nt < ((PrtIntValue)(new PrtIntValue(((parent).paxosnodes).Size()))).nt))).bl)
                        goto AnonFun1_loop_end_0;
                    (((PrtMachineValue)((((PrtSeqValue)((parent).paxosnodes)).Lookup((parent).iter)).Clone())).mach).PrtEnqueueEvent((PrtEventValue)(Events.allNodes), new PrtNamedTupleValue(Types.type_13_134078708, (parent).paxosnodes), parent, (PrtMachineValue)((((PrtSeqValue)((parent).paxosnodes)).Lookup((parent).iter)).Clone()));
                    (parent).PrtFunContSend(this, (currFun).locals, 4);
                    return;
                    AnonFun1_4:
                        ;
                    (parent).iter = (new PrtIntValue(((PrtIntValue)((parent).iter)).nt + ((PrtIntValue)(new PrtIntValue(1))).nt)).Clone();
                    goto AnonFun1_loop_start_0;
                    AnonFun1_loop_end_0:
                        ;
                    (application).CreateInterfaceOrMachine((parent).renamedName, "Client", (parent).paxosnodes);
                    (parent).PrtFunContNewMachine(this, (currFun).locals, 5);
                    return;
                    AnonFun1_5:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun1_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun1_Class AnonFun1 = new AnonFun1_Class();
            public class AnonFun2_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun2_StackFrame : PrtFunStackFrame
                {
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Main parent = (Main)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun2_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
            public class Main_Init_Class : PrtState
            {
                public Main_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Main_Init_Class Main_Init;
            static Main()
            {
                Main_Init = new Main_Init_Class("Main_Init", AnonFun1, AnonFun2, false, StateTemperature.Warm);
            }
        }

        public class Timer : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return Timer_Init;
                }
            }

            public PrtValue timeoutvalue
            {
                get
                {
                    return fields[0];
                }

                set
                {
                    fields[0] = value;
                }
            }

            public PrtValue target
            {
                get
                {
                    return fields[1];
                }

                set
                {
                    fields[1] = value;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new Timer();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "Timer";
                }
            }

            public Timer(): base ()
            {
            }

            public Timer(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_1_134078708));
            }

            public class ignore_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class ignore_StackFrame : PrtFunStackFrame
                {
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new ignore_StackFrame(this, locals, retLoc);
                }
            }

            public static ignore_Class ignore = new ignore_Class();
            public class AnonFun0_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun0_StackFrame : PrtFunStackFrame
                {
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun0_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun0_Class AnonFun0 = new AnonFun0_Class();
            public class AnonFun1_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun1_StackFrame : PrtFunStackFrame
                {
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun1_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun1_Class AnonFun1 = new AnonFun1_Class();
            public class AnonFun2_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun2_StackFrame : PrtFunStackFrame
                {
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_6
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun2_1;
                    }

                    (parent).PrtFunContNondet(this, (currFun).locals, 1);
                    return;
                    AnonFun2_1:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((parent).continuation).ReturnAndResetNondet()))).bl)
                        goto AnonFun2_if_0_else;
                    if (!!(Events.local).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (503, 5): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine Timer-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.local)).evt).name);
                    (parent).currentTrigger = Events.local;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    goto AnonFun2_if_0_end;
                    AnonFun2_if_0_else:
                        ;
                    AnonFun2_if_0_end:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun2_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
            public class AnonFun3_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun3_StackFrame : PrtFunStackFrame
                {
                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun3_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun3_Class AnonFun3 = new AnonFun3_Class();
            public class AnonFun4_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun4_StackFrame : PrtFunStackFrame
                {
                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun4_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun4_Class AnonFun4 = new AnonFun4_Class();
            public class AnonFun5_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun5_StackFrame : PrtFunStackFrame
                {
                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun5_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun5_Class AnonFun5 = new AnonFun5_Class();
            public class AnonFun6_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun6_StackFrame : PrtFunStackFrame
                {
                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun6_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun6_Class AnonFun6 = new AnonFun6_Class();
            public class AnonFun7_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun7_StackFrame : PrtFunStackFrame
                {
                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun7_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun7_Class AnonFun7 = new AnonFun7_Class();
            public class AnonFun8_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun8_StackFrame : PrtFunStackFrame
                {
                    public AnonFun8_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun8_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun8_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun8_Class AnonFun8 = new AnonFun8_Class();
            public class AnonFun9_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun9_StackFrame : PrtFunStackFrame
                {
                    public AnonFun9_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun9_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    (parent).target = ((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone()).Clone();
                    (parent).timeoutvalue = ((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone()).Clone();
                    if (!!(Events.local).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (488, 4): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine Timer-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.local)).evt).name);
                    (parent).currentTrigger = Events.local;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun9_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun9_Class AnonFun9 = new AnonFun9_Class();
            public class AnonFun10_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun10_StackFrame : PrtFunStackFrame
                {
                    public AnonFun10_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun10_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun10_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun10_Class AnonFun10 = new AnonFun10_Class();
            public class AnonFun11_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun11_StackFrame : PrtFunStackFrame
                {
                    public AnonFun11_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun11_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    Timer parent = (Timer)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun11_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun11_Class AnonFun11 = new AnonFun11_Class();
            public class Timer_TimerStarted_Class : PrtState
            {
                public Timer_TimerStarted_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Timer_TimerStarted_Class Timer_TimerStarted;
            public class Timer_Loop_Class : PrtState
            {
                public Timer_Loop_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Timer_Loop_Class Timer_Loop;
            public class Timer_Init_Class : PrtState
            {
                public Timer_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static Timer_Init_Class Timer_Init;
            static Timer()
            {
                Timer_TimerStarted = new Timer_TimerStarted_Class("Timer_TimerStarted", AnonFun2, AnonFun3, false, StateTemperature.Warm);
                Timer_Loop = new Timer_Loop_Class("Timer_Loop", AnonFun4, AnonFun5, false, StateTemperature.Warm);
                Timer_Init = new Timer_Init_Class("Timer_Init", AnonFun9, AnonFun10, false, StateTemperature.Warm);
                Timer_TimerStarted.dos.Add(Events.startTimer, PrtFun.IgnoreFun);
                PrtTransition transition_1 = new PrtTransition(AnonFun6, Timer_Loop, false);
                Timer_TimerStarted.transitions.Add(Events.cancelTimer, transition_1);
                PrtTransition transition_2 = new PrtTransition(AnonFun7, Timer_Loop, false);
                Timer_TimerStarted.transitions.Add(Events.local, transition_2);
                Timer_Loop.dos.Add(Events.cancelTimer, PrtFun.IgnoreFun);
                PrtTransition transition_3 = new PrtTransition(AnonFun8, Timer_TimerStarted, false);
                Timer_Loop.transitions.Add(Events.startTimer, transition_3);
                PrtTransition transition_4 = new PrtTransition(AnonFun11, Timer_Loop, false);
                Timer_Init.transitions.Add(Events.local, transition_4);
            }
        }

        public class LeaderElection : PrtImplMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return LeaderElection_Init;
                }
            }

            public PrtValue iter
            {
                get
                {
                    return fields[0];
                }

                set
                {
                    fields[0] = value;
                }
            }

            public PrtValue myRank
            {
                get
                {
                    return fields[1];
                }

                set
                {
                    fields[1] = value;
                }
            }

            public PrtValue currentLeader
            {
                get
                {
                    return fields[2];
                }

                set
                {
                    fields[2] = value;
                }
            }

            public PrtValue parentServer
            {
                get
                {
                    return fields[3];
                }

                set
                {
                    fields[3] = value;
                }
            }

            public PrtValue servers
            {
                get
                {
                    return fields[4];
                }

                set
                {
                    fields[4] = value;
                }
            }

            public override PrtImplMachine MakeSkeleton()
            {
                return new LeaderElection();
            }

            public override int NextInstanceNumber(StateImpl app)
            {
                return app.NextMachineInstanceNumber(this.GetType());
            }

            public override string Name
            {
                get
                {
                    return "LeaderElection";
                }
            }

            public LeaderElection(): base ()
            {
            }

            public LeaderElection(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_2_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_16_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_1_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_9_134078708));
            }

            public class ignore_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class ignore_StackFrame : PrtFunStackFrame
                {
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    LeaderElection parent = (LeaderElection)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new ignore_StackFrame(this, locals, retLoc);
                }
            }

            public static ignore_Class ignore = new ignore_Class();
            public class GetNewLeader_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class GetNewLeader_StackFrame : PrtFunStackFrame
                {
                    public GetNewLeader_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public GetNewLeader_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    LeaderElection parent = (LeaderElection)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    (parent).PrtFunContReturnVal(new PrtNamedTupleValue(Types.type_16_134078708, new PrtIntValue(1), (((PrtSeqValue)((parent).servers)).Lookup(new PrtIntValue(0))).Clone()), (currFun).locals);
                    return;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new GetNewLeader_StackFrame(this, locals, retLoc);
                }
            }

            public static GetNewLeader_Class GetNewLeader = new GetNewLeader_Class();
            public class AnonFun0_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun0_StackFrame : PrtFunStackFrame
                {
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    LeaderElection parent = (LeaderElection)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun0_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun0_Class AnonFun0 = new AnonFun0_Class();
            public class AnonFun1_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun1_StackFrame : PrtFunStackFrame
                {
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    LeaderElection parent = (LeaderElection)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun1_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun1_Class AnonFun1 = new AnonFun1_Class();
            public class AnonFun2_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun2_StackFrame : PrtFunStackFrame
                {
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    LeaderElection parent = (LeaderElection)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    (parent).servers = ((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone()).Clone();
                    (parent).parentServer = ((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone()).Clone();
                    (parent).myRank = ((((PrtTupleValue)((currFun).locals[0])).fieldValues[2]).Clone()).Clone();
                    (parent).currentLeader = (new PrtNamedTupleValue(Types.type_24_134078708, (parent).myRank, parent.self)).Clone();
                    if (!!(Events.local).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (448, 4): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine LeaderElection-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.local)).evt).name);
                    (parent).currentTrigger = Events.local;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun2_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
            public class AnonFun3_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun3_StackFrame : PrtFunStackFrame
                {
                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    LeaderElection parent = (LeaderElection)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun3_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun3_Class AnonFun3 = new AnonFun3_Class();
            public class AnonFun4_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun4_StackFrame : PrtFunStackFrame
                {
                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    LeaderElection parent = (LeaderElection)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun4_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun4_Class AnonFun4 = new AnonFun4_Class();
            public class AnonFun5_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun5_StackFrame : PrtFunStackFrame
                {
                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_5
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    LeaderElection parent = (LeaderElection)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    switch ((currFun).returnToLocation)
                    {
                        case 1:
                            goto AnonFun5_1;
                    }

                    (parent).currentLeader = ((GetNewLeader).ExecuteToCompletion(application, parent)).Clone();
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((((PrtTupleValue)((parent).currentLeader)).fieldValues[0]).Clone())).nt <= ((PrtIntValue)((parent).myRank)).nt))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (457, 4): Assert failed");
                    (((PrtMachineValue)((parent).parentServer)).mach).PrtEnqueueEvent((PrtEventValue)(Events.newLeader), (parent).currentLeader, parent, (PrtMachineValue)((parent).parentServer));
                    (parent).PrtFunContSend(this, (currFun).locals, 1);
                    return;
                    AnonFun5_1:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun5_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun5_Class AnonFun5 = new AnonFun5_Class();
            public class AnonFun6_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun6_StackFrame : PrtFunStackFrame
                {
                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    LeaderElection parent = (LeaderElection)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun6_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun6_Class AnonFun6 = new AnonFun6_Class();
            public class LeaderElection_Init_Class : PrtState
            {
                public LeaderElection_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static LeaderElection_Init_Class LeaderElection_Init;
            public class LeaderElection_SendLeader_Class : PrtState
            {
                public LeaderElection_SendLeader_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static LeaderElection_SendLeader_Class LeaderElection_SendLeader;
            static LeaderElection()
            {
                LeaderElection_Init = new LeaderElection_Init_Class("LeaderElection_Init", AnonFun2, AnonFun3, false, StateTemperature.Warm);
                LeaderElection_SendLeader = new LeaderElection_SendLeader_Class("LeaderElection_SendLeader", AnonFun5, AnonFun6, false, StateTemperature.Warm);
                PrtTransition transition_1 = new PrtTransition(AnonFun4, LeaderElection_SendLeader, false);
                LeaderElection_Init.transitions.Add(Events.local, transition_1);
            }
        }

        public class ValmachineityCheck : PrtSpecMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return ValmachineityCheck_Init;
                }
            }

            public PrtValue ProposedSet
            {
                get
                {
                    return fields[0];
                }

                set
                {
                    fields[0] = value;
                }
            }

            public PrtValue clientSet
            {
                get
                {
                    return fields[1];
                }

                set
                {
                    fields[1] = value;
                }
            }

            public override PrtSpecMachine MakeSkeleton()
            {
                return new ValmachineityCheck();
            }

            public override string Name
            {
                get
                {
                    return "ValmachineityCheck";
                }
            }

            public ValmachineityCheck(): base ()
            {
            }

            public ValmachineityCheck(StateImpl app): base (app)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_15_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_15_134078708));
            }

            public class ignore_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class ignore_StackFrame : PrtFunStackFrame
                {
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    ValmachineityCheck parent = (ValmachineityCheck)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new ignore_StackFrame(this, locals, retLoc);
                }
            }

            public static ignore_Class ignore = new ignore_Class();
            public class AnonFun0_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun0_StackFrame : PrtFunStackFrame
                {
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    ValmachineityCheck parent = (ValmachineityCheck)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtMapValue)((parent).ProposedSet)).Contains((currFun).locals[0])))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (418, 52): Assert failed");
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun0_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun0_Class AnonFun0 = new AnonFun0_Class();
            public class AnonFun1_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun1_StackFrame : PrtFunStackFrame
                {
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    ValmachineityCheck parent = (ValmachineityCheck)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtMapValue)((parent).clientSet)).Contains((currFun).locals[0])))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (416, 50): Assert failed");
                    ((PrtMapValue)((parent).ProposedSet)).Update(PrtValue.PrtCastValue((currFun).locals[0], Types.type_2_134078708), (new PrtIntValue(0)).Clone());
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun1_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun1_Class AnonFun1 = new AnonFun1_Class();
            public class AnonFun2_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun2_StackFrame : PrtFunStackFrame
                {
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue payload
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    ValmachineityCheck parent = (ValmachineityCheck)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    ((PrtMapValue)((parent).clientSet)).Update((currFun).locals[0], (new PrtIntValue(0)).Clone());
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun2_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
            public class AnonFun3_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun3_StackFrame : PrtFunStackFrame
                {
                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    ValmachineityCheck parent = (ValmachineityCheck)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun3_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun3_Class AnonFun3 = new AnonFun3_Class();
            public class AnonFun4_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun4_StackFrame : PrtFunStackFrame
                {
                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    ValmachineityCheck parent = (ValmachineityCheck)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun4_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun4_Class AnonFun4 = new AnonFun4_Class();
            public class AnonFun5_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun5_StackFrame : PrtFunStackFrame
                {
                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    ValmachineityCheck parent = (ValmachineityCheck)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun5_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun5_Class AnonFun5 = new AnonFun5_Class();
            public class AnonFun6_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun6_StackFrame : PrtFunStackFrame
                {
                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_4
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    ValmachineityCheck parent = (ValmachineityCheck)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!!(Events.local).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (409, 4): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine ValmachineityCheck-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.local)).evt).name);
                    (parent).currentTrigger = Events.local;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun6_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun6_Class AnonFun6 = new AnonFun6_Class();
            public class AnonFun7_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun7_StackFrame : PrtFunStackFrame
                {
                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    ValmachineityCheck parent = (ValmachineityCheck)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun7_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun7_Class AnonFun7 = new AnonFun7_Class();
            public class AnonFun8_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun8_StackFrame : PrtFunStackFrame
                {
                    public AnonFun8_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun8_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    ValmachineityCheck parent = (ValmachineityCheck)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun8_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun8_Class AnonFun8 = new AnonFun8_Class();
            public class ValmachineityCheck_Wait_Class : PrtState
            {
                public ValmachineityCheck_Wait_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static ValmachineityCheck_Wait_Class ValmachineityCheck_Wait;
            public class ValmachineityCheck_Init_Class : PrtState
            {
                public ValmachineityCheck_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static ValmachineityCheck_Init_Class ValmachineityCheck_Init;
            static ValmachineityCheck()
            {
                ValmachineityCheck_Wait = new ValmachineityCheck_Wait_Class("ValmachineityCheck_Wait", AnonFun4, AnonFun5, false, StateTemperature.Warm);
                ValmachineityCheck_Init = new ValmachineityCheck_Init_Class("ValmachineityCheck_Init", AnonFun6, AnonFun7, false, StateTemperature.Warm);
                ValmachineityCheck_Wait.dos.Add(Events.announce_proposer_chosen, AnonFun0);
                ValmachineityCheck_Wait.dos.Add(Events.announce_proposer_sent, AnonFun1);
                ValmachineityCheck_Wait.dos.Add(Events.announce_client_sent, AnonFun2);
                PrtTransition transition_1 = new PrtTransition(AnonFun8, ValmachineityCheck_Wait, false);
                ValmachineityCheck_Init.transitions.Add(Events.local, transition_1);
            }
        }

        public class BasicPaxosInvariant_P2b : PrtSpecMachine
        {
            public override PrtState StartState
            {
                get
                {
                    return BasicPaxosInvariant_P2b_Init;
                }
            }

            public PrtValue receivedValue
            {
                get
                {
                    return fields[0];
                }

                set
                {
                    fields[0] = value;
                }
            }

            public PrtValue returnVal
            {
                get
                {
                    return fields[1];
                }

                set
                {
                    fields[1] = value;
                }
            }

            public PrtValue lastValueChosen
            {
                get
                {
                    return fields[2];
                }

                set
                {
                    fields[2] = value;
                }
            }

            public override PrtSpecMachine MakeSkeleton()
            {
                return new BasicPaxosInvariant_P2b();
            }

            public override string Name
            {
                get
                {
                    return "BasicPaxosInvariant_P2b";
                }
            }

            public BasicPaxosInvariant_P2b(): base ()
            {
            }

            public BasicPaxosInvariant_P2b(StateImpl app): base (app)
            {
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_0_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_27_134078708));
                (fields).Add(PrtValue.PrtMkDefaultValue(Types.type_5_134078708));
            }

            public class ignore_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class ignore_StackFrame : PrtFunStackFrame
                {
                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public ignore_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    BasicPaxosInvariant_P2b parent = (BasicPaxosInvariant_P2b)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new ignore_StackFrame(this, locals, retLoc);
                }
            }

            public static ignore_Class ignore = new ignore_Class();
            public class lessThan_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return false;
                    }
                }

                internal class lessThan_StackFrame : PrtFunStackFrame
                {
                    public lessThan_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public lessThan_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue p1
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }

                    public PrtValue p2
                    {
                        get
                        {
                            return locals[1];
                        }

                        set
                        {
                            locals[1] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    BasicPaxosInvariant_P2b parent = (BasicPaxosInvariant_P2b)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone())).nt < ((PrtIntValue)((((PrtTupleValue)((currFun).locals[1])).fieldValues[0]).Clone())).nt))).bl)
                        goto lessThan_if_5_else;
                    (parent).PrtFunContReturnVal(new PrtBoolValue(true), (currFun).locals);
                    return;
                    goto lessThan_if_5_end;
                    lessThan_if_5_else:
                        ;
                    if (!((PrtBoolValue)(new PrtBoolValue(((((PrtTupleValue)((currFun).locals[0])).fieldValues[0]).Clone()).Equals((((PrtTupleValue)((currFun).locals[1])).fieldValues[0]).Clone())))).bl)
                        goto lessThan_if_4_else;
                    if (!((PrtBoolValue)(new PrtBoolValue(((PrtIntValue)((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).nt < ((PrtIntValue)((((PrtTupleValue)((currFun).locals[1])).fieldValues[1]).Clone())).nt))).bl)
                        goto lessThan_if_3_else;
                    (parent).PrtFunContReturnVal(new PrtBoolValue(true), (currFun).locals);
                    return;
                    goto lessThan_if_3_end;
                    lessThan_if_3_else:
                        ;
                    (parent).PrtFunContReturnVal(new PrtBoolValue(false), (currFun).locals);
                    return;
                    lessThan_if_3_end:
                        ;
                    goto lessThan_if_4_end;
                    lessThan_if_4_else:
                        ;
                    (parent).PrtFunContReturnVal(new PrtBoolValue(false), (currFun).locals);
                    return;
                    lessThan_if_4_end:
                        ;
                    lessThan_if_5_end:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new lessThan_StackFrame(this, locals, retLoc);
                }
            }

            public static lessThan_Class lessThan = new lessThan_Class();
            public class AnonFun0_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun0_StackFrame : PrtFunStackFrame
                {
                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun0_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    BasicPaxosInvariant_P2b parent = (BasicPaxosInvariant_P2b)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun0_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun0_Class AnonFun0 = new AnonFun0_Class();
            public class AnonFun1_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun1_StackFrame : PrtFunStackFrame
                {
                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun1_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    BasicPaxosInvariant_P2b parent = (BasicPaxosInvariant_P2b)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun1_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun1_Class AnonFun1 = new AnonFun1_Class();
            public class AnonFun2_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun2_StackFrame : PrtFunStackFrame
                {
                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun2_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_3
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    BasicPaxosInvariant_P2b parent = (BasicPaxosInvariant_P2b)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun2_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun2_Class AnonFun2 = new AnonFun2_Class();
            public class AnonFun3_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun3_StackFrame : PrtFunStackFrame
                {
                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun3_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    BasicPaxosInvariant_P2b parent = (BasicPaxosInvariant_P2b)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun3_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun3_Class AnonFun3 = new AnonFun3_Class();
            public class AnonFun4_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun4_StackFrame : PrtFunStackFrame
                {
                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun4_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    BasicPaxosInvariant_P2b parent = (BasicPaxosInvariant_P2b)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun4_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun4_Class AnonFun4 = new AnonFun4_Class();
            public class AnonFun5_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun5_StackFrame : PrtFunStackFrame
                {
                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun5_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    BasicPaxosInvariant_P2b parent = (BasicPaxosInvariant_P2b)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun5_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun5_Class AnonFun5 = new AnonFun5_Class();
            public class AnonFun6_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun6_StackFrame : PrtFunStackFrame
                {
                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun6_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue receivedValue
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    BasicPaxosInvariant_P2b parent = (BasicPaxosInvariant_P2b)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    (parent).returnVal = ((lessThan).ExecuteToCompletion(application, parent, (((PrtTupleValue)((((PrtMapValue)((parent).lastValueChosen)).Lookup((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).Clone())).fieldValues[0]).Clone(), (((PrtTupleValue)((currFun).locals[0])).fieldValues[2]).Clone())).Clone();
                    if (!((PrtBoolValue)((parent).returnVal)).bl)
                        goto AnonFun6_if_1_else;
                    if (!((PrtBoolValue)(new PrtBoolValue(((((PrtTupleValue)((((PrtMapValue)((parent).lastValueChosen)).Lookup((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).Clone())).fieldValues[1]).Clone()).Equals((((PrtTupleValue)((currFun).locals[0])).fieldValues[3]).Clone())))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (387, 5): Assert failed");
                    goto AnonFun6_if_1_end;
                    AnonFun6_if_1_else:
                        ;
                    AnonFun6_if_1_end:
                        ;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun6_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun6_Class AnonFun6 = new AnonFun6_Class();
            public class AnonFun7_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun7_StackFrame : PrtFunStackFrame
                {
                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun7_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue receivedValue
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    BasicPaxosInvariant_P2b parent = (BasicPaxosInvariant_P2b)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!((PrtBoolValue)(new PrtBoolValue(((((PrtTupleValue)((((PrtMapValue)((parent).lastValueChosen)).Lookup((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone())).Clone())).fieldValues[1]).Clone()).Equals((((PrtTupleValue)((currFun).locals[0])).fieldValues[3]).Clone())))).bl)
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (381, 4): Assert failed");
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun7_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun7_Class AnonFun7 = new AnonFun7_Class();
            public class AnonFun8_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun8_StackFrame : PrtFunStackFrame
                {
                    public AnonFun8_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun8_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue receivedValue
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    BasicPaxosInvariant_P2b parent = (BasicPaxosInvariant_P2b)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    ((PrtMapValue)((parent).lastValueChosen)).Update((((PrtTupleValue)((currFun).locals[0])).fieldValues[1]).Clone(), (new PrtNamedTupleValue(Types.type_6_134078708, (((PrtTupleValue)((currFun).locals[0])).fieldValues[2]).Clone(), (((PrtTupleValue)((currFun).locals[0])).fieldValues[3]).Clone())).Clone());
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun8_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun8_Class AnonFun8 = new AnonFun8_Class();
            public class AnonFun9_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun9_StackFrame : PrtFunStackFrame
                {
                    public AnonFun9_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun9_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_2
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    BasicPaxosInvariant_P2b parent = (BasicPaxosInvariant_P2b)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    if (!!(Events.local).Equals(Events.@null))
                        throw new PrtAssertFailureException("C:\\\\Workspace\\\\P\\\\Src\\\\PTester\\\\Regressions\\\\MultiPaxos\\\\Multi_Paxos_4.p (343, 4): Raised event must be non-null");
                    (application).Trace("<RaiseLog> Machine BasicPaxosInvariant_P2b-{0} raised Event {1}", (parent).instanceNumber, (((PrtEventValue)(Events.local)).evt).name);
                    (parent).currentTrigger = Events.local;
                    (parent).currentPayload = Events.@null;
                    (parent).PrtFunContRaise();
                    return;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun9_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun9_Class AnonFun9 = new AnonFun9_Class();
            public class AnonFun10_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun10_StackFrame : PrtFunStackFrame
                {
                    public AnonFun10_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun10_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    BasicPaxosInvariant_P2b parent = (BasicPaxosInvariant_P2b)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun10_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun10_Class AnonFun10 = new AnonFun10_Class();
            public class AnonFun11_Class : PrtFun
            {
                public override bool IsAnonFun
                {
                    get
                    {
                        return true;
                    }
                }

                internal class AnonFun11_StackFrame : PrtFunStackFrame
                {
                    public AnonFun11_StackFrame(PrtFun fun, List<PrtValue> locs): base (fun, locs)
                    {
                    }

                    public AnonFun11_StackFrame(PrtFun fun, List<PrtValue> locs, int retLocation): base (fun, locs, retLocation)
                    {
                    }

                    public override PrtFunStackFrame Clone()
                    {
                        return this.Clone();
                    }

                    public PrtValue _payload_skip
                    {
                        get
                        {
                            return locals[0];
                        }

                        set
                        {
                            locals[0] = value;
                        }
                    }
                }

                public override void Execute(StateImpl application, PrtMachine _parent)
                {
                    BasicPaxosInvariant_P2b parent = (BasicPaxosInvariant_P2b)(_parent);
                    PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                    PrtValue swap;
                    parent.PrtFunContReturn(null);
                }

                public override List<PrtValue> CreateLocals(params PrtValue[] args)
                {
                    var locals = new List<PrtValue>();
                    foreach (var item in args)
                    {
                        locals.Add(item.Clone());
                    }

                    return locals;
                }

                public override PrtFunStackFrame CreateFunStackFrame(List<PrtValue> locals, int retLoc)
                {
                    return new AnonFun11_StackFrame(this, locals, retLoc);
                }
            }

            public static AnonFun11_Class AnonFun11 = new AnonFun11_Class();
            public class BasicPaxosInvariant_P2b_WaitForValueChosen_Class : PrtState
            {
                public BasicPaxosInvariant_P2b_WaitForValueChosen_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static BasicPaxosInvariant_P2b_WaitForValueChosen_Class BasicPaxosInvariant_P2b_WaitForValueChosen;
            public class BasicPaxosInvariant_P2b_CheckValueProposed_Class : PrtState
            {
                public BasicPaxosInvariant_P2b_CheckValueProposed_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static BasicPaxosInvariant_P2b_CheckValueProposed_Class BasicPaxosInvariant_P2b_CheckValueProposed;
            public class BasicPaxosInvariant_P2b_Init_Class : PrtState
            {
                public BasicPaxosInvariant_P2b_Init_Class(string name, PrtFun entryFun, PrtFun exitFun, bool hasNullTransition, StateTemperature temperature): base (name, entryFun, exitFun, hasNullTransition, temperature)
                {
                }
            }

            public static BasicPaxosInvariant_P2b_Init_Class BasicPaxosInvariant_P2b_Init;
            static BasicPaxosInvariant_P2b()
            {
                BasicPaxosInvariant_P2b_WaitForValueChosen = new BasicPaxosInvariant_P2b_WaitForValueChosen_Class("BasicPaxosInvariant_P2b_WaitForValueChosen", AnonFun2, AnonFun3, false, StateTemperature.Warm);
                BasicPaxosInvariant_P2b_CheckValueProposed = new BasicPaxosInvariant_P2b_CheckValueProposed_Class("BasicPaxosInvariant_P2b_CheckValueProposed", AnonFun4, AnonFun5, false, StateTemperature.Warm);
                BasicPaxosInvariant_P2b_Init = new BasicPaxosInvariant_P2b_Init_Class("BasicPaxosInvariant_P2b_Init", AnonFun9, AnonFun10, false, StateTemperature.Warm);
                BasicPaxosInvariant_P2b_WaitForValueChosen.dos.Add(Events.announce_valueProposed, PrtFun.IgnoreFun);
                PrtTransition transition_1 = new PrtTransition(AnonFun8, BasicPaxosInvariant_P2b_CheckValueProposed, false);
                BasicPaxosInvariant_P2b_WaitForValueChosen.transitions.Add(Events.announce_valueChosen, transition_1);
                PrtTransition transition_2 = new PrtTransition(AnonFun6, BasicPaxosInvariant_P2b_CheckValueProposed, false);
                BasicPaxosInvariant_P2b_CheckValueProposed.transitions.Add(Events.announce_valueProposed, transition_2);
                PrtTransition transition_3 = new PrtTransition(AnonFun7, BasicPaxosInvariant_P2b_CheckValueProposed, false);
                BasicPaxosInvariant_P2b_CheckValueProposed.transitions.Add(Events.announce_valueChosen, transition_3);
                PrtTransition transition_4 = new PrtTransition(AnonFun11, BasicPaxosInvariant_P2b_WaitForValueChosen, false);
                BasicPaxosInvariant_P2b_Init.transitions.Add(Events.local, transition_4);
            }
        }
    }
}
