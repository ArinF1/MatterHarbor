FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/MatterHarbor.Worker/MatterHarbor.Worker.csproj --locked-mode
RUN dotnet publish src/MatterHarbor.Worker/MatterHarbor.Worker.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /app .
ENTRYPOINT ["dotnet", "MatterHarbor.Worker.dll"]
