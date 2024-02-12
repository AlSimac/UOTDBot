FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH=x64
WORKDIR /src

# copy csproj and restore as distinct layers
COPY *.csproj .
RUN dotnet restore -a $TARGETARCH

# copy and publish app and libraries
COPY . .
RUN dotnet publish -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app --self-contained


# final stage/image
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine
ENV \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8
RUN apk add --no-cache \
    icu-data-full \
    icu-libs \
    tzdata
WORKDIR /app
COPY --from=build /app .
RUN chown $APP_UID:$APP_GID Data
USER $APP_UID
ENTRYPOINT ["./UOTDBot"]