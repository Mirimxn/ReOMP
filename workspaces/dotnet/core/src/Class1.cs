namespace OMP.LSWTSS
{
    public class Class1
    {
    }

    public static class OmpLog
    {
        public static void Info(string category, string message) { }
        public static void Info(string category, string message, System.Exception ex) { }
        public static void Warn(string category, string message) { }
        public static void Warn(string category, string message, System.Exception ex) { }
        public static void Error(string category, string message) { }
        public static void Error(string category, string message, System.Exception ex) { }
        public static void LogDllInventory(string category, string message) { }
        public static void LogLoadedModulesSnapshot(string message) { }
        public static void Flush() { }
    }
}