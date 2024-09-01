namespace Discord_OpenAI.Util.Extra
{
    internal class LogUtil
    {
        /* VARIABLES */

        private static DateTime time = DateTime.UtcNow;

        /* METHODS */

        public static void Log(string prefix, string message)
        {
            Console.WriteLine(Output(prefix, message));
        }

        public static void Error(string prefix, string message, string error = "")
        {
            Console.Error.WriteLine(Output(prefix, message, error));

        }

        public static void Separator()
        {
            Console.WriteLine("------------------------------------------------------------------------------------------");
        }

        public static async Task LogAsync(string prefix, string message)
        {
            await Console.Out.WriteLineAsync(Output(prefix, message));
        }

        public static async Task ErrorAsync(string prefix, string message, string error = "")
        {
            await Console.Error.WriteLineAsync(Output(prefix, message, error));
        }

        /* HELPERS */

        private static string Output(string prefix, string message, string error = "")
        {
            bool isError = !string.IsNullOrEmpty(error);
            string date = $"{time.Hour}:{time.Minute}:{time.Second}";

            return $"[{date} - {(isError ? "ERROR" : "INFO")}] {(!string.IsNullOrEmpty(prefix) ? $"{prefix}: " : "")}{message}{(isError ? $"\n-> {error}" : "")}";
        }
    }
}