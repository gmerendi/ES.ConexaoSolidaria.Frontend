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

# Copia os arquivos publicados pelo build do .NET
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
RUN mv /usr/share/nginx/html/appsettings.json /usr/share/nginx/html/appsettings.json.template

# Copia o entrypoint
COPY entrypoint.sh /entrypoint.sh

# Corrige quebras de linha CRLF para LF (caso tenha sido editado no Windows) e dá permissão
RUN tr -d '\r' < /entrypoint.sh > /entrypoint_unix.sh \
    && mv /entrypoint_unix.sh /entrypoint.sh \
    && chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]

EXPOSE 80