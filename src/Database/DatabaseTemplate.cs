namespace Ares.Database;

internal interface DatabaseTemplate
{
    Task ConnectAsync();
    Task CloseAsync();
    bool IsConnected();
}