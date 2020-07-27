
/***************************************************
* Invariants described in the paper
***************************************************/

// This monitor checks the Update Propagation Invariant 
// Hist_j <= Hist_i forall i<=j --- Invariant 1
// Hist_i = Hist_j + Sent_i -- Invariant 2


event eMonitorHistoryUpdate: (node : ChainReplicationNodeInterface, history: seq[int]);
event eMonitorSentUpdate: (node : ChainReplicationNodeInterface, sent : seq[(seqId: int, smrop: SMROperationType)]);
event eMonitorUpdateNodes : (nodes: seq[ChainReplicationNodeInterface]);

spec UpdatePropagationInvariants observes eMonitorHistoryUpdate, eMonitorSentUpdate, eMonitorUpdateNodes {
	var nodes : seq[ChainReplicationNodeInterface];
	var histMap : map[ChainReplicationNodeInterface, seq[int]];
	var sentMap : map[ChainReplicationNodeInterface, seq[int]];
	
	start state WaitForMessages {
		on eMonitorSentUpdate do (payload: (node : ChainReplicationNodeInterface, sent : seq[(seqId: int, smrop: SMROperationType)])) {
			var sentSeqIds: seq[int];
			var mergedSeq : seq[int];
			var next : ChainReplicationNodeInterface;
			var prev : ChainReplicationNodeInterface;
			//update the sent map
			sentSeqIds = ExtractSeqId(payload.sent);
			sentMap[payload.node] = sentSeqIds;
			
			next = GetNextNode(payload.node);
			//hist(node) = hist(node+1) + sent(node)
			if(next in histMap)
			{
				mergedSeq = MergeSeq(histMap[next], sentMap[payload.node]);
				CheckEqual(histMap[payload.node], mergedSeq);
			}
					
			prev = GetPrevNode(payload.node);
			//hist(node-1) = hist(node) + sent(node-1)	
			if(prev in sentMap)
			{
				mergedSeq = MergeSeq(histMap[payload.node], sentMap[prev]);
				CheckEqual(histMap[prev], mergedSeq);
			}
		}
		on eMonitorHistoryUpdate do (payload: (node : ChainReplicationNodeInterface, history: seq[int])){
			var next : ChainReplicationNodeInterface;
			var prev : ChainReplicationNodeInterface;
			IsSorted(payload.history);
			//update the history
			histMap[payload.node] = payload.history;
			
			//hist(node+1) <= hist(node)
			next = GetNextNode(payload.node);
			if(next in histMap) {
				CheckIsPrefix(histMap[next], histMap[payload.node]);
			}
			
			//hist(node) <= hist(node-1)
			prev = GetPrevNode(payload.node);
			if(prev in histMap) {
				CheckIsPrefix(histMap[payload.node], histMap[prev]);
			}
		}
		on eMonitorUpdateNodes do (payload: (nodes: seq[ChainReplicationNodeInterface])) { nodes = payload.nodes; } 
	}

	fun CheckIsPrefix(s1 : seq[int], s2 : seq[int]) {
		var iter: int;
		IsSorted(s1);
		IsSorted(s2);
		assert(sizeof(s1) <= sizeof(s2));
		iter = sizeof(s1) - 1;
		while(iter >= 0)
		{
			assert(s1[iter] == s2[iter]);
			iter = iter - 1;
		}
	}

	fun GetNextNode(curr: ChainReplicationNodeInterface) : ChainReplicationNodeInterface {
		var iter : int;
		iter = 1;
		while(iter < sizeof(nodes) - 1)
		{
			if(nodes[iter - 1] == curr)
				return nodes[iter];
				
			iter = iter + 1;
		}

		return null;
	}
	
	fun GetPrevNode(curr:ChainReplicationNodeInterface) : ChainReplicationNodeInterface {
		var iter : int;
		iter = 1;
		while(iter < sizeof(nodes) - 1)
		{
			if(nodes[iter] == curr)
				return nodes[iter - 1];
				
			iter = iter + 1;
		}
		return null;
	}
	
	fun ExtractSeqId(s : seq[(seqId: int, smrop: SMROperationType)]) : seq[int] {
		var ret: seq[int];
		var iter: int;
		iter = sizeof(s) - 1;
		while(iter >= 0)
		{
			ret += (0, s[iter].seqId);
			iter = iter - 1;
		}
		print "In Extract: {0}\n", ret;
		IsSorted(ret);
		return ret;
	}
	
	fun MergeSeq(s1 : seq[int], s2 : seq[int]) : seq[int]
	{
		var iter : int;
		var mergedSeq: seq[int];
		IsSorted(s1);
		IsSorted(s2);
		iter = 0;
		if(sizeof(s1) == 0)
			mergedSeq = s2;
		else if(sizeof(s2) == 0)
			mergedSeq = s1;
			
		while(iter <= sizeof(s1) - 1)
		{
			if(s1[iter] < s2[0])
			{
				mergedSeq += (sizeof(mergedSeq), s1[iter]);
			}	
			iter = iter + 1;
		}
		iter = 0;
		while(iter <= sizeof(s2) - 1)
		{
			mergedSeq += (sizeof(mergedSeq), s2[iter]);
			iter = iter + 1;
		}
		IsSorted(mergedSeq);

		return mergedSeq;
	}
	
	fun CheckEqual(s1 : seq[int], s2 : seq[int]) {
		var iter: int;
		assert(sizeof(s1) == sizeof(s2));
		iter = sizeof(s1) - 1;
		while(iter >= 0)
		{
			assert(s1[iter] == s2[iter]);
			iter = iter - 1;
		}
	}
	
	fun IsSorted(l:seq[int]){
		var iter: int;
        iter = 0;
        while (iter < sizeof(l) - 1) {
		   print "In IsSorted: {0}\n", l;
           assert(l[iter] < l[iter+1]);
            iter = iter + 1;
        }
	}
}

