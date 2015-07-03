// unreliable send 
static model fun _SEND(target:machine, e:event, p:any) {
	if ($)
		send target, e, p;
}

// reliable send
static model fun _SENDRELIABLE(target:machine, e:event, p:any) {
	send target, e, p;
}

static model fun _CREATECONTAINER() : machine {
	return default(machine);
}






