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

invariant disagree: forall (m1: Client, m2: Client) :: m1 == m2 ==> m1.tail != m2.head;
