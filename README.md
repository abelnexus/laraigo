# Ingestión y Procesamiento Asíncrono de Eventos

Este repositorio contiene una solución escalable para la ingesta masiva de datos y su procesamiento diferido, diseñada bajo principios de Alta Disponibilidad y Resiliencia.

<img width="1536" height="1024" alt="diagrama" src="https://github.com/user-attachments/assets/88f3b187-ccee-4086-aeb0-66463d17ab05" />

## Arquitectura de la Solución

La solución se basa en un modelo de Arquitectura Orientada a Eventos (EDA) y utiliza el patrón Transactional Outbox para garantizar la consistencia de los datos entre la base de datos y el broker de mensajería.

## Componentes Principales

**Ingestion.Api (Componente A)**  
Endpoint REST en .NET 8 que recibe las solicitudes, las persiste en PostgreSQL y las publica en RabbitMQ. Está diseñado para liberar al cliente de forma inmediata.

**Processor.Worker (Componente B)**  
Servicio de fondo (Background Service) que consume los mensajes de RabbitMQ de forma asíncrona, procesa la lógica de negocio y actualiza el estado en la base de datos.

**RabbitMQ**  
Message Broker que actúa como buffer, permitiendo manejar picos de tráfico (backpressure) sin degradar el rendimiento de la API.

**PostgreSQL**  
Almacenamiento persistente para los eventos recibidos y la tabla de Outbox.

## Decisiones Técnicas Clave

**Persistencia y Consistencia**  
Se utiliza Transactional Outbox. Al guardar el evento y el mensaje pendiente en una misma transacción de base de datos, se elimina el riesgo de perder información si el broker falla.

**Desacoplamiento**  
Los componentes A y B son independientes. Esto permite escalar los Workers sin afectar la disponibilidad de la API.

**Agnosticismo al Entorno**  
Toda la infraestructura está orquestada mediante Docker Compose.

## Estructura del Proyecto

```plaintext
├── src/
│   ├── Ingestion.Api/
│   ├── Processor.Worker/
│   └── Ingestion.Core/
│
├── docker-compose.yml
├── stress_test.sh
└── README.md
```

## Guía de Ejecución

```bash
docker-compose up --build
```

API:
http://localhost:5001/Ingestion/ingest

RabbitMQ:
http://localhost:15672
guest / guest

## Validación de Carga

```bash
chmod +x stress_test.sh
./stress_test.sh
```

Logs:
```bash
docker logs -f processor-worker
```

## Notas Multi-plataforma

En Windows puedes usar PowerShell:

```powershell
1..50 | ForEach-Object {
 Invoke-RestMethod -Uri "http://localhost:5001/Ingestion/ingest" -Method Post -ContentType "application/json" -Body '{"payload":"Test masivo"}'
}
```
