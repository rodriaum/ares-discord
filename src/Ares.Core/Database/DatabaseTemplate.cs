namespace Ares.Core.Database;

internal interface DatabaseTemplate
{
    Task ConnectAsync();
    Task CloseAsync();
    bool IsConnected();
}