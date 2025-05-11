/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

namespace Ares.Core.Constants;

public class AresConstant
{
    /*
     * App
     */

    public const string AppName = "Ares";
    public static readonly bool AppDevMode = false;
    public const string AppVersion = "1.0.0";

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

    public const string UnablePerformTask = "Ops! Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.";
    public const string UnableGetMember = "Ops! Não foi possível encontrar as informações do seu perfil.";

    /*
     * Redis Channels
     */

    public const string GRedisChannel = "guild_channel";

    /*
     * Emotes
     */

    public const string LoadingEmote = ":hourglass:";

    /*
     * IDs
     */

    public static readonly IReadOnlyList<string> DeveloperUserIds =
    [
        "1065788770739294289" // Rodriaum
    ];

    /*
     * Limits
     */

    public const int MaxPremiumConversations = 5;
    public const int MaxFreeConversations = 1;

    /*
     * URLs
     */

    public const string ImgurApiUrl = "https://api.imgur.com/3/image";
}