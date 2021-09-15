machine Client {
    var fd: machine;
    var nodes: set[Node];
    
    start state Init {
        entry {
            // create 3 nodes
            nodes += (new Node());
            nodes += (new Node());
            nodes += (new Node());

            fd = new FailureDetector(nodes);

            // todo: re-use the failure injector nodes
            Fail();
        }

        on eNodeDown do (node: Node){
            print format("Node {0} is down!", node);
        }
    }
}