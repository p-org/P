fun UnReliableSend(target: machine, message: event, payload: any) {
    // arbitrarily drop messages to act as message loss
    if($)
        send target, message, payload;
}

fun BroadCast(ms: set[machine], ev: event, payload: any) {
    var i: int;
    while (i < sizeof(ms)) {
        UnReliableSend(ms[i], ev, payload);
        i = i + 1;
    }
}