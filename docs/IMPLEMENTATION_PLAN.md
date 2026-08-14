# Plan de construcción — CO2 Monitoring (Anomaly Review)

Plan de implementación del microservicio de detección de registros sospechosos de energía/CO₂, alineado con [`BUSINESS_RULES.md`](./BUSINESS_RULES.md).

**Stack:** .NET 8 / C#, ASP.NET Core Web API, EF Core + SQLite, arquitectura en capas, SOLID pragmático.

---

## 1. Objetivo del MVP

Un **único microservicio** REST que:

1. Persista registros de consumo energético / emisiones por sede y mes (SQLite).
2. Analice un registro (o el dataset completo) aplicando R1–R3.
3. Devuelva resultados con `requiresReview`, `reason` y `severity`.
4. Exponga umbrales vía configuración (`appsettings.json`).
5. Incluya seed del dataset de la prueba.

Fuera del MVP de código (solo documentado / README): integración real con LLM; UI; autenticación.

---

## 2. Principios de diseño (SOLID sin exagerar)

| Principio | Aplicación concreta |
|-----------|---------------------|
| **S** | Una clase = una responsabilidad: reglas R1/R2/R3 separadas; repositorio solo persiste; controller solo HTTP. |
| **O** | Nuevas reglas = nueva implementación de `IAnomalyRule` sin tocar el orquestador. |
| **L** | Las reglas son intercambiables bajo el mismo contrato. |
| **I** | Interfaces pequeñas: `IConsumptionRecordRepository`, `IAnomalyDetectionService`, `IAnomalyRule`. |
| **D** | Dominio/aplicación dependen de abstracciones; EF/SQLite vive en Infrastructure e implementa interfaces. |

No se fuerza CQRS, mediadores ni event bus: el alcance de la prueba no lo justifica.

---

## 3. Arquitectura de capas

```
┌─────────────────────────────────────────┐
│  Api  (ASP.NET Core — Controllers)      │  ← HTTP, DTOs de entrada/salida, validación básica
├─────────────────────────────────────────┤
│  Application                            │  ← Casos de uso, orquestación, contratos
├─────────────────────────────────────────┤
│  Domain                                 │  ← Entidades, enums, reglas (IAnomalyRule)
├─────────────────────────────────────────┤
│  Infrastructure                         │  ← EF Core, SQLite, seed, Options binding
└─────────────────────────────────────────┘
```

**Dependencias (inversión):**

```
Api → Application → Domain
Api → Infrastructure  (solo Composition Root / DI)
Infrastructure → Application (implementa interfaces)
Infrastructure → Domain
```

`Domain` **no** referencia EF ni ASP.NET.

### Estructura de solution

```
CO2-monitoring/
├── docs/
│   ├── BUSINESS_RULES.md
│   └── IMPLEMENTATION_PLAN.md   ← este archivo
├── src/
│   ├── Co2Monitoring.Api/
│   ├── Co2Monitoring.Application/
│   ├── Co2Monitoring.Domain/
│   └── Co2Monitoring.Infrastructure/
├── tests/
│   └── Co2Monitoring.UnitTests/
└── Co2Monitoring.sln
```

---

## 4. Modelo de dominio

### Entidad `ConsumptionRecord`

| Campo | Tipo | Notas |
|-------|------|--------|
| `Id` | `int` | PK, identity |
| `Site` | `string` | Sede (Madrid, Barcelona, …) |
| `Month` | `string` | `YYYY-MM` (o `DateOnly` primer día de mes) |
| `EnergyKwh` | `decimal` | Consumo energético |
| `Co2Kg` | `decimal` | Emisiones |

Índice único sugerido: `(Site, Month)` para evitar duplicados al registrar.

### Value / resultados (no necesariamente tabla)

| Tipo | Campos |
|------|--------|
| `AnomalyAssessment` | `RecordId`, `RequiresReview`, `Reason`, `Severity` |
| `Severity` | `None`, `Low`, `Medium`, `High` |
| `SiteStats` | medias, mes previo, intensidad, % MoM (calculado en Application/Domain) |

### Configuración (`AnomalyDetectionOptions`)

Mapeo 1:1 con parámetros de `BUSINESS_RULES.md` §4 (`Validation.*`, `Intensity.*`, `Stats.*`).

---

## 5. Contratos (Dependency Inversion)

```csharp
// Application
public interface IConsumptionRecordRepository
{
    Task<ConsumptionRecord?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<ConsumptionRecord>> GetBySiteAsync(string site, CancellationToken ct);
    Task<IReadOnlyList<ConsumptionRecord>> GetAllAsync(CancellationToken ct);
    Task<ConsumptionRecord> AddAsync(ConsumptionRecord record, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<ConsumptionRecord> records, CancellationToken ct);
}

public interface IAnomalyDetectionService
{
    Task<AnomalyAssessment> AssessAsync(int recordId, CancellationToken ct);
    Task<IReadOnlyList<AnomalyAssessment>> AssessAllAsync(CancellationToken ct);
    Task<AnomalyAssessment> AssessRecordAsync(ConsumptionRecord record, IReadOnlyList<ConsumptionRecord> siteHistory, CancellationToken ct);
}

// Domain
public interface IAnomalyRule
{
    string Code { get; } // "R1", "R2", "R3"
    AnomalyRuleResult? Evaluate(ConsumptionRecord record, SiteStats stats, AnomalyDetectionOptions options);
}
```

