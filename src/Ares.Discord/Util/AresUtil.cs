using Ares.Core.Objects.Model;
using Discord;

namespace Ares.Ares.Discord.Util;

public class AresUtil
{
    public static Color GetColorForModelCategory(ModelCategory category)
    {
        return category switch
        {
            ModelCategory.OpenAI => Color.Green,
            ModelCategory.Anthropic => Color.Orange,
            ModelCategory.DeepSeek => Color.Blue,
            ModelCategory.xAI => Color.Orange,
            _ => Color.Default
        };
    }
}
