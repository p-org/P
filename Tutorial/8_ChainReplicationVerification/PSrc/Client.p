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

invariant clearly_false_invariant: 0 > 1;