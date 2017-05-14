module ClientModule
private;
{
  ClientMachine
}

module ServerModule
private;
{
  ServerMachine,
  HelperMachine
}

module AbstractServerModule
private;
{
  AbstractServerMachine
}

module TestDriver1 
private;
{
  TestDriver_1Client1Server
}

module TestDriver2 
private;
{
  TestDriver_CheckRefinement
}

//Check that server abstraction is correct.
test testcase0: 
  (compose (rename TestDriver_CheckRefinement to Main in TestDriver2), ServerModule) 
  refines 
  (compose (rename TestDriver_CheckRefinement to Main in TestDriver2), 
           (rename AbstractServerMachine to ServerMachine in AbstractServerModule));

//Check that the composition of ClientModule and AbstractServerModule is safe (no local assertion failures).
test testcase1: 
  (compose (rename TestDriver_1Client1Server to Main in TestDriver1), 
           ClientModule, 
           AbstractServerModule);

//Check that ClientModule satisfies the spec ReqIdsAreMonotonicallyIncreasing.
test testcase2: 
  (compose (rename TestDriver_1Client1Server to Main in TestDriver1), 
           (assert ReqIdsAreMonotonicallyIncreasing in ClientModule), 
           AbstractServerModule);

//C code generation for the implementation.
//Note that implementation module need not be closed.
implementation (compose ClientModule, ServerModule);
