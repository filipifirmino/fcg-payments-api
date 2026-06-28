FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/FCG-PAYMENTS-API.Domain/FCG-PAYMENTS-API.Domain.csproj src/FCG-PAYMENTS-API.Domain/
COPY src/FCG-PAYMENTS-API.Application/FCG-PAYMENTS-API.Application.csproj src/FCG-PAYMENTS-API.Application/
COPY src/FCG-PAYMENTS-API.Infra/FCG-PAYMENTS-API.Infra.csproj src/FCG-PAYMENTS-API.Infra/
COPY src/FCG-PAYMENTS-API.Worker/FCG-PAYMENTS-API.Worker.csproj src/FCG-PAYMENTS-API.Worker/

RUN dotnet restore src/FCG-PAYMENTS-API.Worker/FCG-PAYMENTS-API.Worker.csproj

COPY . .

RUN dotnet publish src/FCG-PAYMENTS-API.Worker/FCG-PAYMENTS-API.Worker.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

COPY --chown=paymentApiUser:paymentApiUser --from=build /app/publish .
USER paymentApiUser

EXPOSE 8082
ENV ASPNETCORE_URLS=http://+:8082

ENTRYPOINT ["dotnet", "FCG-PAYMENTS-API.Worker.dll"]
