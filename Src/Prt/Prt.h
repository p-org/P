/**
* \file Prt.h
* \brief The header file for PRT API.
*/
#ifndef PRT_H
#define PRT_H

#include "PrtProgram.h"

/** The state of a machine instance */
typedef struct PRT_MACHINE_INST
{
	PRT_PROGRAM *program;
	PRT_UINT32  instIndex;
	PRT_VALUE   *varValues;
	PRT_UINT32  crntStateIndex;
} PRT_MACHINE_INST;

/**
* Starts the P runtime. Must be called once per process and before any other calls to the runtime API.
* @param[in] param Configuration-specific startup data.
* @see PrtSpecialStartup
* @see PrtSpecialShutdown
* @see PrtShutdown
*/
void PrtStartup(_In_opt_ void *param);

/**
* Stops the P runtime. Must be called once per process, after PrtStartup(). No runtime API calls are allowed after PrtShutdown().
* @param[in] param Configuration-specific shutdown data.
* @see PrtSpecialStartup
* @see PrtSpecialShutdown
* @see PrtStartup
*/
void PrtShutdown(_In_opt_ void *param);

#endif