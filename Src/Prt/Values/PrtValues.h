/**
* \file PrtValues.h
* \brief Functions and data structures for P values.
* Every P value is a combination of a type expression and an in-memory representation
* of the value. The type expression cannot be `any`, but `any` may appear as an inner type.
* Here are some examples:
*
* `true : bool`, `1 : int`, `null : event`, `null : mid`, `(1, 2) : (any, int)`.
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

/** A value is a pointer to a type expression*/
typedef PRT_TYPE *PRT_VALUE;

/** A primitive value is primitive type paired with a primitive value. */
typedef struct PRT_PRIMVALUE
{
	PRT_TYPE type;        /**< Must be a primitive type. */
	union
	{
		PRT_BOOLEAN bl;   /**< A boolean value.    */
		PRT_UINT32  ev;   /**< An event id.        */
		PRT_INT32   nt;   /**< A signed integer.   */
		PRT_UINT32  id;   /**< A machine id.       */
		PRT_UINT32  md;   /**< A model machine id. */
	} value;              /**< A primitive value is one of the above. */
} PRT_PRIMVALUE;

/** A foreign value is foreign type paired with a void *. */
typedef struct PRT_FORGNVALUE
{
	PRT_TYPE type;    /**< Must be a foreign type.         */
	void    *value;   /**< A pointer to the foreign value. */
} PRT_FORGNVALUE;

/** A tuple value is a (named) tuple represented as an array. */
typedef struct PRT_TUPVALUE
{
	PRT_TYPE   type;     /**< Must be a (named) tuple type. */
	PRT_VALUE *values;   /**< Is an array of tuple args.    */
} PRT_TUPVALUE;

/** A sequence value is a sequence represented as a dynamic array. */
typedef struct PRT_SEQVALUE
{
	PRT_TYPE   type;     /**< Must be a sequence type.                */
	PRT_UINT32 size;     /**< The number of elements in the sequence. */
	PRT_VALUE *values;   /**< An array of values in the sequence.     */
} PRT_SEQVALUE;

/** A map value is represented as a hash-table. */
typedef struct PRT_MAPVALUE
{
	PRT_TYPE     type;       /**< Must be a map type.                     */
	PRT_UINT32   size;       /**< The number of elements in the map.      */
	PRT_UINT32   nBuckets;   /**< The number of buckets in the hash table */
	struct PRT_MAPNODE *buckets;   /**< An array of values in the sequence.  */
	struct PRT_MAPNODE *first;     /**< First element inserted into the map. */
} PRT_MAPVALUE;

/** A key-value node of a map. */
typedef struct PRT_MAPNODE
{
	PRT_VALUE key;     /**< The key of this node. */
	PRT_VALUE value;   /**< The value of this node. */
	struct PRT_MAPNODE *bucketNext;   /**< The next node in this bucket.     */
	struct PRT_MAPNODE *insertNext;   /**< The next node in insertion order. */
} PRT_MAPNODE;

#endif