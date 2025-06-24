FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ARG UID=1000
RUN usermod -u $UID app \
    && chown -R app /home/app

USER app


FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["src/SqlExercises.Razor/SqlExercises.Razor.csproj", "src/SqlExercises.Razor/"]
RUN dotnet restore "src/SqlExercises.Razor/SqlExercises.Razor.csproj"
COPY . .
WORKDIR "/src/src/SqlExercises.Razor"
RUN dotnet build "SqlExercises.Razor.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "SqlExercises.Razor.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SqlExercises.Razor.dll"]
