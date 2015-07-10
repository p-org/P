#include "PrtDist.h"
#include "PrtDistIDL_c.c"

extern handle_t
PrtDistCreateRPCClient(
PRT_VALUE* target
);

// function to send the event
PRT_BOOLEAN PrtDistSend(
	PRT_VALUE* source,
	PRT_VALUE* target,
	PRT_VALUE* event,
	PRT_VALUE* payload
	)
{
	handle_t handle;
	handle = PrtDistCreateRPCClient(target);
	PRT_VALUE* serial_target, *serial_event, *serial_payload, *serial_source;
	serial_target = PrtDistSerializeValue(target);
	serial_event = PrtDistSerializeValue(event);
	serial_payload = PrtDistSerializeValue(payload);
	serial_source = PrtDistSerializeValue(source);
	//initialize the asynchronous rpc
	RPC_ASYNC_STATE Async;
	RPC_STATUS status;

	// Initialize the handle.
	status = RpcAsyncInitializeHandle(&Async, sizeof(RPC_ASYNC_STATE));
	if (status)
	{
		// Code to handle the error goes here.
	}

	Async.UserInfo = NULL;
	Async.NotificationType = RpcNotificationTypeEvent;

	Async.u.hEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
	if (Async.u.hEvent == 0)
	{
		// Code to handle the error goes here.
	}

	RpcTryExcept
	{
		PRT_INT64 seqNum = InterlockedIncrement64(&sendMessageSeqNumber);
		c_PrtDistSendEx(&Async, handle, serial_source, seqNum, serial_target, serial_event, serial_payload);
		//c_PrtDistSendEx(handle, serial_target, serial_event, serial_payload);
	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		char log[MAX_LOG_SIZE];
		sprintf_s(log, MAX_LOG_SIZE, "Runtime reported RPC exception 0x%lx = %ld\n when executing function c_PrtDistSendEx", ulCode, ulCode);
		PrtDistLog(log);
		return PRT_FALSE;
	}
	RpcEndExcept
	return PRT_TRUE;
}