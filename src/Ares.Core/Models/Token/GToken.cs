/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using System.Text.Json.Serialization;

namespace Ares.Core.Models.Token;

public class GToken
{
    [JsonInclude]
    [JsonPropertyName("list")]
    public Dictionary<string, string> List { get; private set; }

    public GToken(Dictionary<string, string>? list = null)
    {
        this.List = list ?? new Dictionary<string, string>();
    }

    public string? GetToken(string key)
    {
        return this.List.GetValueOrDefault(key);
    }

    public void SetToken(string key, string value)
    {
        this.List[key] = value;
    }

    public void RemoveToken(string key)
    {
        this.List.Remove(key);
    }

    public void ClearTokens()
    {
        this.List.Clear();
    }

    public bool ContainsToken(string key)
    {
        return this.List.ContainsKey(key);
    }

    public bool ContainsValue(string value)
    {
        return this.List.ContainsValue(value);
    }

    public bool IsEmpty()
    {
        return this.List.Count == 0;
    }

    public int Count()
    {
        return this.List.Count;
    }
}