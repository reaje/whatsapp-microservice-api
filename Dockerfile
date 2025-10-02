# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY WhatsAppMicroservice.sln ./
COPY src/WhatsApp.API/WhatsApp.API.csproj ./src/WhatsApp.API/
COPY src/WhatsApp.Core/WhatsApp.Core.csproj ./src/WhatsApp.Core/
COPY src/WhatsApp.Infrastructure/WhatsApp.Infrastructure.csproj ./src/WhatsApp.Infrastructure/
COPY tests/WhatsApp.Tests/WhatsApp.Tests.csproj ./tests/WhatsApp.Tests/

# Restore dependencies
RUN dotnet restore

# Copy all source files
COPY . .

# Build the application
WORKDIR /src/src/WhatsApp.API
RUN dotnet build -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install dependencies for WhatsApp (if using Node.js wrapper for Baileys)
# RUN apt-get update && apt-get install -y nodejs npm && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=publish /app/publish .

# Expose port
EXPOSE 5000
EXPOSE 5001

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "WhatsApp.API.dll"]