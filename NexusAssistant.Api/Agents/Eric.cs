using Microsoft.Extensions.Options;
using NexusAssistant.Api.Config;
using NexusAssistant.Api.Models;
using NexusAssistant.Api.Providers;

namespace NexusAssistant.Api.Agents;

public class Eric : Auguste
{
    protected override string SystemPrompt => """
You must assess whether a development question is TRIVIAL, STANDARD, or COMPLEX.
Answer ONLY TRIVIAL, STANDARD, or COMPLEX.
One word only. No other text. No punctuation.
Answer in the language in which the question is asked.
"What is X?" → always TRIVIAL
"Explain X" → always TRIVIAL

TRIVIAL if: definition of a concept, explanation of a keyword,
basic syntax question, "what is X?",
"how does X work?", "explain X to me".
Examples: "what is a delegate?",
"how do I declare a list?",
"explain lambda expressions",
"what is a Unity coroutine?"

STANDARD if: implementation of a specific feature,
question about a specific pattern,
only one technical area involved.
Examples: "How to structure a Unity inventory?",
"How to create a basic REST API?",
"Implement a backup system"

COMPLEX if: complete system architecture,
choice between several technologies,
performance optimization,
several technical domains involved.
Examples: "Complete application architecture?",
"How to optimize game performance?",
"Manage members of two associations"
""";

    public Eric(ILLMProvider provider, IOptions<NexusConfig> config)
    : base(provider, config, config.Value.Agents.Eric) { }

    public async Task<ComplexityLevel> EvaluateComplexityAsync(string userMessage, string context = "")
    {
        var history = BuildHistory(userMessage, context);
        var response = await CompleteAsync(history);
        return response.Trim().ToUpper() switch
        {
            "TRIVIAL" or "TRIVIALE" => ComplexityLevel.Fast,
            "COMPLEX" or "COMPLEXE" => ComplexityLevel.Debate,
            _ => ComplexityLevel.Normal
        };
    }

}
