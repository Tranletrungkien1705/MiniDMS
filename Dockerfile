# MiniDMS — build-from-source (SQLite, không cần SQL Server) cho deploy cloud 24/7.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/MiniDMS.Web/MiniDMS.Web.csproj src/MiniDMS.Web/
RUN dotnet restore src/MiniDMS.Web/MiniDMS.Web.csproj
COPY . .
RUN dotnet publish src/MiniDMS.Web/MiniDMS.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
# App tự đọc $PORT (Render/Koyeb) hoặc 8080; SQLite file minidms.db tạo tại /app khi seed.
ENTRYPOINT ["dotnet", "MiniDMS.Web.dll"]
