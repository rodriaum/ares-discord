namespace Ares.Backend.Database
{
 
    internal interface Database
    {
        void Connect();
        void Close();
        bool IsConnected();
    }
}
