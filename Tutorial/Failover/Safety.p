spec Safety observes PING, PONG { 
    var pending: int;
    start state Init { 
        on PING do { 
			assert (pending == 0);
            pending = pending + 1; 
        }
        on PONG do {
            assert (pending == 1); 
            pending = pending - 1;
        }
    }
} 
