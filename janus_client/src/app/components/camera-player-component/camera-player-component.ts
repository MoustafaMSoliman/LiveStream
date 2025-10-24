import { Component, ElementRef, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms'; // ✅ هنا
import { WebRTCService } from '../../services/web-rtc-service';
import { StreamTokenService } from '../../services/stream-token-service';
import Hls from 'hls.js';
@Component({
  selector: 'app-camera-player',
  standalone: true,
  imports: [NgIf, NgFor, FormsModule], // ✅ ضيف FormsModule هنا
  templateUrl: './camera-player-component.html',
  styleUrls: ['./camera-player-component.css']
})
export class CameraPlayerComponent implements OnInit, OnDestroy {
  @ViewChild('videoElement') videoElement!: ElementRef<HTMLVideoElement>;
  @Input() cameraId: string = 'cam1';

  isPlaying = false;
  error = '';
  streamMode: 'webrtc' | 'hls' = 'webrtc';
  currentToken = '';

  constructor(
    private webrtcService: WebRTCService,
    private tokenService: StreamTokenService
  ) {}

  async ngOnInit() {
    this.tokenService.getStreamInfo(this.cameraId).subscribe(streamInfo => {
      if (streamInfo) {
        this.currentToken = streamInfo.accessToken;
      }
    });
  }

  async play(): Promise<void> {
    this.stop();
    this.isPlaying = true;
    this.error = '';

    if (this.streamMode === 'webrtc') {
      await this.webrtcService.playStream(this.cameraId, this.videoElement.nativeElement);
    } else {
      const hlsUrl = this.tokenService.getSecureHLSUrl(this.cameraId, this.currentToken);
      await this.playHls(hlsUrl);
    }
  }

  stop(): void {
    if (this.streamMode === 'webrtc') {
      this.webrtcService.stopStream(this.cameraId);
    } else if (this.videoElement?.nativeElement) {
      this.videoElement.nativeElement.pause();
      this.videoElement.nativeElement.src = '';
    }
    this.isPlaying = false;
  }

  async playHls(hlsUrl: string): Promise<void> {
    try {
      const video = this.videoElement.nativeElement;

      if (Hls.isSupported()) {
        const hls = new Hls();
        hls.loadSource(hlsUrl);
        hls.attachMedia(video);
        hls.on(Hls.Events.MANIFEST_PARSED, () => {
          video.play();
        });
      } else if (video.canPlayType('application/vnd.apple.mpegurl')) {
        video.src = hlsUrl;
        await video.play();
      } else {
        this.error = 'HLS not supported in this browser.';
      }
    } catch (err) {
      this.error = 'Failed to play HLS: ' + (err as Error).message;
    }
  }

  ngOnDestroy(): void {
    this.stop();
  }
}
