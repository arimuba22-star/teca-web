# Usar la imagen base de .NET 8 SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo del proyecto y restaurar dependencias
COPY SWebEnergia.csproj .
RUN dotnet restore

# Copiar el resto de los archivos y compilar
COPY . .
RUN dotnet build -c Release -o /app/build

# Publicar la aplicación
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Usar la imagen runtime para la aplicación final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Exponer el puerto
EXPOSE 80
EXPOSE 443

# Punto de entrada
ENTRYPOINT ["dotnet", "SWebEnergia.dll"]
