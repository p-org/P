#include "CoffeeMachine.h"
#include <stdio.h>

PRT_VALUE *P_FUN_BeginHeating_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE **c)
{
    printf("P_FUN_BeginHeating_FOREIGN\n");
    return 0;
}

PRT_VALUE *P_FUN_CheckIsOpen_FOREIGN(PRT_MACHINEINST *context)
{
    printf("P_FUN_CheckIsOpen_FOREIGN\n");
    return PrtMkBoolValue(PRT_FALSE);
}

PRT_VALUE *P_FUN_DumpGrinds_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE **c)
{
    printf("P_FUN_DumpGrinds_FOREIGN\n");
    return 0;
}

PRT_VALUE *P_FUN_EmergencyStop_FOREIGN(PRT_MACHINEINST *context)
{
    printf("P_FUN_EmergencyStop_FOREIGN\n");
    return 0;
}

PRT_VALUE *P_FUN_GrindBeans_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE **c)
{
    printf("P_FUN_GrindBeans_FOREIGN\n");
    return 0;
}

PRT_VALUE *P_FUN_ShowError_FOREIGN(PRT_MACHINEINST *context)
{
    return 0;
}

PRT_VALUE *P_FUN_StartEspresso_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE **c)
{
    printf("P_FUN_StartEspresso_FOREIGN\n");
    return 0;
}

PRT_VALUE *P_FUN_StartSteamer_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE **c)
{
    printf("P_FUN_StartSteamer_FOREIGN\n");
    return 0;
}

PRT_VALUE *P_FUN_StopSteamer_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE **c)
{
    printf("P_FUN_StopSteamer_FOREIGN\n");
    return 0;
}