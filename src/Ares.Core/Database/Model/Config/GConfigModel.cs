/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

namespace Ares.Core.Database.Model.Config;

public partial class GuildConfigData
{
    public string Lang { get; set; }

    public ulong MemberRoleId { get; set; }
    public ulong UsageRoleId { get; set; }
    public ulong ExclusiveRoleId { get; set; }

    public ulong SetupChannelId { get; set; }
    public ulong LogChannelId { get; set; }

    public ulong ChatsCategoryId { get; set; }

    public GuildConfigData()
    {
        // By default, the language is Portuguese
        this.Lang = "pt";
    }
}