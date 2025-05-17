/*
* Copyright (C) Rodrigo Ferreira, All Rights Reserved
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
*/

using Ares.Core.Models.Data;
using Ares.Core.Models.Language;
using Ares.Core.Models.Preference;
using Ares.Core.Models.Token;
using Ares.Core.Objects;
using Ares.Core.Util;
using System.Collections.Concurrent;

namespace Ares.Core.Manager.Data;

/// <summary>
/// Guild service to manage data and operations of an guild.
/// </summary>
public class GuildDataManager
{
    /// <summary>
    /// Dictionary of locks for concurrent operations on the same guild
    /// </summary>
    private static readonly ConcurrentDictionary<ulong, SemaphoreSlim> _guildLocks = new ConcurrentDictionary<ulong, SemaphoreSlim>();

    /// <summary>
    /// Gets an existing lock or creates a new one for the specified guild ID.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <returns>The semaphore for the guild.</returns>
    private static SemaphoreSlim GetGuildLock(ulong guildId)
    {
        return _guildLocks.GetOrAdd(guildId, _ => new SemaphoreSlim(1, 1));
    }

    /// <summary>
    /// Internal implementation of SaveAsync that doesn't acquire a lock.
    /// </summary>
    /// <param name="guild">The guild to save.</param>
    /// <param name="fields">List of field names to be saved.</param>
    /// <returns>Returns true if fields were successfully saved, false otherwise.</returns>
    private static async Task<bool> SaveInternalAsync(Guild guild, params string[] fields)
    {
        if (fields == null || fields.Length == 0)
        {
            AresLogger.Log(nameof(SaveAsync), "The field list is null or empty.", severity: Severity.Error);
            return false;
        }

        if (AppCore.GuildRepository is not { } repository)
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
    /// Saves the specified fields of the guild to the database.
    /// </summary>
    /// <param name="guild">The guild to save.</param>
    /// <param name="fields">List of field names to be saved.</param>
    /// <returns>Returns true if fields were successfully saved, false otherwise.</returns>
    public static async Task<bool> SaveAsync(Guild guild, params string[] fields)
    {
        SemaphoreSlim semaphore = GetGuildLock(guild.Id);

        try
        {
            await semaphore.WaitAsync();
            return await SaveInternalAsync(guild, fields);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Saves token data about the guild to the database.
    /// </summary>
    /// <param name="guild">The guild to save the config data.</param>
    /// <param name="token">Object containing guild token data.</param>
    /// <returns>Returns true if information was successfully saved, false otherwise.</returns>
    public static async Task<bool> SaveTokenDataAsync(Guild guild, GToken? token = null)
    {
        SemaphoreSlim semaphore = GetGuildLock(guild.Id);

        try
        {
            await semaphore.WaitAsync();

            // If is null, maybe it was probably modified in the variable itself, so it will save anyway.
            if (token != null)
            {
                guild.Token = token;
            }

            return await SaveInternalAsync(guild, "token");
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Saves preference data about the guild to the database.
    /// </summary>
    /// <param name="guild">The guild to save the config data.</param>
    /// <param name="config">Object containing guild preference data.</param>
    /// <returns>Returns true if information was successfully saved, false otherwise.</returns>
    public static async Task<bool> SavePreferenceDataAsync(Guild guild, GPreference? config = null)
    {
        SemaphoreSlim semaphore = GetGuildLock(guild.Id);

        try
        {
            await semaphore.WaitAsync();

            // If is null, maybe it was probably modified in the variable itself, so it will save anyway.
            if (config != null)
            {
                guild.Preferences = config;
            }

            return await SaveInternalAsync(guild, "preference");
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Gets the language code configured for this guild.
    /// </summary>
    /// <param name="guild">The guild to get the language for.</param>
    /// <returns>The language code string.</returns>
    public static string Language(Guild guild)
    {
        return guild.Preferences.Lang;
    }

    /// <summary>
    /// Gets the language category object based on the guild's configured language.
    /// </summary>
    /// <param name="guild">The guild to get the language category for.</param>
    /// <returns>The language category object or null if not found.</returns>
    public static LanguageCategory? LangCategory(Guild guild)
    {
        return AppCore.LangManager.GetCategoryByCode(Language(guild));
    }

    /// <summary>
    /// Gets a translated string based on the guild's configured language.
    /// </summary>
    /// <param name="guild">The guild to get the translation for.</param>
    /// <param name="code">The translation code to look up.</param>
    /// <returns>The translated string or the original code if translation was not found.</returns>
    public static string GetTranslation(Guild guild, string code)
    {
        LanguageCategory? category = LangCategory(guild);
        if (category == null) return code;

        return AppCore.LangManager.GetTranslation(category, code);
    }

    /// <summary>
    /// Cleanup method to remove unused locks and free memory
    /// </summary>
    public static void CleanupLocks(TimeSpan olderThan)
    {
        foreach (var key in _guildLocks.Keys)
        {
            if (_guildLocks.TryRemove(key, out var semaphore))
            {
                semaphore.Dispose();
            }
        }
    }
}