/*
We will check liveness properties 
1 -> In the absence of failures all client update request should be followed eventually by a response.
2 -> In the presence of n nodes and n-1 failures and the head node does not fail then all client requests should be followed eventually by a response.
*/

event eMonitorUpdateForLiveness : (seqId : int);
event eMonitorResponseForLiveness : (seqId : int, commitId: int);

spec ProgressUpdateHasResponse observes eMonitorUpdateForLiveness, eMonitorResponseForLiveness {
	var ReqtoResp : map[int, int];
	start state Init {
		entry {
			
		}
		on eMonitorUpdateForLiveness goto WaitForAllResponses with (payload: (seqId : int)) {
			assert(!(payload.seqId in ReqtoResp));
			ReqtoResp[payload.seqId] = -1;
		}
	}

	hot state WaitForAllResponses {
		entry {
			var iter : int;
			var ks : seq[int];
			iter = 0;
			
			ks = keys(ReqtoResp);
			print "In WaitForAllResponses\n";
			print ":: {0}\n", ReqtoResp;
			while(iter < sizeof(ks))
			{
				if(ReqtoResp[ks[iter]] == -1)
					return;
				iter = iter + 1;
			}
			goto AllResponded;
		}
		on eMonitorUpdateForLiveness goto WaitForAllResponses with (payload: (seqId : int)) {
			assert(!(payload.seqId in ReqtoResp));
			ReqtoResp[payload.seqId] = -1;
		}
		on eMonitorResponseForLiveness goto WaitForAllResponses with (payload: (seqId : int, commitId: int)) {
			assert(ReqtoResp[payload.seqId] == -1 || ReqtoResp[payload.seqId] == payload.commitId);
			ReqtoResp[payload.seqId] = payload.commitId;
		}
	}
	
	cold state AllResponded {
		on eMonitorResponseForLiveness goto WaitForAllResponses with (payload: (seqId : int, commitId: int)) {
			if(payload.seqId in ReqtoResp)
			{
				
				assert(ReqtoResp[payload.seqId] == -1 || ReqtoResp[payload.seqId] == payload.commitId);
			}
			else
			{
				ReqtoResp[payload.seqId] = payload.commitId;
			}
		}

		on eMonitorUpdateForLiveness goto WaitForAllResponses with (payload: (seqId : int)) {
			assert(!(payload.seqId in ReqtoResp));
			ReqtoResp[payload.seqId] = -1;
		}
	}
}
