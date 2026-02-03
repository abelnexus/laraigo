FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos TODOS los archivos de proyecto para que el restore vea el grafo completo
COPY ["Processor.Worker/Processor.Worker.csproj", "Processor.Worker/"]
COPY ["Ingestion.Core/Ingestion.Core.csproj", "Ingestion.Core/"]

# Restauramos apuntando al Worker
RUN dotnet restore "Processor.Worker/Processor.Worker.csproj"

# Copiamos el código fuente
COPY Ingestion.Core/ Ingestion.Core/
COPY Processor.Worker/ Processor.Worker/

WORKDIR "/src/Processor.Worker"
# QUITAMOS el --no-restore para evitar el error NETSDK1064
RUN dotnet publish -c Release -o /app/worker

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/worker .
ENTRYPOINT ["dotnet", "Processor.Worker.dll"]