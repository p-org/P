fun UnReliableNetworkSend(target: machine, message: event, payload: any) {
    // arbitrarily drop messages to act as message loss
    if($)
        send target, message, payload;
}