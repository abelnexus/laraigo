Ingestión y Procesamiento Asíncrono de Eventos
Este repositorio contiene una solución escalable para la ingesta masiva de datos y su procesamiento diferido, diseñada bajo principios de Alta Disponibilidad y Resiliencia.

Arquitectura de la Solución
La solución se basa en un modelo de Arquitectura Orientada a Eventos (EDA) y utiliza el patrón Transactional Outbox para garantizar la consistencia de los datos entre la base de datos y el broker de mensajería.

Componentes Principales:
Ingestion.Api (Componente A): Endpoint REST en .NET 8 que recibe las solicitudes, las persiste en PostgreSQL y las publica en RabbitMQ. Está diseñado para liberar al cliente de forma inmediata.

Processor.Worker (Componente B): Servicio de fondo (Background Service) que consume los mensajes de RabbitMQ de forma asíncrona, procesa la lógica de negocio y actualiza el estado en la base de datos.

RabbitMQ: Message Broker que actúa como buffer, permitiendo manejar picos de tráfico (backpressure) sin degradar el rendimiento de la API.

PostgreSQL: Almacenamiento persistente para los eventos recibidos y la tabla de Outbox.

Decisiones Técnicas Clave
Persistencia y Consistencia: Se utiliza Transactional Outbox. Al guardar el evento y el mensaje pendiente en una misma transacción de base de datos, eliminamos el riesgo de perder información si el Broker de mensajería falla o se desconecta tras recibir la petición HTTP.

Desacoplamiento: Los componentes A y B son totalmente independientes. Esto permite escalar el número de Workers según la carga de procesamiento sin afectar la disponibilidad de la API de ingesta.

Agnosticismo al Entorno: Toda la infraestructura está orquestada mediante Docker Compose, asegurando que el sistema se comporte de manera idéntica en desarrollo, pruebas y producción.

Estructura del Proyecto
Plaintext
├── src/
│ ├── Ingestion.Api/ # API de entrada de datos
│ ├── Processor.Worker/ # Consumidor y procesador de eventos
│ └── Ingestion.Core/ # Lógica de dominio y abstracciones
├── docker-compose.yml # Orquestación de infraestructura y servicios
├── stress_test.sh # Script de validación de carga concurrente
└── README.md # Documentación técnica
Guía de Ejecución
Requisitos
Docker y Docker Desktop (con soporte para Docker Compose).

Despliegue
Desde la raíz del proyecto, ejecutar:

Bash
docker-compose up --build
La API recibira datos a esta ruta: http://localhost:5001/Ingestion/ingest POST

Validación de Carga (Stress Test)
Para validar el procesamiento asíncrono y la concurrencia, ejecutar el script adjunto:

Bash
chmod +x stress_test.sh
./stress_test.sh
Este script envía 50 peticiones concurrentes. El resultado del procesamiento se puede monitorear en tiempo real mediante los logs del worker:

Bash
docker logs -f processor-worker
Monitoreo
Se puede acceder al panel de administración de RabbitMQ para visualizar el flujo de colas:

URL: http://localhost:15672

Credenciales: guest / guest
