/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using System.Reflection;

namespace Ares.Common.Models.Data.Chat.Model;

public enum ModelCategory
{
    /// <summary>
    /// OpenAI
    /// </summary>
    [WebApi(endpoint: "https://api.openai.com/v1")]
    OpenAI,

    /// <summary>
    /// Anthropic
    /// </summary>
    [WebApi(endpoint: "https://api.anthropic.com/v1/")]
    Anthropic,

    /// <summary>
    /// DeepSeek
    /// </summary>
    [WebApi(endpoint: "https://api.deepseek.com")]
    DeepSeek,

    /// <summary>
    /// xAI
    /// </summary>
    [WebApi(endpoint: "https://api.x.ai/v1")]
    xAI,

    /// <summary>
    /// Google
    /// </summary>
    [WebApi(endpoint: "https://generativelanguage.googleapis.com/v1beta/openai/")]
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
    public string? RemoteEndpoint { get; }

    public WebApiAttribute(string? endpoint = null)
    {
        RemoteEndpoint = endpoint;
    }
}

public static class ModelCategoryExtensions
{
    public static Uri? GetEndpoint(this ModelCategory category)
    {
        WebApiAttribute? attribute = category.GetWebApiAttribute();

        if (attribute == null)
            return null;

        string? endpoint = attribute.RemoteEndpoint;
        if (string.IsNullOrEmpty(endpoint)) return null;

        return new Uri(endpoint) ?? null;
    }

    private static WebApiAttribute? GetWebApiAttribute(this ModelCategory category)
    {
        Type type = category.GetType();
        MemberInfo[] member = type.GetMember(category.ToString());
        return member[0].GetCustomAttribute<WebApiAttribute>();
    }
}