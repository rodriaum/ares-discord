/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

namespace Ares.Common.Interfaces;

public interface IDatabase
{
    Task ConnectAsync();
    Task CloseAsync();
    bool IsConnected();
}