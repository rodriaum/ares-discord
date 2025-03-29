/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using StackExchange.Redis;

namespace Ares.Core.Database.Redis.Channel;

public class RedisChannelMessage
{
    public string? Channel { get; set; }
    public RedisValue Message { get; set; }
}