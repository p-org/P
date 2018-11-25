using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.Types
{
    public class PermissionType : PLanguageType
    {
        private readonly IPDecl origin;

        public PermissionType(Machine machine) : base(TypeKind.Base)
        {
            origin = machine;
            AllowedPermissions = new Lazy<IReadOnlyList<PEvent>>(() => machine.Receives.Events.ToList());
        }

        public PermissionType(Interface pInterface) : base(TypeKind.Base)
        {
            origin = pInterface;
            AllowedPermissions = new Lazy<IReadOnlyList<PEvent>>(() => pInterface.ReceivableEvents.Events.ToList());
        }

        public PermissionType(NamedEventSet eventSet) : base(TypeKind.Base)
        {
            origin = eventSet;
            AllowedPermissions = new Lazy<IReadOnlyList<PEvent>>(() => eventSet.Events.ToList());
        }

        public override string OriginalRepresentation => origin.Name;
        public override string CanonicalRepresentation => origin.Name;

        public override Lazy<IReadOnlyList<PEvent>> AllowedPermissions { get; }

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            if (otherType is PermissionType permission)
            {
                var eventSet1 = new EventSet();
                var eventSet2 = new EventSet();
                eventSet1.AddEvents(otherType.AllowedPermissions.Value);
                eventSet2.AddEvents(AllowedPermissions.Value);
                return eventSet1.IsSame(eventSet2);
            }

            return false;
        }

        public override PLanguageType Canonicalize()
        {
            return this;
        }
    }
}