import { Component, ElementRef, OnDestroy, OnInit, QueryList, ViewChildren } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WebRTCService } from '../../services/web-rtc-service';
import { HlsService } from '../../services/hls-service';
import { CameraService } from '../../services/camera-service';

@Component({
  selector: 'app-multi-live-stream-component',
  standalone: true,
  imports: [NgIf, NgFor, FormsModule],
  templateUrl: './multi-live-stream-component.html',
  styleUrl: './multi-live-stream-component.css'
})
export class MultiLiveStreamComponent implements OnInit, OnDestroy {
  @ViewChildren('videoElement') videoElements!: QueryList<ElementRef<HTMLVideoElement>>;

  cameras = Array.from({ length: 16 }, (_, i) => ({ id: `cam${i + 1}` }));
  isPlaying = false;
  status = '';
  streamMode: 'webrtc' | 'hls' = 'webrtc';

  constructor(
    private webrtcService: WebRTCService,
    private hlsService: HlsService,
    private cameraService: CameraService
  ) {}

  ngOnInit(): void {
    this.status = 'Ready to play 16 streams.';
  }

  async playAll(): Promise<void> {
    this.status = `Starting all streams in ${this.streamMode.toUpperCase()} mode...`;
    const videoList = this.videoElements.toArray();

    for (let i = 0; i < this.cameras.length; i++) {
      const camera = this.cameras[i];
      const videoEl = videoList[i].nativeElement;

      try {
        if (this.streamMode === 'webrtc') {
          await this.webrtcService.playStream(camera.id, videoEl);
        } else {
          await this.hlsService.playStream(camera.id, videoEl);
        }
        console.log(`✅ Started ${camera.id}`);
      } catch (error) {
        console.error(`❌ Failed to start ${camera.id}`, error);
      }
    }

    this.isPlaying = true;
    this.status = `All ${this.streamMode.toUpperCase()} streams started`;
  }

  stopAll(): void {
    for (const camera of this.cameras) {
      if (this.streamMode === 'webrtc') {
        this.webrtcService.stopStream(camera.id);
      } else {
        this.hlsService.stopStream(camera.id);
      }
    }

    this.videoElements.forEach(v => {
      const video = v.nativeElement;
      video.pause();
      video.src = '';
      video.srcObject = null;
    });

    this.isPlaying = false;
    this.status = 'All streams stopped';
  }

  getAllCameras() {
    return this.cameraService.getAllCameras();
  }

  ngOnDestroy(): void {
    this.stopAll();
  }
}
