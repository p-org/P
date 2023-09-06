event eAllocateRequest;

machine Main {
    start state Init {
        entry
        {
            var myevents: set[event];
            var myevent: event;
            myevent = eAllocateRequest;
            myevents += (eAllocateRequest);
            print(format("myevents: {0}", myevents));
            assert (myevent in myevents && eAllocateRequest == myevent);
        }
    }
}