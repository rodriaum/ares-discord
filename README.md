# Ares

Este repositório contém o código-fonte privado de um bot para Discord que permite criar chats privados para geração de imagens e conversas. O bot utiliza as versões mais recentes dos modelos de texto e imagem do OpenAI, Anthropic, Deepseek, etc. O histórico das conversas é armazenado em um banco de dados MongoDB, mas não pode ser utilizado para corrigir as imagens geradas.

## Funcionalidades

- **Criação de chats privados**: O bot permite aos usuários criar canais privados onde podem gerar imagens e realizar conversas.
- **Geração de imagens**: O bot integra-se com a API mais recente da OpenAI para criar imagens com base em texto.
- **Geração de texto**: O bot usa a API mais recente de processamento de linguagem natural da OpenAI para gerar conversas.
- **Histórico de conversas**: O histórico das conversas e imagens geradas é armazenado em um banco de dados MongoDB.
- **Imagens armazenadas**: As imagens geradas são armazenadas no banco de dados e podem ser acessadas posteriormente.
- **Segurança**: O histórico de conversas não pode ser usado para corrigir ou alterar as imagens geradas.

## Pré-requisitos

- **.NET 6 ou superior**: O bot é desenvolvido em C# e requer a instalação do .NET 6 ou uma versão superior.
- **MongoDB**: O banco de dados utilizado é o MongoDB, que deve estar configurado e acessível.
- **Discord Bot Token**: Você precisa de um token de bot do Discord para integrar o bot. Configure a variável de ambiente `DISCORD_TOKEN` com o token do seu bot.

## Instalação

1. Clone este repositório:

    ```bash
    git clone https://github.com/rodriaum/Ares.git
    cd Ares
    ```

2. Instale as dependências com o NuGet. As bibliotecas necessárias são:

    - [Anthropic.SDK](https://github.com/tghamm/Anthropic.SDK)
    - [Ater.DeepSeek.Core](https://github.com/niltor/DeepSeekSDK-NET)
    - [Discord.Net](https://github.com/discord-net/Discord.Net)
    - [DotNetEnv](https://github.com/torvalds/dotnetenv)
    - [Lombok.Nte](https://github.com/Lombok-Nte)
    - [MongoDB.Driver](https://github.com/mongodb/mongo-csharp-driver)
    - [OpenAI .NET](https://github.com/openai/openai-dotnet)

    Você pode instalar todas essas dependências utilizando o comando:

    ```bash
    dotnet add package Anthropic.SDK
    dotnet add package Ater.DeepSeek.Core
    dotnet add package Discord.Net
    dotnet add package DotNetEnv
    dotnet add package Lombok.Nte
    dotnet add package MongoDB.Driver
    dotnet add package OpenAI
    ```

3. Configure a variável de ambiente `DISCORD_TOKEN` com o token do seu bot no Discord. Isso pode ser feito no terminal ou no arquivo de configuração do ambiente.

    ```bash
    export DISCORD_TOKEN="seu_token_aqui"
    ```

4. Configure o MongoDB no seu ambiente, certificando-se de que ele esteja acessível para o bot.

5. Compile e execute o bot:

    ```bash
    dotnet build
    dotnet run
    ```

## Utilização

- **Adicionar o Bot à sua Guilda**: O bot permite que os usuários adicionem o bot ao seu servidor Discord de maneira pública. Visite o link do bot no Discord para adicionar à sua guilda.
  
- **Criar Chat Privado**: Para criar um chat privado, basta configurar a Embed num canal específico e o bot criará um canal privado com o modelo escolhido para você interagir com ele.

- **Gerar Imagens**: Para gerar imagens, basta fornecer uma descrição no chat privado.

- **Interagir com o Bot**: Você pode enviar mensagens de texto e o bot responderá com conversas geradas de forma natural. As conversas são armazenadas no banco de dados, mas não podem ser usadas para alterar as imagens.

## Arquitetura

- **C#**: O código-fonte foi desenvolvido utilizando a linguagem C# e o .NET 6.
- **API Discord**: O bot utiliza a API do Discord para interagir com os usuários e adicionar o bot aos servidores.
- **OpenAI e Anthropic Deepseek**: O bot usa a versão mais recente das APIs de geração de texto e imagem do OpenAI e do Deepseek para gerar as conversas e imagens.
- **MongoSQL**: O banco de dados utilizado é o MongoDB, que armazena tanto as conversas quanto as imagens geradas.

## Licença

Este projeto é licenciado sob a [GPL-3.0 license](https://github.com/rodriaum/Ares?tab=GPL-3.0-1-ov-file).