```csharp
using OpenAI.Chat;
using System;
using System.Threading.Tasks;

// Exemplo de como adicionar modelos customizados em C# dentro de um arquivo .md

public class CustomModelExample
{
    public async Task Run()
    {
        // Exemplo de inicialização de um cliente para um endpoint customizado (Minimax)
        var minimaxClient = new OpenAIClient(
            apiKey: "SUA_API_KEY_MINIMAX",
            apiBase: "https://api.minimax.chat/v2"
        );

        var minimaxRequest = new ChatRequest(
            model: "abab5.5-chat",
            messages: new[]
            {
                new ChatMessage("user", "Olá, Minimax!"),
            }
        );
        var minimaxResponse = await minimaxClient.ChatEndpoint.CreateChatCompletionAsync(minimaxRequest);
        Console.WriteLine("Resposta Minimax: " + minimaxResponse.Choices[0].Message.Content);

        // Exemplo para Alibaba Qwen3 (via HuggingFace)
        var alibabaClient = new OpenAIClient(
            apiKey: "SUA_API_KEY_HUGGINGFACE",
            apiBase: "https://api.huggingface.co"
        );

        var alibabaRequest = new ChatRequest(
            model: "Qwen/Qwen3-235B-A22B",
            messages: new[]
            {
                new ChatMessage("user", "Demonstre um exemplo com o modelo Qwen3!")
            }
        );
        var alibabaResponse = await alibabaClient.ChatEndpoint.CreateChatCompletionAsync(alibabaRequest);
        Console.WriteLine("Resposta Alibaba: " + alibabaResponse.Choices[0].Message.Content);

        // Exemplo para Nvidia Nemotron (endpoint customizado)
        var nvidiaClient = new OpenAIClient(
            apiKey: "SUA_API_KEY_NVIDIA",
            apiBase: "https://api.build.nvidia.com"
        );

        var nvidiaRequest = new ChatRequest(
            model: "nvidia/llama-3_1-nemotron-ultra-253b-v1",
            messages: new[]
            {
                new ChatMessage("user", "Fale sobre integração via NVIDIA Build.")
            }
        );
        var nvidiaResponse = await nvidiaClient.ChatEndpoint.CreateChatCompletionAsync(nvidiaRequest);
        Console.WriteLine("Resposta Nvidia: " + nvidiaResponse.Choices[0].Message.Content);
    }
}
```
