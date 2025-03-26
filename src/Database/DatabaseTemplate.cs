namespace Ares.Database;

internal interface DatabaseTemplate
{
    void Connect();
    void Close();
    bool IsConnected();
}