namespace Plang.Compiler.Backend.PInfer
{
    internal class PreambleConstants
    {
        internal static string StaticMethods = @"
            public static bool eq(Object o1, Object o2) {
                return Objects.equals(o1, o2);
            }
        ";
    }
}