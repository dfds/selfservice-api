FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine

WORKDIR /app

# ADD Curl
RUN apk update && apk add curl && apk add ca-certificates && rm -rf /var/cache/apk/*

# AWS RDS Certificate
RUN curl -o /tmp/rds-combined-ca-bundle.pem https://s3.amazonaws.com/rds-downloads/rds-combined-ca-bundle.pem \
  && mv /tmp/rds-combined-ca-bundle.pem /usr/local/share/ca-certificates/rds-combined-ca-bundle.crt \
  && update-ca-certificates

# OpenSSL cert for Kafka
RUN curl -sS -o /app/cert.pem https://curl.se/ca/cacert.pem
ENV DEFAULT_KAFKA_SSL_CA_LOCATION=/app/cert.pem

# create and use non-root user
RUN adduser \
  --disabled-password \
  --home /app \
  --gecos '' app \
  && chown -R app /app
USER app

ENV DOTNET_RUNNING_IN_CONTAINER=true \
  ASPNETCORE_URLS=http://+:8080

COPY ./.output/app ./

EXPOSE 8080 8888

ENTRYPOINT [ "dotnet", "SelfService.dll" ]