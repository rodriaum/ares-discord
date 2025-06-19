/*
* Copyright (C) Rodrigo Ferreira, All Rights Reserved
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
*/

namespace Ares.Core.Database.Postgres;

using Ares.Core.Interfaces;
using Ares.Core.Models.Database;
using Ares.Core.Objects;
using Ares.Core.Util;
using Npgsql;
using System.Text.RegularExpressions;

public class PostgresDatabase : IDatabase
{
    private static readonly string _pattern = "([01]?[0-9]{1,2}|2[0-4][0-9]|25[0-5])";
    private static readonly Regex _ipPattern = new Regex(_pattern + "\\." + _pattern + "\\." + _pattern + "\\." + _pattern);
    private readonly DatabaseCredentials _credentials;
    private readonly string connectionString;
    private readonly string defaultConnectionString;
    private NpgsqlConnection? connection;

    public PostgresDatabase(DatabaseCredentials credentials)
    {
        _credentials = credentials;

        if (_credentials.Host == null)
        {
            throw new ArgumentException($"Host cannot be null ({nameof(PostgresDatabase)})");
        }

        NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder
        {
            Host = _credentials.Host,
            Username = _credentials.User,
            Password = _credentials.Password,
            Port = _credentials.Port,
            Pooling = true,
            MaxPoolSize = 20,
            MinPoolSize = 5,
            ConnectionIdleLifetime = 300,
            CommandTimeout = 30,
            Timeout = 15
        };

        defaultConnectionString = builder.ToString();

        builder.Database = _credentials.Database;
        connectionString = builder.ToString();
    }

    public async Task ConnectAsync()
    {
        long start = TimeUtil.CurrentTimeMillis();
        await AresLogger.LogAsync("DB: Postgres", "Starting connection to PostgreSQL...");

        int time = 15;
        int currentTries = 1;
        int maxTries = 3;
        bool connected = false;

        while (!connected)
        {
            try
            {
                bool databaseExists = await CheckDatabaseExistsAsync();

                if (!databaseExists)
                {
                    await AresLogger.LogAsync("DB: Postgres", $"Database '{_credentials.Database}' does not exist. Creating...");
                    await CreateDatabaseAsync();
                    await AresLogger.LogAsync("DB: Postgres", $"Database '{_credentials.Database}' created successfully.", severity: Severity.Success);
                }

                connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                using NpgsqlCommand cmd = new("SELECT 1", connection);
                await cmd.ExecuteScalarAsync();

                await AresLogger.LogAsync("DB: Postgres", $"Connection established. ({currentTries}x/{FormatterUtil.FormatSeconds(start)})", severity: Severity.Success);
                connected = true;
            }
            catch (Exception e)
            {
                await AresLogger.LogAsync("DB: Postgres", $"Unable to connect. ({currentTries}x)", severity: Severity.Error, extra: e.Message);
                connected = false;
                currentTries++;

                if (currentTries > maxTries)
                {
                    await AresLogger.LogAsync("DB: Postgres", "Max tries reached, stopping connection attempts.", severity: Severity.Error);
                    Environment.Exit(1);
                    break;
                }

                await AresLogger.LogAsync("DB: Postgres", $"Trying to connect in {time}s...", severity: Severity.Error);
                await Task.Delay(TimeSpan.FromSeconds(time));
            }
        }
    }

    private async Task<bool> CheckDatabaseExistsAsync()
    {
        try
        {
            string? databaseName = _credentials.Database;

            if (string.IsNullOrWhiteSpace(databaseName))
                return false;

            using var tempConnection = new NpgsqlConnection(defaultConnectionString);
            await tempConnection.OpenAsync();

            string checkDbQuery = "SELECT 1 FROM pg_database WHERE datname = @dbname";

            using var cmd = new NpgsqlCommand(checkDbQuery, tempConnection);
            cmd.Parameters.AddWithValue("@dbname", databaseName);

            var result = await cmd.ExecuteScalarAsync();
            await tempConnection.CloseAsync();

            return result != null;
        }
        catch (Exception e)
        {
            await AresLogger.LogAsync("DB: Postgres", "Error checking if database exists.", severity: Severity.Error, extra: e.Message);
            throw;
        }
    }

    private async Task CreateDatabaseAsync()
    {
        try
        {
            string? databaseName = _credentials.Database;

            if (string.IsNullOrWhiteSpace(databaseName))
                return;

            using var tempConnection = new NpgsqlConnection(defaultConnectionString);
            await tempConnection.OpenAsync();

            string escapedDbName = databaseName.Replace("\"", "\"\"");
            string createDbQuery = $"CREATE DATABASE \"{escapedDbName}\"";

            using var cmd = new NpgsqlCommand(createDbQuery, tempConnection);
            await cmd.ExecuteNonQueryAsync();

            await tempConnection.CloseAsync();
        }
        catch (Exception e)
        {
            await AresLogger.LogAsync("DB: Postgres", "Error creating database.", severity: Severity.Error, extra: e.Message);
            throw;
        }
    }

    public async Task CloseAsync()
    {
        if (connection != null)
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }

                await connection.DisposeAsync();
            }
            catch (Exception e)
            {
                await AresLogger.LogAsync("DB: Postgres", "Unable to close connection.", severity: Severity.Error, extra: e.Message);
            }
        }
    }

    public bool IsConnected()
    {
        return connection != null && connection.State == System.Data.ConnectionState.Open;
    }

    public NpgsqlConnection? GetConnection()
    {
        return connection;
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, params NpgsqlParameter[] parameters)
    {
        if (connection == null || connection.State != System.Data.ConnectionState.Open)
        {
            throw new InvalidOperationException("Database connection is not open.");
        }

        try
        {
            using var cmd = new NpgsqlCommand(sql, connection);
            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters);
            }

            var result = await cmd.ExecuteScalarAsync();
            return result is T ? (T)result : default(T);
        }
        catch (Exception e)
        {
            await AresLogger.LogAsync("DB: Postgres", $"Error executing scalar query: {sql}", severity: Severity.Error, extra: e.Message);
            throw;
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, params NpgsqlParameter[] parameters)
    {
        if (connection == null || connection.State != System.Data.ConnectionState.Open)
        {
            throw new InvalidOperationException("Database connection is not open.");
        }

        try
        {
            using var cmd = new NpgsqlCommand(sql, connection);
            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters);
            }

            return await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            await AresLogger.LogAsync("DB: Postgres", $"Error executing non-query: {sql}", severity: Severity.Error, extra: e.Message);
            throw;
        }
    }

    public async Task<NpgsqlDataReader> ExecuteReaderAsync(string sql, params NpgsqlParameter[] parameters)
    {
        if (connection == null || connection.State != System.Data.ConnectionState.Open)
        {
            throw new InvalidOperationException("Database connection is not open.");
        }

        try
        {
            var cmd = new NpgsqlCommand(sql, connection);
            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters);
            }

            return await cmd.ExecuteReaderAsync();
        }
        catch (Exception e)
        {
            await AresLogger.LogAsync("DB: Postgres", $"Error executing reader query: {sql}", severity: Severity.Error, extra: e.Message);
            throw;
        }
    }
}