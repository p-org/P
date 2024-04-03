## Generating C Program

P programs can be compiled to their C equivalents. Compilation to C can be performed by specifying the target language to the P compiler as follow.

```bash
pc -generate:C myProgram.p
```

Executing this command generates two files in the current directory: `myProgram.h` and `myProgram.c`, which are the C equivalent of the P program.

## Compiling Generated C Program

`myProgram.h` and `myProgram.c` only contain C representations of the constructs defined in the P program; therefore, a driver C program defining the main function and instantiating one of the machines is needed to start the execution. Below, an example driver program is presented.

```c
#include "myProgram.h"
#include "Prt.h"
#include <stdio.h>

PRT_PROCESS* MAIN_P_PROCESS;
static PRT_BOOLEAN cooperative = PRT_TRUE;
static int threads = 2;
long threadsRunning = 0;
pthread_mutex_t threadsRunning_mutex;

static const char* parg = NULL;

void ErrorHandler(PRT_STATUS status, PRT_MACHINEINST* ptr) {
	if (status == PRT_STATUS_ASSERT) {
		fprintf_s(stdout, "exiting with PRT_STATUS_ASSERT (assertion failure)\n");
		exit(1);
	} else if (status == PRT_STATUS_EVENT_OVERFLOW) {
		fprintf_s(stdout, "exiting with PRT_STATUS_EVENT_OVERFLOW\n");
		exit(1);
	} else if (status == PRT_STATUS_EVENT_UNHANDLED) {
		fprintf_s(stdout, "exiting with PRT_STATUS_EVENT_UNHANDLED\n");
		exit(1);
	} else if (status == PRT_STATUS_QUEUE_OVERFLOW) {
		fprintf_s(stdout, "exiting with PRT_STATUS_QUEUE_OVERFLOW \n");
		exit(1);
	} else if (status == PRT_STATUS_ILLEGAL_SEND) {
		fprintf_s(stdout, "exiting with PRT_STATUS_ILLEGAL_SEND \n");
		exit(1);
	} else {
		fprintf_s(stdout, "unexpected PRT_STATUS in ErrorHandler: %d\n", status);
		exit(2);
	}
}

void Log(PRT_STEP step, PRT_MACHINESTATE *senderState, PRT_MACHINEINST *receiver, PRT_VALUE* event, PRT_VALUE* payload) {
	PrtPrintStep(step, senderState, receiver, event, payload);
}

void decrement_threadsRunning() {
    pthread_mutex_lock(&threadsRunning_mutex);
    threadsRunning = threadsRunning - 1;
    pthread_mutex_unlock(&threadsRunning_mutex);
}

long get_threadsRunning() {
    long c;
    pthread_mutex_lock(&threadsRunning_mutex);
    c = threadsRunning;
    pthread_mutex_unlock(&threadsRunning_mutex);
    return (c);
}

void PRT_CALL_CONV MyAssert(PRT_INT32 condition, PRT_CSTRING message) {
    if (condition != 0) {
        return;
    } else if (message == NULL) {
        fprintf_s(stderr, "ASSERT");
    } else {
        fprintf_s(stderr, "ASSERT: %s", message);
    }
    exit(1);
}

static void RunToIdle(void* process) {
    // In the tester we run the state machines until there is no more work to do then we exit
    // instead of blocking indefinitely.  This is then equivalent of the non-cooperative case
    // where we PrtRunStateMachine once (inside PrtMkMachine).  So we do NOT call PrtWaitForWork.
    // PrtWaitForWork((PRT_PROCESS*)process);
    PRT_PROCESS_PRIV* privateProcess = (PRT_PROCESS_PRIV*)process;
    while (privateProcess->terminating == PRT_FALSE) {
    	;
    }
    decrement_threadsRunning();
}

int main(int argc, char *argv[]) {
    PRT_DBG_START_MEM_BALANCED_REGION
    {
        PRT_GUID processGuid;
        PRT_VALUE *payload;

        processGuid.data1 = 1;
        processGuid.data2 = 0;
        processGuid.data3 = 0;
        processGuid.data4 = 0;
        MAIN_P_PROCESS = PrtStartProcess(processGuid, &P_GEND_IMPL_DefaultImpl, ErrorHandler, Log);

        if (cooperative) {
            PrtSetSchedulingPolicy(MAIN_P_PROCESS, PRT_SCHEDULINGPOLICY_COOPERATIVE);
        }
        if (parg == NULL) {
            payload = PrtMkNullValue();
        } else {
            int i = atoi(parg);
            payload = PrtMkIntValue(i);
        }

        PrtUpdateAssertFn(MyAssert);
        PRT_UINT32 machineId;
        PRT_BOOLEAN foundMainMachine = PrtLookupMachineByName("myMachine", &machineId);

        if (foundMainMachine == PRT_FALSE) {
            printf("%s\n", "FAILED TO FIND DroneMachine");
            exit(1);
        }

        PrtMkMachine(MAIN_P_PROCESS, machineId, 1, &payload);

        if (cooperative) {
            typedef void *(*start_routine) (void *);
            pthread_t tid[threads];
            for (int i = 0; i < threads; i++)
            {
                threadsRunning++;
                pthread_create(&tid[i], NULL, (start_routine)RunToIdle, (void*)MAIN_P_PROCESS);
            }
            while(get_threadsRunning() != 0);

        }
        PrtFreeValue(payload);
        PrtStopProcess(MAIN_P_PROCESS);
    }
    PRT_DBG_END_MEM_BALANCED_REGION
}
```

