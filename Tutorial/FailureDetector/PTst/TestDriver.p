//include "FailureDetector.p"

//Main machine:
machine Client {
    var fd: machine;
    var nodeseq: seq[machine];
    
    start state Init {
        entry {
            // create 3 nodes
            nodeseq += (0, new Node());
            nodeseq += (0, new Node());
            nodeseq += (0, new Node());

            fd = new FailureDetector(nodeseq as seq[Node]);

            // non deterministically fail one nodes
            Fail();
        }

        on eNodeDown do (node: Node){
            print format("Node {0} is down!", node);
        }
    }

    
    fun Fail() {
        var pick: machine;
        pick = choose(nodeseq);
        send pick, eKill;
    }
}