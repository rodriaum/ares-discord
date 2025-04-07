/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using System.Reflection;

namespace Ares.Core.Objects.Model;

public enum ModelCategory
{
    /// <summary>
    /// OpenAI
    /// </summary>
    [WebApi(endpoint: "https://api.openai.com/v1", streamingResponses: true)]
    OpenAI,

    /// <summary>
    /// Anthropic
    /// </summary>
    [WebApi(endpoint: "https://api.anthropic.com/v1/", streamingResponses: false)]
    Anthropic,

    /// <summary>
    /// DeepSeek
    /// </summary>
    [WebApi(endpoint: "https://api.deepseek.com", streamingResponses: true)]
    DeepSeek,

    /// <summary>
    /// xAI
    /// </summary>
    [WebApi(endpoint: "https://api.x.ai/v1", streamingResponses: true)]
    xAI,

    /// <summary>
    /// Google
    /// </summary>
    [WebApi(endpoint: "https://generativelanguage.googleapis.com/v1beta/openai/", streamingResponses: true)]
    Google,

    /// <summary>
    /// Meta AI
    /// </summary>
    MetaAI,

    /// <summary>
    /// Microsoft
    /// </summary>
    Microsoft,

    /// <summary>
    /// Others
    /// </summary>
    Other
}

[AttributeUsage(AttributeTargets.Field)]
public class WebApiAttribute : Attribute
{
    public string Endpoint { get; }
    public bool StreamingResponses { get; }

    public WebApiAttribute(string endpoint = "", bool streamingResponses = false)
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