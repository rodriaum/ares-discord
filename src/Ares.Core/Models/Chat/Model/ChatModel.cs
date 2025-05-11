/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Objects.Chat.Price;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Ares.Core.Objects.Model;

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
        this.RequestType = requestType;
        this.Category = category;
        this.Type = type;
        this.DisplayName = displayName;
        this.Id = id;
        this.DescriptionKey = descriptionKey;
        this.Price = price;
        this.Exclusive = exclusive;
        this.Available = available;
        this.Dev = dev; // Only in dev mode
    }

    public bool IsAvailable()
    {
        return (this.Dev ? false : this.Available);
    }
}