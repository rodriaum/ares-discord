/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Objects.Chat.Image;
using Ares.Core.Objects.Model;
using System.Text.Json.Serialization;

namespace Ares.Core.Models.Chat.Sub;

public class GChatInfoModel
{
    [JsonInclude]
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonInclude]
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonInclude]
    [JsonPropertyName("channel")]
    public ulong Channel { get; set; }

    [JsonInclude]
    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonInclude]
    [JsonPropertyName("imageGenOptions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImageGenOptions? ImageGenOptions { get; set; }

    [JsonInclude]
    [JsonPropertyName("historics")]
    public List<GChatHistoricModel> Historics { get; set; }

    public GChatInfoModel(ulong channel, string model, bool active = false, ImageGenOptions? imageGenOptions = null, List<GChatHistoricModel>? historics = null)
    {
        Id = Guid.NewGuid().ToString();
        Channel = channel;
        Model = model;
        Active = active;

        if (
                ImageGenOptions == null &&
                ChatModel.GetByModel(model) != null && ChatModel.GetByModel(model)?.Type == ModelType.Image
            )
        {
            ImageGenOptions = new ImageGenOptions();
        }

        Historics = historics ?? new List<GChatHistoricModel>();
    }
}