#pragma once
#include "SmfDepends.h"
#include "SmfPublicTypes.h"

typedef struct _SMF_BUCKET_ENTRY SMF_BUCKET_ENTRY, * PSMF_BUCKET_ENTRY;

struct _SMF_BUCKET_ENTRY 
{
  ULONG_PTR Key;
  ULONG_PTR Val;
  PSMF_BUCKET_ENTRY NextInBucketList;
  PSMF_BUCKET_ENTRY NextInIterList;
  PSMF_BUCKET_ENTRY PrevInIterList;
};

typedef struct _SMF_HASHTABLE SMF_HASHTABLE, * PSMF_HASHTABLE;

struct _SMF_HASHTABLE
{
  SMF_TYPEDECL_INDEX KeyType;
  SMF_TYPEDECL_INDEX ValType;
  ULONG NumBuckets;
  PSMF_BUCKET_ENTRY *Buckets;
  ULONG Size;
  PSMF_BUCKET_ENTRY IterList;
  // IterList points to a doubly-linked circular list; 
  // IterList entry points to where the next element will be inserted
  PSMF_BUCKET_ENTRY IterCache;
  ULONG IterCacheIndex;
  // IterCache and IterCacheIndex caches the last index lookup
};

//
// Build the default empty hashtable of from DomainType to RangeType in the caller provided memory *Dst
//
VOID
BuildEmptyHashtable(
PSMF_HASHTABLE Dst,
SMF_TYPEDECL_INDEX DomainType,
SMF_TYPEDECL_INDEX RangeType);

//
// Update the mapping Key -> Val in the hashtable
//
VOID
SmfHashtableUpdate(
PSMF_DRIVERDECL Driver,
PSMF_HASHTABLE Dst,
ULONG_PTR Key,
ULONG_PTR Val);

//
// Remove Key 
//
VOID
SmfHashtableRemove(
PSMF_DRIVERDECL Driver,
PSMF_HASHTABLE Dst,
ULONG_PTR Key);

//
// Is Key present?
//
BOOLEAN
SmfHashtableContains(
PSMF_DRIVERDECL Driver,
PSMF_HASHTABLE Dst,
ULONG_PTR Key);

// 
// Retrieve Val corresponding to Key
//
ULONG_PTR 
SmfHashtableLookup(
PSMF_DRIVERDECL Driver,
PSMF_HASHTABLE Dst,
ULONG_PTR Key);

//
// Retrieve Key at Index.  Used for iteration.
// 
ULONG_PTR 
SmfHashtableLookupKeyAtIndex(
PSMF_DRIVERDECL Driver,
PSMF_HASHTABLE Dst,
ULONG Index);

//
// Clone a Hashtable from one location into another. This performs a deep clone.
//
VOID
SmfHashtableClone(
PSMF_DRIVERDECL Driver,
PSMF_HASHTABLE Dst,
PSMF_HASHTABLE Src);

//
// Destroy a Hashtable
//
VOID
SmfHashtableDestroy(
PSMF_DRIVERDECL Driver,
PSMF_HASHTABLE Obj);
