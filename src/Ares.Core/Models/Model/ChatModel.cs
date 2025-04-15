/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Models.Model;
using Ares.Core.Manager;
using Ares.Core.Objects.Chat.Price;
using MongoDB.Driver.Linq;

namespace Ares.Core.Objects.Model;

public class ChatModel
{
    public ChatRequestType RequestType;
    public ModelCategory Category;
    public ModelType Type;
    public string DisplayName;
    public string Model;
    public string DescriptionKey;
    public ModelTaskCategory Task;
    public ChatPriceUsage? Price;
    public bool Exclusive;
    public bool Available;
    public bool Dev;

    public ChatModel
        (
            ChatRequestType request,
            ModelCategory category,
            ModelType type,
            string display,
            string model,
            string descriptionKey = "",
            ModelTaskCategory task = ModelTaskCategory.Other,
            ChatPriceUsage? price = null,
            bool exclusive = false,
            bool available = true,
            bool dev = false
        )
    {
        this.RequestType = request;
        this.Category = category;
        this.Type = type;
        this.DisplayName = display;
        this.Model = model;
        this.DescriptionKey = descriptionKey;
        this.Task = task;
        this.Price = price;
        this.Exclusive = exclusive;
        this.Available = available;
        this.Dev = dev; // Only in dev mode
    }

    public bool IsAvailable()
    {
        return (this.Dev ? false : this.Available);
    }

    public static List<ChatModel> GetModelsByCategory(ModelType category)
    {
        return AiManager.Models
            .Where(model => model.Type.Equals(category))
            .ToList();
    }

    public static ChatModel? GetByDisplayName(string displayName)
    {
        return AiManager.Models
                .Where(model => model.DisplayName.Equals(displayName))
                .First();
    }

    /// <summary>
    /// Retorna o modelo exato baseado no nome fornecido, sem considerar variações ou versões.
    /// O nome do modelo deve ser exato, incluindo a versão correta, como 'gpt-4-turbo'.
    /// Funciona para qualquer modelo, não limitado à categoria OpenAI, e a busca é sensível a maiúsculas/minúsculas.
    /// </summary>
    /// <param name="model">O nome exato do modelo a ser buscado.</param>
    /// <returns>O modelo correspondente ou null caso não encontrado.</returns>
    public static ChatModel? GetByModel(string model)
    {
        return AiManager.Models
                .Where(it => it.Model.Equals(model, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
    }

    /// <summary>
    /// Retorna o modelo mais próximo baseado no nome fornecido. 
    /// É recomendado usar para se o <paramref name="model"/> for OpenAI. 
    /// O sistema pode retornar uma versão mais recente do modelo, como 'gpt-4-turbo-2024-04-09', 
    /// uma vez que a OpenAI utiliza sempre a versão mais recente do modelo disponível, 
    /// sem forçar ou especificar uma versão exata.
    /// </summary>
    /// <param name="model">O nome do modelo a ser buscado.</param>
    /// <returns>O modelo correspondente ou null caso não encontrado.</returns>
    public static ChatModel? GetByNearestModel(string model)
    {
        return AiManager.Models
                .Where(it => model.StartsWith(it.Model))
                .FirstOrDefault();
    }
}