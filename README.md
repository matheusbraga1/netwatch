<div align="center">
  <h1>NetWatch</h1>
  <p><strong>Open-source observability platform for .NET APIs</strong></p>
  
  <p>
    <a href="#features">Features</a> •
    <a href="#quick-start">Quick Start</a> •
    <a href="#roadmap">Roadmap</a> •
    <a href="#contributing">Contributing</a>
  </p>

  <p>
    <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet" alt=".NET 8.0" />
    <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License" />
    <img src="https://img.shields.io/badge/status-in%20development-orange" alt="Status" />
  </p>
</div>

---

## ⚠️ Work in Progress

NetWatch is currently under active development. The first MVP release is expected in **[03/2026]**.

⭐ **Star this repo** to follow the progress!

---

## 🎯 What is NetWatch?

NetWatch is a lightweight, open-source observability platform designed specifically for .NET APIs. 

Unlike expensive enterprise solutions, NetWatch gives you:
- ✅ **2-line integration** - Add monitoring in seconds
- ✅ **Real-time dashboard** - See what's happening now
- ✅ **Smart alerts** - Know when things break
- ✅ **Self-hosted** - Your data stays with you
- ✅ **Free & Open Source** - MIT licensed

Perfect for:
- Indie developers and small teams
- Startups watching their budget
- Anyone who needs simple, effective API monitoring

---

## 🚀 Features (Planned)

### MVP (v0.1.0)
- [ ] .NET SDK with automatic request tracking
- [ ] Real-time metrics collection
- [ ] Dashboard with key metrics:
  - Requests per minute
  - Response time (avg, p95, p99)
  - Error rates
  - Top endpoints
- [ ] Basic alerting (email)

### Future Releases
- [ ] Distributed tracing
- [ ] Log aggregation
- [ ] Custom metrics
- [ ] Multi-project support
- [ ] Webhook integrations (Slack, Discord)
- [ ] Mobile app

---

## 📦 Quick Start

> **Note:** Not yet available. Coming soon!
```bash
# Install SDK (NuGet)
dotnet add package NetWatch.Sdk

# Add to your API (2 lines)
builder.Services.AddNetWatch(options => {
    options.ApiKey = "your-api-key";
});

app.UseNetWatch();
```

---

## 🏗️ Architecture
```
Your .NET API (with SDK) 
    ↓ (HTTP)
Collector API (receives metrics)
    ↓ (Queue)
Background Workers (process)
    ↓ (Store)
TimescaleDB (metrics) + PostgreSQL (metadata)
    ↓ (Query)
Dashboard (real-time UI)
```

---

## 🗺️ Roadmap

**Phase 1: Foundation** (Weeks 1-2)
- SDK core implementation
- Collector API setup
- Basic storage layer

**Phase 2: Processing** (Weeks 3-4)
- Background workers
- Aggregation logic
- Alert system

**Phase 3: Visualization** (Weeks 5-6)
- Dashboard UI
- Real-time updates (SignalR)
- Public demo deployment

**Phase 4: Polish** (Weeks 7-8)
- Documentation
- Examples
- CI/CD pipeline
- v0.1.0 release

---

## 🤝 Contributing

NetWatch is in early development. Contributions are welcome once v0.1.0 is released!

For now, you can:
- ⭐ Star the repo
- 👀 Watch for updates
- 💡 Open issues with ideas/suggestions

---

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

---

## 👨‍💻 Author

Built with ❤️ by [Matheus](https://github.com/matheusbraga1)

---

<div align="center">
  <sub>⭐ Star this repo if you find it useful!</sub>
</div>
