/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Models.Database.Chat.Sub;
using System.Text.Json.Serialization;

namespace Ares.Core.Models.Database.Chat;

public class GChatModel
{
    [JsonInclude]
    [JsonPropertyName("infos")]
    public Dictionary<ulong, List<GChatInfoModel>> Infos { get; set; }

    public GChatModel()
    {
        Infos = new Dictionary<ulong, List<GChatInfoModel>>();
    }
}