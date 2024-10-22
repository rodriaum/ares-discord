using Lombok.NET;

namespace Ares.src.Backend.Database
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