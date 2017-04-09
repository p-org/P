spec Liveness observes PING, PONG { 
    start state WaitPing { 
        on PING goto WaitPong;
    }
    hot state WaitPong {
        on PONG goto WaitPing;
    }
} 
