# Reglas de negocio — anomalías ESG

Criterios para marcar un registro de energía/CO₂ como “requiere revisión” antes de meterlo en el reporting.

## Ideas base

- Los umbrales van en config, no hardcodeados.
- Los datos llegan **por sede y mes** (`YYYY-MM`). No hay serie diaria ni semanal.
- Cada alerta trae `reason` y `severity`.
- Ante la duda, mejor revisión humana que ensuciar el reporting.
- Las reglas del sistema mandan. Un LLM, si aparece, es apoyo (ver escenario B), no el juez principal.

## Datos de entrada

```json
{
  "id": 1,
  "site": "Madrid",
  "month": "2026-01",
  "energyKwh": 12000,
  "co2Kg": 2800
}
```

Cada sede se analiza con su propio histórico. Ordenamos por `month`.

También calculamos la **intensidad**:

`intensity = co2Kg / energyKwh` (solo si `energyKwh > 0`)

## Parámetros

Valores de partida; se pueden afinar después:

| Parámetro | Clave | Default |
|-----------|-------|---------|
| Energía mínima | `Validation.MinEnergyKwh` | `0` |
| CO₂ mínimo | `Validation.MinCo2Kg` | `0` |
| Intensidad mín. | `Intensity.MinKgPerKwh` | `0.05` |
| Intensidad máx. | `Intensity.MaxKgPerKwh` | `0.50` |
| Meses de histórico | `Stats.LookbackMonths` | `3` |
| Cambio MoM energía (%) | `Stats.EnergyMomChangePercent` | `50` |
| Desvío vs media energía (%) | `Stats.EnergyVsAvgPercent` | `80` |
| Cambio MoM CO₂ (%) | `Stats.Co2MomChangePercent` | `50` |
| Desvío vs media CO₂ (%) | `Stats.Co2VsAvgPercent` | `80` |
| Desvío intensidad vs media (%) | `Stats.IntensityVsAvgPercent` | `40` |

Con pocos meses, una media de 3 es más útil que un z-score. Si más adelante hay series largas, se puede añadir.

## Reglas

Se evalúan en orden. Si saltan varias, nos quedamos con la severidad más alta y juntamos las razones.

### R1 — Valores inválidos

Si `energyKwh` o `co2Kg` están por debajo del mínimo (p. ej. negativos).

Ejemplo: Barcelona id 7 (`-900` / `-210`) → review, **High**.

### R2 — Relación energía / CO₂ (intensidad)

Salta si:

1. La intensidad sale del rango `[Min, Max]`, o
2. Hay histórico y se desvía demasiado de la media de la sede.

Ejemplo: Barcelona id 8 (energía normal, CO₂ 8500 → intensidad ~0.96 vs ~0.23 histórico) → review, **High** si está fuera de rango; **Medium** si solo choca con la media.

### R3 — Cambio raro vs histórico de la sede

Hace falta al menos un mes previo en la misma sede.

Salta si la energía o el CO₂:

1. Cambian demasiado mes a mes (MoM), o
2. Se alejan demasiado de la media de los últimos N meses.

Ejemplo: Madrid id 4 (~12k → 79k) → review. **High** si cumple MoM y media; **Medium** si solo uno.

### R4 — Poca historia

Si casi no hay histórico, no marcamos anomalía estadística solo por eso. Sí aplican R1 y los rangos absolutos de R2.

Valencia id 9–10, si los números cuadran → OK.

## Salida

```json
{
  "id": 4,
  "requiresReview": true,
  "reason": "Energy consumption significantly exceeds historical behavior for site",
  "severity": "High"
}
```

- **High:** inválidos, intensidad fuera de rango, o pico fuerte (MoM + media).
- **Medium:** solo MoM, solo media, o intensidad rara solo vs histórico.
- **Low:** reservado por si más adelante queremos avisos suaves.
- Sin reglas → `requiresReview: false`.

## Escenario A — crecimiento real de la fábrica

El sistema marca esto como anómalo:

```json
{ "site": "Madrid", "month": "2026-05", "energyKwh": 25000, "co2Kg": 5900 }
```

El cliente dice que en mayo ampliaron la fábrica. Tiene sentido: no es un error de dato.

¿Cambiamos el sistema? Un poco. No apagamos la detección; añadimos contexto:

1. Registrar un “cambio de capacidad” en la sede y, a partir de ahí, subir umbrales o resetear la media.
2. Dejar que el revisor marque “justificado” y que ese mes entre al histórico aceptado.
3. Umbrales distintos por tipo de sede (estable vs en expansión).
4. En High, solo encolar revisión; no tirar el dato del reporting sin decisión humana.

## Escenario B — ¿mandamos cada registro a un LLM?

No como único criterio: sale caro, no es determinista y cuesta auditarlo en ESG.

Sí como apoyo opcional: el sistema calcula stats y reglas, y el LLM solo ayuda a redactar el `reason` o a contrastar.

Prompt template:

```text
You are an assistant supporting ESG data quality review.
You do NOT invent thresholds. You only interpret the provided statistics
against the stated business rules.

## Site context
- Site: {{site}}
- Month under review: {{month}}
- Record id: {{id}}

## Current values
- energyKwh: {{energyKwh}}
- co2Kg: {{co2Kg}}
- intensityKgPerKwh: {{intensity}}

## Site statistics (same site, prior months)
- lookbackMonths: {{lookbackMonths}}
- avgEnergyKwh: {{avgEnergyKwh}}
- avgCo2Kg: {{avgCo2Kg}}
- avgIntensity: {{avgIntensity}}
- prevMonthEnergyKwh: {{prevMonthEnergyKwh}}
- prevMonthCo2Kg: {{prevMonthCo2Kg}}
- energyMomChangePercent: {{energyMomChangePercent}}
- co2MomChangePercent: {{co2MomChangePercent}}
- intensityVsAvgPercent: {{intensityVsAvgPercent}}

## Rule evaluation (system)
- triggeredRules: {{triggeredRules}}
- suggestedSeverity: {{suggestedSeverity}}

## Task
1. Confirm whether the system flags are consistent with the numbers.
2. Suggest a short human-readable reason in English (1 sentence).
3. Reply ONLY with JSON:
   { "agreesWithSystem": true|false, "reason": "...", "severity": "High|Medium|Low" }
```

## Qué esperamos del dataset de ejemplo

| Id | Sede | Resultado | Regla |
|----|------|-----------|-------|
| 1–3 | Madrid | OK | — |
| 4 | Madrid | Review / High | R3 |
| 5–6 | Barcelona | OK | — |
| 7 | Barcelona | Review / High | R1 |
| 8 | Barcelona | Review / High | R2 |
| 9–10 | Valencia | OK | — |
