using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class PermissionType : PLanguageType
    {
        public PermissionType(Machine machine) : base(TypeKind.Base)
        {
            origin = machine;
            EventSet = machine.Receives;
            InitAllowedPermissions();
        }

        public PermissionType(Interface pInterface) : base(TypeKind.Base)
        {
            origin = pInterface;
            EventSet = pInterface.ReceivableEvents;
            InitAllowedPermissions();
        }

        public PermissionType(NamedEventSet eventSet) : base(TypeKind.Base)
        {
            origin = eventSet;
            EventSet = eventSet;
            InitAllowedPermissions();
        }

        private void InitAllowedPermissions()
        {
            allowedPermissions = new Lazy<IReadOnlyList<PEvent>>(() => EventSet.Events.ToList());
        }

        private readonly IPDecl origin;
        public IEventSet EventSet { get; }

        public override string OriginalRepresentation => origin.Name;
        public override string CanonicalRepresentation => origin.Name;

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            if (otherType is PermissionType permission)
            {
                return permission.EventSet.IsSame(EventSet);
            }
            return false;
        }

        public override PLanguageType Canonicalize()
        {
            return this;
        }

        private Lazy<IReadOnlyList<PEvent>> allowedPermissions;
        public override IReadOnlyList<PEvent> AllowedPermissions => allowedPermissions.Value;
    }
}