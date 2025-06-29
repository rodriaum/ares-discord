FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Ares.Api/Ares.Api.csproj", "src/Ares.Api/"]
COPY ["src/Ares.Common/Ares.Common.csproj", "src/Ares.Common/"]

RUN dotnet restore "src/Ares.Api/Ares.Api.csproj"

COPY . .

WORKDIR "/src/src/Ares.Api"
RUN dotnet build "Ares.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ares.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY .env .env
ENTRYPOINT ["dotnet", "Ares.Api.dll"]