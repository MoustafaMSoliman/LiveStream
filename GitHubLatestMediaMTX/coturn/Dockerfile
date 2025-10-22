FROM coturn/coturn:latest

#USER root

# نسخ ملف الإعدادات
COPY turnserver.conf /etc/turnserver.conf

# إنشاء مجلد للسجلات
#RUN mkdir -p /var/log/coturn && chown -R coturn:coturn /var/log/coturn

#USER coturn

EXPOSE 3478 3478/udp
EXPOSE 5349 5349/udp
EXPOSE 49152-65535/udp

CMD ["turnserver", "-c", "/etc/turnserver.conf"]