module Client {
  ClientMachine
}

module Server {
  ServerMachine,
  HelperMachine
}

module ServerAbs {
  ServerAbstraction
}

module TestDriver1 {
  TestDriver_1Client1Server
}

module TestDriver2 {
  TestDriver_Refinement
}
//test case 0: check that our abstraction is sound.
test testcase0: TestDriver2 || Server refines TestDriver2 || ServerAbs;

//test case 1: check that the client || serverAbs is safe (no local assertion failures).
test testcase1: TestDriver1 || Client || ServerAbs;

//test case 2: check that the client satisfies the spec of ReqIdsAreMonotonicallyIncreasing
test testcase2: TestDriver1 || (assert ReqIdsAreMonotonicallyIncreasing in Client) || ServerAbs




//final c code generation for the implementation.
//note that implementation module need not be closed.
implementation Client || Server;