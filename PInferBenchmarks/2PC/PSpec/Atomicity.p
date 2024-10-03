// event: initialize the AtomicityInvariant spec monitor
event eMonitor_AtomicityInitialize: (numParticipants: int);

/**********************************
We would like to assert the atomicity property that:
if a transaction is committed by the coordinator then it was agreed on by all participants
***********************************/
spec AtomicityInvariant observes eWriteTransResp, ePrepareResp, eMonitor_AtomicityInitialize
{
  // a map from transaction id to a map from responses status to number of participants with that response
  var participantsResponse: map[int, map[tTransStatus, int]];
  var numParticipants: int;
  start state Init {
    on eMonitor_AtomicityInitialize goto WaitForEvents with (n: (numParticipants: int)) {
      numParticipants = n.numParticipants;
    }
  }

  state WaitForEvents {
    on ePrepareResp do (resp: tPrepareResp){
      var transId: int;
      transId = resp.transId;

      if(!(transId in participantsResponse))
      {
        participantsResponse[transId] = default(map[tTransStatus, int]);
        participantsResponse[transId][SUCCESS] = 0;
        participantsResponse[transId][ERROR] = 0;
      }
      participantsResponse[transId][resp.status] = participantsResponse[transId][resp.status] + 1;
    }

    on eWriteTransResp do (resp: tWriteTransResp) {
      assert (resp.transId in participantsResponse || resp.status == TIMEOUT),
      format ("Write transaction was responded to the client without receiving any responses from the participants!");

      if(resp.status == SUCCESS)
      {
        assert participantsResponse[resp.transId][SUCCESS] == numParticipants,
        format ("Write transaction was responded as committed before receiving success from all participants. ") +
        format ("participants sent success: {0}, participants sent error: {1}", participantsResponse[resp.transId][SUCCESS],
        participantsResponse[resp.transId][ERROR]);
      }
      else if(resp.status == ERROR)
      {
        assert participantsResponse[resp.transId][ERROR] > 0,
          format ("Write transaction {0} was responded as failed before receiving error from atleast one participant.", resp.transId) +
          format ("participants sent success: {0}, participants sent error: {1}", participantsResponse[resp.transId][SUCCESS],
            participantsResponse[resp.transId][ERROR]);
      }
      // remove the transaction information
      participantsResponse -= (resp.transId);
    }
  }
}

/**************************************************************************
Every received transaction from a client must be eventually responded back.
Note, the usage of hot and cold states.
***************************************************************************/
spec Progress observes eWriteTransReq, eWriteTransResp {
  var pendingTransactions: int;
  start state Init {
    on eWriteTransReq goto WaitForResponses with { pendingTransactions = pendingTransactions + 1; }
  }

  hot state WaitForResponses
  {
    on eWriteTransResp do {
      pendingTransactions = pendingTransactions - 1;
      if(pendingTransactions == 0)
      {
        goto AllTransactionsFinished;
      }
    }

    on eWriteTransReq do { pendingTransactions = pendingTransactions + 1; }
  }

  cold state AllTransactionsFinished {
    on eWriteTransReq goto WaitForResponses with { pendingTransactions = pendingTransactions + 1; }
  }
}



