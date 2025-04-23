/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using System.Text.Json.Serialization;

namespace Ares.Core.Models.Preference;

public partial class GPreference
{
    /*
     * Application Predefinitions
     */

    [JsonInclude]
    [JsonPropertyName("lang")]
    public string Lang { get; set; }

    /*
     * Channels and Categories
     */

    [JsonInclude]
    [JsonPropertyName("memberRoleId")]
    public ulong MemberRoleId { get; set; }

    [JsonInclude]
    [JsonPropertyName("usageRoleId")]
    public ulong UsageRoleId { get; set; }

    [JsonInclude]
    [JsonPropertyName("exclusiveRoleId")]
    public ulong ExclusiveRoleId { get; set; }

    [JsonInclude]
    [JsonPropertyName("setupChannelId")]
    public ulong SetupChannelId { get; set; }

    [JsonInclude]
    [JsonPropertyName("logChannelId")]
    public ulong LogChannelId { get; set; }

    [JsonInclude]
    [JsonPropertyName("chatsCategoryId")]
    public ulong ChatsCategoryId { get; set; }

    public GPreference()
    {
        // By default, the language is Portuguese.
        Lang = "pt";
    }
}