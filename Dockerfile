# Use the official .NET SDK image as a build environment
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the solution file and restore dependencies
COPY ["nexusDB.sln", "."]
COPY ["nexusDB.Api/nexusDB.Api.csproj", "nexusDB.Api/"]
COPY ["nexusDB.Application/nexusDB.Application.csproj", "nexusDB.Application/"]
COPY ["nexusDB.Domain/nexusDB.Domain.csproj", "nexusDB.Domain/"]
COPY ["nexusDB.Infrastructure/nexusDB.Infrastructure.csproj", "nexusDB.Infrastructure/"]
# Use a more explicit COPY command for Directory.Packages.props
COPY Directory.Packages.props ./ # Copy Directory.Packages.props for CPM

RUN dotnet restore "nexusDB.sln"

# Copy the rest of the application code
COPY . .

# Publish the API
WORKDIR /src/nexusDB.Api
RUN dotnet publish "nexusDB.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET ASP.NET runtime image as the base image for the final application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Expose the port the application will listen on
EXPOSE 8080

# Set the entry point for the application
ENTRYPOINT ["dotnet", "nexusDB.Api.dll"]
