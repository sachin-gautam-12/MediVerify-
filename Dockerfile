FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish MediVerify/MediVerify.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
CMD ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000} dotnet MediVerify.dll
