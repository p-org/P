namespace Plang.Compiler.Backend.PInfer
{
    internal class PreambleConstants
    {
        internal static string CheckEventTypeFunName = "checkEventType";

        public static string CheckEventType(string varname, string eventType)
        {
            return $"{CheckEventTypeFunName}({varname}, \"{eventType}\")";
        }
    }
}