using Ares.src.Service.Chat;
using Ares.src.Utils.Extra;

namespace Ares.src.Guild.Chat.Sub;

/// <summary>
/// Representa o histórico de conversas.
/// </summary>
public class ChatHistoric
{
    /// <summary>
    /// Pergunta ou comando enviado pelo usuário.
    /// </summary>
    public string Prompt { get; set; }

    /// <summary>
    /// Resposta gerada pela AI, se aplicável.
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// URL de uma imagem gerada na conversa, se aplicável.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Informações sobre o uso de tokens na conversa.
    /// </summary>
    public ChatValueUsage? Usage { get; set; }

    /// <summary>
    /// Timestamp da conversa.
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="ChatHistoric"/>.
    /// </summary>
    /// <param name="prompt">Pergunta</param>
    /// <param name="response">Resposta ou Prompt Revisado</param>
    /// <param name="imageUrl">Opcional: Url da Image</param>
    /// <param name="usage">Uso de Tokens</param>
    /// <param name="timestamp">Timestamp da Conversa</param>
    public ChatHistoric(string prompt = "", string? response = "", string? imageUrl = "", ChatValueUsage? usage = null, long timestamp = -1)
    {
        this.Prompt = prompt;
        this.Response = response;
        this.ImageUrl = imageUrl;
        this.Usage = usage;
        this.Timestamp = timestamp;

        if (timestamp == -1)
        {
            this.Timestamp = TimeUtil.CurrentTimeMillis();
        }
    }
}