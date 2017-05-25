machine Main {
    var reliableStorage: IReliableStorage;
    var service: IService;
    start state Init {
        entry {
            var m: machine;
            reliableStorage = new ReliableStorage();
            service = new Service();
            m = new FaultTolerantMachine(service, reliableStorage);
            send m, halt;
            m = new FaultTolerantMachine(service, reliableStorage); 
        }
    }
}