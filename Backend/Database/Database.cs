namespace Discord_OpenAI.Backend.Database
{
 
    internal interface Database
    {
        void Connect();
        void Close();
        bool IsConnected();
    }
}
