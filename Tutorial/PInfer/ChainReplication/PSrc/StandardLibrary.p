// relialbe broadcast function
fun ReliableBroadCast(ms: set[machine], ev: event, payload: any) {
  var i: int;
  while (i < sizeof(ms)) {
    send ms[i], ev, payload;
    i = i + 1;
  }
}

// unreliable send operation that drops messages on the ether nondeterministically
fun UnReliableSend(target: machine, message: event, payload: any) {
  // nondeterministically drop messages
  // $: choose()
  if($) send target, message, payload;
}

fun seqAppend2Tail(a: seq[any], b: any): seq[any] {
  var result : seq[any];
  result = a;
  // print format ("sizeof(result) = {0}, index = {1}", sizeof(result), sizeof(result) - 1);
  result += (sizeof(result), b);
  return result;
}

fun seqCons2Head(a: seq[any], b: any): seq[any] {
  var result : seq[any];
  result = a;
  result += (0, b);
  return result;
}

fun seqToSet(a: seq[any]) : set[any] {
  var i: int;
  var result: set[any];
  while (i < sizeof(a)) {
    result += (a[i]);
    i = i + 1;
  }
  return result;
}

fun seqGetPosition(a: seq[any], b: any): tPos {
  var i: tPos;
  var j: tPos;
  j = sizeof(a);
  i = 0;
  while (i < j) {
    if (a[i] == b) {
      return i;
    }
    i = i + 1;
  }
  return -1;
}

fun printSeq(name: string, a: seq[Replicate]) {
  var i: int;
  print format("printing {0}", name);
  while (i < sizeof(a)) {
    print format("{0}[{1}] = {2}", name, i, a[i]);
    i = i + 1;
  }
}

fun printSet(name: string, a: set[Replicate]) {
  var i: int;
  print format("printing {0}", name);
  while (i < sizeof(a)) {
    print format("{0}[{1}] = {2}", name, i, a[i]);
    i = i + 1;
  }
}