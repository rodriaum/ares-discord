/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

namespace Ares.Common.Models.Chat;

/// <summary>
/// Representa os diferentes papéis das mensagens no chat.
/// </summary>
/// <remarks>
/// Inspirado no código-fonte disponível em: 
/// <see href="https://github.com/openai/openai-dotnet/blob/main/src/Custom/Chat/ChatMessageRole.cs" />
/// </remarks>
public enum ChatRole
{
    /// <summary>
    /// Instruções para o modelo que guiam o comportamento das mensagens futuras do <c>Assistant</c>.
    /// </summary>
    System,

    /// <summary>
    /// Mensagens de entrada do usuário, normalmente combinadas com mensagens do <c>Assistant</c> em uma conversa.
    /// </summary>
    User,

    /// <summary>
    /// Mensagens de saída do modelo, contendo respostas ao usuário ou chamadas para ferramentas e funções necessárias para continuar a conversa.
    /// </summary>
    Assistant,

    /// <summary>
    /// Quando não há um papel definido para a mensagem.
    /// </summary>
    None
}