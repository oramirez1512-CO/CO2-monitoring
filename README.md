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

## Instalar y correr

### 1. Instalar .NET 8

Hace falta el SDK 8 (`dotnet --version` debe empezar por `8.`).

**macOS (Homebrew):**

```bash
brew install dotnet@8
export PATH="$(brew --prefix dotnet@8)/bin:$PATH"
```

Añade el `export` a `~/.zshrc` si quieres que quede permanente.

**Otras plataformas:** [dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)

SQLite va embebido con EF Core: no hay que instalar motor de base de datos.

### 2. Restaurar, build y tests

Desde la raíz del repo:

```bash
dotnet restore
dotnet build
dotnet test
```

### 3. Arrancar el API

```bash
dotnet run --project src/Co2Monitoring.Api
```

Por defecto (perfil `http`): `http://localhost:5120`

| Recurso | URL |
|---------|-----|
| Swagger | http://localhost:5120/swagger |
| Health | http://localhost:5120/api/v1/health |

Al arrancar se crea `co2monitoring.db` y, si está vacía, se carga el dataset de ejemplo.

### 4. Probar detección

```bash
curl -X POST http://localhost:5120/api/v1/anomaly-reviews
```

Esperado: ids **4, 7, 8** con `requiresReview: true` / `High`.

Más curls y colección Postman: [`docs/API_CURLS.md`](docs/API_CURLS.md).

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

## Escenario A — “Madrid subió el consumo, pero es verdad”

El algoritmo marca esto como raro:

```json
{ "site": "Madrid", "month": "2026-05", "energyKwh": 25000, "co2Kg": 5900 }
```

Luego el cliente dice: en mayo ampliaron la fábrica y metieron una línea nueva. El dato es correcto; el sistema no “se equivocó” del todo: vio un salto vs histórico y pidió revisión, que es exactamente lo que tiene que hacer.

Lo que no haría es apagar la detección o subir umbrales a lo loco para que este caso pase. Si lo hago, el próximo pico de verdad (un 79k como el id 4) se me cuela al reporting.

Lo que sí cambiaría: darle **contexto de negocio** al sistema.

- Un evento de sede (“en mayo crecimos X%”) para resetear la media o aflojar umbrales a partir de esa fecha.
- Que el revisor pueda marcar “justificado”. Ese mes entra al histórico bueno y deja de parecer un outlier eterno.
- Umbrales distintos si la sede está en expansión vs estable.
- En High, solo encolar revisión. Nadie tira el dato del ESG sin una persona de por medio.

En corto: el falso positivo se arregla con feedback y capacidad, no desconectando las reglas.

## Escenario B — “¿Y si le preguntamos a un LLM cada registro?”

Mala idea como juez único.

Cada alta iría a un modelo: caro, lento, no siempre dice lo mismo dos veces, y en ESG te van a preguntar *por qué* se aceptó o se rechazó. “Porque el chat lo dijo” no aguanta una auditoría. Además el modelo no conoce el histórico de Madrid a menos que se lo pases tú, y puede inventar umbrales.

Sí tiene sentido como **apoyo**, detrás de las reglas:

1. El sistema calcula stats y dispara R1–R3 (determinista, barato, explicable).
2. Si hace falta, el LLM recibe esos números ya hechos y ayuda a redactar el `reason` o a contrastar.

No mandamos el registro crudo a que “adivine” si es anómalo. El prompt template (campos `{{site}}`, `{{avgEnergyKwh}}`, etc.) está en [`docs/BUSINESS_RULES.md`](docs/BUSINESS_RULES.md). En este MVP no hay llamada real a ningún proveedor.
