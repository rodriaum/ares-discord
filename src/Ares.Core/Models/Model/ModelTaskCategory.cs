namespace Ares.Ares.Core.Models.Model;

public enum ModelTaskCategory
{
    /// <summary>
    /// Models that excel at complex, multi-step tasks.
    /// </summary>
    Reasoning,

    /// <summary>
    /// Our versatile, high-intelligence flagship models.
    /// </summary>
    Flagship,

    /// <summary>
    /// Smaller, faster models that cost less to run.
    /// </summary>
    CostOptimized,

    /// <summary>
    /// Models capable of realtime text and audio inputs and outputs.
    /// </summary>
    Realtime,

    /// <summary>
    /// Supported older versions of our general purpose and chat models.
    /// </summary>
    Older,

    /// <summary>
    /// Models that can generate and edit images, given a natural language prompt.
    /// </summary>
    Image,

    /// <summary>
    /// Models that can convert text into natural sounding spoken audio.
    /// </summary>
    TTS,

    /// <summary>
    /// Model that can transcribe and translate audio into text.
    /// </summary>
    Transcription,

    /// <summary>
    /// A set of models that can convert text into vector representations.
    /// </summary>
    Embedding,

    /// <summary>
    /// Fine-tuned models that detect whether input may be sensitive or unsafe.
    /// </summary>
    Moderation,

    /// <summary>
    /// Models to support specific built-in tools.
    /// </summary>
    ToolSpecific,

    /// <summary>
    /// They interpret images and videos and can answer questions about them.
    /// </summary>
    Vision,

    /// <summary>
    /// Older models that aren't trained with instruction following.
    /// </summary>
    Base,

    /// <summary>
    /// Another category for models that don't fit in the above categories.
    /// </summary>
    Other
}