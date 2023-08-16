FROM mcr.microsoft.com/dotnet/sdk:7.0 as builder

COPY src /src
RUN apt update && apt install -y librdkafka-dev
RUN cd /src/SelfService && dotnet publish -c Release -o /build/out

FROM mcr.microsoft.com/dotnet/aspnet:7.0

WORKDIR /app

# ADD Curl
# RUN apk update && apk add curl && apk add ca-certificates librdkafka-dev && rm -rf /var/cache/apk/*
RUN apt update && apt install -y curl librdkafka-dev

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

COPY --from=builder /build/out/ ./

EXPOSE 8080 8888

ENTRYPOINT [ "dotnet", "SelfService.dll" ]