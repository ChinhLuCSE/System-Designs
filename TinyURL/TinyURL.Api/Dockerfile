FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["TinyURL.Api/TinyURL.Api.csproj", "TinyURL.Api/"]
RUN dotnet restore "TinyURL.Api/TinyURL.Api.csproj"

COPY . .
WORKDIR /src/TinyURL.Api
RUN dotnet publish "TinyURL.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TinyURL.Api.dll"]
