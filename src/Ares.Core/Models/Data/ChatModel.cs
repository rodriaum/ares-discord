/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */
using Ares.Core.Models.Chat.Price;
using Ares.Core.Models.Data.Chat.Model;
using Ares.Core.Objects;
using System.Text.Json.Serialization;

namespace Ares.Core.Models.Data;

public class ChatModel
{
    [JsonInclude]
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonInclude]
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    [JsonInclude]
    [JsonPropertyName("descriptionKey")]
    public string DescriptionKey { get; set; }

    [JsonInclude]
    [JsonPropertyName("requestType")]
    public ChatRequestType RequestType { get; set; }

    [JsonInclude]
    [JsonPropertyName("category")]
    public ModelCategory Category { get; set; }

    [JsonInclude]
    [JsonPropertyName("type")]
    public ModelType Type { get; set; }

    [JsonInclude]
    [JsonPropertyName("price")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ChatPriceUsage? Price { get; set; }

    [JsonInclude]
    [JsonPropertyName("exclusive")]
    public bool Exclusive { get; set; }

    [JsonInclude]
    [JsonPropertyName("available")]
    public bool Available { get; set; }

    [JsonInclude]
    [JsonPropertyName("dev")]
    public bool Dev { get; set; }

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    public ChatModel()
    {
        Id = string.Empty;
        DisplayName = "Unavailable";
        DescriptionKey = string.Empty;
        RequestType = ChatRequestType.None;
        Category = ModelCategory.Other;
        Type = ModelType.None;
        Price = null;
        Exclusive = false;
        Available = true;
        Dev = false;
    }

    public ChatModel
        (
            string id,
            string displayName = "Unavailable",
            string descriptionKey = "",
            ChatRequestType requestType = ChatRequestType.None,
            ModelCategory category = ModelCategory.Other,
            ModelType type = ModelType.None,
            ChatPriceUsage? price = null,
            bool exclusive = false,
            bool available = true,
            bool dev = false
        )
    {
        RequestType = requestType;
        Category = category;
        Type = type;
        DisplayName = displayName;
        Id = id;
        DescriptionKey = descriptionKey;
        Price = price;
        Exclusive = exclusive;
        Available = available;
        Dev = dev; // Only in dev mode
    }

    public bool IsAvailable()
    {
        return Dev ? false : Available;
    }
}