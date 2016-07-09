/**
* \file Prt.h
* \brief The main interface to the runtime
* Use these methods to start and stop instances of the runtime for a given P program.
*/
#ifndef PRT_H
#define PRT_H

#include "PrtProgram.h"

#ifdef __cplusplus
extern "C"{
#endif

    /** The kinds of steps a state machine can perform. Used for logging.
    *   @see PrtMkMachine
    */
    typedef enum PRT_STEP
    {
        PRT_STEP_CREATE = 0,      /**< Occurs when a machine is created.                            */
        PRT_STEP_DEQUEUE = 1,     /**< Occurs when an event is dequeued.                            */
        PRT_STEP_DO = 2,          /**< Occurs when an do handler is executed.                       */
        PRT_STEP_ENQUEUE = 3,     /**< Occurs when an event is enqueued.                            */
        PRT_STEP_ENTRY = 4,       /**< Occurs when an entry function is invoked.                    */
        PRT_STEP_EXIT = 5,        /**< Occurs when an exit function is invoked.                     */
        PRT_STEP_HALT = 6,        /**< Occurs when a machine halts.                                 */
        PRT_STEP_POP = 7,         /**< Occurs when a state is popped.                               */
        PRT_STEP_PUSH = 8,        /**< Occurs when a state is pushed.                               */
        PRT_STEP_RAISE = 9,       /**< Occurs when an event is raised.                              */
        PRT_STEP_IGNORE = 10,	  /**< Occurs when an event is ignored                              */
        PRT_STEP_UNHANDLED = 11,  /**< Occurs when an event is unhandled.                           */
        PRT_STEP_COUNT = 12,      /**< The number of valid step members.                            */
    } PRT_STEP;

    /** Status codes for normal and error conditions. Used for error reporting and indicating success/failure of API operations.
    *   An error status is in the exclusive range (PRT_STATUS_SUCCESS, PRT_STATUS_COUNT).
    *   @see PrtMkMachine
    */
    typedef enum PRT_STATUS
    {
        PRT_STATUS_SUCCESS = 0,        /**< Indicates normal completion of operation. Any status greater than SUCCESS is an error. */
        PRT_STATUS_ASSERT = 1,        /**< Indicates an assertion failure.                               */
        PRT_STATUS_EVENT_OVERFLOW = 2,  /**< Indicates too many occurrences of the same event in a queue.  */
        PRT_STATUS_EVENT_UNHANDLED = 3,  /**< Indicates failure of a machine to handle an event.            */
        PRT_STATUS_QUEUE_OVERFLOW = 4,   /**< Indicates that a queue has grown too large.                   */
        PRT_STATUS_ILLEGAL_SEND = 5,	 /**< Indicates illegal use of send primitive for sending message across process */
        PRT_STATUS_COUNT = 6,            /**< The valid number of status codes.                             */
    } PRT_STATUS;

    /** Represents a running P program. Every process has a GUID and client is responsible
    *   for ensuring that any communicating set of processes have unique GUIDs. Processes
    *   that never communicate may have the same GUIDs.
    *   @see PrtStartProcess
    *   @see PrtStopProcess
    */
    typedef struct PRT_PROCESS {
        PRT_GUID         guid;     /**< The unique ID for this process. Cannot be 0-0-0-0. */
        PRT_PROGRAMDECL *program;  /**< The program running in this process.               */
    } PRT_PROCESS;

    /** The state of running machine in a process.
    *   @see PrtMkMachine
    */
    typedef struct PRT_MACHINEINST
    {
        PRT_PROCESS		    *process;     /**< The process that owns this machine.             */
        PRT_UINT32			instanceOf;   /**< Index of machine type in PRT_PROGRAMDECL.       */
        PRT_VALUE			*id;          /**< The id of this machine.                         */
        void				*extContext;  /**< Pointer to an external context owned by client. */
        PRT_BOOLEAN			isModel;	  /**< Indicates whether this is a model machine. */
    } PRT_MACHINEINST;

    /** The scheduling policy determines how the state machine is executed.  
    * On the caller's thread or on a separate thread.
    *   @see PrtSetSchedulingPolicy
    */
    typedef enum PRT_SCHEDULINGPOLICY
    {
        PRT_SCHEDULINGPOLICY_TASKNEUTRAL,   /**< The default policy is task neutral, meaning the caller's thread is used to run the state machine */
        PRT_SCHEDULINGPOLICY_COOPERATIVE    /**< This policy means the caller plans to advance the state machine from a separate thread using PrtRunProcess */
    } PRT_SCHEDULINGPOLICY;


    /** An error function that will be called whenever an error arises. */
    typedef void(PRT_CALL_CONV * PRT_ERROR_FUN)(PRT_STATUS, PRT_MACHINEINST *);

    /** A log function that will be called whenever a step occurs. If an event is the reason, then sender, eventId and payload are also provided */
    typedef void(PRT_CALL_CONV * PRT_LOG_FUN)(PRT_STEP step, PRT_MACHINEINST *sender, PRT_MACHINEINST *receiver, PRT_VALUE *eventid, PRT_VALUE *payload);

    /** Starts a new Process running program.
    *   @param[in] guid Id for process; client must guarantee uniqueness for processes that may communicate. Cannot be 0-0-0-0.
    *   @param[in] program Program to run (not cloned). Client must free. Client cannot free or modify before calling PrtStopProcess.
    *   @param[in] errorFun  The error function to call if an error status occurrs. Must be thread-safe. If NULL, then no error reporting.
    *   @param[in] loggerFun The logging function to call when a machine step occurrs. Must be thread-safe. If NULL, then no logging.
    *   @returns A pointer to a new process. Client must free with PrtStopProcess
    *   @see PRT_PROCESS
    *   @see PrtStopProcess
    */
    PRT_API PRT_PROCESS * PRT_CALL_CONV PrtStartProcess(
        _In_ PRT_GUID guid,
        _In_ PRT_PROGRAMDECL *program,
        _In_ PRT_ERROR_FUN errorFun,
        _In_ PRT_LOG_FUN loggerFun
        );

    /** Set the scheduling policy for this process.  The default policy is TaskNeutral
    *   @param[in] policy The new policy.
    *   @see PRT_PROCESS
    *   @see PRT_SCHEDULINGPOLICY
    *   @see PrtStartProcess
    */
    PRT_API void PRT_CALL_CONV PrtSetSchedulingPolicy(_In_ PRT_PROCESS *process, _In_ PRT_SCHEDULINGPOLICY policy);

    /** Call this method if you set PRT_SCHEDULINGPOLICY to Cooperative.  This means the caller wants to control which thread
    *   runs the state machine, where this thread will block when there is no work to do, and it will automatically wake up
    *   via a semaphore when there is work to do.  It will terminate when you call PrtStopProcess.  You must then ensure you
    *   do not call PrtRunProcess after PrtStopProcess because the process will be deleted at that point.
    *   @param[in] process The process defines which state machines this method will run.
    *   @see PRT_SCHEDULINGPOLICY
    *   @see PrtSetSchedulingPolicy
    */
    PRT_API void PRT_CALL_CONV PrtRunProcess(PRT_PROCESS *process);


    typedef enum PRT_STEP_RESULT
    {
        PRT_STEP_IDLE = 0,         /**< No more work */
        PRT_STEP_MORE = 1,         /**< More work is available */
        PRT_STEP_TERMINATING = 2,  /**< We are terminating the process  */
    } PRT_STEP_RESULT;


    /** Call this method if you set PRT_SCHEDULINGPOLICY to Cooperative.  This means the caller wants to control which thread
    *   runs the state machine. PrtStepProcess does one step and returns so the caller can also yield 
    *   the thread, this is how this method is different from PrtRunProcess.   It returns PRT_FALSE if there is no work to do, 
    *   at which time you should call PrtWaitForWork.  It will terminate if you call PrtStopProcess.  
    *   @param[in] process The process defines which state machines this method will run.
    *   @see PRT_SCHEDULINGPOLICY
    *   @see PrtSetSchedulingPolicy
    */
    PRT_API PRT_STEP_RESULT PRT_CALL_CONV PrtStepProcess(PRT_PROCESS *process);


    /** Call this method when PrtStepProcess returns PRT_STEP_IDLE.  This means PrtStepProcess has found that all machines
    * are waiting for work.  This method will block on a semaphore until more work becomes available.  It will also return
    * if you call PrtStopProcess.
    *   @param[in] process The process defines which state machines this method will run.
    *   @returns   PRT_TRUE if we are terminating (PrtStopProcess has been called).
    *   @see PrtStepProcess
    */
    PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtWaitForWork(PRT_PROCESS *process);

    /** Stops a started process. Reclaims all resources allocated to the process.
    *   Client must call exactly once for each started process. Once called,
    *   no other API function affecting this process can occur from any thread.
    *   Once called, no interaction with data owned by this process should occur from any thread.
    *  This method also causes PrtRunProcess and PrtWaitForWork to terminate.
    *   @param[in,out] process The process to stop.
    *   @see PRT_PROCESS
    *   @see PrtStartProcess
    */
    PRT_API void PRT_CALL_CONV PrtStopProcess(_Inout_ PRT_PROCESS* process);

    /** Creates a new machine instance in remote container process. Will be freed when container process is stopped.
    * @param[in,out] process    The process that will own this machine.
    * @param[in]     instanceOf An index of a machine type in process' program.
    * @param[in]     payload The payload to pass to the start state of machine instance (cloned, user frees).
    * @param[in]	 pointer to the container machine where the state-machine should be created
    * @returns       A pointer to a PRT_MACHINEINST.
    * @see PrtSend
    * @see PRT_MACHINEINST
    */
    PRT_MACHINEINST * PRT_CALL_CONV PrtMkMachineRemote(
        _Inout_ PRT_PROCESS *process,
        _In_ PRT_UINT32 instanceOf,
        _In_ PRT_VALUE *payload,
        _In_ PRT_VALUE* container);

    /** Creates a new machine instance in process. Will be freed when process is stopped.
    * @param[in,out] process    The process that will own this machine.
    * @param[in]     instanceOf An index of a machine type in process' program.
    * @param[in]     payload The payload to pass to the start state of machine instance (cloned, user frees).
    * @returns       A pointer to a PRT_MACHINEINST.
    * @see PrtSend
    * @see PRT_MACHINEINST
    */
    PRT_API PRT_MACHINEINST * PRT_CALL_CONV PrtMkMachine(
        _Inout_ PRT_PROCESS *process,
        _In_ PRT_UINT32 instanceOf,
        _In_ PRT_VALUE *payload);

    /** Creates a new model machine instance in process. Will be freed when process is stopped.
    * @param[in,out] process    The process that will own this machine.
    * @param[in]     instanceOf An index of a machine type in process' program.
    * @param[in]     payload The payload to pass to the start state of machine instance (cloned, user frees).
    * @returns       A pointer to a PRT_MACHINEINST.
    * @see PrtSend
    * @see PRT_MACHINEINST
    */
    PRT_API PRT_MACHINEINST * PRT_CALL_CONV PrtMkModel(
        _Inout_ PRT_PROCESS *process,
        _In_ PRT_UINT32 instanceOf,
        _In_ PRT_VALUE *payload);

    /** Gets machine instance corresponding to id in process.
    * @param[in] process    The process containing the machine id.
    * @returns       A pointer to a PRT_MACHINEINST or NULL if id is not valid for process.
    * @see PrtMkMachine
    * @see PRT_MACHINEINST
    */
    PRT_API PRT_MACHINEINST * PRT_CALL_CONV PrtGetMachine(
        _In_ PRT_PROCESS *process,
        _In_ PRT_VALUE *id);

    /** Sends message to P state machine.
    * @param[in,out] sender The machine that is sending this message.
    * @param[in,out] receiver The machine that will receive this message.
    * @param[in] event The event to send with this message (cloned, user frees).
    * @param[in] payload The payload to send with this message.
    * @param[in] doTransfer The callee is reponsible for freeing the payload iff doTransfer is true.
    */
    PRT_API void PRT_CALL_CONV PrtSend(
		_Inout_ PRT_MACHINEINST *sender,
        _Inout_ PRT_MACHINEINST *receiver,
        _In_ PRT_VALUE *evt,
        _In_ PRT_VALUE *payload,
        _In_ PRT_BOOLEAN doTransfer);

#ifdef __cplusplus
}
#endif
#endif
