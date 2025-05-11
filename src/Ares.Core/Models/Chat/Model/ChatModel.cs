/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Models.Chat.Price;
using Ares.Core.Models.Data.Chat.Model;
using Ares.Core.Objects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Ares.Core.Models.Chat.Model;

public class ChatModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonInclude]
    [JsonPropertyName("id")]
    public string Id;

    [JsonInclude]
    [JsonPropertyName("displayName")]
    public string DisplayName;

    [JsonInclude]
    [JsonPropertyName("descriptionKey")]
    public string DescriptionKey;

    [JsonInclude]
    [JsonPropertyName("requestType")]
    public ChatRequestType RequestType;

    [JsonInclude]
    [JsonPropertyName("category")]
    public ModelCategory Category;

    [JsonInclude]
    [JsonPropertyName("type")]
    public ModelType Type;

    [JsonInclude]
    [JsonPropertyName("price")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ChatPriceUsage? Price;

    [JsonInclude]
    [JsonPropertyName("exclusive")]

    public bool Exclusive;

    [JsonInclude]
    [JsonPropertyName("available")]
    public bool Available;

    [JsonInclude]
    [JsonPropertyName("dev")]
    public bool Dev;

    public ChatModel
        (
            string id,
            string displayName = "Unavalable",
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