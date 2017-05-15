module ClientModule
{
  ClientMachine
}

module ServerModule
{
  ServerMachine,
  HelperMachine
}

module AbstractServerModule
{
  AbstractServerMachine
}

//Check that the composition of ClientModule and AbstractServerModule is safe.
module TestDriver0 
{
  Main
}
test testcase0: 
  (compose TestDriver0, 
           (assert ReqIdsAreMonotonicallyIncreasing, RespIdsAreMonotonicallyIncreasing in
                   (compose ClientModule, AbstractServerModule)));

//Check that server abstraction is correct.
module TestDriver1 = (compose TestDriver0, ClientModule);
test testcase1: 
  (compose TestDriver1, ServerModule) 
  refines 
  (compose TestDriver1, 
           (rename AbstractServerMachine to ServerMachine in AbstractServerModule));

//C code generation for the implementation.
implementation (compose ClientModule, ServerModule);
