event dummy;
event E;
event F;

machine Main {
start state InitPingPong {
entry {
}
on dummy goto Fail with {Action1();}
on E push Success;
//on F do {}
}

state Fail {
entry {
}
on dummy goto InitPingPong  with Action2;
on E goto Success with {assert false;}
}
state Success {
on dummy goto InitPingPong  with {;}
on E push Fail;
}
fun Action1() {}
fun Action2() {}
}