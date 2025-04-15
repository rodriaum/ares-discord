using Ares.Core.Objects.Model;
using Discord;

namespace Ares.Discord.Util;

public class AresUtil
{
    public static Color GetColorByModelCategory(ModelCategory category)
    {
        return category switch
        {
            ModelCategory.OpenAI => Color.Green,
            ModelCategory.Anthropic => Color.Orange,
            ModelCategory.DeepSeek => Color.Blue,
            ModelCategory.xAI => Color.Orange,
            ModelCategory.Google => Color.DarkBlue,
            ModelCategory.MetaAI => Color.Blue,
            ModelCategory.Microsoft => Color.Blue,
            _ => Color.Default
        };
    }

    public static Emoji GetEmojiByModelType(ModelType type)
    {
        return type switch
        {
            ModelType.Chat => new Emoji("\U0001F4DC"),             // 📜
            ModelType.Question => new Emoji("\U0001F4D3"),         // 📃
            ModelType.Image => new Emoji("\U0001F4F7"),            // 📷
            ModelType.TTS => new Emoji("\U0001F50A"),              // 🔊
            ModelType.Vision => new Emoji("\U0001F441\U0000FE0F"), // 👁️
            _ => new Emoji("\U00002753")                           // ❓
        };
    }
}