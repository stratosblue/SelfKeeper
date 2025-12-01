FROM mcr.microsoft.com/dotnet/runtime:10.0-noble AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src
COPY ["../Directory.Build.props", "../Directory.Build.props"]
COPY ["sample/ConsoleAppSample/ConsoleAppSample.csproj", "sample/ConsoleAppSample/"]
COPY ["src/SelfKeeper/SelfKeeper.csproj", "src/SelfKeeper/"]
RUN dotnet restore "sample/ConsoleAppSample/ConsoleAppSample.csproj"
COPY . .
WORKDIR "/src/sample/ConsoleAppSample"
RUN dotnet build "ConsoleAppSample.csproj" -c Release -f net10.0 --no-self-contained -o /app/build

FROM build AS publish
RUN dotnet publish "ConsoleAppSample.csproj" -c Release -f net10.0 --no-self-contained -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsoleAppSample.dll"]
