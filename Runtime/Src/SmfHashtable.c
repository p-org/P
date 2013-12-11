#include "SmfHashtable.h"
#include "SmfPrivate.h"

PSMF_BUCKET_ENTRY
	AllocateAndInitializeBucketEntry()
{
	PSMF_BUCKET_ENTRY elem;
	elem = (PSMF_BUCKET_ENTRY) SmfAllocateMemory(sizeof(SMF_BUCKET_ENTRY));
	elem->Key = 0;
	elem->Val = 0;
	elem->NextInBucketList = NULL;
	elem->NextInIterList = elem;
	elem->PrevInIterList = elem;
	return elem;
}

VOID
	Resize(
	PSMF_DRIVERDECL Driver,
	PSMF_HASHTABLE Dst)
{
	PSMF_BUCKET_ENTRY prev, iter, head;
	ULONG i;
	if (Dst->Size > Dst->NumBuckets) {
		Dst->NumBuckets = Dst->NumBuckets * 2;
	} else {
		Dst->NumBuckets = Dst->NumBuckets / 2;
	}
	SmfFreeMemory(Dst->Buckets);
	Dst->Buckets = (PSMF_BUCKET_ENTRY *) SmfAllocateMemory(Dst->NumBuckets * sizeof(PSMF_BUCKET_ENTRY));
	for (i = 0; i < Dst->NumBuckets; i++) {
		Dst->Buckets[i] = NULL;
	}
	Dst->Size = 0;  
	head = Dst->IterList;
	Dst->IterList = AllocateAndInitializeBucketEntry();
	Dst->IterCache = Dst->IterList;
	Dst->IterCacheIndex = 0;

	prev = head;
	iter = prev->NextInIterList;
	while (iter != head) {
		SmfFreeMemory(prev);
		SmfHashtableUpdate(Driver, Dst, iter->Key, iter->Val);
		prev = iter;
		iter = prev->NextInIterList;
	}
	SmfFreeMemory(prev);  
}

ULONG
	GetBucket(
	PSMF_DRIVERDECL Driver,
	PSMF_HASHTABLE Dst,
	ULONG_PTR Key)
{
	ULONG hashCode;
	if (PRIMITIVE(Driver, Dst->KeyType))
		hashCode = (ULONG) Key;
	else 
		hashCode = HASHCODE(Driver, Dst->KeyType)(Driver, (PVOID) Key);
	return hashCode % Dst->NumBuckets;
}

BOOLEAN
	Equals(
	PSMF_DRIVERDECL Driver,
	PSMF_HASHTABLE Dst,
	ULONG_PTR Key1,
	ULONG_PTR Key2)
{
	if (PRIMITIVE(Driver, Dst->KeyType))
		return Key1 == Key2;
	else 
		return EQUALS(Driver, Dst->KeyType)(Driver, (PVOID) Key1, (PVOID) Key2);
}

//
// Build the default empty hashtable of from KeyType to ValType in the caller provided memory *Dst
//
VOID
	BuildEmptyHashtable(
	PSMF_HASHTABLE Dst,
	SMF_TYPEDECL_INDEX KeyType,
	SMF_TYPEDECL_INDEX ValType)
{
	ULONG i;
	Dst->KeyType = KeyType;
	Dst->ValType = ValType;
	Dst->NumBuckets = 1;
	Dst->Buckets = (PSMF_BUCKET_ENTRY *) SmfAllocateMemory(Dst->NumBuckets * sizeof(PSMF_BUCKET_ENTRY));
	for (i = 0; i < Dst->NumBuckets; i++) {
		Dst->Buckets[i] = NULL;
	}
	Dst->Size = 0;
	Dst->IterList = AllocateAndInitializeBucketEntry();
	Dst->IterCache = Dst->IterList;
	Dst->IterCacheIndex = 0;
}

//
// Insert (Key, Val) into the hashtable
//
VOID
	SmfHashtableUpdate(
	PSMF_DRIVERDECL Driver,
	PSMF_HASHTABLE Dst,
	ULONG_PTR Key,
	ULONG_PTR Val)
{
	ULONG bucket = GetBucket(Driver, Dst, Key);
	PSMF_BUCKET_ENTRY *iter = &Dst->Buckets[bucket];
	PSMF_BUCKET_ENTRY elem;
	while (TRUE) {
		if (*iter == NULL) {
			elem = AllocateAndInitializeBucketEntry();
			elem->NextInIterList = Dst->IterList->NextInIterList;
			Dst->IterList->NextInIterList->PrevInIterList = elem;
			elem->PrevInIterList = Dst->IterList;
			Dst->IterList->NextInIterList = elem;

			if (PRIMITIVE(Driver, Dst->KeyType))
				Dst->IterList->Key = Key;
			else 
				CLONE(Driver, Dst->KeyType)(Driver, (PVOID) Dst->IterList->Key, (PVOID) Key);
			if (PRIMITIVE(Driver, Dst->ValType)) 
				Dst->IterList->Val = Val;
			else
				CLONE(Driver, Dst->ValType)(Driver, (PVOID) Dst->IterList->Val, (PVOID)Val);

			*iter = Dst->IterList;
			Dst->IterList = elem;
			Dst->Size = Dst->Size + 1;
			if (Dst->Size > Dst->NumBuckets * 2) {
				Resize(Driver, Dst);
			}
			return;
		}
		if (Equals(Driver, Dst, Key, (*iter)->Key)) {
			(*iter)->Val = Val;
			return;
		}
		iter = &(*iter)->NextInBucketList;
	}
}

