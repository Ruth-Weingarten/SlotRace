# שלב 1: בנייה (SDK מלא)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish SlotRace.API/SlotRace.API.csproj -c Release -o /app/publish

# שלב 2: הרצה (רק ה-runtime, תמונה קטנה יותר)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# Render מעביר את הפורט במשתנה PORT (ברירת מחדל 10000). מקומית: 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet SlotRace.API.dll"]
