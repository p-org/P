machine Main {
    var s1: seq[int];
    var s2: seq[any];
    var s3: seq[bool];
    var a: int;
	
    start state S
    {
    entry
       {
          s1 = {| 1 , 2 , 3 |};
          assert (s1[0] == 1);
          assert (s1[1] == 2);
          assert (s1[2] == 3);

          s2 = {| true, false, true |};
          assert (s2[0] as bool);
          assert (!(s2[1] as bool));
          assert (s2[2] as bool);

          s1 = {| a, a, a |};
          assert (s1[0] == 0);
          assert (s1[1] == 0);
          assert (s1[2] == 0);

          s3 = {||} as seq[bool];
       }
    }
}
