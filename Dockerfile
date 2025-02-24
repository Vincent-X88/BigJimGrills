# Use the official .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project file and restore dependencies
COPY ["devDynast.csproj", "./"]
RUN dotnet restore "devDynast.csproj"

# Copy the entire project and build it
COPY . ./
RUN dotnet build "devDynast.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "devDynast.csproj" -c Release -o /app/publish

# Use the official ASP.NET runtime image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "devDynast.dll"]
