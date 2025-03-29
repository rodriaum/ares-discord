/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

namespace Ares.Core.Database;

internal interface DatabaseTemplate
{
    Task ConnectAsync();
    Task CloseAsync();
    bool IsConnected();
}