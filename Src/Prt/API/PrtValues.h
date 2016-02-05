/**
* \file PrtValues.h
* \brief Functions and data structures for P values.
* Every P value is a combination of a type expression and an in-memory representation
* of the value. The type expression cannot be `any`, but `any` may appear as an inner type.
* Here are some examples:
*
* `true : bool`, `1 : int`, `null : event`, `null : model`, `(1, 2) : (any, int)`.
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
extern "C"{
#endif

#ifdef PRT_USE_IDL
#include "PrtValues_IDL.h"
#else

	typedef enum PRT_SPECIAL_EVENT
	{
		PRT_SPECIAL_EVENT_NULL = 0,  /**< The id of the null event */
		PRT_SPECIAL_EVENT_HALT = 1   /**< The id of the halt event */
	} PRT_SPECIAL_EVENT;

	/**
	* \enum
	* These are the kinds of values in the P type system.
	**/
	typedef enum PRT_VALUE_KIND
	{
		PRT_VALKIND_NULL = 0,    /**< The kind of the null value in type null */
		PRT_VALKIND_BOOL = 1,    /**< The kind of bool values                 */
		PRT_VALKIND_INT = 2,    /**< The kind of int values                  */
		PRT_VALKIND_EVENT = 3,    /**< The kind of event id values             */
		PRT_VALKIND_MID = 4,    /**< The kind of (model) machine id values   */
		PRT_VALKIND_FORGN = 5,    /**< The kind of all foreign values          */
		PRT_VALKIND_TUPLE = 6,    /**< The kind of all (named) tuple values    */
		PRT_VALKIND_SEQ = 7,    /**< The kind of all sequence values         */
		PRT_VALKIND_MAP = 8,    /**< The kind of all map values              */
		PRT_VALUE_KIND_COUNT = 9, /**< The number of value kinds               */
	} PRT_VALUE_KIND;

	/** A Union type to discriminate the Prt value */
	typedef struct PRT_VALUE {
		PRT_VALUE_KIND discriminator; /**< A value kind to discriminate the union */
		union
		{
			PRT_BOOLEAN bl;                 /**< A boolean value            */
			PRT_INT32   nt;                 /**< An integer value           */
			PRT_UINT32  ev;                 /**< An event id value          */
			struct PRT_MACHINEID *mid;      /**< A (model) machine id value */
			struct PRT_FORGNVALUE *frgn;	/**< A foreign value            */
			struct PRT_TUPVALUE *tuple;		/**< A (named) tuple value      */
			struct PRT_SEQVALUE *seq;		/**< A sequence value	        */
			struct PRT_MAPVALUE *map;		/**< A map value		        */
		} valueUnion;
	} PRT_VALUE;

	/** The id of a (model) machine is a combination of the id of the owner process and an id unique to that process.
	*/
	typedef struct PRT_MACHINEID
	{
		PRT_GUID   processId;
		PRT_UINT32 machineId;
	} PRT_MACHINEID;

	/** A tuple value is a (named) tuple represented as an array. */
	typedef struct PRT_TUPVALUE
	{
		PRT_UINT32 size;
		PRT_VALUE **values;   /**< Is an array of tuple args.    */
	} PRT_TUPVALUE;

	/** A sequence value is a sequence represented as a dynamic array. */
	typedef struct PRT_SEQVALUE
	{
		PRT_UINT32 size;     /**< The number of elements in the sequence. */
		PRT_UINT32 capacity; /**< The number of elements before resizing  */
		PRT_VALUE **values;  /**< An array of values in the sequence.     */
	} PRT_SEQVALUE;

	/** A map value is represented as a hash-table. */
	typedef struct PRT_MAPVALUE
	{
		PRT_UINT32   size;       /**< The number of elements in the map.      */
		PRT_UINT32   capNum;     /**< An opaque number related to the number of buckets */
		struct PRT_MAPNODE *first;    /**< First element inserted into the map. */
		struct PRT_MAPNODE *last;     /**< Last element inserted into the map. */
		struct PRT_MAPNODE **buckets; /**< An array of pointers to chained nodes.  */
	} PRT_MAPVALUE;

	/** A key-value node of a map. */
	typedef struct PRT_MAPNODE
	{
		PRT_VALUE *key;     /**< The key of this node. */
		PRT_VALUE *value;   /**< The value of this node. */
		struct PRT_MAPNODE *bucketNext;   /**< The next node in this bucket.         */
		struct PRT_MAPNODE *insertNext;   /**< The next node in insertion order.     */
		struct PRT_MAPNODE *insertPrev;   /**< The previous node in insertion order. */
	} PRT_MAPNODE;

	/** A foreign value is foreign type paired with a char *. */
	typedef struct PRT_FORGNVALUE
	{
		PRT_UINT16            typeTag;    /**< The index of the foreign type. */
		PRT_UINT64			  value;      /**< The foreign value. */
	} PRT_FORGNVALUE;
