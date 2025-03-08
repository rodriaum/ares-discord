namespace Ares.src.Database;

internal interface DatabaseTemplate
{
    void Connect();
    void Close();
    bool IsConnected();
}