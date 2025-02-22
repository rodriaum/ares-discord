using Ares.src.Guild.Chat.Sub;
using Ares.src.Guild.Config;
using Ares.src.Guild.Data;
using Ares.src.Guild.Information;
using Ares.src.Service.Model;
using Ares.src.Utils.Extra;
using Discord;
using OpenAI.Chat;

namespace Ares.src.Guild
{
    public class Guild
    {
        public readonly string Id;

        public GuildInformation Information;

        /// <summary>
        /// Construtor da classe Guild.
        /// </summary>
        /// <param name="id">Identificador da guilda.</param>

        public Guild(string id)
        {
            this.Id = id;
            this.Information = new GuildInformation();
        }

        /// <summary>
        /// Salva os campos especificados na guilda.
        /// </summary>
        /// <param name="fields">Lista de campos a serem salvos.</param>
        /// <returns>Retorna true se os campos foram salvos com sucesso, false caso contrário.</returns>

        public async Task<bool> SaveAsync(List<string> fields)
        {
            if (fields == null || fields.Count == 0)
                throw new ArgumentException("A lista de campos não pode ser nula ou vazia.", nameof(fields));

            if (Core.GuildData is not { } guildData)
            {
                LogUtil.Error(nameof(SaveAsync), "GuildData está nulo. Não foi possível salvar os campos.");
                return false;
            }

            try
            {
                foreach (var field in fields)
                {
                    if (string.IsNullOrWhiteSpace(field))
                        throw new ArgumentException("A lista de campos contém um valor nulo ou vazio.", nameof(fields));

                    await guildData.Update(this, field);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Error(nameof(SaveAsync), "Erro ao atualizar um ou vários campos no banco de dados.", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Salva um único campo especificado na guilda.
        /// </summary>
        /// <param name="field">Campo a ser salvo.</param>
        /// <returns>Retorna true se o campo foi salvo com sucesso, false caso contrário.</returns>

        public async Task<bool> SaveAsync(string field)
        {
            return await SaveAsync(new List<string> { field });
        }

        /// <summary>
        /// Salva as informações gerais da guilda.
        /// </summary>
        /// <param name="information">Objeto com as informações da guilda.</param>
        /// <returns>Retorna true se as informações foram salvas com sucesso, false caso contrário.</returns>

        public async Task<bool> SaveInformation(GuildInformation information)
        {
            if (information == null)
            {
                LogUtil.Error("InformationNull", "Não foi possível pegar as informações da guilda. (SaveInformation)");
                return false;
            }

            this.Information = information;

            return await SaveAsync("Information");
        }

        /// <summary>
        /// Atualiza os dados de chat da guilda no banco de dados.
        /// </summary>
        /// <param name="data">Objeto contendo os dados de chat da guilda.</param>
        /// <returns>Retorna true se os dados foram atualizados com sucesso, false caso contrário.</returns>

        public async Task<bool> SaveChatDataAsync(GuildChatData chatData)
        {
            this.Information.Chat = chatData;
            return await SaveInformation(this.Information);
        }

        public async Task<bool> SaveHistoricAsync(IUser user, List<ChatHistoric> historics)
        {
            this.Information.Chat.Historics[user.Id] = historics;
            return await SaveInformation(this.Information);
        }

        public async Task<bool> SaveHistoricAsync(IUser user, ChatHistoric historic)
        {
            var list = this.ChatHistorics(user);

            list.Add(historic);

            await SaveHistoricAsync(user, list);

            return await SaveInformation(this.Information);
        }

        /// <summary>
        /// Atualiza os dados de ID da guilda no banco de dados.
        /// </summary>
        /// <param name="data">Objeto contendo os dados de ID da guilda.</param>
        /// <returns>Retorna true se os dados foram atualizados com sucesso, false caso contrário.</returns>

        public async Task<bool> SaveGuildIdDataAsync(GuildConfigData configData)
        {
            this.Information.Config = configData;

            return await SaveInformation(this.Information);
        }

        /** Sistema de Conversa */

        /// <summary>
        /// Retorna o histórico de conversas da guilda.
        /// </summary>
        /// <returns>Dicionário contendo os históricos de conversas ou null caso não existam.</returns>

        public Dictionary<ulong, List<ChatHistoric>>? Historics()
        {
            return Information.Chat.Historics;
        }

        public List<ChatHistoric>? ChatHistorics(IUser user)
        {
            return Historics()?[user.Id];
        }

        public ChatHistoric? LastChatHistoric(IUser user, bool active = true)
        {
            List<ChatHistoric>? historic = this.ChatHistorics(user);
            if (historic == null || (historic != null && historic.Count == 0)) return null;

            return historic.Last(historic => historic.Active == active);
        }

        public async Task<bool> CreateChatData(IUser user, ChatHistoric historic)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (historic.Model == null) throw new ArgumentNullException(nameof(historic.Model));

            if (HasActiveUserConversation(user))
            {
                LogUtil.Log(nameof(CreateChatData), "O usuário já possui uma conversa ou modelo. Nenhuma ação necessária.");
                return false;
            }

            var chat = Information.Chat;

            if (chat == null)
            {
                LogUtil.Error(nameof(CreateChatData), "GuildChatData está nulo. Não foi possível criar dados de chat para o usuário.");
                return false;
            }

            try
            {
                var historics = new List<ChatHistoric>();

                if (chat.Historics.ContainsKey(user.Id))
                {
                    historics = chat.Historics[user.Id];
                }

                historics.Add(historic);

                bool success = chat.Historics.TryAdd(user.Id, historics);

                if (!success)
                {
                    LogUtil.Error(nameof(CreateChatData), "Falha ao adicionar o usuário nos dados de chat.");
                    return false;
                }

                return await this.SaveChatDataAsync(chat);

            }
            catch (Exception ex)
            {
                LogUtil.Error(nameof(CreateChatData), "Erro ao tentar criar dados de chat para o usuário.", ex.Message);
                return false;
            }
        }

        public async Task<bool> UpdateChatHistoricsAsync(IUser user, List<ChatHistoric> historics)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (historics == null) throw new ArgumentNullException(nameof(historics));

            if (Information.Chat is not { } chat)
                return false;

            chat.Historics[user.Id] = historics;

            Information.Chat = chat;
            return await SaveInformation(Information);
        }

        public async Task<bool> UpdateChatHistoricsAsync(IUser user, ChatHistoric historic)
        {
            var historics = this.Historics();

            if (historics == null)
            {
                LogUtil.Error(nameof(this.UpdateChatHistoricsAsync), "Conversation historics are null.");
                return false;
            }

            if (!historics.TryGetValue(user.Id, out var userHistorics) || userHistorics == null)
            {
                LogUtil.Error(nameof(this.UpdateChatHistoricsAsync), $"Cannot retrieve chat messages for user ID {user.Id}.");
                return false;
            }

            userHistorics.Add(historic);

            return await this.UpdateChatHistoricsAsync(user, userHistorics);
        }

        public async Task<bool> RemoveConversationAsync(IUser user, ChatHistoric historic)
        {
            var historics = this.Historics();

            if (historics == null)
            {
                LogUtil.Error(nameof(this.RemoveConversationAsync), "Conversation historics are null.");
                return false;
            }

            if (!historics.TryGetValue(user.Id, out var userHistorics) || userHistorics == null)
            {
                LogUtil.Error(nameof(this.RemoveConversationAsync), $"Cannot retrieve chat messages for user ID {user.Id}.");
                return false;
            }

            userHistorics.Remove(historic);

            return await this.UpdateChatHistoricsAsync(user, userHistorics);
        }

        /// <summary>
        /// Verifica se o usuário possui uma conversa existente.
        /// </summary>
        /// <param name="user">Usuário alvo.</param>
        /// <returns>Retorna true se a conversa existe, caso contrário, false.</returns>

        public bool HasActiveUserConversation(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var historics = this.Historics();

            if (historics == null)
            {
                LogUtil.Error(nameof(HasActiveUserConversation), "Não foi possível obter o histórico de conversas.");
                return false;
            }

            var historic = historics.TryGetValue(user.Id, out var value);

            return value != null && (value.Count > 0 && value[value.Count - 1].Active);
        }

        public ChatModel? GetLastModelByUser(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var historic = this.LastChatHistoric(user);
            if (historic == null) return null;

            var model = historic.Model;
            if (string.IsNullOrWhiteSpace(model)) return null;

            return ChatModel.GetByNearestModel(model);
        }

        public int GetLastMessagesByRole(IUser user, ChatMessageRole role)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var historic = this.ChatHistorics(user);

            if (historic != null)
            {
                return historic.Count(m => m.Role == AiUtil.ConvertOpenAiRole(role));
            }

            return 0;
        }
    }
}