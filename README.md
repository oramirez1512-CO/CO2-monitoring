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
- Capas: `Api` → `Application` → `Domain` ← `Infrastructure`

## Estructura

```
src/
  Co2Monitoring.Api/
  Co2Monitoring.Application/
  Co2Monitoring.Domain/
  Co2Monitoring.Infrastructure/
tests/
  Co2Monitoring.UnitTests/
docs/
  BUSINESS_RULES.md
  IMPLEMENTATION_PLAN.md
```

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

Swagger (Development): `https://localhost:<puerto>/swagger`  
Health: `GET /api/v1/health`

Los umbrales están en `src/Co2Monitoring.Api/appsettings.json` bajo `AnomalyDetection`.

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

Ejemplo de salida de revisión:

```json
{
  "id": 4,
  "requiresReview": true,
  "reason": "Energy consumption significantly exceeds historical behavior for site",
  "severity": "High"
}
```

## Estado actual (scaffolding)

Listo:

- Solution y capas con DIP
- Modelo de dominio + contratos
- Controllers REST
- SQLite + repositorio + `EnsureCreated`
- Options de anomalías en config
- Orquestador de detección (sin reglas R1–R3 todavía)

Pendiente (siguientes fases del plan):

- Implementar R1 / R2 / R3 + `SiteStatsCalculator`
- Seed del dataset de la prueba
- Migraciones EF
- Tests de las reglas
- Notas de demo Escenario A / B en vídeo

## Tests

```bash
dotnet test
```

## Escenarios de negocio (resumen)

**A — Crecimiento real de fábrica:** no apagar la detección; añadir eventos de capacidad / feedback del revisor para bajar falsos positivos.

**B — LLM:** no como juez único. Opcional como apoyo con stats ya calculadas (template en `BUSINESS_RULES.md`).
