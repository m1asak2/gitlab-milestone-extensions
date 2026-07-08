FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY gitlab-milestone-extensions.Web/*.csproj gitlab-milestone-extensions.Web/

RUN dotnet restore gitlab-milestone-extensions.Web/gitlab-milestone-extensions.Web.csproj

COPY gitlab-milestone-extensions.Web/ gitlab-milestone-extensions.Web/

RUN dotnet publish gitlab-milestone-extensions.Web/gitlab-milestone-extensions.Web.csproj -c Release -o /app/publish

FROM nginxinc/nginx-unprivileged:1.27-alpine AS final

COPY docker/web/nginx/default.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 8080
