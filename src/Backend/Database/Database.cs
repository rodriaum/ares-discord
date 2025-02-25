namespace Ares.src.Backend.Database;

internal interface Database
{
    void Connect();
    void Close();
    bool IsConnected();
}