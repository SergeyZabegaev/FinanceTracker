# Этап 1: Сборка приложения
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Копируем csproj и восстанавливаем зависимости (для кэширования)
COPY *.csproj ./
RUN dotnet restore

# Копируем все остальное и публикуем
COPY . ./
RUN dotnet publish -c Release -o /app --no-restore

# Этап 2: Финальный образ для запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
# Важно: приложение должно слушать на порту 8080 внутри контейнера
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "FinanceTracker.dll"]