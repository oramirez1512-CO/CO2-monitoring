# CO2 Monitoring

Microservicio .NET 8 para detectar registros raros de energía/CO₂ antes de meterlos en el reporting ESG.

Detalles de criterios: [`docs/BUSINESS_RULES.md`](docs/BUSINESS_RULES.md)  
Plan técnico: [`docs/IMPLEMENTATION_PLAN.md`](docs/IMPLEMENTATION_PLAN.md)

## Qué hace

- Guarda consumos por sede y mes (SQLite).
- Evalúa cada registro con reglas configurables (valores inválidos, intensidad energía–CO₂, picos vs histórico).
- Devuelve si hace falta revisión, por qué y con qué severidad.

## Stack

- .NET 8 / C#
- ASP.NET Core Web API (REST)
- EF Core + SQLite
- **Un solo proyecto** con carpetas (suficiente para una prueba técnica)

## Estructura (mapa rápido)

```
src/Co2Monitoring.Api/
  Controllers/     → HTTP (endpoints REST)
  Domain/          → modelo + umbrales + IAnomalyRule
  Services/        → detección + stats; Rules/ = R1–R3
  Data/            → EF Core + SQLite
  Dtos/            → request/response JSON
  Program.cs       → arranque y DI
tests/
docs/
```

| Carpeta / archivo | Para qué sirve |
|-------------------|----------------|
| `Controllers/` | Recibe HTTP, valida input básico, llama a DB o al servicio |
| `Domain/` | Entidades y contratos de reglas (sin EF ni HTTP) |
| `Services/` | Orquesta R1–R3 y calcula stats del histórico |
| `Services/Rules/` | Aquí van las reglas concretas (pendiente) |
| `Data/` | Persistencia SQLite |
| `Dtos/` | Formas del JSON de entrada/salida |
| `appsettings.json` | Umbrales (`AnomalyDetection`) y connection string |

## Requisitos

- SDK .NET 8 (`dotnet --version` → 8.x)

Si instalaste `dotnet@8` con Homebrew:

```bash
export PATH="$(brew --prefix dotnet@8)/bin:$PATH"
```

## Cómo correr

```bash
dotnet restore
dotnet build
dotnet run --project src/Co2Monitoring.Api
```

Swagger (Development): `http://localhost:5120/swagger`  
Health: `GET /api/v1/health`

## API (v1)

| Método | Ruta | Qué hace |
|--------|------|----------|
| `POST` | `/api/v1/consumption-records` | Alta de un registro |
| `POST` | `/api/v1/consumption-records/bulk` | Alta masiva |
| `GET` | `/api/v1/consumption-records` | Listar (`?site=Madrid` opcional) |
| `GET` | `/api/v1/consumption-records/{id}` | Detalle |
| `POST` | `/api/v1/anomaly-reviews` | Evaluar todos |
| `POST` | `/api/v1/anomaly-reviews/{id}` | Evaluar uno |
| `GET` | `/api/v1/health` | Health check |

## Estado actual

Listo: API + SQLite + orquestador de detección + options.

Pendiente: R1/R2/R3, seed del dataset, tests de reglas, notas demo Escenario A/B.

## Tests

```bash
dotnet test
```

## Escenarios de negocio (resumen)

**A — Crecimiento real de fábrica:** no apagar la detección; añadir eventos de capacidad / feedback del revisor.

**B — LLM:** no como juez único. Opcional como apoyo (template en `BUSINESS_RULES.md`).
