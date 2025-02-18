test tcOneNode [main = OneNode]:
  union RingLeader, { OneNode };

test tcTwoNodes [main = TwoNodes]:
  union RingLeader, { TwoNodes };

test tcThreeNodes [main = ThreeNodes]:
  union RingLeader, { ThreeNodes };

test tcFiveNodes [main = FiveNodes]:
  union RingLeader, { FiveNodes };

test tcTenNodes [main = TenNodes]:
  union RingLeader, { TenNodes };

// hint exact Nominate (e1: eNominate) {
//   arity = 1;
//   exists = 0;
//   num_guards = 0;
// }