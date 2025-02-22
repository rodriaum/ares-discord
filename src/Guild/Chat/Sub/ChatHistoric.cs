using Ares.src.Service.Chat;
using Ares.src.Utils.Extra;

namespace Ares.src.Guild.Chat.Sub;

/// <summary>
/// Representa o histórico de conversas.
/// </summary>
public class ChatHistoric
{
    /// <summary>
    /// Aplica um ID Unico.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Indica se o histórico está ativo.
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Indica o canal de texto.
    /// </summary>
    public ulong Channel { get; set; }

    /// <summary>
    /// Modelo da AI utilizado na conversa.
    /// </summary>
    public string Model { get; set; }

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
    /// Papel do participante no chat.
    /// </summary>
    public ChatRole Role { get; set; }

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
    /// <param name="model">Modelo da AI</param>
    /// <param name="prompt">Pergunta</param>
    /// <param name="role">Papel do Participante</param>
    /// <param name="usage">Uso de Tokens</param>
    /// <param name="timestamp">Timestamp da Conversa</param>
    public ChatHistoric(ulong channel, string model = "", string prompt = "", string? response = "", string? imageUrl = "", ChatRole role = ChatRole.None, ChatValueUsage? usage = null, long timestamp = -1)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Active = true;
        this.Channel = channel;
        this.Model = model;
        this.Prompt = prompt;
        this.Response = response;
        this.ImageUrl = imageUrl;
        this.Role = role;
        this.Usage = usage;
        this.Timestamp = timestamp;

        if (timestamp == -1)
        {
            this.Timestamp = TimeUtil.CurrentTimeMillis();
        }
    }
}