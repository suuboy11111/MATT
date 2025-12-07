# Use .NET 9.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Use .NET 9.0 runtime for running
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose port (Railway will set PORT env var)
EXPOSE 8080

# Start the application (PORT will be set by Railway)
ENTRYPOINT ["dotnet", "MaiAmTinhThuong.dll"]



