FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/MusicRoom/MusicRoom.csproj src/MusicRoom/
RUN dotnet restore src/MusicRoom/MusicRoom.csproj

COPY src/MusicRoom/ src/MusicRoom/
RUN dotnet publish src/MusicRoom/MusicRoom.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MusicRoom.dll"]
