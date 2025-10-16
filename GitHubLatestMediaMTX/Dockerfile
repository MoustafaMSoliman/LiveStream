# ======================
# Stage 1: Build Stage
# ======================
FROM golang:1.25 AS builder

# Install dependencies
RUN apt-get update && apt-get install -y git build-essential && rm -rf /var/lib/apt/lists/*

# Set working directory inside container
WORKDIR /app

# Copy local MediaMTX source from the subfolder
COPY ./mediamtx ./

# Ensure VERSION file exists (some builds depend on it)
RUN echo "1.0.0" > VERSION

# Fetch dependencies and build
RUN go mod download
RUN go build -o mediamtx .

# ======================
# Stage 2: Runtime Stage
# ======================
FROM debian:bookworm-slim

# Optional: install ffmpeg or other runtime tools
RUN apt-get update && apt-get install -y ffmpeg && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy built binary + config
COPY --from=builder /app/mediamtx /usr/local/bin/mediamtx
COPY ./mediamtx/mediamtx.yml /app/mediamtx.yml

# Expose ports (RTSP, HTTP, RTMP, API)
EXPOSE 8554 8888 1935 9997

CMD ["/usr/local/bin/mediamtx", "/app/mediamtx.yml"]
