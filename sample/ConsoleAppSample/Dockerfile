FROM mcr.microsoft.com/dotnet/runtime:6.0-focal AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["../Directory.Build.props", "../Directory.Build.props"]
COPY ["sample/ConsoleAppSample/ConsoleAppSample.csproj", "sample/ConsoleAppSample/"]
COPY ["src/SelfKeeper/SelfKeeper.csproj", "src/SelfKeeper/"]
RUN dotnet restore "sample/ConsoleAppSample/ConsoleAppSample.csproj"
COPY . .
WORKDIR "/src/sample/ConsoleAppSample"
RUN dotnet build "ConsoleAppSample.csproj" -c Release -f net6.0 --no-self-contained -o /app/build

FROM build AS publish
RUN dotnet publish "ConsoleAppSample.csproj" -c Release -f net6.0 --no-self-contained -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsoleAppSample.dll"]