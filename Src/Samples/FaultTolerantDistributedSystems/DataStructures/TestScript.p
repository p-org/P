module List { ListMachine }
module HashSet { HashSetMachine }
module TestDriver1 { TestDriver1 }
module DSClient { DSClientMachine }
module LinearAbs { LinearizabilityAbs }


module DSClientAndList = 
(compose DSClient, List, LinearAbs);

module DSClientAndHashSet = 
(compose DSClient, List, LinearAbs);

// Test 0: Test that the DSClientAndList is safe
test Test0: (rename TestDriver1 to Main in (compose TestDriver1, DSClientAndList));

// Test 1: Test that the DSClientAndHashSet is safe
test Test1: (rename TestDriver1 to Main in (compose TestDriver1, DSClientAndHashSet));

// Test 2: Check if the client of list performs legal operation.
// This check will fail as the client implemented performs random operations
test Test2: (rename TestDriver1 to Main in (compose TestDriver1, (assert ListClientSpec in DSClientAndList)));