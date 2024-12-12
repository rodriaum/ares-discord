namespace Ares.src.Objects.OpenAI.Error
{
    public enum ErrorCode
    {
        InvalidAuthentication = 401,
        IncorrectApiKey = 401,
        NotMemberOfOrganization = 401,
        UnsupportedRegion = 403,
        RateLimitReached = 429,
        QuotaExceeded = 429,
        ServerError = 500,
        EngineOverloaded = 503
    }
}