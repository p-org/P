machine Main {
    start state Init {
        var m: machine;
        m = new FaultTolerantMachine();
        send m, halt;
        m = new FaultTolerantMachine(); 
    }
}