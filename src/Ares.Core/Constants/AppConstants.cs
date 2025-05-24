/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

namespace Ares.Core.Constants;

public class AppConstants
{
    #region App

    public const string AppName = "Ares";
    public const string AppVersion = "1.0.0";
    public const string AppNativeLanguage = "pt";

    public const bool AppDevMode = false;

    public static readonly bool AppDebugMode = true;
    public static readonly bool AppMonitorDebugMode = true;

    #endregion
    #region Project

    public static readonly string BasePath = AppContext.BaseDirectory;
    public static readonly string ProjectPath = Path.GetFullPath(Path.Combine(BasePath, $@"..\..\..\..\..\"));

    #region Messages
    #endregion

    public const string UnablePerformTask = "Ops! Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.";
    public const string UnableGetMember = "Ops! Não foi possível encontrar as informações do seu perfil.";

    #endregion
    #region Redis Channels

    public const string GRedisChannel = "guild_channel";

    #endregion
    #region Emotes

    public const string LoadingEmote = ":hourglass:";

    #endregion
    #region IDs

    public static readonly IReadOnlyList<string> DeveloperUserIds =
    [
        "1065788770739294289" // Rodriaum
    ];

    #endregion
    #region Limits

    public const int MaxPremiumConversations = 5;
    public const int MaxFreeConversations = 1;

    #endregion
    #region URLs

    public const string ImgurApiUrl = "https://api.imgur.com/3/image";

    #endregion
}