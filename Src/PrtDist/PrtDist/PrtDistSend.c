#include "PrtDist.h"
#include "PrtDistIDL/PrtDistIDL_c.c"

// function to send the event
PRT_BOOLEAN PrtDistSend(
	PRT_VALUE* target,
	PRT_VALUE* event,
	PRT_VALUE* payload
	)
{
	handle_t handle;
	handle = PrtDistCreateRPCClient(target);
	PRT_VALUE* temp = PrtMkNullValue();
	PRT_VALUE* serial_target, *serial_event, *serial_payload;
	serial_target = PrtDistSerializeValue(target);
	serial_event = PrtDistSerializeValue(event);
	//PrtPrintValue(payload);
	serial_payload = PrtDistSerializeValue(payload);

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

		c_PrtDistSendEx(&Async, handle, serial_target, serial_event, serial_payload);
		//c_PrtDistSendEx(handle, serial_target, serial_event, serial_payload);
	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		PrtDistLog("Runtime reported exception in RPC");
		printf("Runtime reported exception 0x%lx = %ld\n", ulCode, ulCode);
		char log[100];
		_itoa(ulCode, log, 10);
		PrtDistLog(log);
		return PRT_FALSE;
	}
	RpcEndExcept

		return PRT_TRUE;
}