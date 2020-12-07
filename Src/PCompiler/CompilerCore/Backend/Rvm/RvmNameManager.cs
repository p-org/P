/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

using System;
using System.Linq;

namespace Plang.Compiler.Backend.Rvm
{
    internal class RvmNameManager : NameManagerBase
    {
        public RvmNameManager(string namePrefix) : base(namePrefix)
        {
        }

        protected override string ComputeNameForDecl(IPDecl decl)
        {
            string name = decl.Name;

            switch (decl)
            {
                case PEvent pEvent:
                    if (pEvent.IsNullEvent)
                    {
                        name = "DefaultEvent";
                    }

                    if (pEvent.IsHaltEvent)
                    {
                        name = "PHalt";
                    }

                    return name;

                case Interface _:
                    return "I_" + name;
                default:
                  name = string.IsNullOrEmpty(name) ? "Anon" : name;
                  if (name.StartsWith("$"))
                  {
                      name = "TMP_" + name.Substring(1);
                  }

                  return UniquifyName(name);
            }
        }

        public string GetJavaRuntimeMonitorName(Machine m)
        {
            return $"{GetNameForDecl(m)}RuntimeMonitor";
        }

        public string GetAspectClassName()
        {
            return $"PSpecMonitorAspect";
        }

        public string GetSpecConstructorName(Machine m)
        {
            return "__CONSTRUCTOR";
        }

        public string GetJavaEventHandlerName(Machine m, PEvent e)
        {
            return $"{GetNameForDecl(m)}_{GetRvmEventName(e)}Event";
        }

        public string GetRvmEventName(PEvent e)
        {
            return GetNameForDecl(e);
        }

        public string GetEventHandlerName(State s, PEvent e)
        {
            return $"{GetNameForDecl(s)}_{GetNameForDecl(e)}";
        }

        public string GetEventsClassName()
        {
            return "Events";
        }

        public string GetEventFileName()
        {
            return $"{GetEventsClassName()}.java";
        }

        public string GetRvmSpecName(Machine m)
        {
            return GetNameForDecl(m);
        }

        public string GetStateName(State s)
        {
            return GetNameForDecl(s);
        }

        public string GetExitHandlerName()
        {
            return "exit";
        }

        public string GetEntryHandlerName()
        {
            return "entry";
        }

        public string GetChangeStateFunctionName()
        {
            return "changeStateTo";
        }

        public string GetHandleRaisedEventFunctionName()
        {
            return "handleRaisedEvent";
        }

        public string GetNextStateArgumentName()
        {
            return "nextState";
        }

        public string GetStateVariableName()
        {
            return "state";
        }

        public string GetGotoStmtExceptionName()
        {
            return "GotoStmtException";
        }

        public string GetGotoStmtExceptionStateGetterName()
        {
            return "getState";
        }

        public string GetRaiseStmtExceptionName()
        {
            return "RaiseStmtException";
        }

        public string GetRaiseStmtExceptionEventGetterName()
        {
            return "getEvent";
        }

        public string GetExceptionPayloadGetterName()
        {
            return "getPayload";
        }

        public string GetLocalVarName(Variable v)
        {
            return GetNameForDecl(v);
        }

        public string GetExceptionVariableName()
        {
            return "exception";
        }


        public string GetDefaultPayloadTypeName()
        {
            return "Object";
        }

        public string GetPayloadArgumentName()
        {
            return "payload";
        }

        public string GetMaybePayloadArgumentName()
        {
            return "maybePayload";
        }

        public string GetStateEventHandlerName(PEvent e)
        {
            return $"{GetNameForDecl(e)}Handler";
        }

        public string GetStateInterfaceName()
        {
            return "IState";
        }

        public string GetStateBaseClassName()
        {
            return "StateBase";
        }

        public string GetStateBaseFileName()
        {
            return $"{GetStateBaseClassName()}.java";
        }

