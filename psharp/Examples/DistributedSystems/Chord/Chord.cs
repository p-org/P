using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PSharp;

namespace Chord
{
    #region Events

    internal class eLocal : Event { }

    internal class eConfigure : Event
    {
        public eConfigure(Object payload)
            : base(payload)
        { }
    }

    internal class eJoin : Event
    {
        public eJoin(Object payload)
            : base(payload)
        { }
    }

    internal class eJoinAck : Event { }

    internal class eFail : Event { }

    internal class eStop : Event { }

    internal class eStabilize : Event { }

    internal class eNotifySuccessor : Event
    {
        public eNotifySuccessor(Object payload)
            : base(payload)
        { }
    }

    internal class eAskForKeys : Event
    {
        public eAskForKeys(Object payload)
            : base(payload)
        { }
    }

    internal class eAskForKeysAck : Event
    {
        public eAskForKeysAck(Object payload)
            : base(payload)
        { }
    }

    internal class eFindSuccessor : Event
    {
        public eFindSuccessor(Object payload)
            : base(payload)
        { }
    }

    internal class eFindSuccessorResp : Event
    {
        public eFindSuccessorResp(Object payload)
            : base(payload)
        { }
    }

    internal class eFindPredecessor : Event
    {
        public eFindPredecessor(Object payload)
            : base(payload)
        { }
    }

    internal class eFindPredecessorResp : Event
    {
        public eFindPredecessorResp(Object payload)
            : base(payload)
        { }
    }

    internal class eQueryId : Event
    {
        public eQueryId(Object payload)
            : base(payload)
        { }
    }

    internal class eQueryIdResp : Event
    {
        public eQueryIdResp(Object payload)
            : base(payload)
        { }
    }

    internal class eQueryJoin : Event
    {
        public eQueryJoin(Object payload)
            : base(payload)
        { }
    }

    internal class eNotifyClient : Event { }

    #endregion

    #region Machines

    [Main]
    internal class Cluster : Machine
    {
        private int M;
        private int NumOfId;
        private int QueryCounter;

        private List<int> Keys;
        private List<int> NodeIds;
        private List<Machine> Nodes;

        private Machine Client;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Cluster;

                //Console.WriteLine("[Cluster] Initializing ...\n");

                machine.M = ((Tuple<int, List<int>, List<int>>)this.Payload).Item1;
                machine.NodeIds = ((Tuple<int, List<int>, List<int>>)this.Payload).Item2;
                machine.Keys = ((Tuple<int, List<int>, List<int>>)this.Payload).Item3;

                machine.NumOfId = (int)Math.Pow(2, machine.M);
                machine.Nodes = new List<Machine>();
                machine.QueryCounter = 0;

                for (int idx = 0; idx < machine.NodeIds.Count; idx++)
                {
                    machine.Nodes.Add(Machine.Factory.CreateMachine<ChordNode>(new Tuple<Machine, int, int>(
                        machine, machine.NodeIds[idx], machine.M)));
                }

                var nodeKeys = new Dictionary<int, List<int>>();
                for (int i = machine.Keys.Count - 1; i >= 0; i--)
                {

                    bool assigned = false;
                    for (int j = 0; j < machine.NodeIds.Count; j++)
                    {
                        if (machine.Keys[i] <= machine.NodeIds[j])
                        {
                            if (nodeKeys.ContainsKey(machine.NodeIds[j]))
                            {
                                nodeKeys[machine.NodeIds[j]].Add(machine.Keys[i]);
                            }
                            else
                            {
                                nodeKeys.Add(machine.NodeIds[j], new List<int>());
                                nodeKeys[machine.NodeIds[j]].Add(machine.Keys[i]);
                            }

                            assigned = true;
                            break;
                        }
                    }

                    if (!assigned)
                    {
                        if (nodeKeys.ContainsKey(machine.NodeIds[0]))
                        {
                            nodeKeys[machine.NodeIds[0]].Add(machine.Keys[i]);
                        }
                        else
                        {
                            nodeKeys.Add(machine.NodeIds[0], new List<int>());
                            nodeKeys[machine.NodeIds[0]].Add(machine.Keys[i]);
                        }
                    }
                }

