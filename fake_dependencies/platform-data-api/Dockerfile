FROM mcr.microsoft.com/dotnet/sdk:7.0

WORKDIR /app
COPY ./FakePlatformDataApi.App ./

ENV DOTNET_RUNNING_IN_CONTAINER=true \
  ASPNETCORE_URLS=http://+:8070

EXPOSE 8070

# dotnet watch --no-hot-reload -- run --no-launch-profile
ENTRYPOINT [ "dotnet", "watch", "--no-hot-reload", "--", "run", "--no-launch-profile", "-c", "Release" ]