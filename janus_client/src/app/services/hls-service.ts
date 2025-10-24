import { Injectable } from '@angular/core';
import Hls from 'hls.js';
import { StreamInfo, StreamTokenService } from './stream-token-service';

@Injectable({
  providedIn: 'root'
})
export class HlsService {
  private hlsInstances = new Map<string, Hls>();

  constructor(private tokenService: StreamTokenService) {}

  /**
   * Start playing HLS stream for the specified camera.
   */
  async playStream(cameraId: string, videoElement: HTMLVideoElement): Promise<void> {
    // Stop existing stream if already playing
    this.stopStream(cameraId);

    // Subscribe to stream info (auto refresh when token updates)
    this.tokenService.getStreamInfo(cameraId).subscribe(async (streamInfo) => {
      if (!streamInfo) return;
      await this.startHlsStream(cameraId, streamInfo, videoElement);
    });
  }

  /**
   * Internal: Create and attach HLS instance.
   */
  private async startHlsStream(cameraId: string, streamInfo: StreamInfo, videoElement: HTMLVideoElement): Promise<void> {
    try {
      // Construct the secure HLS URL
      const hlsUrl = this.tokenService.getSecureHLSUrl(cameraId, streamInfo.accessToken);

      // Check browser HLS support
      if (Hls.isSupported()) {
        const hls = new Hls({
          maxBufferLength: 10,
          liveSyncDurationCount: 3,
          enableWorker: true,
          lowLatencyMode: true
        });

        this.hlsInstances.set(cameraId, hls);

        hls.loadSource(hlsUrl);
        hls.attachMedia(videoElement);

        hls.on(Hls.Events.MANIFEST_PARSED, () => {
          videoElement.play().catch((err) => console.warn('Autoplay blocked:', err));
        });

        hls.on(Hls.Events.ERROR, (event, data) => {
          console.error(`HLS error (${cameraId}):`, data);
          if (data.fatal) {
            switch (data.type) {
              case Hls.ErrorTypes.NETWORK_ERROR:
                console.warn('Trying to recover network error...');
                hls.startLoad();
                break;
              case Hls.ErrorTypes.MEDIA_ERROR:
                console.warn('Trying to recover media error...');
                hls.recoverMediaError();
                break;
              default:
                console.error('Fatal error, destroying HLS instance');
                this.stopStream(cameraId);
                break;
            }
          }
        });
      } else if (videoElement.canPlayType('application/vnd.apple.mpegurl')) {
        // Fallback for Safari / iOS
        videoElement.src = hlsUrl;
        await videoElement.play();
      } else {
        console.error('HLS not supported in this browser.');
      }
    } catch (error) {
      console.error('Failed to start HLS stream:', error);
      this.stopStream(cameraId);
    }
  }

  /**
   * Stop and clean up HLS stream for the given camera.
   */
  stopStream(cameraId: string): void {
    const hls = this.hlsInstances.get(cameraId);
    if (hls) {
      hls.destroy();
      this.hlsInstances.delete(cameraId);
    }
    this.tokenService.cleanup(cameraId);
  }
}