// Monitor for spec: ∀e1: eWriteTransResp :: (e1.payload.status == SUCCESS) -> ∃e2: ePrepareResp :: (e1.payload.transId == e2.payload.transId) ∧ (e2.payload.status == SUCCESS) ∧ (e1.payload.status == e2.payload.status) ∧ (_num_e_exists_ == numParticipants) ∧ (indexof(e2) < indexof(e1))
spec Atomicity_1 observes eMonitor_AtomicityInitialize, eWriteTransResp, ePrepareResp {
  fun checkGuardsImpl(e1: tWriteTransResp, e1_idx: int): bool {
      return (e1.status == SUCCESS);
  }
  fun checkFiltersImpl(e1: tWriteTransResp, e2: tPrepareResp, e1_idx: int, e2_idx: int): bool {
      return (e1.transId == e2.transId) && (e2.status == SUCCESS) && (e1.status == e2.status) && (e2_idx < e1_idx);
  }
  fun checkMetaFiltersImpl(_num_e_exists_: int, e1: tWriteTransResp, e2: tPrepareResp, e1_idx: int, e2_idx: int): bool {
      return (_num_e_exists_ == numParticipants);
  }
  fun checkGuards(e1_idx: int): bool {
      return checkGuardsImpl(Hist_eWriteTransResp[e1_idx].payload, Hist_eWriteTransResp[e1_idx].idx);
  }
  fun checkFilters(e1_idx: int, e2_idx: int): bool {
      return checkFiltersImpl(Hist_eWriteTransResp[e1_idx].payload, Hist_ePrepareResp[e2_idx].payload, Hist_eWriteTransResp[e1_idx].idx, Hist_ePrepareResp[e2_idx].idx);
  }
  fun checkMetaFilters(_num_e_exists_: int, e1_idx: int, e2_idx: int): bool {
      return checkMetaFiltersImpl(_num_e_exists_, Hist_eWriteTransResp[e1_idx].payload, Hist_ePrepareResp[e2_idx].payload, Hist_eWriteTransResp[e1_idx].idx, Hist_ePrepareResp[e2_idx].idx);
  }
  
  var numParticipants: int;
  var event_counter: int;
  var Hist_eWriteTransResp: seq[(payload: tWriteTransResp, idx: int)];
  var Pending: set[(Counter_e1: int)];
  var Hist_ePrepareResp: seq[(payload: tPrepareResp, idx: int)];
  start state Init {
      entry {
          numParticipants = default(int);
          event_counter = 0;
          Hist_eWriteTransResp = default(seq[(payload: tWriteTransResp, idx: int)]);
          Pending = default(set[(Counter_e1: int)]);
          Hist_ePrepareResp = default(seq[(payload: tPrepareResp, idx: int)]);
      }
      on eMonitor_AtomicityInitialize do (payload: (numParticipants:int)) {
          numParticipants = payload.numParticipants;
          goto Serving_Cold;
      }
  }
  cold state Serving_Cold {
      on eWriteTransResp do (payload: tWriteTransResp) {
          var Counter_e1: int;
          var Counter_e2: int;
          var exists: bool;
          var n_exists: int;
          var combo: (Counter_e1: int);
          Hist_eWriteTransResp += (sizeof(Hist_eWriteTransResp), (payload=payload, idx=event_counter));
          Counter_e1 = sizeof(Hist_eWriteTransResp) - 1;
          event_counter = event_counter + 1;
          if (checkGuards(Counter_e1)) {
              exists = false;
              n_exists = 0;
              Counter_e2 = 0;
              while (Counter_e2 < sizeof(Hist_ePrepareResp) && !exists) {
                  if (checkFilters(Counter_e1, Counter_e2)) {
                      n_exists = n_exists + 1;
                      if (checkMetaFilters(n_exists, Counter_e1, Counter_e2)) {
                          exists = true;
                          break;
                      }
                  }
                  Counter_e2 = Counter_e2 + 1;
              }
              if (!exists) {
                  Pending += ((Counter_e1=Counter_e1,));
              }
          }
          if (sizeof(Pending) > 0) {
              goto Serving_Hot;
          }
      }
      on ePrepareResp do (payload: tPrepareResp) {
          var exists: bool;
          var n_exists: int;
          var resolved: set[(Counter_e1: int)];
          var combo_iter: (Counter_e1: int);
          var Counter_e2: int;
          resolved = default(set[(Counter_e1: int)]);
          Hist_ePrepareResp += (sizeof(Hist_ePrepareResp), (payload=payload, idx=event_counter));
          event_counter = event_counter + 1;
          foreach (combo_iter in Pending) {
              exists = false;
              n_exists = 0;
              Counter_e2 = 0;
              while (Counter_e2 < sizeof(Hist_ePrepareResp) && !exists) {
                  if (checkFilters(combo_iter.Counter_e1, Counter_e2)) {
                      n_exists = n_exists + 1;
                      if (checkMetaFilters(n_exists, combo_iter.Counter_e1, Counter_e2)) {
                          exists = true;
                          resolved += (combo_iter);
                          break;
                      }
                  }
                  Counter_e2 = Counter_e2 + 1;
              }
          }
          foreach (combo_iter in resolved) {
              Pending -= (combo_iter);
          }
          if (sizeof(Pending) > 0) {
              goto Serving_Hot;
          }
      }
  }
  hot state Serving_Hot {
      on eWriteTransResp do (payload: tWriteTransResp) {
          var Counter_e1: int;
          var Counter_e2: int;
          var exists: bool;
          var n_exists: int;
          var combo: (Counter_e1: int);
          Hist_eWriteTransResp += (sizeof(Hist_eWriteTransResp), (payload=payload, idx=event_counter));
          Counter_e1 = sizeof(Hist_eWriteTransResp) - 1;
          event_counter = event_counter + 1;
          if (checkGuards(Counter_e1)) {
              exists = false;
              n_exists = 0;
              Counter_e2 = 0;
              while (Counter_e2 < sizeof(Hist_ePrepareResp) && !exists) {
                  if (checkFilters(Counter_e1, Counter_e2)) {
                      n_exists = n_exists + 1;
                      if (checkMetaFilters(n_exists, Counter_e1, Counter_e2)) {
                          exists = true;
                          break;
                      }
                  }
                  Counter_e2 = Counter_e2 + 1;
              }
              if (!exists) {
                  Pending += ((Counter_e1=Counter_e1,));
              }
          }
          if (sizeof(Pending) == 0) {
              goto Serving_Cold;
          }
      }
      on ePrepareResp do (payload: tPrepareResp) {
          var exists: bool;
          var n_exists: int;
          var resolved: set[(Counter_e1: int)];
          var combo_iter: (Counter_e1: int);
          var Counter_e2: int;
          resolved = default(set[(Counter_e1: int)]);
          Hist_ePrepareResp += (sizeof(Hist_ePrepareResp), (payload=payload, idx=event_counter));
          event_counter = event_counter + 1;
          foreach (combo_iter in Pending) {
              exists = false;
              n_exists = 0;
              Counter_e2 = 0;
              while (Counter_e2 < sizeof(Hist_ePrepareResp) && !exists) {
                  if (checkFilters(combo_iter.Counter_e1, Counter_e2)) {
                      n_exists = n_exists + 1;
                      if (checkMetaFilters(n_exists, combo_iter.Counter_e1, Counter_e2)) {
                          exists = true;
                          resolved += (combo_iter);
                          break;
                      }
                  }
                  Counter_e2 = Counter_e2 + 1;
              }
          }
          foreach (combo_iter in resolved) {
              Pending -= (combo_iter);
          }
          if (sizeof(Pending) == 0) {
              goto Serving_Cold;
          }
      }
  }
} // Atomicity

