/**
* \file PrtValues.h
* \brief Functions and data structures for P values.
* Every P value is a combination of a type expression and an in-memory representation
* of the value. The type expression cannot be `any`, but `any` may appear as an inner type.
* Here are some examples:
*
* `true : bool`, `1 : int`, `null : event`, `null : id`, `(1, 2) : (any, int)`.
*
* The type a value does not change under mutation, and the inner-types of an expression
* constrain the legal mutations of a value. For instance, it is illegal to update `(1, 2) : (any, int)` to become
* `(1, true) : (any, int)` because the second element in the tuple must be an integer.
* Also note that values contained in complex data types may have more specific type information. For instance,
* the value obtained by reading the first element of `(1, true) : (any, int)` is `1 : int`.
*/
#ifndef PRTVALUES_H
#define PRTVALUES_H
#include "PrtTypes.h"

#ifdef __cplusplus
extern "C" {
#endif

	/** Structure for representing a standard GUID */
#pragma pack()

typedef struct PRT_GUID
{
	PRT_UINT32 data1; /**< 0 First data field (32 bits)  */
	PRT_UINT16 data2; /**< 0 Second data field (16 bits) */
	PRT_UINT16 data3; /**< 0 Third data field (16 bits)  */
	PRT_UINT64 data4; /**< 0 Fourth data field (64 bits) */
} PRT_GUID;

typedef enum PRT_SPECIAL_EVENT
{
	PRT_SPECIAL_EVENT_NULL = 0,
	/**< The id of the null event */
	PRT_SPECIAL_EVENT_HALT = 1,
	/**< The id of the halt event */
	PRT_EVENT_USER_START = 2 /**< The first event id available to user code */
} PRT_SPECIAL_EVENT;

/**
* \enum
* These are the kinds of values in the P type system.
**/
typedef enum PRT_VALUE_KIND
{
	PRT_VALUE_KIND_NULL = 0,
	/**< The kind of the null value in type null */
	PRT_VALUE_KIND_BOOL = 1,
	/**< The kind of bool values                 */
	PRT_VALUE_KIND_INT = 2,
	/**< The kind of int values                  */
	PRT_VALUE_KIND_FLOAT = 3,
	/**< The kind of int values                  */
	PRT_VALUE_KIND_STRING = 4,
	/**< The kind of string values                  */
	PRT_VALUE_KIND_EVENT = 5,
	/**< The kind of event id values             */
	PRT_VALUE_KIND_MID = 6,
	/**< The kind of machine id values   */
	PRT_VALUE_KIND_FOREIGN = 7,
	/**< The kind of all foreign values          */
	PRT_VALUE_KIND_TUPLE = 8,
	/**< The kind of all (named) tuple values    */
	PRT_VALUE_KIND_SEQ = 9,
	/**< The kind of all sequence values         */
	PRT_VALUE_KIND_SET = 10,
	/**< The kind of all set values         */
	PRT_VALUE_KIND_MAP = 11,
	/**< The kind of all map values              */
	PRT_VALUE_KIND_COUNT = 12,
	/**< The number of value kinds               */
} PRT_VALUE_KIND;

/** A Union type to discriminate the Prt value */
typedef struct PRT_VALUE
{
	PRT_VALUE_KIND discriminator; /**< A value kind to discriminate the union */
	union
	{
		PRT_BOOLEAN bl; /**< A boolean value            */
		PRT_INT nt; /**< An integer value           */
		PRT_FLOAT ft; /**< An float value           */
		PRT_STRING str; /*< A string value           */
		PRT_UINT32 ev; /**< An event id value          */
		struct PRT_MACHINEID* mid; /**< A machine id value */
		struct PRT_FOREIGNVALUE* frgn; /**< A foreign value            */
		struct PRT_TUPVALUE* tuple; /**< A (named) tuple value      */
		struct PRT_SEQVALUE* seq; /**< A sequence value	        */
		struct PRT_SETVALUE* set; /**< A set value	        */
		struct PRT_MAPVALUE* map; /**< A map value		        */
	} valueUnion;
} PRT_VALUE;

/** The id of a machine is a combination of the id of the owner process and an id unique to that process.
*/
typedef struct PRT_MACHINEID
{
	PRT_GUID processId;
	PRT_UINT32 machineId;
} PRT_MACHINEID;

/** A set value is represented as a hash-table. */
typedef struct PRT_SETVALUE
{
	PRT_UINT32 size; /**< The number of elements in the set.      */
	PRT_UINT32 capNum; /**< An opaque number related to the number of buckets */
	struct PRT_SETNODE* first; /**< First element inserted into the set. */
	struct PRT_SETNODE* last; /**< Last element inserted into the set. */
	struct PRT_SETNODE** buckets; /**< An array of pointers to chained nodes.  */
} PRT_SETVALUE;

/** A key-value node of a map. */
typedef struct PRT_SETNODE
{
	PRT_VALUE* item; /**< The key of this node. */
	struct PRT_SETNODE* bucketNext; /**< The next node in this bucket.         */
	struct PRT_SETNODE* insertNext; /**< The next node in insertion order.     */
	struct PRT_SETNODE* insertPrev; /**< The previous node in insertion order. */
} PRT_SETNODE;



/** Adds item to set.
* If item is not in the set, then adds it.
* If item is already in the set, then ignores it.
* @param[in,out] set   A set to mutate.
* @param[in]     item   The item to add.
* @param[in]     cloneItem Only set to PRT_FALSE if item will be forever owned by this set.
*/
PRT_API void PRT_CALL_CONV PrtSetAddEx(
	_Inout_	        PRT_VALUE* set,
	_In_	        PRT_VALUE* item,
	_In_	        PRT_BOOLEAN cloneIey);

/** Adds item to set.
* If key is not in the set, then adds it.
* If key is already in the set, then ignores it.
* @param[in,out] set   A set to mutate.
* @param[in]     item   The item to add (will be cloned).
*/
PRT_API void PRT_CALL_CONV PrtSetAdd(
	_Inout_	        PRT_VALUE* set,
	_In_	        PRT_VALUE* item);

/** Remove the item from the set.
* If the key is not in then set, then the set is unchanged.
* @param[in,out] set   A set to mutate.
* @param[in]     item   The item to remove.
*/
PRT_API void PRT_CALL_CONV PrtSetRemove(
	_Inout_	        PRT_VALUE* set,
	_In_	        PRT_VALUE* item);

/** Returns true if the set contains item; false otherwise.
* @param[in] set A set.
* @param[in] item The item to lookup.
* @returns Returns true if the set contains item; false otherwise.
*/
PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtSetExists(
	_In_	     PRT_VALUE* set,
	_In_	     PRT_VALUE* item);

/** Gets the size of a set.
* @param[in] set A set.
* @returns The size of the set.
*/
PRT_API PRT_UINT32 PRT_CALL_CONV PrtSetSizeOf(_In_ PRT_VALUE* set);

/** The hypothetical maximum number of items that could be accessed in constant-time.
* @param[in] set A set.
* @returns The capacity of the set.
*/
PRT_API PRT_UINT32 PRT_CALL_CONV PrtSetCapacity(_In_ PRT_VALUE* set);


	/** A tuple value is a (named) tuple represented as an array. */
	typedef struct PRT_TUPVALUE
	{
		PRT_UINT32 size;
		PRT_VALUE** values; /**< Is an array of tuple args.    */
	} PRT_TUPVALUE;

	/** A sequence value is a sequence represented as a dynamic array. */
	typedef struct PRT_SEQVALUE
	{
		PRT_UINT32 size; /**< The number of elements in the sequence. */
		PRT_UINT32 capacity; /**< The number of elements before resizing  */
		PRT_VALUE** values; /**< An array of values in the sequence.     */
	} PRT_SEQVALUE;

	/** A map value is represented as a hash-table. */
	typedef struct PRT_MAPVALUE
	{
		PRT_UINT32 size; /**< The number of elements in the map.      */
		PRT_UINT32 capNum; /**< An opaque number related to the number of buckets */
		struct PRT_MAPNODE* first; /**< First element inserted into the map. */
		struct PRT_MAPNODE* last; /**< Last element inserted into the map. */
		struct PRT_MAPNODE** buckets; /**< An array of pointers to chained nodes.  */
	} PRT_MAPVALUE;

	/** A key-value node of a map. */
	typedef struct PRT_MAPNODE
	{
		PRT_VALUE* key; /**< The key of this node. */
		PRT_VALUE* value; /**< The value of this node. */
		struct PRT_MAPNODE* bucketNext; /**< The next node in this bucket.         */
		struct PRT_MAPNODE* insertNext; /**< The next node in insertion order.     */
		struct PRT_MAPNODE* insertPrev; /**< The previous node in insertion order. */
	} PRT_MAPNODE;

	/** A foreign value is foreign type paired with a char *. */
	typedef struct PRT_FOREIGNVALUE
	{
		PRT_UINT32 typeTag; /**< The index of the foreign type. */
		PRT_UINT64 value; /**< The foreign value. */
	} PRT_FOREIGNVALUE;

	/** Makes a default value of an arbitrary type. The defaults (def) are as follows:
	* 1.  def(null)                = `null : null`.
	* 2.  def(any)                 = `null : null`.
	* 3.  def(bool)                = `false : bool`.
	* 4.  def(string)              = `"" : string`.
	* 5.  def(event)               = `null : event`.
	* 6.  def(foreign)             = `MkDef_foreign_IMPL() : foreign`.
	* 7.  def(machine)             = `null : machine`.
	* 8.  def(int)                 = `0 : int`.
	* 9.  def(map[S, T])           = `[] : map[S, T]`.
	* 10.  def(set[S])           = `[] : set[S, T]`.
	* 11. def((l1: S1,.., ln: Sn)) = `(l1 = def(S1),..., ln = def(Sn)) : (l1: S1,..., ln: Sn)`.
	* 12. def([S])                 = `[] : [S]`.
	* 13. def((S1,..,Sn))          = `(def(S1),..., def(S2)) : (S1,..., Sn)`.
	* @param[in] type A type expression (will be cloned).
	* @returns The default value of the type. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtMkDefaultValue(_In_ PRT_TYPE* type);

	/** Makes a boolean value.
	* @param[in] value A boolean value.
	* @returns A proper boolean value. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtMkBoolValue(_In_ PRT_BOOLEAN value);

	/** Makes an event value.
	* @param[in] value An event id.
	* @returns A proper event value. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtMkEventValue(_In_ PRT_UINT32 value);

	/** Makes an integer value.
	* @param[in] value An int value.
	* @returns A proper int value. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtMkIntValue(_In_ PRT_INT value);

	/** Makes an float value.
	* @param[in] value A float value.
	* @returns A proper float value. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtMkFloatValue(_In_ PRT_FLOAT value);

	/** Makes an string value.
	* @param[in] value A string value.
	* @returns A proper string value. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtMkStringValue(_In_ PRT_STRING value);

	/** Makes null value.
	* The types null, event, and machine all share the null value.
	* The null value projected onto event is the id PRT_SPECIAL_EVENT_NULL.
	* The null value projected onto machine is the processId 0-0-0-0 with machineId PRT_SPECIAL_EVENT_NULL.
	* @returns A proper null value. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtMkNullValue(void);

	/** Makes an machine value.
	* @param[in] value A machine id.
	* @returns A proper machine value. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtMkMachineValue(_In_ PRT_MACHINEID value);

	/** Makes a foreign value.
	* @param[in] value A pointer to foreign data (will be cloned).
	* @param[in] type The foreign type.
	* @returns A proper foreign value. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtMkForeignValue(_In_ PRT_UINT64 value, _In_ PRT_TYPE* type);

	/** Gets foreign data inside a value.
	* @param[in] v A pointer to a value.
	* @returns The (uncloned) foreign data inside v.
	*/
	PRT_API PRT_UINT64 PrtGetForeignValue(_In_ PRT_VALUE* v);

	/** Sets the value of a boolean.
	* @param[in,out] prmVal A primitive boolean value to mutate.
	* @param[in]     value The value to set.
	*/
	PRT_API void PRT_CALL_CONV PrtPrimSetBool(_Inout_ PRT_VALUE* prmVal, _In_ PRT_BOOLEAN value);

	/** Gets the value of a boolean.
	* @param[in] prmVal A primitive boolean value.
	* @returns A boolean.
	*/
	PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtPrimGetBool(_In_ PRT_VALUE* prmVal);

	/** Sets the value of a string.
	* @param[in,out] prmVal A primitive string value to mutate.
	* @param[in]     value The value to set.
	*/
	PRT_API void PRT_CALL_CONV PrtPrimSetString(_Inout_ PRT_VALUE* prmVal, _In_ PRT_STRING value);

	/** Gets the value of a string.
	* @param[in] prmVal A primitive string value.
	* @returns A string.
	*/
	PRT_API PRT_STRING PRT_CALL_CONV PrtPrimGetString(_In_ PRT_VALUE* prmVal);

	/** Sets the value of an event.
	* @param[in,out] prmVal A primitive event value to mutate.
	* @param[in]     value The value to set.
	*/
	PRT_API void PRT_CALL_CONV PrtPrimSetEvent(_Inout_ PRT_VALUE* prmVal, _In_ PRT_UINT32 value);

	/** Gets the value of an event.
	* @param[in] prmVal A primitive event value.
	* @returns An event id.
	*/
	PRT_API PRT_UINT32 PRT_CALL_CONV PrtPrimGetEvent(_In_ PRT_VALUE* prmVal);

	/** Sets the value of an int.
	* @param[in,out] prmVal A primitive int value to mutate.
	* @param[in]     value The value to set.
	*/
	PRT_API void PRT_CALL_CONV PrtPrimSetInt(_Inout_ PRT_VALUE* prmVal, _In_ PRT_INT value);

	/** Gets the value of an integer.
	* @param[in] prmVal A primitive int value.
	* @returns An integer.
	*/
	PRT_API PRT_INT PRT_CALL_CONV PrtPrimGetInt(_In_ PRT_VALUE* prmVal);

	/** Sets the value of a float.
	* @param[in,out] prmVal A primitive float value to mutate.
	* @param[in]     value The value to set.
	*/
	PRT_API void PRT_CALL_CONV PrtPrimSetFloat(_Inout_ PRT_VALUE* prmVal, _In_ PRT_FLOAT value);

	/** Gets the value of a float.
	* @param[in] prmVal A primitive float value.
	* @returns An integer.
	*/
	PRT_API PRT_FLOAT PRT_CALL_CONV PrtPrimGetFloat(_In_ PRT_VALUE* prmVal);

	/** Sets the value of an Machine.
	* @param[in,out] prmVal A primitivemachinevalue to mutate.
	* @param[in]     value The value to set.
	*/
	PRT_API void PRT_CALL_CONV PrtPrimSetMachine(_Inout_ PRT_VALUE* prmVal, _In_ PRT_MACHINEID value);

	/** Gets the value of an Machine.
	* @param[in] prmVal A primitive machine value.
	* @returns A machine machine
	*/
	PRT_API PRT_MACHINEID PRT_CALL_CONV PrtPrimGetMachine(_In_ PRT_VALUE* prmVal);

	/** Concatenates two strings. 
	* @param[in]     str1 first input string.
	* @param[in]     str2 second input string.
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtStringConcat(_In_ PRT_VALUE* str1, _In_ PRT_VALUE* str2);

	/** Sets an element in a (named) tuple by index.
	* @param[in,out] tuple A (named) tuple to mutate.
	* @param[in]     index A 0-based element index.
	* @param[in]     value The value to set (will be cloned).
	*/
	PRT_API void PRT_CALL_CONV PrtTupleSet(
		_Inout_	        PRT_VALUE* tuple,
		_In_	        PRT_UINT32 index,
		_In_	        PRT_VALUE* value);

	/** Sets an element in a (named) tuple by index.
	* @param[in,out] tuple A (named) tuple to mutate.
	* @param[in]     index A 0-based element index.
	* @param[in]     value The value to set (will be cloned if cloneValue is PRT_TRUE).
	* @param[in]     cloneValue Only set to PRT_FALSE if value will be forever owned by this tuple.
	*/
	PRT_API void PRT_CALL_CONV PrtTupleSetEx(
		_Inout_	        PRT_VALUE* tuple,
		_In_	        PRT_UINT32 index,
		_In_	        PRT_VALUE* value,
		_In_	        PRT_BOOLEAN cloneValue);

	/** Gets an element in a (named) tuple without cloning. Only used for internal manipulation of state variables.
	* @param[in] tuple A (named) tuple.
	* @param[in] index A 0-based element index.
	* @returns The element at index i.
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtTupleGetNC(
		_In_	     PRT_VALUE* tuple,
		_In_	     PRT_UINT32 index);

	/** Gets a pointer to the element in a (named) tuple. Only used for internal manipulation of state variables.
	* @param[in] tuple A (named) tuple.
	* @param[in] index A 0-based element index.
	* @returns The pointer to element at index i.
	*/
	PRT_API PRT_VALUE** PRT_CALL_CONV PrtTupleGetLValue(
		_In_	     PRT_VALUE* tuple,
		_In_	     PRT_UINT32 index);

	/** Gets an element in a sequence without cloning. Only used for internal manipulation of state variables.
	* @param[in] seq   A sequence.
	* @param[in] index A 0-based index s.t. 0 <= index < size(seq).
	* @returns The value at index.
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtSeqGetNC(
		_In_	     PRT_VALUE* seq,
		_In_	     PRT_VALUE* index);

	/** Gets a pointer to an element in a sequence. Only used for internal manipulation of state variables.
	* @param[in] seq   A sequence.
	* @param[in] index A 0-based index s.t. 0 <= index < size(seq).
	* @returns The pointer to the value at index.
	*/
	PRT_API PRT_VALUE** PRT_CALL_CONV PrtSeqGetLValue(
		_In_	     PRT_VALUE* seq,
		_In_	     PRT_VALUE* index);

	/** Gets a value from a map without cloning. Only used for internal manipulation of state variables.
	* The key must be present the map.
	* @param[in] map A map.
	* @param[in] key The key to lookup.
	* @returns The value to which the key maps.
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtMapGetNC(
		_In_	     PRT_VALUE* map,
		_In_	     PRT_VALUE* key);

	/** Gets an element in a (named) tuple by index.
	* @param[in] tuple A (named) tuple.
	* @param[in] index A 0-based element index.
	* @returns The element at index i. Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtTupleGet(
		_In_	     PRT_VALUE* tuple,
		_In_	     PRT_UINT32 index);

	/** Returns true if the seq contains value; false otherwise.
	* @param[in] seq A Seq.
	* @param[in] val The value to lookup.
	* @returns Returns true if the seq contains key; false otherwise.
	*/
	PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtSeqExists(
		_In_	     PRT_VALUE* seq,
		_In_	     PRT_VALUE* val);

	/** Updates the sequence at index.
	* @param[in,out] seq   A sequence to mutate.
	* @param[in]     index The name of the element to set. A value must already exist at this index.
	* @param[in]     value The value to store at index (will be cloned).
	*/
	PRT_API void PRT_CALL_CONV PrtSeqUpdate(
		_Inout_	        PRT_VALUE* seq,
		_In_	        PRT_VALUE* index,
		_In_	        PRT_VALUE* value);

	/** Updates the sequence at index.
	* @param[in,out] seq   A sequence to mutate.
	* @param[in]     index The name of the element to set. A value must already exist at this index.
	* @param[in]     value The value to store at index (will be cloned if cloneValue is PRT_TRUE).
	* @param[in]     cloneValue Only set to PRT_FALSE if value will be forever owned by this seq.
	*/
	PRT_API void PRT_CALL_CONV PrtSeqUpdateEx(
		_Inout_	        PRT_VALUE* seq,
		_In_	        PRT_VALUE* index,
		_In_	        PRT_VALUE* value,
		_In_	        PRT_BOOLEAN cloneValue);

	/** Inserts value into the sequence at index.
	* Index cannot be larger than the size of the sequence. Insertion causes:
	* seq'[index] = value.
	* For all i >= index, if seq[i] is defined, then seq'[i + 1] = seq[i].
	* For all i < index, if seq[i] is defined, then seq'[i] = seq[i].
	* @param[in,out] seq   A sequence to mutate.
	* @param[in]     index An 0-based index s.t. 0 <= index <= size(seq).
	* @param[in]     value The value to store at index (will be cloned).
	*/
	PRT_API void PRT_CALL_CONV PrtSeqInsert(
		_Inout_	        PRT_VALUE* seq,
		_In_	        PRT_VALUE* index,
		_In_	        PRT_VALUE* value);

	/** Inserts value into the sequence at index.
	* Index cannot be larger than the size of the sequence. Insertion causes:
	* seq'[index] = value.
	* For all i >= index, if seq[i] is defined, then seq'[i + 1] = seq[i].
	* For all i < index, if seq[i] is defined, then seq'[i] = seq[i].
	* @param[in,out] seq   A sequence to mutate.
	* @param[in]     index An 0-based index s.t. 0 <= index <= size(seq).
	* @param[in]     value The value to store at index (will be cloned if cloneValue is PRT_TRUE).
	* @param[in]     cloneValue Only set to PRT_FALSE if value will be forever owned by this seq.
	*/
	PRT_API void PRT_CALL_CONV PrtSeqInsertEx(
		_Inout_	        PRT_VALUE* seq,
		_In_	        PRT_VALUE* index,
		_In_	        PRT_VALUE* value,
		_In_	        PRT_BOOLEAN cloneValue);

	/** Inserts value into the sequence at index.
	* Index cannot be larger than the size of the sequence. Insertion causes:
	* seq'[index] = value.
	* For all i >= index, if seq[i] is defined, then seq'[i + 1] = seq[i].
	* For all i < index, if seq[i] is defined, then seq'[i] = seq[i].
	* @param[in,out] seq   A sequence to mutate.
	* @param[in]     index An 0-based index s.t. 0 <= index <= size(seq).
	* @param[in]     value The value to store at index (will be cloned if cloneValue is PRT_TRUE).
	* @param[in]     cloneValue Only set to PRT_FALSE if value will be forever owned by this seq.
	*/
	PRT_API void PRT_CALL_CONV PrtSeqInsertExIntIndex(
		_Inout_	        PRT_VALUE* seq,
		_In_	        PRT_INT index,
		_In_	        PRT_VALUE* value,
		_In_	        PRT_BOOLEAN cloneValue);

	/** Gets the sequence at index.
	* @param[in] seq   A sequence.
	* @param[in] index A 0-based index s.t. 0 <= index < size(seq).
	* @returns The value at index (clones). Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE** PrtSeqGetNCIntIndex(
		_In_	     PRT_VALUE* seq,
		_In_	     PRT_INT index);

	/** Removes the value at index from the sequence, and shortens the sequence by one.
	* seq[index] must be defined. Removal causes:
	* For all i > index, if seq[i] is defined, then seq'[i - 1] = seq[i].
	* For all i < index, if seq[i] is defined, then seq'[i] = seq[i].
	* @param[in,out] seq   A sequence to mutate.
	* @param[in]     index An 0-based index s.t. 0 <= index < size(seq).
	*/
	PRT_API void PRT_CALL_CONV PrtSeqRemove(
		_Inout_	        PRT_VALUE* seq,
		_In_	        PRT_VALUE* index);

	/** Gets the sequence at index.
	* @param[in] seq   A sequence.
	* @param[in] index A 0-based index s.t. 0 <= index < size(seq).
	* @returns The value at index (clones). Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtSeqGet(
		_In_	     PRT_VALUE* seq,
		_In_	     PRT_VALUE* index);

	/** Gets the size of a sequence.
	* @param[in] seq A sequence.
	* @returns The size of the sequence.
	*/
	PRT_API PRT_UINT32 PRT_CALL_CONV PrtSeqSizeOf(_In_ PRT_VALUE* seq);

	/** Updates the map at key.
	* If key is not in the map, then adds it.
	* If key is already in the map, then changes its mapping.
	* @param[in,out] map   A map to mutate.
	* @param[in]     key   The key to update (will be cloned if cloneKey is PRT_TRUE).
	* @param[in]     cloneKey Only set to false if key will be forever owned by this map.
	* @param[in]     value The value to which the key maps (will be cloned if cloneValue is PRT_TRUE).
	* @param[in]     cloneValue Only set to PRT_FALSE if value will be forever owned by this map.
	*/
	PRT_API void PRT_CALL_CONV PrtMapUpdateEx(
		_Inout_	        PRT_VALUE* map,
		_In_	        PRT_VALUE* key,
		_In_	        PRT_BOOLEAN cloneKey,
		_In_	        PRT_VALUE* value,
		_In_	        PRT_BOOLEAN cloneValue);

	/** Returns a pointer to the value at key location in the map.
	* If key is not in the map, then stores a NULL value and returns a pointer to it.
	* If key is already in the map, then returns a pointer to the mapped value.
	* @param[in,out] map   A map to mutate.
	* @param[in]     key   The key to update (will be cloned if cloneKey is PRT_TRUE).
	* @param[in]     cloneKey Only set to false if key will be forever owned by this map.
	*/
	PRT_API PRT_VALUE** PRT_CALL_CONV PrtMapGetLValue(
		_Inout_	        PRT_VALUE* map,
		_In_	        PRT_VALUE* key,
		_In_	        PRT_BOOLEAN cloneKey,
		_In_	        PRT_TYPE* mapType
	);

	/** Updates the map at key.
	* If key is not in the map, then adds it.
	* If key is already in the map, then changes its mapping.
	* @param[in,out] map   A map to mutate.
	* @param[in]     key   The key to update (will be cloned).
	* @param[in]     value The value to which the key maps (will be cloned).
	*/
	PRT_API void PRT_CALL_CONV PrtMapUpdate(
		_Inout_	        PRT_VALUE* map,
		_In_	        PRT_VALUE* key,
		_In_	        PRT_VALUE* value);

	/** Remove the key from the map.
	* If the key is not in then map, then the map is unchanged.
	* @param[in,out] map   A map to mutate.
	* @param[in]     key   The key to remove.
	*/
	PRT_API void PRT_CALL_CONV PrtMapRemove(
		_Inout_	        PRT_VALUE* map,
		_In_	        PRT_VALUE* key);

	/** Gets the value to which this key maps.
	* The key must be present the map.
	* @param[in] map A map.
	* @param[in] key The key to lookup.
	* @returns The value to which the key maps. Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtMapGet(
		_In_	     PRT_VALUE* map,
		_In_	     PRT_VALUE* key);

	/** Converts a map to sequence of keys. Keys are returned in insertion order.
	* @param[in] map A map.
	* @returns The sequence of its keys. Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtMapGetKeys(_In_ PRT_VALUE* map);

	/** Converts a map to sequence of values. values are returned in insertion order.
	* @param[in] map A map.
	* @returns The sequence of its values (map image). Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtMapGetValues(_In_ PRT_VALUE* map);

	/** Returns true if the map contains key; false otherwise.
	* @param[in] map A map.
	* @param[in] key The key to lookup.
	* @returns Returns true if the map contains key; false otherwise.
	*/
	PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtMapExists(
		_In_	     PRT_VALUE* map,
		_In_	     PRT_VALUE* key);

	/** Gets the size of a map.
	* @param[in] map A map.
	* @returns The size of the map.
	*/
	PRT_API PRT_UINT32 PRT_CALL_CONV PrtMapSizeOf(_In_ PRT_VALUE* map);

	/** The hypothetical maximum number of keys that could be accessed in constant-time.
	* @param[in] map A map.
	* @returns The capacity of the map.
	*/
	PRT_API PRT_UINT32 PRT_CALL_CONV PrtMapCapacity(_In_ PRT_VALUE* map);

	/** Determines if value inhabits type.
	* @param[in] value The value to check.
	* @param[in] type  The type to check.
	* @returns `true` if value inhabits type, `false` otherwise.
	*/
	PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtInhabitsType(_In_ PRT_VALUE* value, _In_ PRT_TYPE* type);

	/** Determines if value is a null event or machine id.
	* @param[in] value The value to check.
	* @returns `true` if the null value, `false` otherwise.
	*/
	PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtIsNullValue(_In_ PRT_VALUE* value);

	/** Casts value to type.
	* Caller must know that type cast will succeed.
	* Method checks validity of type cast and causes an assertion failure if the case is invalid.
	* @param[in] value The value to cast (will be cloned).
	* @param[in] type  The type to cast (will be cloned).
	* @returns A copy of value that is a subtype of type. Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtCastValue(_In_ PRT_VALUE* value, _In_ PRT_TYPE* type);

	/** Converts value to type.
	* Caller must know that type convert will succeed.
	* Method checks validity of type conversion and causes an assertion failure if the case is invalid.
	* @param[in] value The value to convert (will be cloned).
	* @param[in] type  The type to convert (will be cloned).
	* @returns A converted value that is a subtype of type. Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtConvertValue(_In_ PRT_VALUE* value, _In_ PRT_TYPE* type);

	/** Returns a hash of this value.
	* @param[in] value The value to hash.
	* @returns The hash code.
	*/
	PRT_API PRT_UINT32 PRT_CALL_CONV PrtGetHashCodeValue(_In_ PRT_VALUE* value);

	/** Removes the value at index from the sequence, and shortens the sequence by one.
	* seq[index] must be defined. Removal causes:
	* For all i > index, if seq[i] is defined, then seq'[i - 1] = seq[i].
	* For all i < index, if seq[i] is defined, then seq'[i] = seq[i].
	* @param[in,out] seq   A sequence to mutate.
	* @param[in]     index An 0-based index s.t. 0 <= index < size(seq).
	*/
	PRT_API void PRT_CALL_CONV PrtRemoveByKey(_Inout_ PRT_VALUE* mapOrSeq, _In_ PRT_VALUE* key);

	/** Returns `true` if values are equivalent; `false` otherwise.
	* @param[in] value1 The first value.
	* @param[in] value2 The second value.
	* @returns `true` if values are equivalent; `false` otherwise.
	*/
	PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtIsEqualValue(
		_In_	     PRT_VALUE* value1,
		_In_	     PRT_VALUE* value2);

	/** Deeply clones a value.
	* @param[in] value The value to clone.
	* @returns The cloned value. Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtCloneValue(_In_ PRT_VALUE* value);

	/** Recursively frees a value. Should only be called on values created using PrtMkXValue()
	* @param[in,out] value The value to free.
	*/
	PRT_API void PRT_CALL_CONV PrtFreeValue(_Inout_ PRT_VALUE* value);

	/** Shallow test that value members are non-null.
	* @param[in] value The value to check.
	*/
	PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtIsValidValue(_In_ PRT_VALUE* value);

	/** Nondeterministic Boolean choice
	* @returns A nondeterministic Boolean value.  Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE* PRT_CALL_CONV PrtMkNondetBoolValue(void);

#ifdef __cplusplus
}
#endif
#endif
