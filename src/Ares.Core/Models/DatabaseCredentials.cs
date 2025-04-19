/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

namespace Ares.Core.Models;

public partial class DatabaseCredentials
{
    public string? Host { get; set; }
    public string? User { get; set; }
    public string? Password { get; set; }
    public string? Database { get; set; }
    public int Port { get; set; }
}