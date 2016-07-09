#include "ForeignStringType.h"

void ErrorHandler(PRT_STATUS status, PRT_MACHINEINST *ptr)
{
	if (status == PRT_STATUS_ASSERT)
	{
		fprintf_s(stdout, "exiting with PRT_STATUS_ASSERT (assertion failure)\n");
		exit(1);
	}
	else if (status == PRT_STATUS_EVENT_OVERFLOW)
	{
		fprintf_s(stdout, "exiting with PRT_STATUS_EVENT_OVERFLOW\n");
		exit(1);
	}
	else if (status == PRT_STATUS_EVENT_UNHANDLED)
	{
		fprintf_s(stdout, "exiting with PRT_STATUS_EVENT_UNHANDLED\n");
		exit(1);
	}
	else if (status == PRT_STATUS_QUEUE_OVERFLOW)
	{
		fprintf_s(stdout, "exiting with PRT_STATUS_QUEUE_OVERFLOW \n");
		exit(1);
	}
	else if (status == PRT_STATUS_ILLEGAL_SEND)
	{
		fprintf_s(stdout, "exiting with PRT_STATUS_ILLEGAL_SEND \n");
		exit(1);
	}
	else
	{
		fprintf_s(stdout, "unexpected PRT_STATUS in ErrorHandler: %d\n", status);
		exit(2);
	}
}

void Log(PRT_STEP step, PRT_MACHINEINST *sender, PRT_MACHINEINST *receiver, PRT_VALUE* event, PRT_VALUE* payload)
{
	PrtPrintStep(step, sender, receiver, event, payload);
}

extern PRT_UINT64 UserMkDefaultForeignValue(_In_ PRT_UINT16 typeTag);

extern PRT_UINT64 UserCloneForeignValue(_In_ PRT_UINT16 typeTag, _In_ PRT_UINT64 frgnVal);

extern void UserFreeForeignValue(_In_ PRT_UINT16 typeTag, _Inout_ PRT_UINT64 frgnVal);

extern PRT_UINT32 UserHashForeignValue(_In_ PRT_UINT16 typeTag, _In_ PRT_UINT64 frgnVal);

extern PRT_STRING UserToStringForeignValue(_In_ PRT_UINT16 typeTag, _In_ PRT_UINT64 frgnVal);

extern PRT_BOOLEAN UserIsEqualForeignValue(_In_ PRT_UINT16 typeTag1, _In_ PRT_UINT64 frgnVal1, _In_ PRT_UINT16 typeTag2, _In_ PRT_UINT64 frgnVal2);

int main(int argc, char *argv[])
{
	PRT_PROCESS *process;
	PRT_GUID processGuid;
	PRT_VALUE *payload;
	processGuid.data1 = 1;
	processGuid.data2 = 0;
	processGuid.data3 = 0;
	processGuid.data4 = 0;
	process = PrtStartProcess(processGuid, &P_GEND_PROGRAM, ErrorHandler, Log);
	payload = PrtMkNullValue();
	PrtMkMachine(process, _P_MACHINE_MAIN, payload);
	PrtFreeValue(payload);
	PrtStopProcess(process);
}