//
// Remove Key 
//
VOID
	SmfHashtableRemove(
	PSMF_DRIVERDECL Driver,
	PSMF_HASHTABLE Dst,
	ULONG_PTR Key)
{
	ULONG bucket = GetBucket(Driver, Dst, Key);
	PSMF_BUCKET_ENTRY *iter = &Dst->Buckets[bucket];
	PSMF_BUCKET_ENTRY elem = NULL;
	while (TRUE) {
		elem = *iter;
		if (elem == NULL) {
			return;
		}
		if (Equals(Driver, Dst, Key, elem->Key)) {
			elem->NextInIterList->PrevInIterList = elem->PrevInIterList;
			elem->PrevInIterList->NextInIterList = elem->NextInIterList;
			*iter = elem->NextInBucketList;
			SmfFreeType(Driver, Dst->KeyType, (PVOID) elem->Key);
			SmfFreeType(Driver, Dst->ValType, (PVOID) elem->Val);
			SmfFreeMemory(elem);
			Dst->IterCache = Dst->IterList->NextInIterList;
			Dst->IterCacheIndex = 0;
			Dst->Size = Dst->Size - 1;
			if (Dst->Size < Dst->NumBuckets / 2) {
				Resize(Driver, Dst);
			}
			return;
		}
		iter = &elem->NextInBucketList;
	}
}

//
// Is Key present?
//
BOOLEAN
	SmfHashtableContains(
	PSMF_DRIVERDECL Driver,
	PSMF_HASHTABLE Dst,
	ULONG_PTR Key)
{
	ULONG bucket = GetBucket(Driver, Dst, Key);
	PSMF_BUCKET_ENTRY iter = Dst->Buckets[bucket];
	while (TRUE) {
		if (iter == NULL) {
			return FALSE;
		}
		if (Equals(Driver, Dst, Key, iter->Key)) {
			return TRUE;
		}
		iter = iter->NextInBucketList;
	}
}

// 
// Retrieve Val corresponding to Key
//
ULONG_PTR 
	SmfHashtableLookup(
	PSMF_DRIVERDECL Driver,
	PSMF_HASHTABLE Dst,
	ULONG_PTR Key)
{
	ULONG bucket = GetBucket(Driver, Dst, Key);
	PSMF_BUCKET_ENTRY iter = Dst->Buckets[bucket];
	while (TRUE) {
		SMF_ASSERTMSG("Hashtable Lookup: Key not found", iter != NULL);
		if (Equals(Driver, Dst, Key, iter->Key)) {
			return iter->Val;
		}
		iter = iter->NextInBucketList;
	}
}

//
// Retrieve Key at Index.  Used for iteration.
// 
ULONG_PTR 
	SmfHashtableLookupKeyAtIndex(
	PSMF_DRIVERDECL Driver,
	PSMF_HASHTABLE Dst,
	ULONG Index)
{
	ULONG i = 0;
	PSMF_BUCKET_ENTRY iter = Dst->IterList->NextInIterList;
	SMF_ASSERTMSG("Hashtable LookupKeyAtIndex: Invalid index", 0 <= Index && Index < Dst->Size);
	if (Dst->IterCacheIndex <= Index) {
		i = Dst->IterCacheIndex;
		iter = Dst->IterCache;
	}
	while (i < Index) {
		iter = iter->NextInIterList;
		i = i + 1;
	}
	Dst->IterCacheIndex = i;
	Dst->IterCache = iter;
	return iter->Key;
}

//
// Clone a Hashtable from one location into another. This performs a deep clone.
//
VOID
	SmfHashtableClone(
	PSMF_DRIVERDECL Driver,
	PSMF_HASHTABLE Dst,
	PSMF_HASHTABLE Src)
{
	ULONG i;
	ULONG_PTR Key, Val;

	BuildEmptyHashtable(Dst, Src->KeyType, Src->ValType);
	for (i = 0; i < Src->Size; i++) {
		Key = SmfHashtableLookupKeyAtIndex(Driver, Src, i);
		Val = SmfHashtableLookup(Driver, Src, Key);
		SmfHashtableUpdate(Driver, Dst, Key, Val);
	}
}

//
// Destroy a Hashtable
//
VOID
	SmfHashtableDestroy(
	PSMF_DRIVERDECL Driver,
	PSMF_HASHTABLE Obj)
{
	PSMF_BUCKET_ENTRY next;
	PSMF_BUCKET_ENTRY iter = Obj->IterList->NextInIterList;
	while (iter != Obj->IterList) {
		next = iter->NextInIterList;
		SmfFreeType(Driver, Obj->KeyType, (PVOID) iter->Key);
		SmfFreeType(Driver, Obj->ValType, (PVOID) iter->Val);
		SmfFreeMemory(iter);
		iter = next;
	}
	SmfFreeMemory(Obj->IterList);
	SmfFreeMemory(Obj->Buckets);
	SmfFreeMemory(Obj);
}

//
// Clone the Hashtable to an ArrayList
//
PSMF_ARRAYLIST
	SmfHashtableConvertToArrayList(
	PSMF_DRIVERDECL Driver,
	PSMF_HASHTABLE Src)
{
	ULONG i;
	ULONG_PTR Key;
	PSMF_ARRAYLIST Dst;
	PVOID mem = SmfAllocateMemory(sizeof(SMF_ARRAYLIST));
	SMF_ASSERTMSG("Failed to create an instance of ArrayList inside SmfHashtableConvertToArrayList", mem != NULL);
	Dst = (PSMF_ARRAYLIST) mem;
	BuildEmptyArrayList(Dst, Src->KeyType);
	for (i = 0; i < Src->Size; i++) {
		Key = SmfHashtableLookupKeyAtIndex(Driver, Src, i);
		SmfArrayListInsert(Driver, Dst, i, Key);
	}
	return Dst;
}