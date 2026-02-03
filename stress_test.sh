#!/bin/bash
echo "🔥 Iniciando prueba de estrés: 50 solicitudes..."
for i in {1..50}
do
   curl -s -X POST http://localhost:5001/Ingestion/ingest \
   -H "Content-Type: application/json" \
   -d "{\"payload\": \"Evento de alta carga numero $i\"}" > /dev/null &
done
echo "✅ 50 solicitudes enviadas. Revisa los logs: docker logs -f processor-worker"