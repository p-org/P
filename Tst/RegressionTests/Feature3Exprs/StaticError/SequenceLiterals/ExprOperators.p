machine Main {
    var s1 : seq[bool];

    start state S
    {
       entry
       {
          s1 = {| true, 1, 2 |}; //error: "got type int, expected bool"
		  raise halt;
       }
    }
}
