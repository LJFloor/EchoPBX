#############
# Backend
#############

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build

WORKDIR /src

COPY ./EchoPBX.Data/ ./EchoPBX.Data/
COPY ./EchoPBX.Web/ ./EchoPBX.Web/
COPY ./EchoPBX.Repositories/ ./EchoPBX.Repositories/
COPY ./EchoPBX.sln ./EchoPBX.sln

RUN dotnet restore EchoPBX.sln
RUN dotnet publish EchoPBX.Web -c Release -o /build
RUN rm /build/appsettings.Development.json

#############
# Frontend
#############

FROM node:24 AS frontend-build

ENV COREPACK_ENABLE_DOWNLOAD_PROMPT=0
RUN corepack enable

WORKDIR /src
COPY ./EchoPBX.Frontend/ .
RUN pnpm install --frozen-lockfile
RUN pnpm run build --outDir /build

#############
# Final Stage
#############

FROM ubuntu:24.04

EXPOSE 5060/udp
EXPOSE 8740/tcp
EXPOSE 10000-20000/udp

ENV DEBIAN_FRONTEND=noninteractive
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

RUN apt-get update && apt-get install -y \
    asterisk \
    ffmpeg \
    dotnet-runtime-8.0 \
    aspnetcore-runtime-8.0

RUN rm -rf /var/lib/apt/lists/*
RUN mkdir -p /data
RUN mkdir -p /data/sounds

WORKDIR /app
COPY --from=backend-build /build/ ./
COPY --from=frontend-build /build/ ./wwwroot/
COPY LICENSE ./wwwroot/license.txt

# everything in ./roofs should be copied to the root. So ./rootfs/etc/asterisk/* -> /etc/asterisk/*
COPY ./rootfs/ /

CMD ["dotnet", "EchoPBX.Web.dll"]
