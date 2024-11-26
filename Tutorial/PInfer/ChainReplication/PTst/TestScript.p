test tc1C1K [main = SingleClientTest]:
    assert StrongConsistency in
    // assert StrongConsistency, ReadLiveness, WriteLiveness in
    (union Client, ChainRepEnv, {SingleClientTest}, FailureInjector);

test tc1C3K [main = SingleClientMultipleKeys]:
    assert StrongConsistency in
    // assert StrongConsistency, ReadLiveness, WriteLiveness in
    (union Client, ChainRepEnv, {SingleClientMultipleKeys}, FailureInjector);

test tc3C1K [main = MultipleClientSingleKey]:
    assert StrongConsistency in
    // assert StrongConsistency, ReadLiveness, WriteLiveness in
    (union Client, ChainRepEnv, {MultipleClientSingleKey}, FailureInjector);

test tc3C3K [main = MultipleClientMultipleKeys]:
    assert StrongConsistency in
    // assert StrongConsistency, ReadLiveness, WriteLiveness in
    (union Client, ChainRepEnv, {MultipleClientMultipleKeys}, FailureInjector);