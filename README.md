# Ares

Ares is a powerful and flexible private chat platform designed to enhance your Discord experience through intelligent interactions. It uses modern AI technologies to generate text, images, and more while keeping data storage secure and reliable.

## Video Demo

https://github.com/user-attachments/assets/4adcdaf8-8a5a-4a45-ac0b-68eb1518bc1f

*The video above demonstrates how to create a private chat. Note: real-time message editing was removed due to Discord rate limits.*

## Features

### Private Chat
- **Customizable Chats:** Create private chats tailored to your needs.
- **Multilingual Support:** Supports multiple languages, including Portuguese and English. Add a `.json` file for each language.

### AI-Powered Interactions
- **Text Generation:** Generate natural conversations using models from multiple AI providers.
- **Image Generation:** Create high-quality images from user-provided prompts.
- **Model Management:** Easily access and switch between different AI models.

### Data Storage
- **Local Storage:** Acts as the primary cache before Redis, providing fast temporary storage for efficient data retrieval and deletion.
- **MongoDB:** Securely stores extensive conversation history, image URLs, and token usage.
- **Redis:** Provides a high-speed intermediate cache, improving access to persisted MongoDB data and reducing interaction latency.

## Prerequisites

Before using Ares, make sure you have the following:

1. **.NET and Discord.Net:** The platform and library used to interact with Discord.
2. **AI Providers:** Access to AI models from providers such as OpenAI, Anthropic, DeepSeek, and xAI.
3. **MongoDB Driver:** Used to store conversation data and related information.
4. **Redis:** Used for caching and faster data access.

## Installation

Follow these steps to set up your development environment:

1. Clone the repository:

   ```bash
   git clone https://github.com/rodriaum/ares-discord.git
   cd ares-discord
   ```

2. Install the required NuGet packages:

   ```bash
   dotnet add package Discord.Net
   dotnet add package DotNetEnv
   dotnet add package Lombok.Nte
   dotnet add package MongoDB.Driver
   dotnet add package StackExchange.Redis
   dotnet add package OpenAI
   ```

3. Create a `.env` file in the project's build output directory (`Release` or `Build`), using `.env.example` as a reference:

   ```env
   DISCORD_TOKEN=your_discord_bot_token
   ```

## Architecture

Ares follows a modular architecture designed to make it easier to integrate new AI models and services. It uses:

- **AI Services:** OpenAI, Anthropic, DeepSeek, and xAI.
- **Data Storage:** MongoDB for persistent data and Redis for optimized caching.
- **Discord Integration:** Discord API support for creating and managing bots.
