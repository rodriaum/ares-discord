/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Models.Chat.Historic;
using System.Text.Json.Serialization;

namespace Ares.Core.Models.Chat;

public class UserChat
{
    [JsonInclude]
    [JsonPropertyName("infos")]
    public Dictionary<ulong, List<UserChatInfo>> Infos { get; set; }

    [JsonInclude]
    [JsonPropertyName("snippets")]
    public Dictionary<ulong, List<UserChatSnippet>> Snippets { get; set; }

    public UserChat()
    {
        Infos = new();
        Snippets = new();
    }
}