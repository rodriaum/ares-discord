/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Constants;
using Ares.Core.Objects;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Ares.Core.Util;

public class WebUtil
{
    public static async Task<string?> UploadMediaFromUrl(string token, string imageUrl)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", token);

            MultipartFormDataContent content = new MultipartFormDataContent
            {
                { new StringContent(imageUrl), "image" }
            };

            HttpResponseMessage response = await client.PostAsync(AppConstants.ImgurApiUrl, content);
            string jsonResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                AresLogger.Log(nameof(UploadMediaFromUrl), $"Error sending a response.", severity: Severity.Error, extra: jsonResponse);
            }

            using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
            {
                JsonElement root = doc.RootElement;

                string? imgurLink = root.GetProperty("data").GetProperty("link").GetString();
                return imgurLink ?? null;
            }
        }
    }

    public static bool IsValidUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return uri != null && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        return false;
    }
}