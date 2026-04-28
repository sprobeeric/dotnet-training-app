FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DocumentTracker.slnx ./
COPY src/DocumentTracker/DocumentTracker.csproj src/DocumentTracker/
COPY tests/DocumentTracker.Tests/DocumentTracker.Tests.csproj tests/DocumentTracker.Tests/
RUN dotnet restore DocumentTracker.slnx

COPY . .
RUN dotnet publish src/DocumentTracker/DocumentTracker.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DocumentTracker.dll"]
