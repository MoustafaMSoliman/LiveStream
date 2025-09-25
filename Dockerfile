FROM debian:bullseye

ENV PREFIX=/opt/janus
ENV JANUS_VERSION=1.2.5

# المتطلبات الأساسية
RUN apt-get update && apt-get install -y \
    wget git build-essential automake libtool pkg-config \
    libmicrohttpd-dev libjansson-dev libssl-dev libsrtp2-dev \
    libsofia-sip-ua-dev libglib2.0-dev libopus-dev libogg-dev \
    libini-config-dev libcollection-dev libconfig-dev \
    libcurl4-openssl-dev liblua5.3-dev \
    libwebsockets-dev zlib1g-dev \
    meson ninja-build cmake \
    && rm -rf /var/lib/apt/lists/*

# نبني libnice (مكتبة ICE)
WORKDIR /usr/local/src
RUN git clone https://gitlab.freedesktop.org/libnice/libnice.git && \
    cd libnice && \
    meson setup builddir --prefix=/usr && \
    ninja -C builddir && ninja -C builddir install && \
    ldconfig

# تحميل Janus باستخدام الرابط الصحيح
WORKDIR /usr/local/src
RUN wget https://github.com/meetecho/janus-gateway/releases/download/v${JANUS_VERSION}/janus-gateway-${JANUS_VERSION}.tar.gz && \
    tar xzf janus-gateway-${JANUS_VERSION}.tar.gz && \
    cd janus-gateway-${JANUS_VERSION} && \
    ./configure --prefix=${PREFIX} \
        --enable-post-processing \
        --enable-rest \
        --enable-data-channels \
        --enable-websockets \
        --enable-whip \
        --enable-whep \
        --enable-plugin-audiobridge \
        --enable-plugin-videoroom \
        --enable-plugin-videocall \
        --enable-plugin-textroom \
        --enable-plugin-echotest \
        --enable-plugin-recordplay \
        --enable-plugin-streaming && \
    make -j$(nproc) && \
    make install && \
    make configs

# تنظيف الملفات المؤقتة
RUN rm -rf /usr/local/src/*

# Ports (REST + WS + WSS + RTP/ICE)
EXPOSE 8088 8089 8188 8989 10000-10200/udp

# config
VOLUME ["/opt/janus/etc/janus"]

WORKDIR /opt/janus/bin
CMD ["./janus", "-F", "/opt/janus/etc/janus"]