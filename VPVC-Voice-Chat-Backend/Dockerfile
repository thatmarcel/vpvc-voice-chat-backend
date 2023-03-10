FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["VPVC-Voice-Chat-Backend/VPVC-Voice-Chat-Backend.csproj", "VPVC-Voice-Chat-Backend/"]
RUN dotnet restore "VPVC-Voice-Chat-Backend/VPVC-Voice-Chat-Backend.csproj"
COPY . .
WORKDIR "/src/VPVC-Voice-Chat-Backend"
RUN dotnet build "VPVC-Voice-Chat-Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VPVC-Voice-Chat-Backend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VPVC-Voice-Chat-Backend.dll"]
