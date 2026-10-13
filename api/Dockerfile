FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY src/NoiseVault.Api/NoiseVault.Api.csproj                 src/NoiseVault.Api/
COPY src/NoiseVault.Application/NoiseVault.Application.csproj src/NoiseVault.Application/
COPY src/NoiseVault.Domain/NoiseVault.Domain.csproj           src/NoiseVault.Domain/
COPY src/NoiseVault.Infra.Data/NoiseVault.Infra.Data.csproj   src/NoiseVault.Infra.Data/
COPY src/NoiseVault.Infra.IoC/NoiseVault.Infra.IoC.csproj     src/NoiseVault.Infra.IoC/
COPY src/NoiseVault.Workers/NoiseVault.Workers.csproj         src/NoiseVault.Workers/
RUN dotnet restore src/NoiseVault.Api/NoiseVault.Api.csproj

COPY src/ src/
RUN dotnet publish src/NoiseVault.Api/NoiseVault.Api.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 tzdata \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV TZ=America/Sao_Paulo
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "NoiseVault.Api.dll"]
