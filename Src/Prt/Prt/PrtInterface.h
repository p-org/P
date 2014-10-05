#ifndef PRTINTERFACE_H
#define PRTINTERFACE_H
#include "PrtHeaders.h"

/* Public structure for P state machines*/
typedef struct PRT_STATEMACHINE	PRT_STATEMACHINE;

/** Send message to P state machine.
* @param[in] machine : target machine to send message
* @param[in] event : event to be sent.
* @param[in] payload : payload to be send with 'even'.
* @see PrtEnqueueEvent
*/
VOID PrtSend (__in PRT_STATEMACHINE machine, __in PRT_VALUE *event, __in PRT_VALUE *payload);

/** Send message to P state machine.
* @param[in] process : p process, instance of a P program.
* @param[in] instanceOfMachine : instance of a statemachine in 'process'.
* @param[in] payload : payload to be used in the start state.
* @param[out] pSM : pointer to the StateMachine
* @see PrtEnqueueEvent
*/

PRT_STATUS PrtCreateMachine(__in PRT_PPROCESS *process, __in PRT_INT32 instanceOfMachine, __in PRT_VALUE *payload, __out PRT_STATEMACHINE *pSM);

	  
#endif

