using System.Net.Http.Headers;
using System.Text.Json;

namespace Ares.src.Utils.Extra;

public class RequestUtil
{
    private const string ImgurApiUrl = "https://api.imgur.com/3/image";

    public static async Task<string> UploadMediaFromUrl(string token, string imageUrl)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", token);

            var content = new MultipartFormDataContent
            {
                { new StringContent(imageUrl), "image" }
            };

            HttpResponseMessage response = await client.PostAsync(ImgurApiUrl, content);
            string jsonResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                LogUtil.Error("Request", $"Erro ao enviar imagem.", jsonResponse);
            }

            using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
            {
                JsonElement root = doc.RootElement;

                string imgurLink = root.GetProperty("data").GetProperty("link").GetString();
                return imgurLink;
            }
        }
    }
}