                for (int idx = 0; idx < machine.Nodes.Count; idx++)
                {
                    this.Send(machine.Nodes[idx], new eConfigure(new Tuple<List<int>, List<Machine>, List<int>>(
                        machine.NodeIds, machine.Nodes, nodeKeys[machine.NodeIds[idx]])));
                }

                machine.Client = Machine.Factory.CreateMachine<Client>(new Tuple<Machine, List<int>>(
                    machine, machine.Keys));

                this.Raise(new eLocal());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eFindSuccessor)
                };
            }
        }

        private class Querying : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Cluster;

                if (machine.QueryCounter < 10)
                {
                    //Console.WriteLine("[Cluster] Query {0} ...\n", machine.QueryCounter);

                    machine.CreateNewNode();

                    machine.QueryCounter++;
                }
                else
                {
                    //Console.WriteLine("[Cluster] Notifying client ...\n");
                    this.Send(machine.Client, new eNotifyClient());
                }

                this.Raise(new eLocal());
            }
        }

        private class Waiting : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Cluster;

                //Console.WriteLine("[Cluster] Waiting ...\n");

                if (machine.QueryCounter == 10)
                {
                    machine.TriggerStop();
                }
            }
        }

        private void CreateNewNode()
        {
            int newId = -1;
            Random random = new Random();
            while ((newId < 0 || this.NodeIds.Contains(newId)) &&
                this.NodeIds.Count < this.NumOfId)
            {
                newId = random.Next(this.NumOfId);
            }

            if (newId < 0)
            {
                this.TriggerStop();
                return;
            }
            
            var index = 0;
            for (int idx = 0; idx < this.NodeIds.Count; idx++)
            {
                if (this.NodeIds[idx] > index)
                {
                    index = idx;
                    break;
                }
            }

            //Console.WriteLine("[Cluster] Creating new node with Id {0} ...\n", newId);

            var newNode = Machine.Factory.CreateMachine<ChordNode>(
                new Tuple<Machine, int, int>(this, newId, this.M));
            this.NodeIds.Insert(index, newId);
            this.Nodes.Insert(index, newNode);

            this.Send(newNode, new eJoin(new Tuple<List<int>, List<Machine>>(this.NodeIds, this.Nodes)));
        }

        private void QueryStabilize()
        {
            foreach (var node in this.Nodes)
            {
                this.Send(node, new eStabilize());
            }

            this.Raise(new eLocal());
        }

        private void TriggerFailure()
        {
            //Console.WriteLine("[Cluster] Triggering a failure ...\n");

            int failId = -1;
            Random random = new Random();
            while ((failId < 0 || !this.NodeIds.Contains(failId)) &&
                this.NodeIds.Count > 0)
            {
                failId = random.Next(this.NumOfId);
            }

            if (failId < 0)
            {
                this.TriggerStop();
                return;
            }

            var nodeToFail = this.Nodes[failId];

            this.Send(nodeToFail, new eFail());

            this.QueryStabilize();
        }

        private void TriggerStop()
        {
            //Console.WriteLine("[Cluster] Stopping ...\n");

            this.Send(this.Client, new eStop());

            foreach (var node in this.Nodes)
            {
                this.Send(node, new eStop());
            }

            this.Delete();
        }

        private void FindSuccessor()
        {
            //Console.WriteLine("[Cluster] Propagating: eFindSuccessor ...\n");
            this.Send(this.Nodes[0], new eFindSuccessor(this.Payload));
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Querying));

            StepStateTransitions queryingDict = new StepStateTransitions();
            queryingDict.Add(typeof(eLocal), typeof(Waiting));

            StepStateTransitions waitingDict = new StepStateTransitions();
            waitingDict.Add(typeof(eLocal), typeof(Querying));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Querying), queryingDict);
            dict.Add(typeof(Waiting), waitingDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings waitingDict = new ActionBindings();
            waitingDict.Add(typeof(eFindSuccessor), new Action(FindSuccessor));
            waitingDict.Add(typeof(eJoinAck), new Action(QueryStabilize));

            dict.Add(typeof(Waiting), waitingDict);

            return dict;
        }
    }

    internal class ChordNode : Machine
    {
        private Machine Cluster;
        private int Id;

        private int M;
        private int NumOfId;

        private List<int> Keys;
        private Dictionary<int, Tuple<int, int, Machine>> FingerTable;
        private Machine Predecessor;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChordNode;

                machine.Cluster = ((Tuple<Machine, int, int>)this.Payload).Item1;
                machine.Id = ((Tuple<Machine, int, int>)this.Payload).Item2;
                machine.M = ((Tuple<Machine, int, int>)this.Payload).Item3;

                //Console.WriteLine("[ChordNode-{0}] Initializing ...\n", machine.Id);

                machine.NumOfId = (int)Math.Pow(2, machine.M);
                machine.Keys = new List<int>();
                machine.FingerTable = new Dictionary<int, Tuple<int, int, Machine>>();
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eFindSuccessor),
                    typeof(eFindPredecessor),
                    typeof(eNotifySuccessor),
                    typeof(eStabilize),
                    typeof(eStop)
                };
            }
        }

        private class Configuring : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChordNode;

                //Console.WriteLine("[ChordNode-{0}] Configuring ...\n", machine.Id);

                var nodeIds = ((Tuple<List<int>, List<Machine>, List<int>>)this.Payload).Item1;
                var nodes = ((Tuple<List<int>, List<Machine>, List<int>>)this.Payload).Item2;
                var keys = ((Tuple<List<int>, List<Machine>, List<int>>)this.Payload).Item3;

                foreach (var key in keys)
                {
                    machine.Keys.Add(key);
                }

                for (var idx = 1; idx <= machine.M; idx++)
                {
                    var start = (machine.Id + (int)Math.Pow(2, (idx - 1))) % machine.NumOfId;
                    var end = (machine.Id + (int)Math.Pow(2, idx)) % machine.NumOfId;

                    var nodeId = machine.GetSuccessorNodeId(start, nodeIds);
                    machine.FingerTable.Add(start, new Tuple<int, int, Machine>(
                        start, end, nodes[nodeId]));
                }

                for (var idx = 0; idx < nodeIds.Count; idx++)
                {
                    if (nodeIds[idx] == machine.Id)
                    {
                        machine.Predecessor = nodes[machine.WrapSubtract(idx, 1, nodeIds.Count)];
                        break;
                    }
                }

                this.Raise(new eLocal());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eFindSuccessor),
                    typeof(eFindPredecessor),
                    typeof(eNotifySuccessor),
                    typeof(eStabilize),
                    typeof(eStop)
                };
            }
        }

        private class Joining : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChordNode;

                //Console.WriteLine("[ChordNode-{0}] Joining ...\n", machine.Id);

                var nodeIds = ((Tuple<List<int>, List<Machine>>)this.Payload).Item1;
                var nodes = ((Tuple<List<int>, List<Machine>>)this.Payload).Item2;

                for (var idx = 1; idx <= machine.M; idx++)
                {
                    var start = (machine.Id + (int)Math.Pow(2, (idx - 1))) % machine.NumOfId;
                    var end = (machine.Id + (int)Math.Pow(2, idx)) % machine.NumOfId;
                    var nodeId = machine.GetSuccessorNodeId(start, nodeIds);
                    machine.FingerTable.Add(start, new Tuple<int, int, Machine>(
                        start, end, nodes[nodeId]));
                }

                var successor = machine.FingerTable[(machine.Id + 1) % machine.NumOfId].Item3;

                this.Send(machine.Cluster, new eJoinAck());
                this.Send(successor, new eNotifySuccessor(machine));

                this.Raise(new eLocal());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eFindSuccessor),
                    typeof(eFindPredecessor),
                    typeof(eNotifySuccessor),
                    typeof(eStabilize),
                    typeof(eStop)
                };
            }
        }

        private class Waiting : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChordNode;

                //Console.WriteLine("[ChordNode-{0}] Waiting ...\n", machine.Id);
            }
        }

        private void SendId()
        {
            //Console.WriteLine("[ChordNode-{0}] Sending Id ...\n", this.Id);

            var sender = (Machine)this.Payload;
            this.Send(sender, new eQueryIdResp(this.Id));
        }

        private void FindSuccessor()
        {
            var sender = ((Tuple<Machine, int, int>)this.Payload).Item1;
            var id = ((Tuple<Machine, int, int>)this.Payload).Item2;
            var timeout = ((Tuple<Machine, int, int>)this.Payload).Item3;

            //Console.WriteLine("[ChordNode-{0}] Finding successor of {1} ...\n", this.Id, id);

            if (this.Keys.Contains(id))
            {
                this.Send(sender, new eFindSuccessorResp(new Tuple<Machine, int>(this, id)));
            }
            else if (this.FingerTable.ContainsKey(id))
            {
                this.Send(sender, new eFindSuccessorResp(new Tuple<Machine, int>(
                    this.FingerTable[id].Item3, id)));
            }
            else if (this.Id.Equals(id))
            {
                this.Send(sender, new eFindSuccessorResp(new Tuple<Machine, int>(
                    this.FingerTable[(this.Id + 1) % this.NumOfId].Item3, id)));
            }
            else
            {
                int idToAsk = -1;
                foreach (var finger in this.FingerTable)
                {
                    if (((finger.Value.Item1 > finger.Value.Item2) &&
                        (finger.Value.Item1 <= id || id < finger.Value.Item2)) ||
                        ((finger.Value.Item1 < finger.Value.Item2) &&
                        (finger.Value.Item1 <= id && id < finger.Value.Item2)))
                    {
                        idToAsk = finger.Key;
                    }
                }

                if (idToAsk < 0)
                {
                    idToAsk = (this.Id + 1) % this.NumOfId;
                }

                if (this.FingerTable[idToAsk].Item3.Equals(this))
                {
                    foreach (var finger in this.FingerTable)
                    {
                        if (finger.Value.Item2 == idToAsk ||
                            finger.Value.Item2 == idToAsk - 1)
                        {
                            idToAsk = finger.Key;
                            break;
                        }
                    }

                    Runtime.Assert(!this.FingerTable[idToAsk].Item3.Equals(this),
                        "Cannot locate successor of {0}.", id);
                }

                timeout--;
                if (timeout == 0)
                {
                    return;
                }

                this.Send(this.FingerTable[idToAsk].Item3, new eFindSuccessor(
                    new Tuple<Machine, int, int>(sender, id, timeout)));
            }
        }

        private void Stabilize()
        {
            //Console.WriteLine("[ChordNode-{0}] Stabilizing ...\n", this.Id);

            var successor = this.FingerTable[(this.Id + 1) % this.NumOfId].Item3;
            this.Send(successor, new eFindPredecessor(this));

            foreach (var finger in this.FingerTable)
            {
                if (!finger.Value.Item3.Equals(successor))
                {
                    this.Send(successor, new eFindSuccessor(
                        new Tuple<Machine, int, int>(this, finger.Key, 100)));
                }
            }
        }

        private void UpdatePredecessor()
        {
            //Console.WriteLine("[ChordNode-{0}] Updating predecessor ...\n", this.Id);

            var predecessor = (Machine)this.Payload;
            if (predecessor.Equals(this))
            {
                return;
            }

            this.Predecessor = predecessor;
        }

        private void UpdateSuccessor()
        {
            //Console.WriteLine("[ChordNode-{0}] Updating successor ...\n", this.Id);

            var successor = (Machine)this.Payload;
            if (successor.Equals(this))
            {
                return;
            }

            this.FingerTable[(this.Id + 1) % this.NumOfId] = new Tuple<int, int, Machine>(
                this.FingerTable[(this.Id + 1) % this.NumOfId].Item1,
                this.FingerTable[(this.Id + 1) % this.NumOfId].Item2,
                successor);

            this.Send(successor, new eNotifySuccessor(this));
            this.Send(successor, new eAskForKeys(new Tuple<Machine, int>(this, this.Id)));
        }

        private void SuccessorFound()
        {
            //Console.WriteLine("[ChordNode-{0}] Successor found ...\n", this.Id);

            var successor = ((Tuple<Machine, int>)this.Payload).Item1;
            var id = ((Tuple<Machine, int>)this.Payload).Item2;

            Runtime.Assert(this.FingerTable.ContainsKey(id), "Finger table does not contain {0}.", id);
            this.FingerTable[id] = new Tuple<int, int, Machine>(
                this.FingerTable[id].Item1,
                this.FingerTable[id].Item2,
                successor);
        }

        private void UpdateKeys()
        {
            //Console.WriteLine("[ChordNode-{0}] Updating keys ...\n", this.Id);

            var keys = (List<int>)this.Payload;
            foreach (var key in keys)
            {
                this.Keys.Add(key);
            }
        }

        private void SendPredecessor()
        {
            //Console.WriteLine("[ChordNode-{0}] Sending predecessor ...\n", this.Id);

            var sender = (Machine)this.Payload;
            if (this.Predecessor != null)
            {
                this.Send(sender, new eFindPredecessorResp(this.Predecessor));
            }
        }

        private void SendCorrespondingKeys()
        {
            var sender = ((Tuple<Machine, int>)this.Payload).Item1;
            var senderId = ((Tuple<Machine, int>)this.Payload).Item2;
            //Console.WriteLine("[ChordNode-{0}] Sending keys to predecessor {1} ...\n", this.Id, senderId);
            Runtime.Assert(this.Predecessor.Equals(sender), "Predecessor is corrupted.");

            List<int> keysToSend = new List<int>();
            foreach (var key in this.Keys)
            {
                if (key <= senderId)
                {
                    keysToSend.Add(key);
                }
            }

            if (keysToSend.Count == 0)
            {
                return;
            }

            this.Send(sender, new eAskForKeysAck(keysToSend));

            foreach (var key in keysToSend)
            {
                this.Keys.Remove(key);
            }
        }

        private void Failing()
        {
            //Console.WriteLine("[ChordNode-{0}] Failing ...\n", this.Id);
            this.Delete();
        }

        private void Stopping()
        {
            //Console.WriteLine("[ChordNode-{0}] Stopping ...\n", this.Id);
            this.Delete();
        }

        private int WrapAdd(int left, int right, int ceiling)
        {
            int result = left + right;
            if (result > ceiling)
            {
                result = ceiling - result;
            }

            return result;
        }

        private int WrapSubtract(int left, int right, int ceiling)
        {
            int result = left - right;
            if (result < 0)
            {
                result = ceiling + result;
            }

            return result;
        }

        private int GetSuccessorNodeId(int start, List<int> nodeIds)
        {
            var candidate = -1;
            foreach (var id in nodeIds.Where(v => v >= start))
            {
                if (candidate < 0 || id < candidate)
                {
                    candidate = id;
                }
            }

            if (candidate < 0)
            {
                foreach (var id in nodeIds.Where(v => v < start))
                {
                    if (candidate < 0 || id < candidate)
                    {
                        candidate = id;
                    }
                }
            }

            for (int idx = 0; idx < nodeIds.Count; idx++)
            {
                if (nodeIds[idx] == candidate)
                {
                    candidate = idx;
                    break;
                }
            }

            return candidate;
        }

        private void PrintFingerTableAndKeys()
        {
            Console.WriteLine("[ChordNode-{0}] Printing finger table:", this.Id);
            foreach (var finger in this.FingerTable)
            {
                Console.WriteLine("  > " + finger.Key + " | [" + finger.Value.Item1 +
                    ", " + finger.Value.Item2 + ") | " + (finger.Value.Item3 as ChordNode).Id);
            }

            Console.WriteLine("[ChordNode-{0}] Printing keys:", this.Id);
            foreach (var key in this.Keys)
            {
                Console.WriteLine("  > Key-" + key);
            }

            Console.WriteLine();
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eConfigure), typeof(Configuring));
            initDict.Add(typeof(eJoin), typeof(Joining));

            StepStateTransitions configuringDict = new StepStateTransitions();
            configuringDict.Add(typeof(eLocal), typeof(Waiting));

            StepStateTransitions joiningDict = new StepStateTransitions();
            joiningDict.Add(typeof(eLocal), typeof(Waiting));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Configuring), configuringDict);
            dict.Add(typeof(Joining), joiningDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings joiningDict = new ActionBindings();
            joiningDict.Add(typeof(eQueryId), new Action(SendId));

            ActionBindings waitingDict = new ActionBindings();
            waitingDict.Add(typeof(eQueryId), new Action(SendId));
            waitingDict.Add(typeof(eStabilize), new Action(Stabilize));
            waitingDict.Add(typeof(eFindPredecessor), new Action(SendPredecessor));
            waitingDict.Add(typeof(eFindSuccessor), new Action(FindSuccessor));
            waitingDict.Add(typeof(eFindSuccessorResp), new Action(SuccessorFound));
            waitingDict.Add(typeof(eNotifySuccessor), new Action(UpdatePredecessor));
            waitingDict.Add(typeof(eFindPredecessorResp), new Action(UpdateSuccessor));
            waitingDict.Add(typeof(eAskForKeys), new Action(SendCorrespondingKeys));
            waitingDict.Add(typeof(eAskForKeysAck), new Action(UpdateKeys));
            waitingDict.Add(typeof(eFail), new Action(Failing));
            waitingDict.Add(typeof(eStop), new Action(Stopping));

            dict.Add(typeof(Joining), joiningDict);
            dict.Add(typeof(Waiting), waitingDict);

            return dict;
        }
    }

    internal class Client : Machine
    {
        private Machine Cluster;
        private List<int> Keys;
        private int QueryKey;

        private int QueryCounter;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                //Console.WriteLine("[Client] Initializing ...\n");

                machine.Cluster = ((Tuple<Machine, List<int>>)this.Payload).Item1;
                machine.Keys = ((Tuple<Machine, List<int>>)this.Payload).Item2;
                machine.QueryCounter = 0;

                this.Raise(new eLocal());
            }
        }

        private class Waiting : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                //Console.WriteLine("[Client] Waiting ...\n");
            }
        }

        private class Querying : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                if (machine.QueryCounter < 3)
                {
                    //Console.WriteLine("[Client] Querying ...\n");

                    Random random = new Random();
                    var randomValue = random.Next(machine.Keys.Count);
                    machine.QueryKey = machine.Keys[randomValue];

                    this.Send(machine.Cluster, new eFindSuccessor(new Tuple<Machine, int>(
                        machine, machine.QueryKey)));

                    machine.QueryCounter++;
                }

                this.Raise(new eLocal());
            }
        }

        private void ReceiveSuccessorId()
        {
            var id = (int)this.Payload;

            //Console.WriteLine("[Client] Received successor with Id {0} for Key {1}  ...\n",
            //    id, this.QueryKey);

            this.Raise(new eLocal());
        }

        private void SuccessorFound()
        {
            //Console.WriteLine("[Client] Successor found  ...\n");

            var successor = ((Tuple<Machine, int>)this.Payload).Item1;
            var id = ((Tuple<Machine, int>)this.Payload).Item2;
            this.Send(successor, new eQueryId(this));
        }

        private void Stopping()
        {
            //Console.WriteLine("[Client] Stopping ...\n");
            this.Delete();
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Waiting));

            StepStateTransitions waitingDict = new StepStateTransitions();
            waitingDict.Add(typeof(eNotifyClient), typeof(Querying));
            waitingDict.Add(typeof(eLocal), typeof(Querying));

            StepStateTransitions queryingDict = new StepStateTransitions();
            queryingDict.Add(typeof(eLocal), typeof(Waiting));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Waiting), waitingDict);
            dict.Add(typeof(Querying), queryingDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings waitingDict = new ActionBindings();
            waitingDict.Add(typeof(eQueryIdResp), new Action(ReceiveSuccessorId));
            waitingDict.Add(typeof(eFindSuccessorResp), new Action(SuccessorFound));
            waitingDict.Add(typeof(eStop), new Action(Stopping));

            dict.Add(typeof(Waiting), waitingDict);

            return dict;
        }
    }

    #endregion
}
