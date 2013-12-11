#include "SmfArrayList.h"
#include "SmfPrivate.h"

//
// Build the default empty list of type InnerType in the caller provided memory *Dst
//
VOID
BuildEmptyArrayList(
PSMF_ARRAYLIST Dst,
SMF_TYPEDECL_INDEX InnerType)
{
	Dst->Size = 0;
	Dst->AllocSize = 1;
	Dst->Values = (ULONG_PTR*) SmfAllocateMemory(Dst->AllocSize * sizeof(ULONG_PTR));
	Dst->InnerType = InnerType;
}

//
// Insert the element val at position ind in the list lst.
//
VOID
SmfArrayListInsert(
PSMF_DRIVERDECL Driver,
PSMF_ARRAYLIST Dst,
ULONG Ind,
ULONG_PTR Val)
{
	ULONG i;
	SMF_ASSERTMSG("List Insert: Index outside of bounds", 0 <= Ind && Ind <= Dst->Size);
	if (Dst->Size == Dst->AllocSize) {
		PVOID newL = SmfAllocateMemory(Dst->AllocSize * 2 * sizeof(ULONG_PTR));
		PVOID oldL = Dst->Values;
		SMF_ASSERT(newL != NULL);
		memcpy(newL, oldL, Dst->Size * sizeof(ULONG_PTR));
		Dst->Values = (ULONG_PTR*)newL;
		Dst->AllocSize = 2 * Dst->AllocSize;
		SmfFreeMemory(oldL);
	}

	for (i = Dst->Size; i > Ind; i--)
		Dst->Values[i] = Dst->Values[i-1];

	if (PRIMITIVE(Driver, Dst->InnerType))
		Dst->Values[Ind] = Val;
	else {
		Dst->Values[Ind] = SmfAllocateType(Driver, Dst->InnerType);
		SMF_ASSERT(Dst->Values[Ind]);
		CLONE(Driver, Dst->InnerType)(Driver, (PVOID)Dst->Values[Ind], (PVOID)Val);
	}

	Dst->Size++;
}

//
// Remove the element at position ind in the list lst.
//
VOID
SmfArrayListRemove(
PSMF_DRIVERDECL Driver,
PSMF_ARRAYLIST Dst,
ULONG Ind)
{
	ULONG i;
	SMF_ASSERTMSG("List Remove: Index outside of bounds", 0 <= Ind && Ind < Dst->Size);

	if (!PRIMITIVE(Driver, Dst->InnerType)) {
		SmfFreeType(Driver, Dst->InnerType, (PVOID)Dst->Values[Ind]);
	}

	for (i = Ind; i < Dst->Size; i++)
		Dst->Values[i] = Dst->Values[i+1];

	Dst->Values[Dst->Size - 1] = 0;
	Dst->Size--;

	if (Dst->Size < Dst->AllocSize / 2) {
		PVOID newL = SmfAllocateMemory((Dst->AllocSize / 2) * sizeof(ULONG_PTR));
		PVOID oldL = Dst->Values;
		SMF_ASSERT(newL != NULL);
		memcpy(newL, oldL, Dst->Size * sizeof(ULONG_PTR));
		Dst->Values = (ULONG_PTR*)newL;
		Dst->AllocSize = Dst->AllocSize / 2;
		SmfFreeMemory(oldL);
	}
}

//
// Clone an ArrayList from one location into another. This performs a deep clone
//
VOID
SmfArrayListClone(
PSMF_DRIVERDECL Driver,
PSMF_ARRAYLIST Dst,
PSMF_ARRAYLIST Src)
{
	ULONG i;
	BuildEmptyArrayList(Dst, Src->InnerType);
	for (i = 0; i < Src->Size; i++)
		SmfArrayListInsert(Driver, Dst, Dst->Size, Src->Values[i]);
}

//
// Destroy an ArrayList
//
VOID
SmfArrayListDestroy(
PSMF_DRIVERDECL Driver,
PSMF_ARRAYLIST Obj)
{
	ULONG i;
	if (!PRIMITIVE(Driver, Obj->InnerType)) {
		for (i = 0; i < Obj->Size; i++)
			SmfFreeType(Driver, Obj->InnerType, (PVOID)Obj->Values[i]);
	}

	SmfFreeMemory((PVOID)Obj->Values);
}