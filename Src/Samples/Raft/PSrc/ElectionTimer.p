// machine ElectionTimer
// {
//     var Target: machine;

//     start state Init
//     {
//         on EConfigureEvent do (payload: machine) {
//             Configure(payload);
//         }
//         on EStartTimer goto Active;
//     }

//     fun Configure(payload: machine)
//     {
//         Target = payload;
//     }

//     state Active
//     {
//         entry
//         {
//             send this, ETickEvent;
//         }

//         on ETickEvent do Tick;
//         on ECancelTimer goto Inactive;
//         defer EStartTimer;
//     }

//     fun Tick()
//     {
//         if ($)
//         {
//             //this.Logger.WriteLine("\n [ElectionTimer] " + this.Target + " | timed out\n");
//             print "[ElectionTimer] {0} | timed out", Target;
//             send Target, ETimeout;
//         } 
//         raise ECancelTimer;
//     }

//     state Inactive
//     {
//         on EStartTimer goto Active;
//         defer ECancelTimer, ETickEvent;
//     }
// }


