/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Objects.Chat.Image;
using Ares.Core.Objects.Model;
using Ares.Core.Repository;
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
    [JsonPropertyName("channelId")]
    public ulong ChannelId { get; set; }

    [JsonInclude]
    [JsonPropertyName("modelId")]
    public string ModelId { get; set; }

    [JsonInclude]
    [JsonPropertyName("imageGenOptions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImageGenOptions? ImageGenOptions { get; set; }

    [JsonInclude]
    [JsonPropertyName("historics")]
    public List<UserChatHistoricModel> Historics { get; set; }

    public UserChatInfo(ulong channelId, string modelId, bool active = false, ImageGenOptions? imageGenOptions = null, List<UserChatHistoricModel>? historics = null)
    {
        Id = Guid.NewGuid().ToString();
        ChannelId = channelId;
        ModelId = modelId;
        Active = active;
        ImageGenOptions = imageGenOptions;

        Historics = historics ?? new List<UserChatHistoricModel>();

        InitializeImageGenOptions();
    }

    [Obsolete]
    private async void InitializeImageGenOptions()
    {
        ChatModelRepository? repository = AresCore.ChatModelRepository;
        if (repository != null && ImageGenOptions == null)
        {
            ChatModel? model = await repository.FetchAsync(ModelId, saveInRedis: true);
            if (model != null && model.Type == ModelType.Image)
            {
                ImageGenOptions = new ImageGenOptions();
            }
        }
    }
}