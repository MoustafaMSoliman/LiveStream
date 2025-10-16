FROM coturn/coturn:latest

# نسخ ملف الإعدادات
COPY turnserver.conf /etc/turnserver.conf

EXPOSE 3478 3478/udp
EXPOSE 5349 5349/udp
EXPOSE 49152-65535/udp

CMD ["turnserver"]