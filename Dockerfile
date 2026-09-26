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
# G.729 codec
#############

# Ubuntu's Asterisk has no G.729 codec, so build the open source asterisk-g72x module against
# Bcg729 and the headers of the same Asterisk package the final stage installs.
FROM ubuntu:24.04 AS g729-build

ENV DEBIAN_FRONTEND=noninteractive
ARG G72X_COMMIT=5106838f3aabf8fe09fa3780a8e9d3ed4c2a0063

RUN apt-get update && apt-get install -y \
    build-essential autoconf automake libtool pkg-config git ca-certificates \
    asterisk-dev libbcg729-dev

WORKDIR /src
RUN git clone https://github.com/arkadijs/asterisk-g72x.git . && git checkout $G72X_COMMIT

# Setting CFLAGS keeps configure from adding -march=native, which would tie the module to the
# CPU of the machine that built the image
RUN ./autogen.sh && ./configure --with-bcg729 CFLAGS="-O2" && make
RUN mkdir /build && cp .libs/codec_g729.so /build/

#############
# Final Stage
#############

FROM ubuntu:24.04

EXPOSE 5060/udp
EXPOSE 8740/tcp
EXPOSE 8741/tcp
EXPOSE 10000-20000/udp

ENV DEBIAN_FRONTEND=noninteractive
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

RUN apt-get update && apt-get install -y \
    asterisk \
    asterisk-core-sounds-en-g722 \
    libbcg729-0 \
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

COPY --from=g729-build /build/codec_g729.so /tmp/
RUN mv /tmp/codec_g729.so /usr/lib/$(uname -m)-linux-gnu/asterisk/modules/

CMD ["dotnet", "EchoPBX.Web.dll"]
