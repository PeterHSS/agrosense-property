# 🌱 AgroSense Property API

Microsserviço responsável pelo gerenciamento de **propriedades rurais e talhões** da plataforma **AgroSense** — um sistema de monitoramento agrícola inteligente baseado em sensores de campo.

---

## 📋 Sobre o Projeto

O **AgroSense Property API** é um serviço da arquitetura de microsserviços do AgroSense, responsável por:

- Cadastrar e gerenciar produtores rurais
- Criar, atualizar e remover propriedades rurais vinculadas a produtores
- Gerenciar talhões (plots) dentro de cada propriedade
- Fornecer dados de propriedades para outros serviços do ecossistema AgroSense

---

## 🏗️ Arquitetura

O serviço faz parte de um ecossistema de microsserviços implantado em **Kubernetes**, composto por:

| Serviço | Responsabilidade |
|---|---|
| `agrosense-api-gateway` | Roteamento e entrada de requisições externas |
| `agrosense-api-identity` | Autenticação e autorização |
| `agrosense-api-alert` | Gestão de alertas baseados em leituras |
| `agrosense-api-property` | **Este serviço** — gestão de propriedades rurais |
| `agrosense-api-sensor` | Ingestão de dados dos sensores |

---

## 🛠️ Tecnologias

- **.NET / C#** — Framework principal
- **PostgreSQL** — Banco de dados relacional (`postgres-property`)
- **Docker** — Containerização
- **Kubernetes** — Orquestração de containers
- **Prometheus** — Coleta de métricas
- **Grafana + Loki** — Observabilidade e centralização de logs

---

## 🚀 Como Executar

### Pré-requisitos

- [.NET SDK](https://dotnet.microsoft.com/download) 10+
- [Docker](https://www.docker.com/)
- [kubectl](https://kubernetes.io/docs/tasks/tools/) (para deploy no cluster)

### Localmente

```bash
# Clone o repositório
git clone https://github.com/PeterHSS/agrosense-property.git
cd agrosense-property

# Restaure as dependências
dotnet restore

# Execute
dotnet run --project Api/
```

### Com Docker

```bash
# Build da imagem
docker build -t agrosense-property .

# Execute o container
docker run -p 8080:80 agrosense-property
```

---

## ☸️ Deploy no Kubernetes

O serviço é implantado no namespace `agrosense` como um `ClusterIP`:

```bash
# Verifique o serviço no cluster
kubectl get services -n agrosense

# Verifique os pods em execução
kubectl get pods -n agrosense -l app=agrosense-api-property

# Logs do serviço
kubectl logs -n agrosense -l app=agrosense-api-property --follow
```

O serviço está acessível internamente no cluster via:
```
http://agrosense-api-property.agrosense.svc.cluster.local:80
```

---

## 📊 Observabilidade

### Prometheus

Métricas expostas no endpoint `/metrics`, coletadas pelo Prometheus em:
```
http://prometheus:9090
```

### Grafana + Loki

Dashboards e logs centralizados acessíveis via Grafana. Datasources configurados:

| Fonte | URL interna |
|---|---|
| Prometheus | `http://prometheus:9090` |
| Loki | `http://loki:3100` |

---

## 🔁 CI/CD

O repositório utiliza **GitHub Actions** (`.github/workflows/`) para:

- Build e testes automáticos a cada push
- Build e push da imagem Docker para o registry

---

## 📁 Estrutura do Projeto

```
agrosense-property/
├── .github/
│   └── workflows/                          # Pipelines de CI/CD
├── Api/
│   ├── Common/
│   │   └── Middlewares/                    # Middlewares globais da aplicação
│   ├── Domain/
│   │   ├── Abstractions/
│   │   │   └── UseCases/                   # Interfaces dos casos de uso
│   │   ├── Entities/                       # Entidades do domínio
│   │   └── Events/                         # Eventos de domínio
│   ├── Features/
│   │   ├── Plot/
│   │   │   ├── Create/                     # Caso de uso: criar talhão
│   │   │   ├── Delete/                     # Caso de uso: remover talhão
│   │   │   └── Update/                     # Caso de uso: atualizar talhão
│   │   ├── Producer/                       # Caso de uso: gestão de produtor
│   │   └── Property/
│   │       ├── Create/                     # Caso de uso: criar propriedade
│   │       └── GetPropertiesFromProducer/  # Caso de uso: listar propriedades do produtor
│   ├── Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── Configurations/             # Configurações do EF Core
│   │   │   ├── Contexts/                   # DbContext
│   │   │   └── Migrations/                 # Migrações do banco de dados
│   │   └── Providers/                      # Provedores de infraestrutura
│   └── Properties/                         # Configurações do projeto .NET
├── .dockerignore
├── .gitignore
├── AgroSense.Property.slnx                 # Solution file
└── README.md
```

---

## 📄 Licença

Este projeto está licenciado sob a licença **MIT**. Consulte o arquivo [LICENSE](./LICENSE) para mais detalhes.
