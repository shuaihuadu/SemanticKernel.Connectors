﻿namespace IdeaTech.SemanticKernel.Connectors.Ollama;

/// <summary>
/// Ollama chat completion service.
/// </summary>
public sealed class OllamaChatCompletionService : OllamaBaseService, IChatCompletionService
{
    private readonly OllamaClient _ollamaClient;

    private Dictionary<string, object?> AttributesInternal { get; } = [];
    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Attributes => this.AttributesInternal;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaChatCompletionService"/> class.
    /// </summary>
    /// <param name="model">The model name.</param>
    /// <param name="endpoint">The uri endpoint including the port where Ollama server is hosted</param>
    /// <param name="loggerFactory">Optional logger factory to be used for logging.</param>
    public OllamaChatCompletionService(string model, Uri endpoint, ILoggerFactory? loggerFactory = null) : base(model, endpoint, loggerFactory)
    {
        this._ollamaClient = new OllamaClient(endpoint, loggerFactory);

        this.AttributesInternal.Add(AIServiceExtensions.ModelIdKey, model);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaChatCompletionService"/> class.
    /// </summary>
    /// <param name="model">The model name.</param>
    /// <param name="endpoint">The uri string endpoint including the port where Ollama server is hosted</param>
    /// <param name="loggerFactory">Optional logger factory to be used for logging.</param>
    public OllamaChatCompletionService(string model, string endpoint, ILoggerFactory? loggerFactory = null) : this(model, new Uri(endpoint), loggerFactory) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaChatCompletionService"/> class.
    /// </summary>
    /// <param name="model">The model name.</param>
    /// <param name="httpClient">HTTP client to be used for communication with the Ollama API.</param>
    /// <param name="loggerFactory">Optional logger factory to be used for logging.</param>
    public OllamaChatCompletionService(string model, HttpClient httpClient, ILoggerFactory? loggerFactory = null) : base(model, httpClient, loggerFactory)
    {
        this._ollamaClient = new OllamaClient(httpClient, loggerFactory);

        this.AttributesInternal.Add(AIServiceExtensions.ModelIdKey, model);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string model = executionSettings?.ModelId ?? this._model;

        OllamaPromptExecutionSettings ollamaPromptExecutionSettings = OllamaPromptExecutionSettings.FromExecutionSettings(executionSettings);

        using Activity? activity = ModelDiagnostics.StartCompletionActivity(this._endpoint, model, ModelProvider, chatHistory, ollamaPromptExecutionSettings);

        StreamingResponse<ChatCompletionResponse> response;

        try
        {
            response = await this._ollamaClient.ChatCompletionStreamingAsync(CreateChatCompletionOptions(model, chatHistory, ollamaPromptExecutionSettings), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (activity is not null)
        {
            activity.SetError(ex);
            throw;
        }

        ConfiguredCancelableAsyncEnumerable<ChatCompletionResponse>.Enumerator responseEnumerator = response.ConfigureAwait(false).GetAsyncEnumerator();

        List<StreamingChatMessageContent>? streamedContents = activity is not null ? [] : null;

        try
        {
            while (true)
            {
                try
                {
                    if (!await responseEnumerator.MoveNextAsync())
                    {
                        break;
                    }
                }
                catch (Exception ex) when (activity is not null)
                {
                    activity.SetError(ex);
                    throw;
                }

                ChatCompletionResponse currentResponse = responseEnumerator.Current;

                StreamingChatMessageContent content = GetStreamingChatMessageContentFromResponse(currentResponse);

                streamedContents?.Add(content);

                yield return content;
            }
        }
        finally
        {
            activity?.EndStreaming(streamedContents);
            await responseEnumerator.DisposeAsync();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
    {
        string model = executionSettings?.ModelId ?? this._model;

        OllamaPromptExecutionSettings ollamaPromptExecutionSettings = OllamaPromptExecutionSettings.FromExecutionSettings(executionSettings);

        using Activity? activity = ModelDiagnostics.StartCompletionActivity(this._endpoint, model, ModelProvider, chatHistory, ollamaPromptExecutionSettings);

        ChatCompletionResponse response;

        try
        {
            response = await this._ollamaClient.ChatCompletionAsync(CreateChatCompletionOptions(model, chatHistory, ollamaPromptExecutionSettings), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (activity is not null)
        {
            activity.SetError(ex);
            throw;
        }

        ChatMessageContent content = GetChatMessageContentFromResponse(response);

        activity?.SetCompletionResponse([content], response.PromptEvalCount, response.EvalCount);

        return [content];
    }

    private static ChatCompletionOptions CreateChatCompletionOptions(string model, ChatHistory chatHistory, OllamaPromptExecutionSettings ollamaPromptExecutionSettings)
    {
        return new ChatCompletionOptions
        {
            Model = model,
            Messages = [.. chatHistory.Select(message => message.ToChatMessage())],
            Format = ollamaPromptExecutionSettings.Format,
            KeepAlive = ollamaPromptExecutionSettings.KeepAlive,
            Options = new ParameterOptions
            {
                NumCtx = ollamaPromptExecutionSettings.MaxTokens,
                FrequencyPenalty = ollamaPromptExecutionSettings.FrequencyPenalty,
                PresencePenalty = ollamaPromptExecutionSettings.PresencePenalty,
                Temperature = ollamaPromptExecutionSettings.Temperature,
                Seed = (int)ollamaPromptExecutionSettings.Seed,
                Stop = ollamaPromptExecutionSettings.Stop?.ToArray(),
                TopK = ollamaPromptExecutionSettings.TopK,
                TopP = ollamaPromptExecutionSettings.TopP
            }
        };
    }


    private static StreamingChatMessageContent GetStreamingChatMessageContentFromResponse(ChatCompletionResponse response) => new(
        role: response.Message?.Role is not null ? new AuthorRole(response.Message.Role.Value.Label) : null,
        content: response.Message?.Content,
        innerContent: response,
        modelId: response.Model,
        encoding: Encoding.UTF8,
        metadata: new OllamaChatGenerationMetadata(response));

    private static ChatMessageContent GetChatMessageContentFromResponse(ChatCompletionResponse response) => new(
        role: response.Message?.Role is not null ? new AuthorRole(response.Message.Role.Value.Label) : AuthorRole.Assistant,
        content: response.Message?.Content,
        modelId: response.Model,
        innerContent: response,
        encoding: Encoding.UTF8,
        metadata: new OllamaChatGenerationMetadata(response));

}