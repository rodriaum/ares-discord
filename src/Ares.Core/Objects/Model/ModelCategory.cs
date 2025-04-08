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
    [WebApi(endpoint: "https://api.openai.com/v1", remoteStreaming: true)]
    OpenAI,

    /// <summary>
    /// Anthropic
    /// </summary>
    [WebApi(endpoint: "https://api.anthropic.com/v1/", remoteStreaming: false)]
    Anthropic,

    /// <summary>
    /// DeepSeek
    /// </summary>
    [WebApi(endpoint: "https://api.deepseek.com", remoteStreaming: true, localStreaming: true)]
    DeepSeek,

    /// <summary>
    /// xAI
    /// </summary>
    [WebApi(endpoint: "https://api.x.ai/v1", remoteStreaming: true)]
    xAI,

    /// <summary>
    /// Google
    /// </summary>
    [WebApi(endpoint: "https://generativelanguage.googleapis.com/v1beta/openai/", remoteStreaming: true)]
    Google,

    /// <summary>
    /// Meta AI
    /// </summary>
    [WebApi(localStreaming: true)]
    MetaAI,

    /// <summary>
    /// Microsoft
    /// </summary>
    [WebApi(localStreaming: true)]
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
    public bool RemoteStreamingResponses { get; }

    public bool LocalStreamingResponses { get; }

    public WebApiAttribute(string? endpoint = null, bool remoteStreaming = false, bool localStreaming = false)
    {
        this.RemoteEndpoint = endpoint;
        this.RemoteStreamingResponses = remoteStreaming;
        this.LocalStreamingResponses = localStreaming;
    }
}

public static class ModelCategoryExtensions
{
    public static Uri? GetEndpoint(this ModelCategory category)
    {
        WebApiAttribute? attribute = GetWebApiAttribute(category);

        if (attribute == null)
            return null;

        string? endpoint = attribute.RemoteEndpoint;
        if (string.IsNullOrEmpty(endpoint)) return null;

        return new Uri(endpoint) ?? null;
    }

    public static bool HasRemoteStreamingResponses(this ModelCategory category)
    {
        WebApiAttribute? attribute = GetWebApiAttribute(category);

        if (attribute == null)
            return false;

        return attribute.RemoteStreamingResponses;
    }

    public static bool HasLocalStreamingResponses(this ModelCategory category)
    {
        WebApiAttribute? attribute = GetWebApiAttribute(category);

        if (attribute == null)
            return false;

        return attribute.LocalStreamingResponses;
    }

    private static WebApiAttribute? GetWebApiAttribute(this ModelCategory category)
    {
        Type type = category.GetType();
        MemberInfo[] member = type.GetMember(category.ToString());
        return member[0].GetCustomAttribute<WebApiAttribute>();
    }
}