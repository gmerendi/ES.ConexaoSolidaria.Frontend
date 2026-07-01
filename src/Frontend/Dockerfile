# --------------------------------------------------------------------------------------------------
# STAGE: BASE (Execução/Runtime)
# Onde a aplicação irá rodar.
# --------------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src



# --------------------------------------------------------------------------------------------------
# STAGE: PUBLISH
# --------------------------------------------------------------------------------------------------
COPY src/Frontend/ .

RUN dotnet publish Frontend.csproj \
    -c Release \
    -o /app/publish


# --------------------------------------------------------------------------------------------------
# STAGE: FINAL (Produção)
# Copia os binários e garante que o usuário FIAP tem as permissões corretas
# --------------------------------------------------------------------------------------------------
FROM nginx:alpine AS final

RUN rm /etc/nginx/conf.d/default.conf
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 80