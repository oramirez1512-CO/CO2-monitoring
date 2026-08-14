# CO2 Monitoring

Microservicio .NET 8 para detectar registros raros de energía/CO₂ antes de meterlos en el reporting ESG.

Detalles de criterios: [`docs/BUSINESS_RULES.md`](docs/BUSINESS_RULES.md)  
Plan técnico: [`docs/IMPLEMENTATION_PLAN.md`](docs/IMPLEMENTATION_PLAN.md)  
Postman + curls: [`docs/CO2-Monitoring.postman_collection.json`](docs/CO2-Monitoring.postman_collection.json) · [`docs/API_CURLS.md`](docs/API_CURLS.md)

## Qué hace

- Guarda consumos por sede y mes (SQLite).
- Evalúa cada registro con reglas configurables (valores inválidos, intensidad energía–CO₂, picos vs histórico).
- Devuelve si hace falta revisión, por qué y con qué severidad.

## Stack

- .NET 8 / C#
- ASP.NET Core Web API (REST)
- EF Core + SQLite
- Un solo proyecto con carpetas

## Estructura

```
src/Co2Monitoring.Api/
  Controllers/        HTTP
  Domain/             modelo + IAnomalyRule + options
  Services/           detección + stats
  Services/Rules/     R1, R2, R3
  Data/               SQLite + seed
  Dtos/
  appsettings.json    ← umbrales editables
```

## Umbrales (config)

Todo lo afinable está en `src/Co2Monitoring.Api/appsettings.json` → sección **`AnomalyDetection`**:

| Clave | Regla | Default |
|-------|-------|---------|
| `Validation.MinEnergyKwh` | R1 | `0` |
| `Validation.MinCo2Kg` | R1 | `0` |
| `Intensity.MinKgPerKwh` | R2 | `0.05` |
| `Intensity.MaxKgPerKwh` | R2 | `0.50` |
| `Stats.LookbackMonths` | R2/R3 | `3` |
| `Stats.EnergyMomChangePercent` | R3 | `50` |
| `Stats.EnergyVsAvgPercent` | R3 | `80` |
| `Stats.Co2MomChangePercent` | R3 | `50` |
| `Stats.Co2VsAvgPercent` | R3 | `80` |
| `Stats.IntensityVsAvgPercent` | R2 | `40` |

Edita el JSON y reinicia (o el siguiente request si el host recarga el archivo).

## Cómo agregar una nueva regla

Las reglas implementan `IAnomalyRule`. El orquestador (`AnomalyDetectionService`) ya las ejecuta todas, junta reasons y toma la severidad máxima — **no hace falta tocarlo**.

1. **(Opcional)** Si necesitas umbrales nuevos, añádelos en `Domain/AnomalyDetectionOptions.cs` y en `appsettings.json` bajo `AnomalyDetection`.
2. **Crea** una clase en `Services/Rules/` que implemente `IAnomalyRule` (`Code` + `Evaluate` → `AnomalyRuleResult` o `null` si no dispara).
3. **Registra** en `Program.cs`:
   ```csharp
   builder.Services.AddSingleton<IAnomalyRule, MaxEnergyCapRule>();
   ```
4. **(Recomendado)** Añade un test en `tests/Co2Monitoring.UnitTests/`.

Solo afinar umbrales de R1–R3 no requiere código: edita `appsettings.json`.

## Requisitos

- SDK .NET 8 (`dotnet --version` → 8.x)

```bash
export PATH="$(brew --prefix dotnet@8)/bin:$PATH"   # si usas Homebrew
```

## Cómo correr

```bash
dotnet restore
dotnet build
dotnet run --project src/Co2Monitoring.Api
```

Swagger: `http://localhost:5120/swagger`  
Health: `GET /api/v1/health`  
Al arrancar se hace seed del dataset de ejemplo (si la DB está vacía).

Evaluar todo:

```bash
curl -X POST http://localhost:5120/api/v1/anomaly-reviews
```

Esperado: ids **4, 7, 8** con `requiresReview: true` / `High`.

## API (v1)

| Método | Ruta | Qué hace |
|--------|------|----------|
| `POST` | `/api/v1/consumption-records` | Alta |
| `POST` | `/api/v1/consumption-records/bulk` | Bulk |
| `GET` | `/api/v1/consumption-records` | Listar (`?site=`) |
| `GET` | `/api/v1/consumption-records/{id}` | Detalle |
| `POST` | `/api/v1/anomaly-reviews` | Evaluar todos |
| `POST` | `/api/v1/anomaly-reviews/{id}` | Evaluar uno |
| `GET` | `/api/v1/health` | Health |

## Tests

```bash
dotnet test
```

## Escenarios A / B

**A — Crecimiento real:** no apagar detección; capacidad / feedback del revisor (`BUSINESS_RULES.md`).

**B — LLM:** apoyo opcional, no juez único (template en `BUSINESS_RULES.md`).