// Monitor for spec: ∀e1: eWriteTransResp ∀e2: ePrepareResp :: (e1.payload.transId == e2.payload.transId) ∧ (e1.payload.status == SUCCESS) -> (indexof(e2) < indexof(e1)) ∧ (e1.payload.status == e2.payload.status)
spec Atomicity_known_1 observes  eWriteTransResp, ePrepareResp {
  fun checkGuardsImpl(e1: tWriteTransResp, e2: tPrepareResp, e1_idx: int, e2_idx: int): bool {
      return (e1.transId == e2.transId) && (e1.status == SUCCESS);
  }
  fun checkFiltersImpl(e1: tWriteTransResp, e2: tPrepareResp, e1_idx: int, e2_idx: int): bool {
      return (e2_idx < e1_idx) && (e1.status == e2.status);
  }
  fun checkMetaFiltersImpl(_num_e_exists_: int, e1: tWriteTransResp, e2: tPrepareResp, e1_idx: int, e2_idx: int): bool {
      return true;
  }
  fun checkGuards(e1_idx: int, e2_idx: int): bool {
      return checkGuardsImpl(Hist_eWriteTransResp[e1_idx].payload, Hist_ePrepareResp[e2_idx].payload, Hist_eWriteTransResp[e1_idx].idx, Hist_ePrepareResp[e2_idx].idx);
  }
  fun checkFilters(e1_idx: int, e2_idx: int): bool {
      return checkFiltersImpl(Hist_eWriteTransResp[e1_idx].payload, Hist_ePrepareResp[e2_idx].payload, Hist_eWriteTransResp[e1_idx].idx, Hist_ePrepareResp[e2_idx].idx);
  }
  fun checkMetaFilters(_num_e_exists_: int, e1_idx: int, e2_idx: int): bool {
      return checkMetaFiltersImpl(_num_e_exists_, Hist_eWriteTransResp[e1_idx].payload, Hist_ePrepareResp[e2_idx].payload, Hist_eWriteTransResp[e1_idx].idx, Hist_ePrepareResp[e2_idx].idx);
  }
  
  var event_counter: int;
  var Hist_eWriteTransResp: seq[(payload: tWriteTransResp, idx: int)];
  var Hist_ePrepareResp: seq[(payload: tPrepareResp, idx: int)];
  var Pending: set[(Counter_e1: int, Counter_e2: int)];
  start state Init {
      entry {
          event_counter = 0;
          Hist_eWriteTransResp = default(seq[(payload: tWriteTransResp, idx: int)]);
          Hist_ePrepareResp = default(seq[(payload: tPrepareResp, idx: int)]);
          Pending = default(set[(Counter_e1: int, Counter_e2: int)]);
          goto Serving_Cold;
      }
  }
  cold state Serving_Cold {
      on eWriteTransResp do (payload: tWriteTransResp) {
          var Counter_e1: int;
          var Counter_e2: int;
          var exists: bool;
          var n_exists: int;
          var combo: (Counter_e1: int, Counter_e2: int);
          Hist_eWriteTransResp += (sizeof(Hist_eWriteTransResp), (payload=payload, idx=event_counter));
          Counter_e1 = sizeof(Hist_eWriteTransResp) - 1;
          event_counter = event_counter + 1;
          Counter_e2 = 0;
          while (Counter_e2 < sizeof(Hist_ePrepareResp)) {
              if (checkGuards(Counter_e1, Counter_e2)) {
                  assert checkFilters(Counter_e1, Counter_e2);
              }
              Counter_e2 = Counter_e2 + 1;
          }
      }
      on ePrepareResp do (payload: tPrepareResp) {
          var Counter_e1: int;
          var Counter_e2: int;
          var exists: bool;
          var n_exists: int;
          var combo: (Counter_e1: int, Counter_e2: int);
          Hist_ePrepareResp += (sizeof(Hist_ePrepareResp), (payload=payload, idx=event_counter));
          Counter_e2 = sizeof(Hist_ePrepareResp) - 1;
          event_counter = event_counter + 1;
          Counter_e1 = 0;
          while (Counter_e1 < sizeof(Hist_eWriteTransResp)) {
              if (checkGuards(Counter_e1, Counter_e2)) {
                  assert checkFilters(Counter_e1, Counter_e2);
              }
              Counter_e1 = Counter_e1 + 1;
          }
      }
  }
} // Atomicity_known
