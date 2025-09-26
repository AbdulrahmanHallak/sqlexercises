FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ARG UID=1000

USER root
# same userid across containers to persist data protection keys.
RUN usermod -u $UID app \
    && groupmod -g $UID app \
    && chown -R $UID:$UID /home/app

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    python3 python3-venv python3-pip graphviz \
    && rm -rf /var/lib/apt/lists/*

USER app


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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


FROM base AS py-build
USER root
WORKDIR /src/ErdGenerator
COPY ErdGenerator/requirements.txt ./

RUN python3 -m venv /venv \
    && . /venv/bin/activate \
    && apt-get update \
    && apt-get install -y --no-install-recommends \
    build-essential libgraphviz-dev pkg-config python3-dev \
    && pip install --upgrade pip \
    && pip install -r requirements.txt \
    && apt-get purge -y --auto-remove build-essential libgraphviz-dev pkg-config python3-dev \
    && rm -rf /var/lib/apt/lists/*

COPY ErdGenerator/ .


FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
COPY --from=py-build /venv /venv
COPY --from=py-build /src/ErdGenerator /src/ErdGenerator

ENTRYPOINT ["dotnet", "SqlExercises.Razor.dll"]
