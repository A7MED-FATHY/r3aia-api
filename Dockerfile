FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# نسخ ملف المشروع وعمل Restore
COPY ["R3AIA.csproj", "./"]
RUN dotnet restore "R3AIA.csproj"

# نسخ باقي ملفات المشروع وبنائه
COPY . .
RUN dotnet publish "R3AIA.csproj" -c Release -o /app/publish /p:UseAppHost=false

# مرحلة التشغيل النهائية
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "R3AIA.dll"]