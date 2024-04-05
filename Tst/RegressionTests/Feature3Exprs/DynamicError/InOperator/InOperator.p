event E assert 1;
event E1 assert 1;
event E2 assert 1;
event E3 assert 1;

machine Main {
	var sequence: seq[any];
	var dictionary: map[int, any];
	
    start state S
    {
       entry
       {
		  sequence += (0, 1);
		  sequence += (1, 2);
		  sequence += (2, (3, 4));
		  assert(((4, 5) in sequence) == true); // errors for failing assertions
		  assert(((3, 4) in sequence) == false); // errors for failing assertions
		  assert((0 in dictionary) == true); // errors for failing assertions
		  dictionary[0] = (1, 2);
		  assert((0 in dictionary) == false); // errors for failing assertions
		  raise halt;
       }
    }
}