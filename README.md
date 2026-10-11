# NexusAssistant

> A .NET 10 multi-agent Web API that answers developer questions with a small team of specialised LLM agents — streamed in real time, with session memory and multi-provider support (OpenAI, Anthropic, Ollama).

---

## 📋 Table of Contents

- [About](#-about)
- [Architecture](#-architecture)
- [The agents](#-the-agents)
- [How a question is processed](#-how-a-question-is-processed)
- [Example output](#-example-output)
- [Technologies](#-technologies)
- [Prerequisites](#-prerequisites)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Running](#-running)
- [Endpoints](#-endpoints)
- [Project Structure](#-project-structure)
- [Status & Roadmap](#-status--roadmap)
- [Contributing](#-contributing)
- [License](#-license)

---

## 🧠 About

Instead of sending a question to a single prompt, **NexusAssistant** routes it through a small team of agents, each with its own role: one triages the difficulty, one analyses the architecture, one proposes the implementation, one remembers past discussions, and one moderates and writes the final answer.

It is aimed at **software development questions** (C# / .NET, Unity, Android, REST APIs, SQL, architecture) and is built with ASP.NET Core (.NET 10) and **Microsoft Semantic Kernel**.

- **Right-sized answers**: a simple question gets a direct answer; a complex one triggers a short debate between agents.
- **Real-time streaming**: every agent's output is streamed token by token over a `text/event-stream` response.
- **Session memory**: each conversation is stored in a session, and relevant earlier exchanges are recalled as context.
- **Provider-agnostic**: switch between Ollama (local), OpenAI and Anthropic through configuration; each agent can use a different model.

---

## 🏗 Architecture

The project is built around four layers:

- **LLM Providers** — abstraction layer (`ILLMProvider`) allowing to switch between OpenAI, Anthropic or Ollama via configuration
- **Agents** — five specialised agents (`Clement`, `Eric`, `Fabienne`, `Mauricette`, `Oscar`) registered as singletons, all derived from the abstract base class `Auguste` (Semantic Kernel setup, history building, response cleaning)
- **Pipeline** (`AgentPipeline`) — orchestrates the agents and produces a single streaming token flow
- **Memory** (`ISessionStore`) — stores sessions and conversation history (in memory by default)

```
Client ──POST /api/chat/ask──▶ ChatController
                                     │
                                AgentPipeline
        ┌────────────────────────────┼────────────────────────────┐
   Mauricette (recall)        Eric (triage)                Mauricette (learn,
        │                           │                       in background)
        └──────────▶ Fast / Normal / Debate ◀───────────────────┘
                      Oscar ◀── Fabienne ∥ Clément
                                     │
                               ILLMProvider
                        (OpenAI / Anthropic / Ollama)
```

---

## 👥 The agents

| Agent | Role | Default model |
|---|---|---|
| **Mauricette** | Memory keeper: recalls relevant context before each question, memorises the answer afterwards, titles sessions and adds a summary every 10 messages | `mixtral:8x7b` |
| **Eric** | Triage: classifies the question as trivial, standard or complex | `deepseek-coder-v2` |
| **Fabienne** | Architecture and functional analysis: patterns, design risks, consistency. Never writes code | `mixtral:8x7b` |
| **Clément** | Implementation: concrete, commented code (C# / .NET, Unity, Kotlin / Java Android, REST, SQL) | `deepseek-coder-v2` |
| **Oscar** | Facilitator: answers trivial questions directly, otherwise writes a short, actionable synthesis | `mixtral:8x7b` |

Agents answer in the language of the question, and their system prompts are plain C# string constants, so roles are easy to adjust.

---

## 🔀 How a question is processed

1. **Mauricette** looks for context: the last messages of the current session plus a few exchanges from similar sessions.
2. **Eric** evaluates the difficulty, which selects one of three modes:

| Mode | Used for | Who answers |
|---|---|---|
| **Fast** | Trivial questions ("what is a delegate?") | Oscar alone |
| **Normal** | A specific feature or pattern | Fabienne and Clément **in parallel**, then Oscar's synthesis |
| **Debate** | Architecture, technology choices, performance | Same as Normal, plus a round where Fabienne and Clément **react to each other's answer**, then Oscar's synthesis |

3. **Oscar**'s final answer is kept short and actionable (it must not repeat what the other agents said).
4. **Mauricette** memorises the exchange in the background, so the response is not delayed.

---

## 📡 Example output

The response is a stream of labelled chunks, one block per agent, ending with a `[DONE]` marker (abridged; timestamps are illustrative, and status lines are currently in French):

```
[Mauricette — 14:02:11.123] Recherche dans la mémoire...
[Mauricette — 14:02:11.130] Aucun contexte antérieur.
[Eric — 14:02:12.480] Mode : Normal

[Fabienne — 14:02:12.482] Analyse :
...architecture analysis, streamed token by token...

[Clément — 14:02:15.907] Implémentation :
...code proposal, streamed token by token...

[Oscar — 14:02:19.331] Synthèse :
...short actionable conclusion...

[DONE — 14:02:24.870]
```

---

## 🛠 Technologies

| Technology | Version |
|---|---|
| .NET / ASP.NET Core | 10.0 |
| Microsoft Semantic Kernel | 1.74.0 |
| Semantic Kernel – Ollama Connector | 1.74.0-alpha |
| Swashbuckle (Swagger) | 10.1.6 |

---

## ✅ Prerequisites

- [.NET SDK 10.0+](https://dotnet.microsoft.com/download)
- An accessible LLM provider:
  - **Ollama** (default) — [ollama.ai](https://ollama.ai) running locally, with the models used in `appsettings.json` pulled (`ollama pull deepseek-coder-v2` and `ollama pull mixtral:8x7b`)
  - **OpenAI** — OpenAI API key
  - **Anthropic** — Anthropic API key
- Recommended IDE: [Visual Studio 2022+](https://visualstudio.microsoft.com/) or [JetBrains Rider](https://www.jetbrains.com/rider/)

---

## 📦 Installation

```bash
git clone https://github.com/Clement-Bonet/NexusAssistant.git
cd NexusAssistant
dotnet restore
```

---

## ⚙️ Configuration

Configuration is done in `NexusAssistant.Api/appsettings.json`. Here is the default structure:

```json
{
  "Nexus": {
    "DefaultProvider": "Ollama",
    "Agents": {
      "Clement":   { "Model": "deepseek-coder-v2:latest" },
      "Eric":      { "Model": "deepseek-coder-v2:latest" },
      "Fabienne":  { "Model": "mixtral:8x7b" },
      "Mauricette":{ "Model": "mixtral:8x7b" },
      "Oscar":     { "Model": "mixtral:8x7b" }
    },
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "Model": "deepseek-coder-v2:latest",
      "TimeoutSeconds": 60000
    },
    "OpenAI": {
      "ApiKey": "",
      "Model": "gpt-4o"
    },
    "Anthropic": {
      "ApiKey": "",
      "Model": "claude-sonnet-4-20250514"
    }
  }
}
```

`DefaultProvider` accepts the values: `"OpenAI"`, `"Anthropic"` or `"Ollama"` (default).

Each agent can use a different model. By default, `Clement` and `Eric` run on `deepseek-coder-v2` (code-oriented), while `Fabienne`, `Mauricette` and `Oscar` use `mixtral:8x7b` (general purpose). An agent with no model configured falls back to the Ollama model.

> ⚠️ Never commit your API keys (`OpenAI.ApiKey`, `Anthropic.ApiKey`) to the repository. Use environment variables or `appsettings.Development.json` (excluded by `.gitignore`).

---

## 🚀 Running

```bash
dotnet run --project NexusAssistant.Api
```

The API listens on: **`http://localhost:5100`**

Swagger UI is available at: **`http://localhost:5100/swagger`**

---

## 🔌 Endpoints

### Chat

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/chat/ask` | Send a message and receive the response as a stream |
| `POST` | `/api/chat/session/new` | Create a new conversation session |
| `GET` | `/api/chat/sessions` | List all existing sessions (id, title, dates, tags, preview) |
| `DELETE` | `/api/chat/session/{sessionId}` | Delete a session |

### Example — Start a session and send a message

```bash
# 1. Create a session
curl -X POST http://localhost:5100/api/chat/session/new
# Response: { "sessionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" }

# 2. Send a message (streamed response; -N disables curl's buffering)
curl -N -X POST http://localhost:5100/api/chat/ask \
  -H "Content-Type: application/json" \
  -d '{"sessionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx", "message": "How should I structure a Unity inventory system?"}'
```

### `AskRequest` body

```json
{
  "sessionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "message": "Your message here"
}
```

---

## 📁 Project Structure

```
NexusAssistant/
├── NexusAssistant.Api/
│   ├── Agents/                      # Auguste (base class) + Clement, Eric, Fabienne, Mauricette, Oscar
│   ├── Config/                      # NexusConfig (appsettings binding)
│   ├── Controllers/
│   │   └── ChatController.cs        # REST endpoints
│   ├── Memory/
│   │   ├── ISessionStore.cs         # Session storage contract
│   │   ├── InMemorySessionStore.cs  # In-memory implementation (default)
│   │   └── PersistentSessionStore.cs# Placeholder for the persistent store (SQLite + Qdrant)
│   ├── Models/                      # AskRequest, NexusSession, ComplexityLevel, etc.
│   ├── Pipeline/
│   │   └── AgentPipeline.cs         # Streaming agent orchestration (Fast / Normal / Debate)
│   ├── Providers/                   # ILLMProvider, OpenAIProvider, AnthropicProvider, OllamaProvider
│   ├── Program.cs                   # Entry point and dependency injection
│   └── appsettings.json
├── LICENSE
└── NexusAssistant.slnx
```

---

## 🗺 Status & Roadmap

NexusAssistant is a personal project in active development.

**Working today**
- Three processing modes (Fast / Normal / Debate) with parallel agent execution
- Streaming responses, multi-provider support, per-agent model selection
- In-memory sessions with context recall and automatic titles / summaries

**Planned**
- [ ] Persistent memory: implement `PersistentSessionStore` (SQLite for sessions, Qdrant for semantic search) and select the store from configuration
- [ ] Unit tests for the pipeline, the complexity triage and the session store
- [ ] Standard SSE framing (`data:` events) and a content type set only on the streaming endpoint
- [ ] Authentication and restricted CORS for non-local use
- [ ] Continuous integration (build and tests on every push)

---

## 🤝 Contributing

Contributions are welcome!

1. Fork the project
2. Create your branch: `git checkout -b feature/my-feature`
3. Commit your changes: `git commit -m 'feat: describe your feature'`
4. Push: `git push origin feature/my-feature`
5. Open a Pull Request

---

## 📄 License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for details.

---

<p align="center">Made with ❤️ by <a href="https://github.com/Clement-Bonet">Clément Bonet</a></p>
