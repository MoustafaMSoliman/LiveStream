FROM debian:bullseye

ENV JANUS_VERSION=main
ENV PREFIX=/opt/janus

# المتطلبات
RUN apt-get update && apt-get install -y \
    git wget curl unzip build-essential automake libtool pkg-config cmake \
    gengetopt libmicrohttpd-dev libjansson-dev libssl-dev libsrtp2-dev \
    libsofia-sip-ua-dev libglib2.0-dev libopus-dev libogg-dev \
    libini-config-dev libcollection-dev libconfig-dev \
    libcurl4-openssl-dev liblua5.3-dev \
    libwebsockets-dev zlib1g-dev \
    meson ninja-build \
    && rm -rf /var/lib/apt/lists/*

# نبني libnice (مكتبة ICE)
WORKDIR /usr/local/src
RUN git clone https://gitlab.freedesktop.org/libnice/libnice.git && \
    cd libnice && \
    meson setup builddir --prefix=/usr && \
    ninja -C builddir && ninja -C builddir install

# نبني Janus
RUN git clone https://github.com/meetecho/janus-gateway.git && \
    cd janus-gateway && \
    sh autogen.sh && \
    ./configure --prefix=$PREFIX --enable-whip --enable-whep && \
    make -j$(nproc) && \
    make install && \
    make configs

# Expose ports (REST + WS + WSS + RTP/ICE)
EXPOSE 8088 8089 8188 8989 10000-10200/udp

# config
VOLUME ["/opt/janus/etc/janus"]

WORKDIR /opt/janus/bin
CMD ["./janus", "-F", "/opt/janus/etc/janus"]
