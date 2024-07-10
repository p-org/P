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

invariant no_write_request_to_client: forall (e: event, m: machine) :: e is eWriteRequest && target(e) == m && m is Client ==> !inflight(e);
invariant no_propagate_to_client: forall (e: event, m: machine) :: e is ePropagateWrite && target(e) == m && m is Client ==> !inflight(e);
invariant no_read_request_to_client: forall (e: event, m: machine) :: e is eReadRequest && target(e) == m && m is Client ==> !inflight(e);
invariant client_head_and_tail_are_not_clients: forall (c: Client) :: !(c.head is Client) && !(c.tail is Client);