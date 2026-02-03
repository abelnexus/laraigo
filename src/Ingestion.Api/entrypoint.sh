#!/bin/bash
set -e

# Si decides usar la herramienta dotnet-ef (debe estar instalada en la imagen)
echo "Aplicando migraciones..."
# Como ya estás en la carpeta de la app publicada:
dotnet Ingestion.Api.dll --migrate 
# (Nota: Esto requiere que programes una pequeña lógica en tu Program.cs)