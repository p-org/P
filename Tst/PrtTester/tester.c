#include "program.h"

void ErrorHandler(PRT_STATUS status, PRT_MACHINEINST *ptr) { exit(1); }

void Log(PRT_STEP step, PRT_MACHINEINST *context) { PrtPrintStep(step, context);  }

int main(int argc, char *argv[])
{
	PRT_DBG_START_MEM_BALANCED_REGION
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
	PRT_DBG_END_MEM_BALANCED_REGION
	//_CrtSetReportMode(_CRT_ERROR, _CRTDBG_MODE_DEBUG);
	//_CrtDumpMemoryLeaks();
}
