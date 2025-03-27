using Ares.Core.Objects.Chat;
using Ares.Core.Util;

namespace Ares.Core.Database.Model.Chat.Sub;

/// <summary>
/// Representa o histórico de conversas.
/// </summary>
public class GChatHistoricModel
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
    /// Inicializa uma nova instância da classe <see cref="GChatHistoricModel"/>.
    /// </summary>
    /// <param name="prompt">Pergunta</param>
    /// <param name="response">Resposta ou Prompt Revisado</param>
    /// <param name="imageUrl">Opcional: Url da Image</param>
    /// <param name="usage">Uso de Tokens</param>
    /// <param name="timestamp">Timestamp da Conversa</param>
    public GChatHistoricModel(string prompt = "", string? response = "", string? imageUrl = "", ChatValueUsage? usage = null, long timestamp = -1)
    {
        Prompt = prompt;
        Response = response;
        ImageUrl = imageUrl;
        Usage = usage;
        Timestamp = timestamp;

        if (timestamp == -1)
        {
            Timestamp = TimeUtil.CurrentTimeMillis();
        }
    }
}