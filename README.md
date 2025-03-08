# Ares

Ares é uma plataforma de chat privado poderosa e flexível projetada para aprimorar sua experiência no Discord com interações inteligentes. Esta plataforma utiliza tecnologias de IA de ponta para gerar texto, imagens e mais, tudo isso mantendo armazenamento de dados seguro e em conformidade.

## Demonstração em Vídeo

https://github.com/user-attachments/assets/demo-ares-platform.mp4

*O vídeo acima demonstra como criar um chat privado, usar comandos de IA e gerenciar modelos no Ares.*

## Recursos

### Gerenciamento de Chat Privado
- **Chats Personalizáveis**: Crie chats privados adaptados às suas necessidades.
- **Rastreamento de Tokens**: Gerencie eficientemente o uso de tokens com mecanismos de rastreamento integrados (veja `ChatValueUsage` para detalhes).

### Interações com IA
- **Geração de Texto**: Participe de conversas naturais usando modelos de vários provedores de IA.
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
3. Crie um arquivo `.env` na pasta de compilação (Release ou Build) do projeto:
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