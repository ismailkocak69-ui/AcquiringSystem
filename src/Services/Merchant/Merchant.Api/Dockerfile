# 1. Aşama: Derleme (Build)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/Merchant/Merchant.Api/Merchant.Api.csproj", "src/Services/Merchant/Merchant.Api/"]
RUN dotnet restore "src/Services/Merchant/Merchant.Api/Merchant.Api.csproj"

COPY src/Services/Merchant/ src/Services/Merchant/
WORKDIR "/src/src/Services/Merchant/Merchant.Api"
RUN dotnet publish "Merchant.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Aşama: Çalıştırma (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Merchant API'yi 5002 portunda çalıştırıyoruz
ENV ASPNETCORE_HTTP_PORTS=5002
EXPOSE 5002

ENTRYPOINT ["dotnet", "Merchant.Api.dll"]