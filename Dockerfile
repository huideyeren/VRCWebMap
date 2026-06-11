FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY VrcWebMap.Backend.csproj ./
RUN dotnet restore VrcWebMap.Backend.csproj

COPY . ./
RUN dotnet publish VrcWebMap.Backend.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VrcWebMap.Backend.dll"]
