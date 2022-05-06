FROM mcr.microsoft.com/dotnet/aspnet:6.0-focal AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["../Directory.Build.props", "../Directory.Build.props"]
COPY ["sample/WebApplicationSample/WebApplicationSample.csproj", "sample/WebApplicationSample/"]
COPY ["src/SelfKeeper/SelfKeeper.csproj", "src/SelfKeeper/"]
RUN dotnet restore "sample/WebApplicationSample/WebApplicationSample.csproj"
COPY . .
WORKDIR "/src/sample/WebApplicationSample"
RUN dotnet build "WebApplicationSample.csproj" -c Release -f net6.0 --no-self-contained -o /app/build

FROM build AS publish
RUN dotnet publish "WebApplicationSample.csproj" -c Release -f net6.0 --no-self-contained -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApplicationSample.dll"]