enum EqEnum {
    YES, NO
}
type LogIndexPair = (log: seq[tServerLog], idx: LogIndex);

// hint exact LeaderComplete(e0: eBecomeLeader, e1: eBecomeLeader) {
//     fun getLogIndexPair(e: tBecomeLeader): LogIndexPair {
//         return (log=e.log, idx=e.commitIndex);
//     }
//     fun logContains(p1: LogIndexPair, p2: LogIndexPair): EqEnum {
//         var i: LogIndex;
//         var j: LogIndex;
//         var found: bool;
//         i = 0;
//         while (i < p1.idx) {
//             j = 0;
//             found = false;
//             while (j < sizeof(p2.log)) {
//                 if (p1.log[i] == p2.log[j]) {
//                     found = true;
//                     break;
//                 }
//                 j = j + 1;
//             }
//             if (!found) return NO;
//             i = i + 1;
//         }
//         return YES;
//     }
//     include_guards = e0.term < e1.term;
//     term_depth = 2;
//     arity = 1;
// }

// hint exact LogMatching (e0: eNotifyLog, e1: eNotifyLog) {
//     fun logMatching(e0: (timestamp: tTS, server: Server, log: seq[tServerLog]), e1: (timestamp: tTS, server: Server, log: seq[tServerLog])): EqEnum {
//         var n: LogIndex;
//         var i: LogIndex;
//         if (sizeof(e0.log) < sizeof(e1.log)) {
//             n = sizeof(e0.log);
//         } else {
//             n = sizeof(e1.log);
//         }
//         i = n - 1;
//         while (i >= 0 && e0.log[i] != e1.log[i]) {
//             i = i - 1;
//         }
//         while (i >= 0) {
//             if (e0.log[i] != e1.log[i]) {
//                 return NO;
//             }
//             i = i - 1;
//         }
//         return YES;
//     }
//     num_guards = 0;
//     exists = 0;
//     arity = 1;
//     term_depth = 1;
// }