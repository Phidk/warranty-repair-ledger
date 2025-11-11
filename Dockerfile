# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WarrantyRepairLedger.sln ./
COPY WarrantyRepairLedger/WarrantyRepairLedger.csproj WarrantyRepairLedger/
COPY tests/WarrantyRepairLedger.Tests/WarrantyRepairLedger.Tests.csproj tests/WarrantyRepairLedger.Tests/

RUN dotnet restore

COPY . .

RUN dotnet publish WarrantyRepairLedger/WarrantyRepairLedger.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /app/data

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "WarrantyRepairLedger.dll"]
