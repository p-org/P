event dummy;
event E;

machine Main {
start state InitPingPong {
entry {
}
on dummy goto Fail with {;}
on E push Success;
}

state Fail {
entry {
}
on dummy goto InitPingPong  with {;}
on E push Success;
}
state Success {
on dummy goto InitPingPong  with {;}
on E push Fail;
}
}