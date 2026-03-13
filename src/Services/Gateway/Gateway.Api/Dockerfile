# 1. Aşama: Derleme (Build)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Proje dosyalarını kopyala (Gateway'in bağımlı olduğu diğer katmanları da alıyoruz)
COPY ["src/Services/Gateway/Gateway.Api/Gateway.Api.csproj", "src/Services/Gateway/Gateway.Api/"]
COPY ["src/Services/Gateway/Gateway.Application/Gateway.Application.csproj", "src/Services/Gateway/Gateway.Application/"]
COPY ["src/Services/Gateway/Gateway.Domain/Gateway.Domain.csproj", "src/Services/Gateway/Gateway.Domain/"]
COPY ["src/Services/Gateway/Gateway.Infrastructure/Gateway.Infrastructure.csproj", "src/Services/Gateway/Gateway.Infrastructure/"]

# Paketleri yükle
RUN dotnet restore "src/Services/Gateway/Gateway.Api/Gateway.Api.csproj"

# Tüm kaynak kodları kopyala ve derle
COPY src/Services/Gateway/ src/Services/Gateway/
WORKDIR "/src/src/Services/Gateway/Gateway.Api"
RUN dotnet publish "Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Aşama: Çalıştırma (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Uygulamanın çalışacağı portu belirle
ENV ASPNETCORE_HTTP_PORTS=5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "Gateway.Api.dll"]