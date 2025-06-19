/*
* Copyright (C) Rodrigo Ferreira, All Rights Reserved
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
*/

using Ares.Core.Models.Data;
using Ares.Core.Models.Preference;
using Ares.Core.Models.Token;
using Ares.Core.Objects;
using Ares.Core.Repository;
using Ares.Core.Util;

namespace Ares.Core.Manager;

/// <summary>
/// Guild service to manage data and operations of an guild.
/// </summary>
public class GuildDataManager
{
    /// <summary>
    /// Repository for guild data operations.
    /// </summary>
    private readonly GuildRepository? _repository;

    public GuildDataManager(GuildRepository guildRepository)
    {
        _repository = guildRepository;

        if (_repository is null)
        {
            AresLogger.Log(nameof(GuildDataManager), "Repository is not initialized.", severity: Severity.Error);
        }
    }

    /// <summary>
    /// Saves the specified fields of the guild to the database.
    /// </summary>
    /// <param name="guild">The guild to save.</param>
    /// <param name="fields">List of field names to be saved.</param>
    /// <returns>Returns true if fields were successfully saved, false otherwise.</returns>
    public async Task<bool> SaveAsync(Guild guild, params string[] fields)
    {
        if (fields == null || fields.Length == 0)
        {
            AresLogger.Log(nameof(SaveAsync), "The field list is null or empty.", severity: Severity.Error);
            return false;
        }

        if (_repository is not { } repository)
        {
            AresLogger.Log(nameof(SaveAsync), "Guild data is null. Unable to save fields.", severity: Severity.Error);
            return false;
        }

        try
        {
            foreach (string field in fields)
            {
                if (string.IsNullOrWhiteSpace(field))
                {
                    AresLogger.Log(nameof(SaveAsync), "The field list contains a null or empty value.", severity: Severity.Error);
                    continue;
                }

                await repository.UpdateAsync(guild, field);
            }

            return true;
        }
        catch (Exception ex)
        {
            AresLogger.Log(nameof(SaveAsync), "Error updating one or more fields in the database.", severity: Severity.Error, extra: ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Saves token data about the guild to the database.
    /// </summary>
    /// <param name="guild">The guild to save the config data.</param>
    /// <param name="token">Object containing guild token data.</param>
    /// <returns>Returns true if information was successfully saved, false otherwise.</returns>
    public async Task<bool> SaveTokenDataAsync(Guild guild, GToken? token = null)
    {
        // If is null, maybe it was probably modified in the variable itself, so it will save anyway.
        if (token != null)
        {
            guild.Token = token;
        }

        return await SaveAsync(guild, "token");
    }

    /// <summary>
    /// Saves preference data about the guild to the database.
    /// </summary>
    /// <param name="guild">The guild to save the config data.</param>
    /// <param name="config">Object containing guild preference data.</param>
    /// <returns>Returns true if information was successfully saved, false otherwise.</returns>
    public async Task<bool> SavePreferenceDataAsync(Guild guild, GPreference? config = null)
    {
        // If is null, maybe it was probably modified in the variable itself, so it will save anyway.
        if (config != null)
        {
            guild.Preferences = config;
        }

        return await SaveAsync(guild, "preference");
    }

    /// <summary>
    /// Gets the language code configured for this guild.
    /// </summary>
    /// <param name="guild">The guild to get the language for.</param>
    /// <returns>The language code string.</returns>
    public string Language(Guild guild)
    {
        return guild.Preferences.Lang;
    }
}