One needs to notice couple of details of the given C program.

* The program includes `myProgram.h` as it is calling constructs defined in the C equivalent of the program.
* Global variables defined at the begining of the program are parameters that are used for different configuration of the program.
* `ErrorHandler` is a function defined to provide an interface for the runtime errors.
* `Log` is a function defined to print the steps taken by the runtime backend.
* `decrement_threadsRunning` and `get_threadsRunning` are helper functions used for multi-threaded execution.
* `MyAssert` is  function defined to provide an interface for the runtime assertions.
* `RunToIdle` is a function defined to run a given process until termination.
* `main` starts the execution by using all the functions and parameters defined previously. First, it starts the main P process by calling `PrtStartProcess`, which returns a `PRT_PROCESS` pointer. Next, if the scheduling policy is set to cooperative by the corresponding global variable, then the main function sets the scheduling policy to cooperative. Then, by using the method `PrtLookupMachineByName`, we look for a machine defined in the program by its name and get its id. With the id we create the machine by calling `PrtMkMachine`. Notice that if cooperative scheduling is disabled, i.e., task neutral policy is selected, then the execution starts immediately after creating the machine. If cooperative scheduling is enabled, one can either call `PrtRunProcess` with `MAIN_P_PROCESS`, i.e., `PrtRunProcess(MAIN_P_PROCESS)`, which will run the program with cooperative scheduling using only one thread, or run the while loop given in the program. The while loop given in the program distributes the work among the specified number of worker threads. In this case, the program will not exit until all worker threads are done.

Now that we have a driver C program, we need to compile the generated C program to an executable. To compile and execute the generated C program with respect to P semantics, we need to link P runtime backend with the C representation of the program and the driver C program during compilation. We recommend using CMake for this purpose, an example `CMakeLists.txt` is given below.

```cmake
cmake_minimum_required (VERSION 3.1)

project (myProgram)
set(projectName myProgram)

find_package (Threads)

include_directories(
    /path/to/P/Bld/Drops/Prt/include
)

add_definitions( -DPRT_PLAT_LINUXUSER )

add_executable(myProgram
    myProgram.h
    myProgram.c
    main.c
    /path/to/P/Bld/Drops/Prt/include/ext_compat.h
    /path/to/P/Bld/Drops/Prt/include/libhandler.h
    /path/to/P/Bld/Drops/Prt/include/libhandler-internal.h
    /path/to/P/Bld/Drops/Prt/include/Prt.h
    /path/to/P/Bld/Drops/Prt/include/PrtConfig.h
    /path/to/P/Bld/Drops/Prt/include/PrtExecution.h
    /path/to/P/Bld/Drops/Prt/include/PrtLinuxUserConfig.h
    /path/to/P/Bld/Drops/Prt/include/PrtProgram.h
    /path/to/P/Bld/Drops/Prt/include/PrtTypes.h
    /path/to/P/Bld/Drops/Prt/include/PrtValues.h
    /path/to/P/Bld/Drops/Prt/include/sal.h
)

target_link_libraries(myProgram
    /path/to/P/Bld/Drops/Prt/lib/libPrt_static.a
    /path/to/P/Ext/libhandler/out/gcc-amd64-apple-darwin20.6.0/debug/libhandler.a # If you are not using MacOS, you need to change gcc-amd64-apple-darwin20.6.0 accordingly!
)
```

This file links all necessary P runtime backend libraries with the project. To compile, run the following.

```bash
cmake CMakeLists.txt
make
```

After running these commands, the executable `myProgram` will be in the current directory, which can be executed simply as follows.

```bash
./myProgram
```

One can extend this project structure, and build more complicated applications.