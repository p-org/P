// unreliable send
fun _SEND(target:machine, e:event, p:any) {
		send target, e, p;
}

fun _CREATECONTAINER() : machine {
	var retVal : machine;
	retVal = new Container();
	return retVal;
}

machine Container {
	start state Init {
		
	}
}
