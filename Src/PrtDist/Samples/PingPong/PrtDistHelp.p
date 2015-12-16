// unreliable send 
static model fun _SEND(target:machine, e:event, p:any) {
	if ($)
		send target, e, p;
}

static model fun _CREATECONTAINER() : machine {
	var retVal : machine;
	retVal = new Container();
	return retVal;
}

machine Container {
	start state Init {
		
	}
}






