# Stage 1: base (uses the target platform architecture for the runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

# Stage 2: build (SDK runs natively on the host platform)
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
ARG TARGETOS
WORKDIR /src

COPY ["Directory.Packages.props", "Directory.Build.props", "./"]
COPY ["Pythia.Api/Pythia.Api.csproj", "Pythia.Api/"]
# Pass the architecture to restore the correct RID-specific packages
RUN dotnet restore "Pythia.Api/Pythia.Api.csproj" -a $TARGETARCH -s https://api.nuget.org/v3/index.json --verbosity n

COPY . .
# Compile specifically for the target OS and Architecture
RUN dotnet build "Pythia.Api/Pythia.Api.csproj" -c Release -a $TARGETARCH -o /app/build

# Stage 3: publish
FROM build AS publish
ARG TARGETARCH
RUN dotnet publish "Pythia.Api/Pythia.Api.csproj" -c Release -a $TARGETARCH -o /app/publish

# Stage 4: final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pythia.Api.dll"]
