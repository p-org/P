fun SEND (target: machine, ev: event, val: any) {
    if($)
        send target, ev, val; 
}

fun SENDRELIABLE(target: machine, ev: event, val: any) {
    send target, ev, val; 
}