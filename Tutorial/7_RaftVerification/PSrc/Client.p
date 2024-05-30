type tClientRequest = (action: int);

event eClientRequest : tClientRequest;

machine Client {
    start state MaybeMakeClientRequest {
        entry (input : (serv: Server, act: int)) {
            if ($) {
                send input.serv, eClientRequest, (action = input.act,);
            }
            goto MaybeMakeClientRequest, (serv=RandomServer(), act=RandomAction());
        }
    }
}

fun RandomServer(): Server;
fun RandomAction(): int;