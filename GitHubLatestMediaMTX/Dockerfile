# ---------- مرحلة البناء ----------
FROM golang:1.25 AS builder

WORKDIR /app

# نثبت الأدوات المطلوبة للبناء
RUN apt-get update && apt-get install -y make git bash curl && rm -rf /var/lib/apt/lists/*

# نسحب أحدث نسخة من المشروع من GitHub
RUN git clone https://github.com/bluenviron/mediamtx.git .

# نتحقق من أحدث tag أو commit
RUN git fetch --all && git checkout $(git describe --tags $(git rev-list --tags --max-count=1))

# نبني الملف التنفيذي الرسمي
RUN make mediamtx

# ---------- مرحلة التشغيل ----------
FROM debian:bookworm-slim

# نضيف أدوات مساعدة اختيارية
RUN apt-get update && apt-get install -y bash curl && rm -rf /var/lib/apt/lists/*

# ننسخ الملف التنفيذي النهائي فقط
COPY --from=builder /app/mediamtx /usr/local/bin/mediamtx

# نحدد نقطة التشغيل
ENTRYPOINT ["/usr/local/bin/mediamtx"]

# نستخدم config خارجي (تقدر تغيّره عند التشغيل)
CMD ["/mediamtx.yml"]
