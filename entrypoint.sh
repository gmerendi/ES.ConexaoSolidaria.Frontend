#!/bin/sh
envsubst < /usr/share/nginx/html/appsettings.json.template > /usr/share/nginx/html/appsettings.json
exec nginx -g 'daemon off;'