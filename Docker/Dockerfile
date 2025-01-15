FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app 

COPY . ./
WORKDIR /app/XYZ-Forge-API

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build-env /app/XYZ-Forge-API/out .

ENTRYPOINT ["dotnet", "XYZ-Forge-API.dll"]