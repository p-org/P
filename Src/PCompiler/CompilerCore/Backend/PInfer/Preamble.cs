namespace Plang.Compiler.Backend.PInfer
{
    internal class PreambleConstants
    {
        internal static string CheckEventTypeFunName = "checkEventType";
        internal static string StaticMethods = $"private boolean {CheckEventTypeFunName}(Event e, String name) {{ return e.name.equals(name); }}";

        public static string CheckEventType(string varname, string eventType)
        {
            return $"{CheckEventTypeFunName}({varname}, \"{eventType}\")";
        }
    }
}