Implementaciones en Infrastructure: `SqliteConsumptionRecordRepository`, `AppDbContext`.  
Orquestador en Application: `AnomalyDetectionService` (inyecta `IEnumerable<IAnomalyRule>` + repo).

---

## 6. API REST (microservicio)

Base: `/api/v1`

| Método | Ruta | Acción |
|--------|------|--------|
| `POST` | `/api/v1/consumption-records` | Registrar un registro |
| `POST` | `/api/v1/consumption-records/bulk` | Cargar varios (seed / import) |
| `GET` | `/api/v1/consumption-records` | Listar (query opcional `?site=Madrid`) |
| `GET` | `/api/v1/consumption-records/{id}` | Obtener por id |
| `POST` | `/api/v1/anomaly-reviews` | Evaluar todos los registros persistidos |
| `POST` | `/api/v1/anomaly-reviews/{id}` | Evaluar un registro por id |
| `GET` | `/api/v1/health` | Health check |

### Convenciones REST

- Nombres de recursos en **plural** y kebab-case o plural nouns claros.
- `POST` crea / dispara acción de análisis (recurso `anomaly-reviews` = resultado de revisión).
- Códigos: `201` create, `200` OK, `400` validación, `404` no encontrado.
- Body de salida de revisión:

```json
{
  "id": 4,
  "requiresReview": true,
  "reason": "Energy consumption significantly exceeds historical behavior for site",
  "severity": "High"
}
```

---

## 7. Persistencia — SQLite

| Decisión | Detalle |
|----------|---------|
| Proveedor | `Microsoft.EntityFrameworkCore.Sqlite` |
| Archivo | `co2monitoring.db` (ruta configurable) |
| Migraciones | EF Core migrations en Infrastructure |
| Seed | Dataset de la prueba al arrancar (Development) o endpoint bulk |
| Por qué SQLite | Cero infra externa, portable para demo/vídeo, suficiente para el MVP |

---

## 8. Flujo de detección

```
HTTP → Controller
     → AnomalyDetectionService
          → Repository.GetById / GetBySite
          → SiteStatsCalculator (histórico misma sede, LookbackMonths)
          → foreach IAnomalyRule (R1 → R2 → R3)
          → Merge reasons + max Severity
     → AnomalyAssessmentDto
```

Reglas (Domain o Application/Rules):

1. **InvalidValueRule (R1)**
2. **IntensityRule (R2)**
3. **StatisticalDeviationRule (R3)**

---

## 9. Fases de construcción (orden de trabajo)

### Fase 0 — Solution skeleton
- [x] Crear solution .NET 8 + 4 proyectos + tests
- [x] Referencias entre capas y DI composition en `Program.cs`
- [x] `appsettings.json` con umbrales de `BUSINESS_RULES.md`

### Fase 1 — Domain
- [ ] `ConsumptionRecord`, `Severity`, `AnomalyAssessment`, `SiteStats`
- [ ] `IAnomalyRule` + resultados de regla
- [ ] Options POCO (`AnomalyDetectionOptions`) — puede vivir en Application

### Fase 2 — Application
- [ ] Interfaces de repositorio y servicio
- [ ] `SiteStatsCalculator`
- [ ] `AnomalyDetectionService`
- [ ] DTOs / mapping mínimo (manual o Mapster/ligero; sin AutoMapper obligatorio)

### Fase 3 — Infrastructure
- [ ] `AppDbContext`, configuración entidad, índice único `(Site, Month)`
- [ ] Repositorio EF + SQLite
- [ ] Migraciones + seed del dataset de ejemplo
- [ ] Extension `AddInfrastructure(IConfiguration)`

### Fase 4 — Api
- [ ] Controllers REST según §6
- [ ] Validación de request (`energyKwh`, `month` formato, etc.)
- [ ] Swagger / OpenAPI habilitado
- [ ] CORS solo si hace falta para demo

### Fase 5 — Tests unitarios
- [ ] R1: negativos → High
- [ ] R2: Barcelona-like intensity → High
- [ ] R3: Madrid spike → High
- [ ] Valencia / series normales → no review
- [ ] Orquestador: severidad máxima al combinar reglas

### Fase 6 — README + demo
- [ ] Cómo restaurar, migrar, ejecutar, llamar endpoints (curl)
- [ ] Resumen de criterios + enlace a `BUSINESS_RULES.md`
- [ ] Notas Escenario A (capacidad / feedback) y B (prompt template, sin llamada real)
- [ ] Preparar guion del vídeo ≤ 5 min

---

## 10. Criterios de “hecho”

- Corre en .NET 8 con `dotnet run` y SQLite local.
- Dataset de ejemplo detecta ids **4, 7, 8** como review; resto OK.
- Capas respetan DIP; reglas extensibles vía `IAnomalyRule`.
- API REST documentada en Swagger.
- README breve suficiente para reproducir la demo.

---

## 11. Riesgos y decisiones conscientes

| Riesgo | Mitigación |
|--------|------------|
| Sobre-ingeniería “microservices” | Un servicio bien layerizado; no N repos/deployments. |
| Poca historia por sede | R3 no fuerza falso positivo; R1/R2 absolutos sí aplican (`BUSINESS_RULES` R4). |
| Duplicados Site+Month | Índice único + `409 Conflict` o upsert documentado. |
| LLM en el hot path | Fuera del runtime; template solo en docs/README. |

---

## 12. Siguiente paso

Tras validar este plan → ejecutar **Fase 0** (crear solution y proyectos) y continuar en orden 1→6.
