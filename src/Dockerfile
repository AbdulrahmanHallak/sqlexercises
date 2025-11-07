FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ARG UID=1000

RUN deluser app \
    && adduser -D -u $UID app

WORKDIR /src/ErdGenerator
COPY --chown=app:app ErdGenerator/requirements.txt .

RUN apk add --no-cache \
    python3 \
    py3-pip \
    graphviz \
    && apk add --no-cache --virtual .build-deps \
    build-base \
    graphviz-dev \
    pkgconfig \
    python3-dev \
    && pip install --no-cache-dir --break-system-packages --upgrade pip \
    && pip install --no-cache-dir --break-system-packages -r requirements.txt \
    && apk del .build-deps

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG web_proj=SqlExercises.Razor
ARG db_proj=SqlExercises.Db

WORKDIR /src
COPY ${web_proj}/${web_proj}.csproj ${web_proj}/
COPY ${db_proj}/${db_proj}.csproj ${db_proj}/
RUN dotnet restore ${web_proj}/${web_proj}.csproj

COPY ${web_proj}/ ${web_proj}/
COPY ${db_proj}/ ${db_proj}/

FROM build AS publish
ARG configuration=Release
RUN dotnet publish ${web_proj}/${web_proj}.csproj -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
COPY --chown=app:app ErdGenerator/ /src/ErdGenerator/

WORKDIR /app
HEALTHCHECK --interval=30s --timeout=30s --start-period=30s --retries=3 \
    CMD wget --quiet --spider http://127.0.0.1:8080/health || exit 1

COPY --from=publish --chown=app:app /app/publish .
RUN chown -R app:app /app
RUN chown -R app:app /src/ErdGenerator

USER app
ENTRYPOINT ["dotnet", "SqlExercises.Razor.dll"]

