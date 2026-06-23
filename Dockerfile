FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/FCG-PAYMENTS-API.Domain/FCG-PAYMENTS-API.Domain.csproj src/FCG-PAYMENTS-API.Domain/
COPY src/FCG-PAYMENTS-API.Application/FCG-PAYMENTS-API.Application.csproj src/FCG-PAYMENTS-API.Application/
COPY src/FCG-PAYMENTS-API.Infra/FCG-PAYMENTS-API.Infra.csproj src/FCG-PAYMENTS-API.Infra/
COPY src/FCG-PAYMENTS-API.Worker/FCG-PAYMENTS-API.Worker.csproj src/FCG-PAYMENTS-API.Worker/

RUN dotnet restore src/FCG-PAYMENTS-API.Worker/FCG-PAYMENTS-API.Worker.csproj

COPY . .

RUN dotnet publish src/FCG-PAYMENTS-API.Worker/FCG-PAYMENTS-API.Worker.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FCG-PAYMENTS-API.Worker.dll"]
