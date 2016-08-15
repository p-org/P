// INSERT for map Zinger error
// this test found bug in Zinger: wrong error message 
machine Main {
  var m: map[int,any];
  
  start state S
    {
       entry
       {
            m += (0,0);
			m += (0,1);     //Zing error: "key must not exist in map" (error message was the opposite before the fix)
			//m[0] = 1;     //Zing passes
       }
     }
 } 