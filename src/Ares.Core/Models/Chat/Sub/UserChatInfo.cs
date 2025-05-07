/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Objects.Chat.Image;
using Ares.Core.Objects.Model;
using System.Text.Json.Serialization;

namespace Ares.Core.Models.Chat.Sub;

public class UserChatInfo
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
    public List<UserChatHistoricModel> Historics { get; set; }

    public UserChatInfo(ulong channel, string model, bool active = false, ImageGenOptions? imageGenOptions = null, List<UserChatHistoricModel>? historics = null)
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

        Historics = historics ?? new List<UserChatHistoricModel>();
    }
}