// User Defined Types
type tQueryId = (query_id: int);

type tFindStatusResult = (status: string);

type tFindErrorMessageResult = (error_message: string);

type tFindExecutionTimeResult = (execution_time: float);

type tCompareQueryPlanResult = (has_same_plan: bool);

type tGetReturnedRowsResult = (slow_query_row_count: int, fast_query_row_count: int);

type tGetTotalSpilledBlocksResult = (fast_query_blocks_spilled: int, slow_query_blocks_spilled: int);

type tCompareTotalSkewnessResult = (has_similar_skewness: bool);

type tGetQueueTimesResult = (slow_query_queue_time: int, fast_query_queue_time: int);

type tGetCompileTimesResult = (slow_query_compile_time: float, fast_query_compile_time: float);

type tGetPlanningTimesResult = (slow_query_planning_time: float, fast_query_planning_time: float);

type tGetLockWaitTimesResult = (slow_query_lock_wait_time: float, fast_query_lock_wait_time: float);

// Events
event eFindStatusRequest: tQueryId;
event eFindStatusResponse: tFindStatusResult;

event eFindErrorMessageRequest: tQueryId;
event eFindErrorMessageResponse: tFindErrorMessageResult;

event eFindExecutionTimeRequest: tQueryId;
event eFindExecutionTimeResponse: tFindExecutionTimeResult;

event eCheckIfSameQueryPlanRequest: tQueryId;
event eCheckIfSameQueryPlanResponse: tCompareQueryPlanResult;

event eGetReturnedRowsRequest: tQueryId;
event eGetReturnedRowsResponse: tGetReturnedRowsResult;

event eGetTotalSpilledBlocksRequest: tQueryId;
event eGetTotalSpilledBlocksResponse: tGetTotalSpilledBlocksResult;

event eCompareTotalSkewnessRequest: tQueryId;
event eCompareTotalSkewnessResponse: tCompareTotalSkewnessResult;

event eGetQueueTimesRequest: tQueryId;
event eGetQueueTimesResponse: tGetQueueTimesResult;

event eGetCompileTimesRequest: tQueryId;
event eGetCompileTimesResponse: tGetCompileTimesResult;

event eGetPlanningTimesRequest: tQueryId;
event eGetPlanningTimesResponse: tGetPlanningTimesResult;

event eGetLockWaitTimesRequest: tQueryId;
event eGetLockWaitTimesResponse: tGetLockWaitTimesResult;

event eFinishInvestigation;

