/*
* Copyright (C) Rodrigo Ferreira, All Rights Reserved
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
*/

using Ares.Core.Models.Chat.Model;
using Ares.Core.Objects;
using Ares.Core.Util;
using System.Collections.Concurrent;

namespace Ares.Core.Manager.Data;

/// <summary>
/// User service to manage data and operations.
/// </summary>
public class ChatModelDataManager
{
    /// <summary>
    /// Dictionary of locks for concurrent operations on the same model
    /// </summary>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _modelLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

    /// <summary>
    /// Saves the specified fields of the user to the database.
    /// </summary>
    /// <param name="model">The user to save.</param>
    /// <param name="fields">List of field names to be saved.</param>
    /// <returns>Returns true if fields were successfully saved, false otherwise.</returns>
    public static async Task<bool> SaveAsync(ChatModel model, params string[] fields)
    {
        var semaphore = _modelLocks.GetOrAdd(model.Id, _ => new SemaphoreSlim(1, 1));

        try
        {
            await semaphore.WaitAsync();

            if (fields == null || fields.Length == 0)
            {
                AresLogger.Log(nameof(SaveAsync), "The field list is null or empty.", severity: Severity.Error);
                return false;
            }

            if (AppCore.ChatModelRepository is not { } repository)
            {
                AresLogger.Log(nameof(SaveAsync), "Chat model data is null. Unable to save fields.", severity: Severity.Error);
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

                    await repository.UpdateAsync(model, field);
                }

                return true;
            }
            catch (Exception ex)
            {
                AresLogger.Log(nameof(SaveAsync), "Error updating one or more fields in the database.", severity: Severity.Error, extra: ex.Message);
                return false;
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Cleanup method to remove unused locks and free memory
    /// </summary>
    public static void CleanupLocks(TimeSpan olderThan)
    {
        foreach (var key in _modelLocks.Keys)
        {
            if (_modelLocks.TryRemove(key, out var semaphore))
            {
                semaphore.Dispose();
            }
        }
    }
}