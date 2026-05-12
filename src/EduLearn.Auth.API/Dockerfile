FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["edulearn-backend/src/EduLearn.Auth.API/EduLearn.Auth.API.csproj", "edulearn-backend/src/EduLearn.Auth.API/"]
COPY ["edulearn-backend/src/EduLearn.Shared/EduLearn.Shared.csproj", "edulearn-backend/src/EduLearn.Shared/"]
RUN dotnet restore "edulearn-backend/src/EduLearn.Auth.API/EduLearn.Auth.API.csproj"

COPY . .
WORKDIR "/src/edulearn-backend/src/EduLearn.Auth.API"
RUN dotnet publish "EduLearn.Auth.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EduLearn.Auth.API.dll"]