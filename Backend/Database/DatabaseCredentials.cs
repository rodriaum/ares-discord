using Lombok.NET;

namespace Discord_OpenAI.Backend.Database
{
    [AllArgsConstructor]
    internal partial class DatabaseCredentials
    {
        public string Host { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
        public int Port { get; set; }
    }
}