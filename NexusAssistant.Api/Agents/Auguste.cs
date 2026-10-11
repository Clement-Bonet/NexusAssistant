using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NexusAssistant.Api.Config;
using NexusAssistant.Api.Providers;
using System.Text.RegularExpressions;

namespace NexusAssistant.Api.Agents;

public abstract class Auguste
{
    protected readonly Kernel Kernel;
    protected abstract string SystemPrompt { get; }

    protected Auguste(ILLMProvider provider, IOptions<NexusConfig> config, AgentConfig agentConfig)
    {
        var modelToUse = !string.IsNullOrEmpty(agentConfig.Model)
    ? agentConfig.Model
    : config.Value.Ollama.Model;

        // Log temporaire
        Console.WriteLine($"[{GetType().Name}] Modèle utilisé : {modelToUse}");

        Kernel = provider.CreateKernelForModel(modelToUse);
    }

    protected async Task<string> RespondToDebateAsync(
    string userMessage,
    string otherAgentName,
    string otherAgentResponse,
    Func<Task<string>> selfAnalyze,
    string context = "")
    {
        var history = BuildHistory(userMessage, context);
        history.AddAssistantMessage(await selfAnalyze());
        history.AddUserMessage(
            $"{otherAgentName} propose ceci : {otherAgentResponse}\n" +
            $"Es-tu d'accord ? Nuance ou complète si nécessaire.");

        return Clean(await CompleteAsync(history));
    }

    protected async Task<string> CompleteAsync(ChatHistory history)
    {
        var chat = Kernel.GetRequiredService<IChatCompletionService>();
        var response = await chat.GetChatMessageContentAsync(history);
        return response.Content ?? string.Empty;
    }

    protected async IAsyncEnumerable<string> StreamAsync(ChatHistory history)
    {
        var chat = Kernel.GetRequiredService<IChatCompletionService>();

        await foreach (var chunk in
            chat.GetStreamingChatMessageContentsAsync(history))
        {
            if (chunk.Content == null) continue;
            yield return chunk.Content;
        }
    }

    protected ChatHistory BuildHistory(
        string userMessage, string context = "")
    {
        var history = new ChatHistory();
        history.AddSystemMessage(SystemPrompt);

        if (!string.IsNullOrEmpty(context))
            history.AddSystemMessage(
                $"Contexte des discussions précédentes :\n{context}");

        history.AddUserMessage(userMessage);
        return history;
    }

    public static string Clean(string response)
    {
        var result = response
            .Replace("<｜begin▁of▁sentence｜>", "")
            .Replace("<｜end▁of▁sentence｜>", "");

        result = Regex.Replace(result,
            @"\*{0,2}(SIMPLE|TRIVIALE?|COMPLEXE?|STANDARD)\*{0,2}\s*", "");

        result = Regex.Replace(result, @" {2,}", " ");
        result = Regex.Replace(result, @" ([.,;:!?])", "$1");
        result = Regex.Replace(result, @"` ([^`]+) `", "`$1`");

        return result;
    }
}