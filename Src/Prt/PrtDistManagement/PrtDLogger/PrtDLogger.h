#ifdef __cplusplus
#include<string>
#include<fstream>
using namespace std;
#else 
#include<string.h>
#include"Macros.h"
#include <sal.h>
#endif
#include "../../Prt/PrtHeaders.h"


#ifdef __cplusplus
extern "C" {
#endif

typedef enum _PRTD_COMPONENT PRTD_COMPONENT;

enum _PRTD_COMPONENT {
	PRTD_DEPLOYER,
	PRTD_SERVICE,
	PRTD_NODEMANAGER,
	PRTD_CENTRALSERVER,
	PRTD_MAINMACHINE
};

	

//Functions
VOID PrtDCreateLogFile(PRTD_COMPONENT pComponent);
VOID PrtDCloseLogFile();
VOID PrtDLog(PRTD_COMPONENT op, char* log);

#ifdef __cplusplus
}
#endif