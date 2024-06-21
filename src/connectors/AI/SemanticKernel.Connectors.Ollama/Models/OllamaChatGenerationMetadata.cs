﻿// Copyright (c) IdeaTech. All rights reserved.

namespace IdeaTech.SemanticKernel.Connectors.Ollama;

/// <summary>
/// Represents the metadata of a Ollama chat completion.
/// </summary>
public sealed class OllamaChatGenerationMetadata : ReadOnlyDictionary<string, object?>
{
    internal OllamaChatGenerationMetadata() : base(new Dictionary<string, object?>())
    {
    }

    /// <inheritdoc />
    private OllamaChatGenerationMetadata(IDictionary<string, object?> dictionary) : base(dictionary)
    {
    }

    internal OllamaChatGenerationMetadata(ChatCompletionResponse response) : this()
    {
        this.PromptEvalCount = response.PromptEvalCount;
        this.PromptEvalDuration = response.PromptEvalDuration;
        this.EvalCount = response.EvalCount;
        this.DoneReason = response.DoneReason;
        this.TotalDuration = response.TotalDuration;
        this.LoadDuration = response.LoadDuration;
        this.EvalDuration = response.EvalDuration;
        this.Done = response.Done;
        this.CreatedAt = response.CreatedAt;
    }

    /// <summary>
    /// Number of tokens in the prompt
    /// </summary>
    public int? PromptEvalCount
    {
        get => this.GetValueFromDictionary(nameof(this.PromptEvalCount)) as int?;
        internal init => this.SetValueInDictionary(value, nameof(this.PromptEvalCount));
    }

    /// <summary>
    /// Number of tokens in the response
    /// </summary>
    public int? EvalCount
    {
        get => this.GetValueFromDictionary(nameof(this.EvalCount)) as int?;
        internal init => this.SetValueInDictionary(value, nameof(this.EvalCount));
    }

    /// <summary>
    /// The reason the model stopped generating text.
    /// </summary>
    public string? DoneReason
    {
        get => this.GetValueFromDictionary(nameof(this.DoneReason)) as string;
        internal init => this.SetValueInDictionary(value, nameof(this.DoneReason));
    }

    /// <summary>
    /// Time spent generating the response in nanoseconds.
    /// </summary>
    public long? TotalDuration
    {
        get => this.GetValueFromDictionary(nameof(this.TotalDuration)) as long?;
        internal init => this.SetValueInDictionary(value, nameof(this.TotalDuration));
    }

    /// <summary>
    /// Time spent in nanoseconds loading the model in nanoseconds.
    /// </summary>
    public long? LoadDuration
    {
        get => this.GetValueFromDictionary(nameof(this.LoadDuration)) as long?;
        internal init => this.SetValueInDictionary(value, nameof(this.LoadDuration));
    }

    /// <summary>
    /// Time spent in nanoseconds evaluating the prompt in nanoseconds.
    /// </summary>
    public long? PromptEvalDuration
    {
        get => this.GetValueFromDictionary(nameof(this.PromptEvalDuration)) as long?;
        internal init => this.SetValueInDictionary(value, nameof(this.PromptEvalDuration));
    }

    /// <summary>
    /// Time in nanoseconds spent generating the response in nanoseconds.
    /// </summary>
    public long? EvalDuration
    {
        get => this.GetValueFromDictionary(nameof(this.EvalDuration)) as long?;
        internal init => this.SetValueInDictionary(value, nameof(this.EvalDuration));
    }

    /// <summary>
    /// The done status.
    /// </summary>
    public bool? Done
    {
        get => this.GetValueFromDictionary(nameof(this.Done)) as bool?;
        internal init => this.SetValueInDictionary(value, nameof(this.Done));
    }

    /// <summary>
    /// The timestamp associated with generation activity for this completions response.
    /// </summary>
    public DateTimeOffset? CreatedAt
    {
        get => this.GetValueFromDictionary(nameof(this.CreatedAt)) as DateTimeOffset?;
        internal init => this.SetValueInDictionary(value, nameof(this.CreatedAt));
    }

    /// <summary>
    /// Converts a dictionary to a <see cref="OllamaChatGenerationMetadata"/> object.
    /// </summary>
    public static OllamaChatGenerationMetadata FromDictionary(IReadOnlyDictionary<string, object?> dictionary) => dictionary switch
    {
        null => throw new ArgumentNullException(nameof(dictionary)),
        OllamaChatGenerationMetadata metadata => metadata,
        IDictionary<string, object?> metadata => new OllamaChatGenerationMetadata(metadata),
        _ => new OllamaChatGenerationMetadata(dictionary.ToDictionary(pair => pair.Key, pair => pair.Value))
    };

    private void SetValueInDictionary(object? value, string propertyName) => this.Dictionary[propertyName] = value;

    private object? GetValueFromDictionary(string propertyName) => this.Dictionary.TryGetValue(propertyName, out object? value) ? value : null;
}
