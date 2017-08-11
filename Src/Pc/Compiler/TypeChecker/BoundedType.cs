namespace Microsoft.Pc.TypeChecker
{
    public class BoundedType : PLanguageType
    {
        public BoundedType(string name, EventSet eventSet) : base(name, TypeKind.Base, $"any<{eventSet.Name}>")
        {
            EventSet = eventSet;
        }

        public EventSet EventSet { get; }
    }
}