// Specification to ensure actions are performed in the correct order according to SOP
spec QueryInvestigationSOP observes 
  eFindStatusRequest, eFindStatusResponse,
  eFindErrorMessageRequest, eFindErrorMessageResponse,
  eFindExecutionTimeRequest, eFindExecutionTimeResponse,
  eCheckIfSameQueryPlanRequest, eCheckIfSameQueryPlanResponse,
  eGetReturnedRowsRequest, eGetReturnedRowsResponse,
  eGetTotalSpilledBlocksRequest, eGetTotalSpilledBlocksResponse,
  eCompareTotalSkewnessRequest, eCompareTotalSkewnessResponse,
  eGetQueueTimesRequest, eGetQueueTimesResponse,
  eGetCompileTimesRequest, eGetCompileTimesResponse,
  eGetPlanningTimesRequest, eGetPlanningTimesResponse,
  eGetLockWaitTimesRequest, eGetLockWaitTimesResponse,
  eFinishInvestigation {

  var status: string;
  var execution_time: float;
  var has_same_plan: bool;
  var slow_query_row_count: int;
  var fast_query_row_count: int;
  var slow_query_blocks_spilled: int;
  var fast_query_blocks_spilled: int;
  var has_similar_skewness: bool;
  var slow_query_queue_time: int;
  var fast_query_queue_time: int;
  var slow_query_compile_time: float;
  var fast_query_compile_time: float;
  var slow_query_planning_time: float;
  var fast_query_planning_time: float;
  var slow_query_lock_wait_time: float;
  var fast_query_lock_wait_time: float;
  var current_context: string;

  // Start with Step 1: Find Status
  start state ExpectFindStatus {
    entry {
        current_context = "SOP Step 1: Investigation must start with finding the status of the slow query";
    }

    on eFindStatusRequest goto WaitingForFindStatusResponse;

    // Any other action at this point is invalid
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }

  state WaitingForFindStatusResponse {
    entry {
      current_context = "SOP Step 1: Waiting for status response before proceeding";
    }

    on eFindStatusResponse do (resp: tFindStatusResult) {
        status = resp.status;
    
        if (status == "aborted") {
            // Step 2: If status is Aborted, finish investigation
            current_context = "SOP Step 2: Status is aborted; investigation must be finished";
            goto ExpectFinishInvestigation;
        } else if (status == "failed") {
            // Step 3: If status is Failed, find error message
            current_context = "SOP Step 3: Status is failed; must find error message";
            goto ExpectFindErrorMessage;
        } else if (status == "canceled") {
            // Step 4: If status is Canceled, finish investigation
            current_context = "SOP Step 4: Status is canceled; investigation must be finished";
            goto ExpectFinishInvestigation;
        } else if (status == "success") {
            // Step 5: If status is success, find execution time
            current_context = "SOP Step 5: Status is success; must find execution time";
            goto ExpectFindExecutionTime;
        } else {
            // Invalid status, finish investigation
            current_context = "Status is unknown; investigation must be finished";
            goto ExpectFinishInvestigation;
        }
    }

    // Any other action while waiting for response is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }

  // Handle Step 3: Find error message if status is "failed"
  state ExpectFindErrorMessage {
    on eFindErrorMessageRequest goto WaitingForFindErrorMessageResponse;

    // Any other action at this point is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }

  state WaitingForFindErrorMessageResponse {
    entry {
      current_context = "SOP Step 3: Waiting for error message response for failed query before finishing investigation";
    }

    on eFindErrorMessageResponse do {
        current_context = "SOP Step 3: Error message received for failed query; must finish investigation";
        goto ExpectFinishInvestigation;
    }

    // Any other action while waiting for response is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }

  // Handle Step 5: Find execution time if status is "success"
  state ExpectFindExecutionTime {
    on eFindExecutionTimeRequest goto WaitingForFindExecutionTimeResponse;

    // Any other action at this point is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }

  state WaitingForFindExecutionTimeResponse {
    entry {
      current_context = "SOP Step 6: Waiting for execution time response to determine next action";
    }

    on eFindExecutionTimeResponse do (resp: tFindExecutionTimeResult) {
        execution_time = resp.execution_time;
        if (execution_time < 60.0) {
          current_context = "SOP Step 6: Execution time < 60 seconds; investigation must be finished";
          goto ExpectFinishInvestigation;
        } else {
          current_context = "SOP Step 7: Execution time >= 60 seconds; must check if query plans are the same";
          goto ExpectCheckIfSameQueryPlan;
        }
    }

    // Any other action while waiting for response is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }

  // Handle Step 6: Check if query plans are same if execution time >= 60
  state ExpectCheckIfSameQueryPlan {
    on eCheckIfSameQueryPlanRequest goto WaitingForCheckIfSameQueryPlanResponse;

    // Any other action at this point is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }

  state WaitingForCheckIfSameQueryPlanResponse {
    entry {
      current_context = "SOP Step 7: Waiting for query plan comparison results to determine next action";
    }

    on eCheckIfSameQueryPlanResponse do (resp: tCompareQueryPlanResult) {
        has_same_plan = resp.has_same_plan;
        if (!has_same_plan) {
        // Step 7: If query plans are not the same, finish investigation
        current_context = "SOP Step 7: Query plans are different; investigation must be finished";
        goto ExpectFinishInvestigation;
        } else {
        // Step 7: If query plans are the same, get returned rows
        current_context = "SOP Step 8: Query plans are the same; must check returned row counts";
        goto ExpectGetReturnedRows;
        };
    }

    // Any other action while waiting for response is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }


  // Handle Step 7: Get returned rows if query plans are same
  state ExpectGetReturnedRows {
    on eGetReturnedRowsRequest goto WaitingForGetReturnedRowsResponse;

    // Any other action at this point is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }

  state WaitingForGetReturnedRowsResponse {
    entry {
      current_context = "SOP Step 8: Waiting for row count comparison to determine next action";
    }

    on eGetReturnedRowsResponse do (resp: tGetReturnedRowsResult) {
        slow_query_row_count = resp.slow_query_row_count;
        fast_query_row_count = resp.fast_query_row_count;
        if (slow_query_row_count > fast_query_row_count) {
            // Step 8: If rows returned by slow query is more than fast query, finish investigation
            current_context = "SOP Step 8: Slow query returns more rows than fast query; investigation must be finished";
            goto ExpectFinishInvestigation;
        } else {
            // Step 8: If not, get total spilled blocks
            current_context = "SOP Step 9: Row counts are similar; must check total spilled blocks";
            goto ExpectGetTotalSpilledBlocks;
        }
    }

    // Any other action while waiting for response is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }


  // Handle Step 8: Get total spilled blocks if slow query rows <= fast query rows
  state ExpectGetTotalSpilledBlocks {
    on eGetTotalSpilledBlocksRequest goto WaitingForGetTotalSpilledBlocksResponse;

    // Any other action at this point is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }

  state WaitingForGetTotalSpilledBlocksResponse {
    entry {
      current_context = "SOP Step 9: Waiting for spilled blocks comparison to determine next action";
    }

    on eGetTotalSpilledBlocksResponse do (resp: tGetTotalSpilledBlocksResult) {
        slow_query_blocks_spilled = resp.slow_query_blocks_spilled;
        fast_query_blocks_spilled = resp.fast_query_blocks_spilled;
        if (slow_query_blocks_spilled > fast_query_blocks_spilled) {
            // Step 9: If total spilled blocks by slow query is more than fast query, finish investigation
            current_context = "SOP Step 9: Slow query spills more blocks than fast query; investigation must be finished";
            goto ExpectFinishInvestigation;
        } else {
            // Step 9: If not, compare total skewness
            current_context = "SOP Step 10: Spilled blocks are similar; must compare total skewness";
            goto ExpectCompareTotalSkewness;
        }
    }

    // Any other action while waiting for response is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }


  // Handle Step 9: Compare total skewness if slow query spilled blocks <= fast query spilled blocks
  state ExpectCompareTotalSkewness {
    on eCompareTotalSkewnessRequest goto WaitingForCompareTotalSkewnessResponse;

    // Any other action at this point is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }

  state WaitingForCompareTotalSkewnessResponse {
    entry {
      current_context = "SOP Step 10: Waiting for skewness comparison to determine next action";
    }

    on eCompareTotalSkewnessResponse do (resp: tCompareTotalSkewnessResult) {
        has_similar_skewness = resp.has_similar_skewness;
        if (!has_similar_skewness) {
            // Step 10: If skewness is different, finish investigation
            current_context = "SOP Step 10: Queries have different skewness; investigation must be finished";
            goto ExpectFinishInvestigation;
        } else {
        // Step 10: If not, get queue times
        current_context = "SOP Step 11: Skewness is similar; must check queue times";
        goto ExpectGetQueueTimes;
        }
    }

    // Any other action while waiting for response is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }


  // Handle Step 10: Get queue times if skewness is similar
  state ExpectGetQueueTimes {
    on eGetQueueTimesRequest goto WaitingForGetQueueTimesResponse;

    // Any other action at this point is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }

  state WaitingForGetQueueTimesResponse {
    entry {
      current_context = "SOP Step 11: Waiting for queue time comparison to determine next action";
    }

    on eGetQueueTimesResponse do (resp: tGetQueueTimesResult) {
        slow_query_queue_time = resp.slow_query_queue_time;
        fast_query_queue_time = resp.fast_query_queue_time;
        if (slow_query_queue_time > fast_query_queue_time) {
            // Step 11: If queue time for slow query is higher than fast query, finish investigation
            current_context = "SOP Step 11: Slow query has higher queue time than fast query; investigation must be finished";
            goto ExpectFinishInvestigation;
        } else {
            // Step 11: If not, get compile times
            current_context = "SOP Step 12: Queue times are similar; must check compile times";
            goto ExpectGetCompileTimes;
        }
    }

    // Any other action while waiting for response is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }


  // Handle Step 11: Get compile times if slow query queue time <= fast query queue time
  state ExpectGetCompileTimes {
    on eGetCompileTimesRequest goto WaitingForGetCompileTimesResponse;

    // Any other action at this point is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }

  state WaitingForGetCompileTimesResponse {
    entry {
      current_context = "SOP Step 12: Waiting for compile time comparison to determine next action";
    }

    on eGetCompileTimesResponse do (resp: tGetCompileTimesResult) {
        slow_query_compile_time = resp.slow_query_compile_time;
        fast_query_compile_time = resp.fast_query_compile_time;
        if (slow_query_compile_time > fast_query_compile_time) {
            // Step 12: If compile time for slow query is more than fast query, finish investigation
            current_context = "SOP Step 12: Slow query has higher compile time than fast query; investigation must be finished";
            goto ExpectFinishInvestigation;
        } else {
            // Step 12: If not, get planning times
            current_context = "SOP Step 13: Compile times are similar; must check planning times";
            goto ExpectGetPlanningTimes;
        }
    }

    // Any other action while waiting for response is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }


  // Handle Step 12: Get planning times if slow query compile time <= fast query compile time
  state ExpectGetPlanningTimes {
    on eGetPlanningTimesRequest goto WaitingForGetPlanningTimesResponse;

    // Any other action at this point is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }

  state WaitingForGetPlanningTimesResponse {
    entry {
      current_context = "SOP Step 13: Waiting for planning time comparison to determine next action";
    }

    on eGetPlanningTimesResponse do (resp: tGetPlanningTimesResult) {
        slow_query_planning_time = resp.slow_query_planning_time;
        fast_query_planning_time = resp.fast_query_planning_time;
        if (slow_query_planning_time > fast_query_planning_time) {
            // Step 13: If planning time for slow query is more than fast query, finish investigation
            current_context = "SOP Step 13: Slow query has higher planning time than fast query; investigation must be finished";
            goto ExpectFinishInvestigation;
        } else {
            // Step 13: If not, get lock wait times
            current_context = "SOP Step 14: Planning times are similar; must check lock wait times";
            goto ExpectGetLockWaitTimes;
        }
    }

    // Any other action while waiting for response is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }


  // Handle Step 13: Get lock wait times if slow query planning time <= fast query planning time
  state ExpectGetLockWaitTimes {
    on eGetLockWaitTimesRequest goto WaitingForGetLockWaitTimesResponse;

    // Any other action at this point is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }

  state WaitingForGetLockWaitTimesResponse {
    entry {
      current_context = "SOP Step 14: Waiting for lock wait time comparison";
    }

    on eGetLockWaitTimesResponse do (resp: tGetLockWaitTimesResult) {
        slow_query_lock_wait_time = resp.slow_query_lock_wait_time;
        fast_query_lock_wait_time = resp.fast_query_lock_wait_time;

        if (slow_query_lock_wait_time > fast_query_lock_wait_time) {
          current_context = "SOP Step 14: Slow query has higher lock wait time; investigation must be finished";
        } else {
          current_context = "SOP Step 15: No significant differences found between queries; investigation must be finished";
        }

        // Step 14: Finish investigation in any case
        goto ExpectFinishInvestigation;
    }

    // Any other action while waiting for response is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
    on eFinishInvestigation do InvalidAction;
  }


  // Final state - expect to finish investigation
  state ExpectFinishInvestigation {
    on eFinishInvestigation goto ExpectFindStatus;

    // Any other action at this point is invalid
    on eFindStatusRequest do InvalidAction;
    on eFindErrorMessageRequest do InvalidAction;
    on eFindExecutionTimeRequest do InvalidAction;
    on eCheckIfSameQueryPlanRequest do InvalidAction;
    on eGetReturnedRowsRequest do InvalidAction;
    on eGetTotalSpilledBlocksRequest do InvalidAction;
    on eCompareTotalSkewnessRequest do InvalidAction;
    on eGetQueueTimesRequest do InvalidAction;
    on eGetCompileTimesRequest do InvalidAction;
    on eGetPlanningTimesRequest do InvalidAction;
    on eGetLockWaitTimesRequest do InvalidAction;
  }

  fun InvalidAction() {
    assert false, current_context + "; unexpected action received";  }
}
