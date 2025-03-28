using System.Reflection;

namespace Ares.Core.Objects.Model;

public enum ModelCategory
{
    [WebApi("https://api.openai.com/v1")]
    OpenAI,

    [WebApi("https://api.anthropic.com/v1/")]
    Anthropic,

    [WebApi("https://api.deepseek.com")]
    DeepSeek
}

[AttributeUsage(AttributeTargets.Field)]
public class WebApiAttribute : Attribute
{
    public string Endpoint { get; }
    public WebApiAttribute(string url) => Endpoint = url;
}

public static class ModelCategoryExtensions
{
    public static Uri? GetEndpoint(this ModelCategory category)
    {
        Type type = category.GetType();
        MemberInfo[] member = type.GetMember(category.ToString());
        WebApiAttribute? attribute = member[0].GetCustomAttribute<WebApiAttribute>();

        if (attribute == null)
            return null;

        return new Uri(attribute.Endpoint) ?? null;
    }
}