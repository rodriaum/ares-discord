using Ares.Core.Models.Database.Token;
using Ares.Core.Objects.Model;

namespace Ares.Core.Util;

public class CoreUtil
{
    public static string? GetTokenByModelCategory(ModelCategory category, GTokenModel data)
    {
        return category switch
        {
            ModelCategory.OpenAI => data.OpenAi,
            ModelCategory.Anthropic => data.Anthropic,
            ModelCategory.DeepSeek => data.Deepseek,
            ModelCategory.xAI => data.xAI,
            ModelCategory.Google => data.Google,
            _ => null
        };
    }
}