namespace Ares.src.Service.Chat;

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
    public double OutputPricePerToken { get; }

    /// <summary> Preço de token no prompt. </summary>
    public double InputPricePerToken { get; }

    /// <summary> Preço de forma detalhista. (Opcional) </summary>
    public ChatPriceUsageDetail? ChatPriceUsageDetail { get; set; }

    public ChatPriceUsage(double output = 0, double input = 0, ChatPriceUsageDetail? detail = null)
    {
        this.OutputPricePerToken = output;
        this.InputPricePerToken = input;
        ChatPriceUsageDetail = detail;
    }

    public double TotalPrice()
    {
        return this.OutputPricePerToken + this.InputPricePerToken;
    }
}