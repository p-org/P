using System;
using PrtSharp.Values;
using PrtSharp.Exceptions;

namespace PrtSharp
{
    public static class PrtValues
    {
        public static PrtBool Box(bool value)
        {
            return value;
        }

        public static PrtInt Box(long value)
        {
            return new PrtInt(value);
        }

        public static PrtInt Box(int value)
        {
            return new PrtInt(value);
        }

        public static PrtInt Box(short value)
        {
            return new PrtInt(value);
        }

        public static PrtInt Box(byte value)
        {
            return new PrtInt(value);
        }

        public static PrtFloat Box(double value)
        {
            return new PrtFloat(value);
        }

        public static PrtFloat Box(float value)
        {
            return new PrtFloat(value);
        }

        public static PrtBool SafeEquals(IPrtValue val1, IPrtValue val2)
        {
            return ReferenceEquals(val1, val2) || (val1 != null && val1.Equals(val2));
        }

        public static bool PrtInhabitsType(IPrtValue value, PrtType type)
        {
            if (type is PrtAnyType)
            {
                return true;
            }
            else if (value == null)
            {
                return (type is PrtNullType || type is PrtEventType || type is PrtMachineType);
            }
            else if (type is PrtEnumType)
            {
                PrtEnumType enumType = type as PrtEnumType;
                PrtInt intValue = value as PrtInt;
                if (intValue == null) return false;
                return enumType.enumConstants.ContainsKey(intValue);
            }
            else if (type is PrtIntType)
            {
                return value is PrtInt;
            }
            else if (type is PrtFloatType)
            {
                return value is PrtFloat;
            }
            else if (type is PrtBoolType)
            {
                return value is PrtBool;
            }
            else if (type is PrtEventType)
            {
                return value is IEventWithPayload;
            }
            else if (type is PrtPermissionType interfaceType)
            {
                var interValue = value as PMachineValue;
                if (interValue == null)
                {
                    return false;
                }
                else
                {
                    if (interValue.Permissions == null)
                    {
                        return interfaceType.permissions == null;
                    }
                    else
                    {
                        if (interfaceType.permissions == null)
                        {
                            return false;
                        }
                    }

                    if (interValue.Permissions.Count != interfaceType.permissions.Count)
                    {
                        return false;
                    }
                    else
                    {
                        foreach (var ev in interValue.Permissions)
                        {
                            if (!(interfaceType).permissions.Contains(ev))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
            }
            else if (type is PrtNamedTupleType) // must come before PrtTupleType since PrtNamedTupleType derives from PrtTupleType
            {

                var nmtupType = type as PrtNamedTupleType;
                var nmtupVal = value as PrtNamedTuple;
                if (nmtupVal == null) return false;
                if (nmtupVal.fieldValues.Count != nmtupType.fieldTypes.Count) return false;
                for (int i = 0; i < nmtupVal.fieldValues.Count; i++)
                {
                    if (nmtupVal.fieldNames[i] != nmtupType.fieldNames[i]) return false;
                }
                for (int i = 0; i < nmtupVal.fieldValues.Count; i++)
                {
                    if (!PrtInhabitsType(nmtupVal.fieldValues[i], nmtupType.fieldTypes[i])) return false;
                }
                return true;
            }
            else if (type is PrtTupleType)
            {
                // TODO: Remaining
                /*var tupType = type as PrtTupleType;
                var tupVal = value as PrtTupleValue;
                if (tupVal == null) return false;
                if (tupVal.fieldValues.Count != tupType.fieldTypes.Count) return false;
                for (int i = 0; i < tupVal.fieldValues.Count; i++)
                {
                    if (!PrtInhabitsType(tupVal.fieldValues[i], tupType.fieldTypes[i])) return false;
                }*/
                return true;
            }
            else if (type is PrtMapType)
            {
                var mapType = type as PrtMapType;
                var mapVal = value as PrtMap;
                if (mapVal == null) return false;
                foreach (var p in mapVal)
                {
                    if (!PrtInhabitsType(p.Key, mapType.keyType)) return false;
                    if (!PrtInhabitsType(p.Value, mapType.valType)) return false;
                }
                return true;
            }
            else if (type is PrtSeqType)
            {
                var seqType = type as PrtSeqType;
                var seqVal = value as PrtSeq;
                if (seqVal == null) return false;
                foreach (var elem in seqVal)
                {
                    if (!PrtInhabitsType(elem, seqType.elemType)) return false;
                }
                return true;
            }
            else
            {
                throw new PrtInhabitsTypeException("Unknown type in PrtInhabitsType");
            }
        }

        
        public static IPrtValue PrtCastValue(IPrtValue value, PrtType type)
        {
            //todo: Needs to be fixed
            /*if (!PrtInhabitsType(value, type))
                throw new PrtInhabitsTypeException(
                    $"value {value.ToString()} is not a member of type {type.ToString()}");*/
            return value.Clone();
        }
    }

}