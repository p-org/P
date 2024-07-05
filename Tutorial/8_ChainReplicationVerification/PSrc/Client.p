machine Client {
    var head: machine;
    var tail: machine;
    
    start state Loop {
        entry {
            var k: int;
            var v: int; 
            
            k = RandomInt();
            
            if ($) {
                v = RandomInt();
                send head, eWriteRequest, (source = this, k = k, v = v);
            } else {
                send tail, eReadRequest, (source = this, k = k);
            }
            
            goto Loop;
        }
    }
}

fun RandomInt(): int;

pure target(e: event): machine;
pure inflight(e: event): bool;

invariant agree: forall (m1: Client, m2: Client) :: m1.tail == m2.tail && m1.head == m2.head;
invariant no_write: forall (e: event) :: e is eWriteRequest && target(e) is Client ==> !inflight(e);
