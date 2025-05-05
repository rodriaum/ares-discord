/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using MongoDB.Bson.Serialization.Conventions;

namespace Ares.Core;

public class AresConstant
{
    /*
     * App
     */

    public static readonly string AppName = "Ares";
    public static readonly bool AppDevMode = false;
    public static readonly string AppVersion = "1.0.0";

    public static readonly bool AppDebugMode = false;
    public static readonly bool AppMonitorDebugMode = true;


    /*
     * Project
     */

    public static readonly string BasePath = AppContext.BaseDirectory;
    public static readonly string ProjectPath = Path.GetFullPath(Path.Combine(BasePath, $@"..\..\..\..\..\"));

    /*
     * Messages
     */

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
    public static readonly int MaxFreeConversations = 1;

    /*
     * URLs
     */

    public static readonly string ImgurApiUrl = "https://api.imgur.com/3/image";
}