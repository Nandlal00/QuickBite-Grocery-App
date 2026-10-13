# Use .NET 10 SDK to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["GroceryStoreApp.csproj", "./"]
RUN dotnet restore "GroceryStoreApp.csproj"

# Copy remaining source code and publish
COPY . .
RUN dotnet publish "GroceryStoreApp.csproj" -c Release -o /app/publish

# Use .NET 10 ASP.NET Runtime to run the app
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port for Render
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GroceryStoreApp.dll"]