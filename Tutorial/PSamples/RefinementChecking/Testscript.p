module ClientModule = { ClientInterface -> ClientMachine };

module ServerModule = {
  ServerClientInterface -> ServerMachine,
  ServerHelperInterface -> ServerMachine,
  HelperInterface -> HelperMachine
};

module AbstractServerModule = { ServerClientInterface -> AbstractServerMachine };

//Check that the composition of ClientModule and AbstractServerModule is safe.
test testcase0: main TestDriver0 in 
  (compose { TestDriver0 }, 
    (assert ReqIdsAreMonotonicallyIncreasing, RespIdsAreMonotonicallyIncreasing in (compose ClientModule, AbstractServerModule)));

//Check that server abstraction is correct.
module TestDriver1 = (compose { TestDriver0 }, ClientModule);
test testcase1: 
  main TestDriver0 in (compose TestDriver1, ServerModule) 
  refines 
  main TestDriver0 in (compose TestDriver1, AbstractServerModule);

//C code generation for the implementation.
implementation (compose ClientModule, ServerModule);
