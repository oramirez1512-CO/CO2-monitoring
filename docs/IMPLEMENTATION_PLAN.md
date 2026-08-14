# Plan de construcción — CO2 Monitoring (Anomaly Review)

Plan alineado con [`BUSINESS_RULES.md`](./BUSINESS_RULES.md).

**Stack:** .NET 8 / C#, ASP.NET Core Web API, EF Core + SQLite.  
**Forma:** un solo proyecto API con carpetas (pragmático para prueba técnica; sin Clean Architecture de 4 proyectos).

---

## 1. Objetivo del MVP

Un **único microservicio** REST que:

1. Persista registros de consumo energético / emisiones por sede y mes (SQLite).
2. Analice un registro (o el dataset completo) aplicando R1–R3.
3. Devuelva resultados con `requiresReview`, `reason` y `severity`.
4. Exponga umbrales vía configuración (`appsettings.json`).
5. Incluya seed del dataset de la prueba.

Fuera del MVP: integración real con LLM; UI; autenticación.

---

## 2. Principios (sin exagerar)

| Principio | Cómo |
|-----------|------|
| **S** | Controllers = HTTP; `AnomalyDetectionService` = orquestar; cada regla = una clase |
| **O** | Nueva regla = nueva `IAnomalyRule` + registro en `Program.cs` |
| **D** | Reglas dependen de modelos/options, no de EF ni HTTP |

No CQRS, no mediadores, no event bus.

---

## 3. Estructura

```
src/Co2Monitoring.Api/
  Controllers/          HTTP
  Domain/               ConsumptionRecord, Severity, models, Options, IAnomalyRule
  Services/             AnomalyDetectionService, SiteStatsCalculator
  Services/Rules/       R1, R2, R3 (pendiente)
  Data/                 AppDbContext (SQLite)
  Dtos/                 JSON in/out
  Program.cs            DI + EnsureCreated
tests/Co2Monitoring.UnitTests/
docs/
```

Flujo:

```
HTTP → Controller → AnomalyDetectionService
                      → SiteStatsCalculator
                      → foreach IAnomalyRule (R1→R3)
                      → merge reason + max Severity
```

---

## 4. Modelo

### `ConsumptionRecord`

| Campo | Tipo | Notas |
|-------|------|--------|
| `Id` | `int` | PK |
| `Site` | `string` | Sede |
| `Month` | `string` | `YYYY-MM` |
| `EnergyKwh` | `decimal` | |
| `Co2Kg` | `decimal` | |

Índice único `(Site, Month)`.

### Resultados

| Tipo | Campos |
|------|--------|
| `AnomalyAssessment` | `RecordId`, `RequiresReview`, `Reason`, `Severity` |
| `Severity` | `None`, `Low`, `Medium`, `High` |
| `SiteStats` | medias, mes previo, MoM, intensidad |

### Config (`AnomalyDetectionOptions`)

Mapeo 1:1 con `BUSINESS_RULES.md` § Parámetros.

---

## 5. API REST

Base: `/api/v1`

| Método | Ruta | Acción |
|--------|------|--------|
| `POST` | `/api/v1/consumption-records` | Alta |
| `POST` | `/api/v1/consumption-records/bulk` | Bulk / seed |
| `GET` | `/api/v1/consumption-records` | Listar (`?site=`) |
| `GET` | `/api/v1/consumption-records/{id}` | Detalle |
| `POST` | `/api/v1/anomaly-reviews` | Evaluar todos |
| `POST` | `/api/v1/anomaly-reviews/{id}` | Evaluar uno |
| `GET` | `/api/v1/health` | Health |

Salida de revisión:

```json
{
  "id": 4,
  "requiresReview": true,
  "reason": "Energy consumption significantly exceeds historical behavior for site",
  "severity": "High"
}
```

---

## 6. Persistencia

| Decisión | Detalle |
|----------|---------|
| Proveedor | EF Core SQLite |
| Archivo | `co2monitoring.db` |
| Bootstrap | `EnsureCreated` (MVP); migraciones opcionales después |
| Seed | Dataset de ejemplo en Development o vía bulk |

---

## 7. Fases

### Hecho — Skeleton + reglas
- [x] Un proyecto Api + tests
- [x] Domain + Services + Data + Controllers
- [x] Options en `appsettings.json` (umbrales editables)
- [x] `SiteStatsCalculator` + R1 / R2 / R3
- [x] Seed dataset; ids **4, 7, 8** → review
- [x] Tests unitarios de reglas

### Siguiente — Demo
- [ ] Notas demo Escenario A / B + guion vídeo ≤ 5 min

---

## 8. Criterios de “hecho”

- `dotnet run` + SQLite local.
- Dataset ejemplo: ids **4, 7, 8** review; resto OK.
- Reglas extensibles vía `IAnomalyRule`.
- Swagger + README para reproducir la demo.

---

## 9. Riesgos

| Riesgo | Mitigación |
|--------|------------|
| Sobre-ingeniería | Un proyecto; carpetas en vez de 4 csproj |
| Poca historia | R3 no inventa falsos positivos (`BUSINESS_RULES` R4) |
| LLM en hot path | Solo docs / template |
