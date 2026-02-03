FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Ingestion.Api/Ingestion.Api.csproj", "Ingestion.Api/"]
COPY ["Ingestion.Core/Ingestion.Core.csproj", "Ingestion.Core/"]

RUN dotnet restore "Ingestion.Api/Ingestion.Api.csproj"

COPY Ingestion.Core/ Ingestion.Core/
COPY Ingestion.Api/ Ingestion.Api/

WORKDIR "/src/Ingestion.Api"
# QUITAMOS el --no-restore
RUN dotnet publish -c Release -o /app/api

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/api .
COPY Ingestion.Api/entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh
ENTRYPOINT ["/app/entrypoint.sh"]