#endif

	/** The PRT_FORGN_MKDEF function is called whenever a default foreign value is created.
	*/
	typedef PRT_UINT64(PRT_CALL_CONV *PRT_FORGN_MKDEF)(_In_ PRT_UINT16 typeTag);

	/** The PRT_FORGN_CLONE function is called whenever a foreign value needs to be cloned.
	*   The cloning semantics depends on the memory management strategy of the client.
	*   @see PRT_FORGN_FREE
	*/
	typedef PRT_UINT64(PRT_CALL_CONV *PRT_FORGN_CLONE)(_In_ PRT_UINT16 typeTag, _In_ PRT_UINT64 frgnVal);

	/** The PRT_FORGN_FREE function is called whenever a foreign value will never be used again.
	*   The semantics of PRT_FORGN_FREE depends on the memory management strategy of the client.
	*   @see PRT_FORGN_CLONE
	*/
	typedef void(PRT_CALL_CONV *PRT_FORGN_FREE)(_In_ PRT_UINT16 typeTag, _Inout_ PRT_UINT64 frgnVal);

	/** The PRT_FORGN_GETHASHCODE function is called to get a hashcode for a foreign value.
	*   The semantics depends of the client's definition of value equality. If two values
	*   are equal, then the function must return the same hashcode.
	*   @see PRT_FORGN_GETHASHCODE
	*/
	typedef PRT_UINT32(PRT_CALL_CONV *PRT_FORGN_GETHASHCODE)(_In_ PRT_UINT16 typeTag, _In_ PRT_UINT64 frgnVal);

	/** The PRT_FORGN_TOSTRING function is called to convert the foreign value to string.
	*   The programmer should provide appropriate function for converting the value to string that needs to be printed for logging
	*   @see PRT_FORGN_TOSTRING
	*/
	typedef PRT_STRING(PRT_CALL_CONV *PRT_FORGN_TOSTRING)(_In_ PRT_UINT16 typeTag, _In_ PRT_UINT64 frgnVal);

	/** The PRT_FORGN_ISEQUAL function tests if two values are equal.
	*   Equality semantics is determined by the client. If two values
	*   are equal, then they should also have the same hashcode.
	*   @see PRT_FORGN_GETHASHCODE
	*/
	typedef PRT_BOOLEAN(PRT_CALL_CONV *PRT_FORGN_ISEQUAL)(_In_ PRT_UINT16 typeTag1, _In_ PRT_UINT64 frgnVal1, _In_ PRT_UINT16 typeTag2, _In_ PRT_UINT64 frgnVal2);

	/** Makes a default value of an abitrary type. The defaults (def) are as follows:
	* 1.  def(null)                = `null : null`.
	* 2.  def(any)                 = `null : null`.
	* 3.  def(bool)                = `false : bool`.
	* 4.  def(event)               = `null : event`.
	* 5.  def(foreign)             = `NULL pointer : foreign`.
	* 6.  def(machine)             = `null : machine`.
	* 7.  def(int)                 = `0 : int`.
	* 8.  def(map[S, T])           = `[] : map[S, T]`.
	* 9.  def(model)               = `null : model`.
	* 10. def((l1: S1,.., ln: Sn)) = `(l1 = def(S1),..., ln = def(Sn)) : (l1: S1,..., ln: Sn)`.
	* 11. def([S])                 = `[] : [S]`.
	* 12. def((S1,..,Sn))          = `(def(S1),..., def(S2)) : (S1,..., Sn)`.
	* @param[in] type A type expression (will be cloned).
	* @returns The default value of the type. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtMkDefaultValue(_In_ PRT_TYPE *type);

	/** Makes a boolean value.
	* @param[in] value A boolean value.
	* @returns A proper boolean value. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtMkBoolValue(_In_ PRT_BOOLEAN value);

	/** Makes an event value.
	* @param[in] value An event id.
	* @returns A proper event value. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtMkEventValue(_In_ PRT_UINT32 value);

	/** Makes an integer value.
	* @param[in] value An int value.
	* @returns A proper int value. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtMkIntValue(_In_ PRT_INT32 value);

	/** Makes null value.
	* The types null, event, machine, and model all share the null value.
	* The null value projected onto event is the id PRT_SPECIAL_EVENT_NULL.
	* The null value projected onto model / machine is the processId 0-0-0-0 with machineId PRT_SPECIAL_EVENT_NULL.
	* @returns A proper null value. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtMkNullValue();

	/** Makes an machine value.
	* @param[in] value A machine id.
	* @returns A proper machine value. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtMkMachineValue(_In_ PRT_MACHINEID value);

	/** Updates the functions related to foreign value processing.
	* @param[in] MkDefault A function to create the default foreign value
	* @param[in] Clone A function to clone a foreign value
	* @param[in] Free A function to free a foreign value
	* @param[in] Hash A function to hash a foreign value
	* @param[in] IsEqual A function to check if two foreign values are equal
	*/
	PRT_API void PRT_CALL_CONV PrtUpdateForeignFn(
		PRT_FORGN_MKDEF MkDefault,
		PRT_FORGN_CLONE Clone,
		PRT_FORGN_FREE Free,
		PRT_FORGN_GETHASHCODE Hash,
		PRT_FORGN_ISEQUAL IsEqual,
		PRT_FORGN_TOSTRING ToString);

	/** Makes a foreign value.
	* @param[in] typeTag The type tag for this type.
	* @param[in] value A pointer to foreign data (will be cloned).
	* @returns A proper foreign value. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtMkForeignValue(_In_ PRT_UINT16 typeTag, _In_ PRT_UINT64 value);

	/** Makes a fresh foreign value.
	* @param[in] type The foreign type.
	* @returns A proper foreign value. Caller is responsible for freeing.
	* @see PrtFreeValue
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtMkFreshForeignValue(_In_ PRT_TYPE *type);

	/** Cleans up the data needed for creating fresh foreign values.
	*/
	PRT_API void PRT_CALL_CONV PrtCleanupForeignData();

	/** Sets the value of a boolean.
	* @param[in,out] prmVal A primitive boolean value to mutate.
	* @param[in]     value The value to set.
	*/
	PRT_API void PRT_CALL_CONV PrtPrimSetBool(_Inout_ PRT_VALUE *prmVal, _In_ PRT_BOOLEAN value);

	/** Gets the value of a boolean.
	* @param[in] prmVal A primitive boolean value.
	* @returns A boolean.
	*/
	PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtPrimGetBool(_In_ PRT_VALUE *prmVal);

	/** Sets the value of an event.
	* @param[in,out] prmVal A primitive event value to mutate.
	* @param[in]     value The value to set.
	*/
	PRT_API void PRT_CALL_CONV PrtPrimSetEvent(_Inout_ PRT_VALUE *prmVal, _In_ PRT_UINT32 value);

	/** Gets the value of an event.
	* @param[in] prmVal A primitive event value.
	* @returns An event id.
	*/
	PRT_API PRT_UINT32 PRT_CALL_CONV PrtPrimGetEvent(_In_ PRT_VALUE *prmVal);

	/** Sets the value of an int.
	* @param[in,out] prmVal A primitive int value to mutate.
	* @param[in]     value The value to set.
	*/
	PRT_API void PRT_CALL_CONV PrtPrimSetInt(_Inout_ PRT_VALUE *prmVal, _In_ PRT_INT32 value);

	/** Gets the value of an integer.
	* @param[in] prmVal A primitive int value.
	* @returns An integer.
	*/
	PRT_API PRT_INT32 PRT_CALL_CONV PrtPrimGetInt(_In_ PRT_VALUE *prmVal);

	/** Sets the value of an Machine.
	* @param[in,out] prmVal A primitivemachinevalue to mutate.
	* @param[in]     value The value to set.
	*/
	PRT_API void PRT_CALL_CONV PrtPrimSetMachine(_Inout_ PRT_VALUE *prmVal, _In_ PRT_MACHINEID value);

	/** Gets the value of an Machine.
	* @param[in] prmVal A primitive machine value.
	* @returns A machine machine
	*/
	PRT_API PRT_MACHINEID PRT_CALL_CONV PrtPrimGetMachine(_In_ PRT_VALUE *prmVal);

	/** Sets an element in a (named) tuple by index.
	* @param[in,out] tuple A (named) tuple to mutate.
	* @param[in]     index A 0-based element index.
	* @param[in]     value The value to set (will be cloned).
	*/
	PRT_API void PRT_CALL_CONV PrtTupleSet(_Inout_ PRT_VALUE *tuple, _In_ PRT_UINT32 index, _In_ PRT_VALUE *value);

	/** Sets an element in a (named) tuple by index.
	* @param[in,out] tuple A (named) tuple to mutate.
	* @param[in]     index A 0-based element index.
	* @param[in]     value The value to set (will be cloned if cloneValue is PRT_TRUE).
	* @param[in]     cloneValue Only set to PRT_FALSE if value will be forever owned by this tuple.
	*/
	PRT_API void PRT_CALL_CONV PrtTupleSetEx(_Inout_ PRT_VALUE *tuple, _In_ PRT_UINT32 index, _In_ PRT_VALUE *value, _In_ PRT_BOOLEAN cloneValue);

	/** Gets an element in a (named) tuple without cloning. Only used for internal manipulation of state variables.
	* @param[in] tuple A (named) tuple.
	* @param[in] index A 0-based element index.
	* @returns The element at index i.
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtTupleGetNC(_In_ PRT_VALUE *tuple, _In_ PRT_UINT32 index);

	/** Gets an element in a sequence without cloning. Only used for internal manipulation of state variables.
	* @param[in] seq   A sequence.
	* @param[in] index A 0-based index s.t. 0 <= index < size(seq).
	* @returns The value at index.
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtSeqGetNC(_In_ PRT_VALUE *seq, _In_ PRT_VALUE *index);

	/** Gets a value from a map without cloning. Only used for internal manipulation of state variables.
	* The key must be present the map.
	* @param[in] map A map.
	* @param[in] key The key to lookup.
	* @returns The value to which the key maps.
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtMapGetNC(_In_ PRT_VALUE *map, _In_ PRT_VALUE* key);

	/** Gets an element in a (named) tuple by index.
	* @param[in] tuple A (named) tuple.
	* @param[in] index A 0-based element index.
	* @returns The element at index i. Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtTupleGet(_In_ PRT_VALUE *tuple, _In_ PRT_UINT32 index);

	/** Updates the sequence at index.
	* @param[in,out] seq   A sequence to mutate.
	* @param[in]     index The name of the element to set. A value must already exist at this index.
	* @param[in]     value The value to store at index (will be cloned).
	*/
	PRT_API void PRT_CALL_CONV PrtSeqUpdate(_Inout_ PRT_VALUE *seq, _In_ PRT_VALUE *index, _In_ PRT_VALUE *value);

	/** Updates the sequence at index.
	* @param[in,out] seq   A sequence to mutate.
	* @param[in]     index The name of the element to set. A value must already exist at this index.
	* @param[in]     value The value to store at index (will be cloned if cloneValue is PRT_TRUE).
	* @param[in]     cloneValue Only set to PRT_FALSE if value will be forever owned by this seq.
	*/
	PRT_API void PRT_CALL_CONV PrtSeqUpdateEx(_Inout_ PRT_VALUE *seq, _In_ PRT_VALUE *index, _In_ PRT_VALUE *value, _In_ PRT_BOOLEAN cloneValue);

	/** Inserts value into the sequence at index.
	* Index cannot be larger than the size of the sequence. Insertion causes:
	* seq'[index] = value.
	* For all i >= index, if seq[i] is defined, then seq'[i + 1] = seq[i].
	* For all i < index, if seq[i] is defined, then seq'[i] = seq[i].
	* @param[in,out] seq   A sequence to mutate.
	* @param[in]     index An 0-based index s.t. 0 <= index <= size(seq).
	* @param[in]     value The value to store at index (will be cloned).
	*/
	PRT_API void PRT_CALL_CONV PrtSeqInsert(_Inout_ PRT_VALUE *seq, _In_ PRT_VALUE *index, _In_ PRT_VALUE* value);

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
	PRT_API void PRT_CALL_CONV PrtSeqInsertEx(_Inout_ PRT_VALUE *seq, _In_ PRT_VALUE *index, _In_ PRT_VALUE* value, _In_ PRT_BOOLEAN cloneValue);

	/** Removes the value at index from the sequence, and shortens the sequence by one.
	* seq[index] must be defined. Removal causes:
	* For all i > index, if seq[i] is defined, then seq'[i - 1] = seq[i].
	* For all i < index, if seq[i] is defined, then seq'[i] = seq[i].
	* @param[in,out] seq   A sequence to mutate.
	* @param[in]     index An 0-based index s.t. 0 <= index < size(seq).
	*/
	PRT_API void PRT_CALL_CONV PrtSeqRemove(_Inout_ PRT_VALUE *seq, _In_ PRT_VALUE *index);

	/** Gets the sequence at index.
	* @param[in] seq   A sequence.
	* @param[in] index A 0-based index s.t. 0 <= index < size(seq).
	* @returns The value at index. Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtSeqGet(_In_ PRT_VALUE *seq, _In_ PRT_VALUE *index);

	/** Gets the size of a sequence.
	* @param[in] seq A sequence.
	* @returns The size of the sequence.
	*/
	PRT_API PRT_UINT32 PRT_CALL_CONV PrtSeqSizeOf(_In_ PRT_VALUE *seq);

	/** Updates the map at key.
	* If key is not in the map, then adds it.
	* If key is already in the map, then changes its mapping.
	* @param[in,out] map   A map to mutate.
	* @param[in]     key   The key to update (will be cloned if cloneKey is PRT_TRUE).
	* @param[in]     cloneKey Only set to false if key will be forever owned by this map.
	* @param[in]     value The value to which the key maps (will be cloned if cloneValue is PRT_TRUE).
	* @param[in]     cloneValue Only set to PRT_FALSE if value will be forever owned by this map.
	*/
	PRT_API void PRT_CALL_CONV PrtMapUpdateEx(_Inout_ PRT_VALUE *map, _In_ PRT_VALUE *key, _In_ PRT_BOOLEAN cloneKey, _In_ PRT_VALUE *value, _In_ PRT_BOOLEAN cloneValue);

	/** Updates the map at key.
	* If key is not in the map, then adds it.
	* If key is already in the map, then changes its mapping.
	* @param[in,out] map   A map to mutate.
	* @param[in]     key   The key to update (will be cloned).
	* @param[in]     value The value to which the key maps (will be cloned).
	*/
	PRT_API void PRT_CALL_CONV PrtMapUpdate(_Inout_ PRT_VALUE *map, _In_ PRT_VALUE* key, _In_ PRT_VALUE* value);

	/** Inserts (key, value) in map.
	* requires that key is not already in the map
	* @param[in,out] map   A map to mutate.
	* @param[in]     key   The key to update (will be cloned if cloneKey is PRT_TRUE).
	* @param[in]     cloneKey Only set to false if key will be forever owned by this map.
	* @param[in]     value The value to which the key maps (will be cloned if cloneValue is PRT_TRUE).
	* @param[in]     cloneValue Only set to PRT_FALSE if value will be forever owned by this map.
	*/
	PRT_API void PRT_CALL_CONV PrtMapInsertEx(_Inout_ PRT_VALUE *map, _In_ PRT_VALUE* key, _In_ PRT_BOOLEAN cloneKey, _In_ PRT_VALUE* value, _In_ PRT_BOOLEAN cloneValue);

	/** Inserts (key, value) in map.
	* requires that key is not already in the map
	* @param[in,out] map   A map to mutate.
	* @param[in]     key   The key to update (will be cloned).
	* @param[in]     value The value to which the key maps (will be cloned).
	*/
	PRT_API void PRT_CALL_CONV PrtMapInsert(_Inout_ PRT_VALUE *map, _In_ PRT_VALUE* key, _In_ PRT_VALUE* value);

	/** Remove the key from the map.
	* If the key is not in then map, then the map is unchanged.
	* @param[in,out] map   A map to mutate.
	* @param[in]     key   The key to remove.
	*/
	PRT_API void PRT_CALL_CONV PrtMapRemove(_Inout_ PRT_VALUE *map, _In_ PRT_VALUE *key);

	/** Gets the value to which this key maps.
	* The key must be present the map.
	* @param[in] map A map.
	* @param[in] key The key to lookup.
	* @returns The value to which the key maps. Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtMapGet(_In_ PRT_VALUE *map, _In_ PRT_VALUE* key);

	/** Converts a map to sequence of keys. Keys are returned in insertion order.
	* @param[in] map A map.
	* @returns The sequence of its keys. Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtMapGetKeys(_In_ PRT_VALUE *map);

	/** Converts a map to sequence of values. values are returned in insertion order.
	* @param[in] map A map.
	* @returns The sequence of its values (map image). Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtMapGetValues(_In_ PRT_VALUE *map);

	/** Returns true if the map contains key; false otherwise.
	* @param[in] map A map.
	* @param[in] key The key to lookup.
	* @returns Returns true if the map contains key; false otherwise.
	*/
	PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtMapExists(_In_ PRT_VALUE *map, _In_ PRT_VALUE *key);

	/** Gets the size of a map.
	* @param[in] map A map.
	* @returns The size of the map.
	*/
	PRT_API PRT_UINT32 PRT_CALL_CONV PrtMapSizeOf(_In_ PRT_VALUE *map);

	/** The hypothetical maximum number of keys that could be accessed in constant-time.
	* @param[in] map A map.
	* @returns The capacity of the map.
	*/
	PRT_API PRT_UINT32 PRT_CALL_CONV PrtMapCapacity(_In_ PRT_VALUE *map);

	/** Determines if value inhabits type.
	* @param[in] value The value to check.
	* @param[in] type  The type to check.
	* @returns `true` if value inhabits type, `false` otherwise.
	*/
	PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtInhabitsType(_In_ PRT_VALUE* value, _In_ PRT_TYPE *type);

	/** Determines if value is a null event, id, or model.
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
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtCastValue(_In_ PRT_VALUE* value, _In_ PRT_TYPE *type);

	/** Returns a hash of this value.
	* @param[in] value The value to hash.
	* @returns The hash code.
	*/
	PRT_API PRT_UINT32 PRT_CALL_CONV PrtGetHashCodeValue(_In_ PRT_VALUE *value);

	/** Returns `true` if values are equivalent; `false` otherwise.
	* @param[in] value1 The first value.
	* @param[in] value2 The second value.
	* @returns `true` if values are equivalent; `false` otherwise.
	*/
	PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtIsEqualValue(_In_ PRT_VALUE *value1, _In_ PRT_VALUE *value2);

	/** Deeply clones a value.
	* @param[in] value The value to clone.
	* @returns The cloned value. Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtCloneValue(_In_ PRT_VALUE *value);

	/** Recursively frees a value. Should only be called on values created using PrtMkXValue()
	* @param[in,out] value The value to free.
	*/
	PRT_API void PRT_CALL_CONV PrtFreeValue(_Inout_ PRT_VALUE *value);

	/** Shallow test that value members are non-null.
	* @param[in] value The value to check.
	*/
	PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtIsValidValue(_In_ PRT_VALUE *value);

	/** Nondeterministic Boolean choice
	* @returns A nondeterministic Boolean value.  Caller is responsible for freeing.
	*/
	PRT_API PRT_VALUE * PRT_CALL_CONV PrtMkNondetBoolValue();

#ifdef __cplusplus
}
#endif
#endif