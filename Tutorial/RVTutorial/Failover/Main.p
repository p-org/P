machine TestDriver {
    var reliableStorage: ReliableStorageMachine;
    var service: ServiceMachine;
    start state Init {
        entry {
            var m: machine;
            reliableStorage = new ReliableStorageMachine();
            service = new ServiceMachine();
            m = new FaultTolerantMachine(service, reliableStorage);
            send m, halt;
            m = new FaultTolerantMachine(service, reliableStorage); 
        }
    }
}