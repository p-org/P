#pragma once
#include "SmfDepends.h"
#include "SmfPublicTypes.h"

//
// Event Decl (All events in the Program are of this type)
//
typedef struct _SMF_ARRAYLIST SMF_ARRAYLIST, * PSMF_ARRAYLIST;

struct _SMF_ARRAYLIST
{
	ULONG Size;
	ULONG AllocSize;
	SMF_TYPEDECL_INDEX InnerType;
	ULONG_PTR* Values;
};

//
// Build the default empty list of type InnerType in the caller provided memory *Dst
//
VOID
BuildEmptyArrayList(
PSMF_ARRAYLIST Dst,
SMF_TYPEDECL_INDEX InnerType);

//
// Insert the element val at position ind in the list lst.
// Precondition:   0 <= ind <= sizeof(oldlst)
// Postconditions: sizeof(lst) = sizeof(oldlst) + 1,
//				   lst[ind] == val
//				   lst[i] == oldlst[i] for 0 <= i < ind
//			       lst[i] == oldlst[i-1] for ind < i < sizeof(lst)
VOID
SmfArrayListInsert(
PSMF_DRIVERDECL Driver,
PSMF_ARRAYLIST Dst,
ULONG Ind,
ULONG_PTR Val);

//
// Remove the element at position ind in the list lst.
// Precondition:   0 <= ind < sizeof(oldlst)
// Postconditions: sizeof(lst) = sizeof(oldlst) - 1,
//				   lst[i] == oldlst[i] for 0 <= i < ind
//			       lst[i] == oldlst[i+1] for ind <= i < sizeof(lst)
VOID
SmfArrayListRemove(
PSMF_DRIVERDECL Driver,
PSMF_ARRAYLIST Dst,
ULONG Ind);

//
// Clone an ArrayList from one location into another. This performs a deep clone
//
VOID
SmfArrayListClone(
PSMF_DRIVERDECL Driver,
PSMF_ARRAYLIST Dst,
PSMF_ARRAYLIST Src);

//
// Destroy an ArrayList
//
VOID
SmfArrayListDestroy(
PSMF_DRIVERDECL Driver,
PSMF_ARRAYLIST Obj);