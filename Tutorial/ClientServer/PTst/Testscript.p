module ClientModule = { ClientMachine ->  ClientInterface};

module ServerModule = { ServerMachine -> ServerClientInterface, HelperMachine -> HelperInterface };

module AbstractServerModule = { AbstractServerMachine -> ServerClientInterface };

//Check that the composition of ClientModule and AbstractServerModule is safe.
test testcase0 [main=TestDriver0]: (union { TestDriver0 }, 
    (assert ReqIdsAreMonotonicallyIncreasing, RespIdsAreMonotonicallyIncreasing in (union ClientModule, AbstractServerModule)));


test testcase1 [main=TestDriver0]: (union { TestDriver0 },
    (assert ReqIdsAreMonotonicallyIncreasing, RespIdsAreMonotonicallyIncreasing in (union ClientModule, ServerModule)));