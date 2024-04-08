event eAllocateRequest: event;

machine Main {
    start state Init {
        entry
        {
            var myevents: set[event];
            var myevent: event;
            myevent = eAllocateRequest;
            myevents += (eAllocateRequest);
            send this, eAllocateRequest, eAllocateRequest;
            print(format("myevents: {0}", myevents));
            assert ((myevent in myevents) && eAllocateRequest == myevent);
        }
        ignore eAllocateRequest;
    }

}