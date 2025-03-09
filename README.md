# Ares

Ares é uma plataforma de chat privado poderosa e flexível projetada para aprimorar sua experiência no Discord com interações inteligentes. Esta plataforma utiliza tecnologias de IA de ponta para gerar texto, imagens e mais, tudo isso mantendo armazenamento de dados seguro e em conformidade.

## Demonstração em Vídeo

https://github.com/user-attachments/assets/4adcdaf8-8a5a-4a45-ac0b-68eb1518bc1f

*O vídeo acima demonstra como criar um chat privado.*

## Recursos

### Chat Privado
- **Chats Personalizáveis**: Crie chats privados adaptados às suas necessidades.
- **Multi Linguagem**: Suporta diversas linguagens tais como Português, Inglês, etc. Podendo criar um arquivo `.json` para cada idioma.

### Interações com IA
- **Geração de Texto**: Crie de conversas naturais usando modelos de vários provedores de IA.
- **Criação de Imagens**: Gere imagens de alta qualidade baseadas em descrições do usuário.
- **Gerenciamento de Modelos**: Acesse e alterne entre diferentes modelos de IA sem problemas.

### Armazenamento de Dados
- **MongoDB**: Armazene extenso histórico de conversas, URLs de imagens e usos de tokens com segurança no MongoDB.

## Pré-requisitos
Para usar o Ares, certifique-se de ter o seguinte configurado:
1. **Discord.Net**: A plataforma principal para interagir com o Ares.
2. **Serviços de IA**: Acesso aos modelos OpenAI, Anthropic, DeepSeek, etc.
3. **Driver MongoDB**: Para armazenar grandes conjuntos de dados eficientemente.

## Instalação
Siga estes passos para configurar seu ambiente:
1. Clone o repositório:
   ```bash
   git clone https://github.com/rodriaum/ares-discord.git
   cd ares-discord
   ```
2. Instale os pacotes NuGet necessários:
   ```bash
   dotnet add package Anthropic.SDK
   dotnet add package Ater.DeepSeek.Core
   dotnet add package Discord.Net
   dotnet add package DotNetEnv
   dotnet add package Lombok.Nte
   dotnet add package MongoDB.Driver
   dotnet add package OpenAI
   ```
3. Crie um arquivo `.env` na pasta de compilação (Release ou Build) do projeto com base no `.env.example`:
   ```
   DISCORD_TOKEN=...
   ```

## Arquitetura
Ares é construído com uma arquitetura modular que permite fácil integração de novos modelos e serviços de IA. A plataforma utiliza:
- Serviços de IA: SDKs OpenAI, Anthropic e DeepSeek.
- Armazenamento de Dados: MongoDB para manipulação eficiente de dados.
- Integração com Discord: Suporte completo à API do Discord para criação e gerenciamento de bots.

## Licença
[GPL-3.0](https://github.com/rodriaum/ares-discord?tab=GPL-3.0-1-ov-file)