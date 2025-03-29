/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using System.Reflection;

namespace Ares.Core.Objects.Model;

public enum ModelCategory
{
    [WebApi(endpoint: "https://api.openai.com/v1", streamingResponses: true)]
    OpenAI,

    [WebApi(endpoint: "https://api.anthropic.com/v1/", streamingResponses: false)]
    Anthropic,

    [WebApi(endpoint: "https://api.deepseek.com", streamingResponses: true)]
    DeepSeek
}

[AttributeUsage(AttributeTargets.Field)]
public class WebApiAttribute : Attribute
{
    public string Endpoint { get; }
    public bool StreamingResponses { get; }

    public WebApiAttribute(string endpoint, bool streamingResponses)
    {
        Endpoint = endpoint;
        StreamingResponses = streamingResponses;
    }
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

    public static bool HasStreamingResponses(this ModelCategory category)
    {
        Type type = category.GetType();
        MemberInfo[] member = type.GetMember(category.ToString());
        WebApiAttribute? attribute = member[0].GetCustomAttribute<WebApiAttribute>();

        if (attribute == null)
            return false;

        return attribute.StreamingResponses;
    }
}