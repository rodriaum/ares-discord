/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

namespace Ares.Core;

internal class AresConstant
{
    /*
     * General 
     */

    public static readonly string AppName = "Ares";

    public static readonly string UnablePerformTask = "Ops! Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.";
    public static readonly string UnableGetMember = "Ops! Não foi possível encontrar as informações do seu perfil.";

    /*
     * Redis Channels
     */

    public static readonly string GRedisChannel = "guild_channel";

    /*
     * Emotes
     */

    public static readonly string LoadingEmote = ":hourglass:";

    /*
     * IDs
     */

    public static readonly List<string> DeveloperUserIds = new List<string>
    {
        "1065788770739294289" // Rodriaum
    };

    /*
     * Limits
     */

    public static readonly int MaxPremiumConversations = 5;
}