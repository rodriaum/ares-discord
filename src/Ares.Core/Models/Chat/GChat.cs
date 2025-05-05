/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Models.Chat.Sub;
using System.Text.Json.Serialization;

namespace Ares.Core.Models.Chat;

public class GChat
{
    [JsonInclude]
    [JsonPropertyName("infos")]
    public Dictionary<ulong, List<GChatInfo>> Infos { get; set; }

    [JsonInclude]
    [JsonPropertyName("snippets")]
    public Dictionary<ulong, List<GChatSnippet>> Snippets { get; set; }

    public GChat()
    {
        Infos = new();
        Snippets = new();
    }
}