/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

namespace Ares.Core.Objects.Model;

public enum ModelType
{
    /// <summary>
    /// Model used for conversational AI, enabling text-based interactions.
    /// </summary>
    Chat,

    /// <summary>
    /// Model specialized in answering specific queries with concise responses.
    /// </summary>
    Question,

    /// <summary>
    /// Model capable of generating and processing images.
    /// </summary>
    Image,

    /// <summary>
    /// Model used for text-to-speech synthesis.
    /// </summary>
    TTS,

    /// <summary>
    /// Model designed for image analysis and interpretation.
    /// </summary>
    Vision,

    /// <summary>
    /// Model responsible for content moderation and filtering.
    /// </summary>
    Moderation,

    /// <summary>
    /// Model used for generating vector embeddings for semantic similarity tasks.
    /// </summary>
    Embedding,

    /// <summary>
    /// Model optimized for low-latency real-time processing.
    /// </summary>
    Realtime,

    /// <summary>
    /// Model used for audio transcription, including Whisper-based systems.
    /// </summary>
    Transcription
}