event nextNode:id;
event newNodeID:id;

machine CentralServer
\begin{CentralServer}
	var nodeID : id;
	model fun LoadNodeList() {
	
	}
	
	model fun GetNextNodeFromList() : id {
	
	}
	
	state Init {
		entry {
			LoadNodeList();
		}
        on nextNode goto GetNextNode;
    }
	state GetNextNode {
		entry {
			nodeID = GetNextNodeFromList();
			_SEND((id)payload, newNodeID, nodeID);
		}
		
		on nextNode goto GetNextNode;
	
	}
\end{CentralServer}