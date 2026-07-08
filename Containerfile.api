FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY gitlab-milestone-extensions.ApiService/*.csproj gitlab-milestone-extensions.ApiService/
COPY gitlab-milestone-extensions.ServiceDefaults/*.csproj gitlab-milestone-extensions.ServiceDefaults/

RUN dotnet restore gitlab-milestone-extensions.ApiService/gitlab-milestone-extensions.ApiService.csproj

COPY gitlab-milestone-extensions.ApiService/ gitlab-milestone-extensions.ApiService/
COPY gitlab-milestone-extensions.ServiceDefaults/ gitlab-milestone-extensions.ServiceDefaults/

RUN dotnet publish gitlab-milestone-extensions.ApiService/gitlab-milestone-extensions.ApiService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "gitlab-milestone-extensions.ApiService.dll"]
