FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY src/Headbanger.Api/Headbanger.Api.csproj                 src/Headbanger.Api/
COPY src/Headbanger.Application/Headbanger.Application.csproj src/Headbanger.Application/
COPY src/Headbanger.Domain/Headbanger.Domain.csproj           src/Headbanger.Domain/
COPY src/Headbanger.Infra.Data/Headbanger.Infra.Data.csproj   src/Headbanger.Infra.Data/
COPY src/Headbanger.Infra.IoC/Headbanger.Infra.IoC.csproj     src/Headbanger.Infra.IoC/
COPY src/Headbanger.Workers/Headbanger.Workers.csproj         src/Headbanger.Workers/
RUN dotnet restore src/Headbanger.Api/Headbanger.Api.csproj

COPY src/ src/
RUN dotnet publish src/Headbanger.Api/Headbanger.Api.csproj \
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
ENTRYPOINT ["dotnet", "Headbanger.Api.dll"]
