# NexusAssistant

> A .NET 10 multi-agent Web API with SSE streaming, session memory and multi-provider LLM support (OpenAI, Anthropic, Ollama).

---

## 📋 Table of Contents

- [About](#-about)
- [Architecture](#-architecture)
- [Technologies](#-technologies)
- [Prerequisites](#-prerequisites)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Running](#-running)
- [Endpoints](#-endpoints)
- [Project Structure](#-project-structure)
- [Contributing](#-contributing)
- [License](#-license)

---

## 🧠 About

**NexusAssistant** is a REST API built with ASP.NET Core (.NET 10) that exposes a multi-agent chat system powered by **Microsoft Semantic Kernel**. Responses are streamed in real time via **Server-Sent Events (SSE)**, and each conversation is isolated in a session with persistent memory.

---

## 🏗 Architecture

The project is built around several layers:

- **LLM Providers** — abstraction layer allowing to switch between OpenAI, Anthropic or Ollama via configuration
- **Agents** — five specialized agents (`Clement`, `Eric`, `Fabienne`, `Mauricette`, `Oscar`) registered as singletons
- **Pipeline** (`AgentPipeline`) — orchestrates agent execution and produces a streaming token flow
- **Memory** (`InMemorySessionStore`) — stores sessions and conversation history in memory

```
Client  ──POST /api/chat/ask──▶  ChatController
                                       │
                                  AgentPipeline
                                  ┌────┴────┐
                               Agent1 ... Agent5
                                  └────┬────┘
                                  ILLMProvider
                              (OpenAI / Anthropic / Ollama)
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
  - **Ollama** (default) — [ollama.ai](https://ollama.ai) running locally
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

Each agent can use a different model. By default, `Clement` and `Eric` run on `deepseek-coder-v2` (code-oriented), while `Fabienne`, `Mauricette` and `Oscar` use `mixtral:8x7b` (general purpose).

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
| `POST` | `/api/chat/ask` | Send a message and receive the response as an SSE stream |
| `POST` | `/api/chat/session/new` | Create a new conversation session |
| `GET` | `/api/chat/sessions` | List all existing sessions |
| `DELETE` | `/api/chat/session/{sessionId}` | Delete a session |

### Example — Start a session and send a message

```bash
# 1. Create a session
curl -X POST http://localhost:5100/api/chat/session/new
# Response: { "sessionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" }

# 2. Send a message (SSE streaming)
curl -X POST http://localhost:5100/api/chat/ask \
  -H "Content-Type: application/json" \
  -d '{"sessionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx", "message": "Hello!"}'
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
│   ├── Agents/                      # Specialized agents (Clement, Eric, Fabienne, Mauricette, Oscar)
│   ├── Config/                      # NexusConfig (appsettings binding)
│   ├── Controllers/
│   │   └── ChatController.cs        # REST endpoints
│   ├── Memory/
│   │   └── InMemorySessionStore.cs  # In-memory session management
│   ├── Models/                      # AskRequest, Session, etc.
│   ├── Pipeline/
│   │   └── AgentPipeline.cs         # Streaming agent orchestration
│   ├── Providers/                   # ILLMProvider, OpenAIProvider, AnthropicProvider, OllamaProvider
│   ├── Program.cs                   # Entry point and dependency injection
│   └── appsettings.json
└── NexusAssistant.slnx
```

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

<p align="center">Made with ❤️ by <a href="https://github.com/Csluikidikilest">Csluikidikilest</a></p>
