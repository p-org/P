//(0, 0): No Main Machine
//(8, 1): no start state in machine
//(16, 4): invalid assignment. right hand side is not a subtype of left hand side
//(16, 10): Bad field name
//(16, 12): Operator expected first argument to be an integer value
//(16, 16): Bad field name

machine Node {
  var x : (a: int, b: bool);
  var y : (int, bool);
  var z : int;
  var b : bool;
  
	fun bar () {
	  z = x.a + x.0;	
	  //z = x.2 + y.hello;	  
	  pop;
	}
}
