// unreliable send 
static model fun SEND(target:machine, e:event, p:any) {
	if ($)
		send target, e, p;
}

// reliable send 
static model fun SEND_REL(target:machine, e:event, p:any, failed: bool) : bool {
	if(!failed) failed = $;
	if(!failed) send target, e, p;
	return failed;
}
static model fun _CREATECONTAINER() : machine {
	var retVal: machine;
	retVal = new Container();
	return retVal;
}

machine Container {
	start state Init {
		
	}
}






