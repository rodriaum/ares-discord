namespace Ares.Core.Objects.Chat.Price;

/// <summary>
/// Representa o preço em dólar por token usado no pedido e resposta.
/// </summary>
/// <remarks>
/// Inspirado no código-fonte disponível em: 
/// <see href="https://github.com/openai/openai-dotnet/blob/main/src/Custom/Chat/ChatTokenUsage.cs" />
/// </remarks>
public class ChatPriceUsage
{
    /// <summary> Preço por token na conclusão gerada. </summary>
    public decimal OutputPricePerToken { get; }

    /// <summary> Preço de token no prompt. </summary>
    public decimal InputPricePerToken { get; }

    /// <summary> Preço de forma detalhista. (Opcional) </summary>
    public List<ChatPriceUsageDetail>? ChatPriceUsageDetail { get; set; }

    public ChatPriceUsage(decimal output = 0, decimal input = 0, List<ChatPriceUsageDetail>? details = null)
    {
        OutputPricePerToken = output;
        InputPricePerToken = input;
        ChatPriceUsageDetail = details ?? new List<ChatPriceUsageDetail>();
    }

    public decimal TotalPrice()
    {
        return OutputPricePerToken + InputPricePerToken;
    }
}