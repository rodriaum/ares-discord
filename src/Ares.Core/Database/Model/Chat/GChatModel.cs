/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Database.Model.Chat.Sub;

namespace Ares.Core.Database.Model.Chat;

public class GChatModel
{
    public Dictionary<ulong, List<GChatInfoModel>> Infos { get; set; }

    public GChatModel()
    {
        Infos = new Dictionary<ulong, List<GChatInfoModel>>();
    }
}