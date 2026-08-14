# CO2 Monitoring — curls de demo

Base: `http://localhost:5120` (perfil `http` de launchSettings).

Colección Postman: [`CO2-Monitoring.postman_collection.json`](./CO2-Monitoring.postman_collection.json)  
Importar en Postman: **Import → File**.

```bash
# Health
curl http://localhost:5120/api/v1/health

# Listar registros
curl http://localhost:5120/api/v1/consumption-records

# Listar por sede
curl "http://localhost:5120/api/v1/consumption-records?site=Madrid"

# Detalle
curl http://localhost:5120/api/v1/consumption-records/4

# Alta
curl -X POST http://localhost:5120/api/v1/consumption-records \
  -H "Content-Type: application/json" \
  -d '{"site":"Madrid","month":"2026-05","energyKwh":25000,"co2Kg":5900}'

# Bulk
curl -X POST http://localhost:5120/api/v1/consumption-records/bulk \
  -H "Content-Type: application/json" \
  -d '[{"site":"Sevilla","month":"2026-01","energyKwh":7000,"co2Kg":1600}]'

# Evaluar todos (esperado: ids 4, 7, 8 → review High)
curl -X POST http://localhost:5120/api/v1/anomaly-reviews

# Evaluar uno
curl -X POST http://localhost:5120/api/v1/anomaly-reviews/4
```
