FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
ARG TARGETARCH
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/FixFlow.Api/FixFlow.Api.csproj src/FixFlow.Api/
RUN dotnet restore src/FixFlow.Api/FixFlow.Api.csproj --arch $TARGETARCH -p:PublishReadyToRun=true
COPY src/ src/
RUN dotnet publish src/FixFlow.Api/FixFlow.Api.csproj \
    --configuration Release \
    --no-restore \
    --arch $TARGETARCH \
    --no-self-contained \
    -p:PublishReadyToRun=true \
    --output /app/publish \
    -p:OpenApiGenerateDocuments=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "FixFlow.Api.dll"]
