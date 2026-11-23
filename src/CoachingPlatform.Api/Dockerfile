FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/CoachingPlatform.Domain/CoachingPlatform.Domain.csproj src/CoachingPlatform.Domain/
COPY src/CoachingPlatform.Infrastructure/CoachingPlatform.Infrastructure.csproj src/CoachingPlatform.Infrastructure/
COPY src/CoachingPlatform.Api/CoachingPlatform.Api.csproj src/CoachingPlatform.Api/
RUN dotnet restore src/CoachingPlatform.Api/CoachingPlatform.Api.csproj
COPY src/ ./src/
RUN dotnet publish src/CoachingPlatform.Api/CoachingPlatform.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
RUN useradd -m -u 10001 appuser && chown -R appuser:appuser /app
COPY --from=build /app/publish .
USER appuser
EXPOSE 8080
ENTRYPOINT ["dotnet", "CoachingPlatform.Api.dll"]
