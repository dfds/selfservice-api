FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

COPY src /src
RUN apt update && apt install -y librdkafka-dev
RUN cd /src/SelfService && dotnet publish -c Release -o /build/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /ssapi

# ADD Curl
# RUN apk update && apk add curl && apk add ca-certificates librdkafka-dev && rm -rf /var/cache/apk/*
RUN apt update && apt install -y curl librdkafka-dev git

# AWS RDS Certificate
RUN curl -o /tmp/rds-combined-ca-bundle.pem https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem \
  && mv /tmp/rds-combined-ca-bundle.pem /usr/local/share/ca-certificates/rds-combined-ca-bundle.crt \
  && update-ca-certificates

# OpenSSL cert for Kafka
RUN curl -sS -o /ssapi/cert.pem https://curl.se/ca/cacert.pem
ENV DEFAULT_KAFKA_SSL_CA_LOCATION=/ssapi/cert.pem

# create and use non-root user
RUN adduser \
  --disabled-password \
  --home /ssapi \
  --gecos '' ssapi \
  && chown -R ssapi /ssapi
USER ssapi

COPY --chown=ssapi:ssapi static/known_hosts /ssapi/.ssh/known_hosts
COPY --chown=ssapi:ssapi static/gitconfig /ssapi/.ssh/config

ENV DOTNET_RUNNING_IN_CONTAINER=true \
  ASPNETCORE_URLS=http://+:8080

COPY --from=builder /build/out/ ./

EXPOSE 8080 8888

ENTRYPOINT [ "dotnet", "SelfService.dll" ]
