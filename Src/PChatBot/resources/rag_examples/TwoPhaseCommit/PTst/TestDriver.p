machine TestWithSingleClient {
    var coordinator: machine;
    var participants: seq[machine];
    var timer: machine;
    var client: machine;
    
    start state Init {
        entry InitEntry;
    }
    
    fun InitEntry() {
        var i: int;
        var participantsList: seq[machine];
        
        // Create timer first
        timer = new Timer(default(machine));
        
        // Create 3 participants
        i = 0;
        while (i < 3) {
            participants += (i, new Participant());
            i = i + 1;
        }
        
        // Prepare participants list for coordinator
        i = 0;
        while (i < 3) {
            participantsList += (i, participants[i]);
            i = i + 1;
        }
        
        // Create coordinator
        coordinator = new Coordinator((parts = participantsList, timerMachine = timer));
        
        // Update timer with coordinator as client
        send timer, eInformCoordinator, (coordinator = coordinator,);
        
        // Inform all participants about coordinator
        i = 0;
        while (i < 3) {
            send participants[i], eInformCoordinator, (coordinator = coordinator,);
            i = i + 1;
        }
        
        // Create and start client with 5 transactions
        client = new Client((coord = coordinator, numTrans = 5));
    }
}

machine TestWithMultipleClients {
    var coordinator: machine;
    var participants: seq[machine];
    var timer: machine;
    var client1: machine;
    var client2: machine;
    
    start state Init {
        entry InitEntry;
    }
    
    fun InitEntry() {
        var i: int;
        var participantsList: seq[machine];
        
        // Create timer first
        timer = new Timer(default(machine));
        
        // Create 3 participants
        i = 0;
        while (i < 3) {
            participants += (i, new Participant());
            i = i + 1;
        }
        
        // Prepare participants list for coordinator
        i = 0;
        while (i < 3) {
            participantsList += (i, participants[i]);
            i = i + 1;
        }
        
        // Create coordinator
        coordinator = new Coordinator((parts = participantsList, timerMachine = timer));
        
        // Update timer with coordinator as client
        send timer, eInformCoordinator, (coordinator = coordinator,);
        
        // Inform all participants about coordinator
        i = 0;
        while (i < 3) {
            send participants[i], eInformCoordinator, (coordinator = coordinator,);
            i = i + 1;
        }
        
        // Create and start two clients with 3 transactions each
        client1 = new Client((coord = coordinator, numTrans = 3));
        client2 = new Client((coord = coordinator, numTrans = 3));
    }
}