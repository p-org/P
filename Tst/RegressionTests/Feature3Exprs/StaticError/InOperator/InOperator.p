event E assert 1;
event E1 assert 1;
event E2 assert 1;
event E3 assert 1;

machine Main {
	var sequence: seq[int];
	var notASequence: int;
	
    start state S
    {
       entry
       {
		  sequence += (0, 1);
		  sequence += (1, 2);
		  assert(((1, 2) in sequence) == true); // errors: wrong data type, expected "int", but got "tuples" instead
		  assert((false in sequence) == true); // errors: wrong data type, expected "int", but got "bool" instead
		  notASequence = 6;
		  assert((5 in notASequence) == false); // errors: unsupported operation on data of type "int"
		  raise halt;
       }
    }
}