        public string GetStateClassName(State s)
        {
            return $"{FirstCharToUpper(GetNameForDecl(s))}State";
        }

        public string GetEventInterfaceName()
        {
            return "IEvent";
        }

        public string GetEventArgumentName()
        {
            return "e";
        }

        public string GetUnqualifiedEventClassName(PEvent e)
        {
            return $"{FirstCharToUpper(GetNameForDecl(e))}Event";
        }

        public string GetQualifiedEventClassName(PEvent e)
        {
            return $"{GetEventsClassName()}.{GetUnqualifiedEventClassName(e)}";
        }

        public string GetEventHandleFunctionName()
        {
            return "handle";
        }

        public string GetSafeCloneFunctionName()
        {
            return "safeClone";
        }

        public string GetNamedTupleTypeName()
        {
            return "NamedTuple";
        }

        public string GetTupleTypeName()
        {
            return "Tuple";
        }

        public string GetTupleFieldGetter()
        {
            return "getField";
        }

        public string GetTupleFieldSetter()
        {
            return "setField";
        }

        public string GetTupleFieldName(string name)
        {
            return name;
        }

        public string GetMapTypeName()
        {
            return "PMap";
        }

        public string GetSeqTypeName()
        {
            return "PSeq";
        }

        public string GetSetTypeName()
        {
            return "PSet";
        }

        public string GetSeqSetIndexFunc()
        {
            return "setIndex";
        }

        public string GetMapPutFunc()
        {
            return "put";
        }

        public string GetMapCloneKeysFunc()
        {
            return "cloneKeys";
        }

        public string GetMapCloneValuesFunc()
        {
            return "cloneValues";
        }

        public string GetGetFunc()
        {
            return "get";
        }

        public string GetInsertFunc()
        {
            return "insert";
        }

        public string GetRemoveFunc()
        {
            return "remove";
        }

        public string GetSizeFunc()
        {
            return "size";
        }

        public string GetContainsFunc(bool isMap)
        {
            if (isMap)
            {
                return "containsKey";
            }
            else
            {
                return "contains";
            }
        }

        public string GetValueInterfaceName()
        {
            return "IValue";
        }

        public string GetValueInterfaceSafeEqualsName()
        {
            return "safeEquals";
        }

        public string GetEnumTypeName(PEnum pEnum)
        {
            return pEnum.Name;
        }

        public string GetEnumElementName(EnumElem elem)
        {
            return elem.Name;
        }

        public string GetEnumValueName()
        {
            return "value";
        }

        public string GetEnumValueGetterName()
        {
            return "getValue";
        }

        public string GetJavaTypeName(PLanguageType type, bool isVar = false)
        {
            switch (type.Canonicalize())
            {
                case DataType _:
                    throw new NotImplementedException("DateType is not implemented.");

                case EnumType enumType:
                    return $"EnumValue<{GetEnumTypeName(enumType.EnumDecl)}>";

                case ForeignType _:
                    throw new NotImplementedException("ForeignType is not implemented.");

                case MapType _:
                    return GetMapTypeName();

                case NamedTupleType _:
                    return GetNamedTupleTypeName();

                case PermissionType _:
                    throw new NotImplementedException("PermissionType is not implemented.");

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Any):
                    throw new NotImplementedException("AnyType is not implemented.");

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "BoolValue";

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "IntValue";

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "FloatValue";

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.String):
                    return "StringValue";

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Event):
                    return GetEventInterfaceName();

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Machine):
                    throw new NotImplementedException("MachineType is not implemented.");

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Null):
                    return isVar ? "" : "void";

                case SequenceType _:
                    return GetSeqTypeName();

                case SetType _:
                    return GetSetTypeName();

                case TupleType _:
                    return GetTupleTypeName();

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private string FirstCharToUpper(string input)
        {
            if (String.IsNullOrEmpty(input))
                throw new ArgumentException("Input is null or empty.");
            return input.First().ToString().ToUpper() + input.Substring(1);
        }
    }
}
