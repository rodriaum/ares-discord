using Ares.src.Guild.Chat.Sub;
using Ares.src.Guild.Config;
using Ares.src.Guild.Data;
using Ares.src.Guild.Information;
using Ares.src.Service.Model;
using Ares.src.Utils.Extra;
using Discord;
using OpenAI.Chat;
using System.ComponentModel.Design;
using System.Threading.Tasks;

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
            List<ChatHistoric>? list = this.ChatHistorics(user);

            if (list == null) return await Task.FromResult(false);

            list.Add(historic);

            await SaveHistoricAsync(user, list);
            return await SaveInformation(this.Information);
        }

        public async Task<bool> SaveInfoAsync(IUser user, List<ChatInfo> infos)
        {
            this.Information.Chat.Infos[user.Id] = infos;
            return await SaveInformation(this.Information);
        }

        public async Task<bool> SaveHistoricAsync(IUser user, ChatInfo info)
        {
            List<ChatInfo>? list = this.ChatInfos(user);

            if (list == null) return await Task.FromResult(false);

            list.Add(info);

            await SaveInfoAsync(user, list);
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

        public Dictionary<ulong, List<ChatInfo>>? Infos()
        {
            return Information.Chat.Infos;
        }

        public List<ChatHistoric>? ChatHistorics(IUser user, ulong channel = 0)
        {
           if (channel != 0)
            {
                return Historics()?[user.Id].FindAll(historic => historic.Channel == channel);
            }

            return Historics()?[user.Id];
        }

        public List<ChatInfo>? ChatInfos(IUser user)
        {
            return Infos()?[user.Id];
        }

        public List<ChatHistoric>? ChatHistoricsByChannel(IUser user, ulong channel)
        {
            return Historics()?[user.Id].FindAll(historic => historic.Channel == channel);
        }

        public ChatInfo? ChatInfoByChannel(IUser user, ulong channel)
        {
            return Infos()?[user.Id].FindLast(historic => historic.Channel == channel);
        }

        public Task<bool> ToggleChatInfo(IUser user, ulong channel, bool active)
        {
            var infos = this.ChatInfos(user);
            if (infos == null)
            {
                LogUtil.Error(nameof(ToggleChatInfo), "Não foi possível alterar o status de uma informação de um chat.");
                return Task.FromResult(false);
            }

            ChatInfo? info = infos.LastOrDefault(i => i.Channel == channel);

            if (info != null)
            {
                info.Active = active;
            }

            return this.SaveInfoAsync(user, infos);
        }

        public ChatHistoric? LastChatHistoric(IUser user, ulong channel = 0)
        {
            List<ChatHistoric>? historics = this.ChatHistorics(user);

            if (historics == null || historics.Count == 0)
                return null;

            return (channel != 0 ? historics.FindAll(it => it.Channel == channel).LastOrDefault() : historics.LastOrDefault());
        }

        public ChatInfo? LastChatInfo(IUser user, ulong channel = 0)
        {
            List<ChatInfo>? infos = this.ChatInfos(user);

            if (infos == null || infos.Count == 0)
                return null;

            return (channel != 0 ? infos.FindAll(it => it.Channel == channel).LastOrDefault() : infos.LastOrDefault());
        }

        public Task<bool> CreateChatData(IUser user, ChatInfo info)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            if (HasActiveUserConversation(user))
            {
                LogUtil.Log(nameof(CreateChatData), "O usuário já possui uma conversa ou modelo. Nenhuma ação necessária.");
                return Task.FromResult(false);
            }

            GuildChatData chat = Information.Chat;

            if (chat == null)
            {
                LogUtil.Error(nameof(CreateChatData), "GuildChatData está nulo. Não foi possível criar dados de chat para o usuário.");
                return Task.FromResult(false);
            }

            try
            {
                if (!chat.Historics.ContainsKey(user.Id))
                {
                    chat.Historics[user.Id] = new List<ChatHistoric>();
                }

                if (!chat.Infos.TryGetValue(user.Id, out var infos))
                {
                    infos = new List<ChatInfo>();
                    chat.Infos[user.Id] = infos;
                }

                infos.Add(info);

                return this.SaveChatDataAsync(chat);
            }
            catch (Exception ex)
            {
                LogUtil.Error(nameof(CreateChatData), "Erro ao tentar criar um histórico de chat para o usuário.", ex.Message);
                return Task.FromResult(false);
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

            var infos = this.Infos();

            if (infos == null)
            {
                LogUtil.Error(nameof(HasActiveUserConversation), "Não foi possível obter o histórico de informações.");
                return false;
            }

            var info = infos.TryGetValue(user.Id, out var value);

            return value != null && (value.Count > 0 && value[value.Count - 1].Active);
        }

        public ChatModel? GetLastModelByUser(IUser user, ulong channel = 0)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var info = this.LastChatInfo(user, channel: channel);
            if (info == null) return null;

            var model = info.Model;
            if (string.IsNullOrWhiteSpace(model)) return null;

            return ChatModel.GetByNearestModel(model);
        }
    }
}