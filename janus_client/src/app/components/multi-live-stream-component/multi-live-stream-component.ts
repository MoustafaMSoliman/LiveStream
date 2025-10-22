import { Component, ElementRef, OnDestroy, OnInit, QueryList, ViewChildren } from '@angular/core';
import { WebRTCService } from '../../services/web-rtc-service';
import { NgFor, NgIf } from '@angular/common';
import { CameraService } from '../../services/camera-service';

@Component({
  selector: 'app-multi-live-stream-component',
  imports: [NgIf, NgFor],
  templateUrl: './multi-live-stream-component.html',
  styleUrl: './multi-live-stream-component.css'
})
export class MultiLiveStreamComponent implements OnInit, OnDestroy {
@ViewChildren('videoElement') videoElements!: QueryList<ElementRef<HTMLVideoElement>>;

  cameras = Array.from({ length: 16 }, (_, i) => ({ id: `cam${i + 1}` }));
  isPlaying = false;
  status = '';

  constructor(private webrtcService: WebRTCService, private cameraService:CameraService) {}

  ngOnInit() {
    this.status = 'Ready to play 16 streams.';
  }

  async playAll(): Promise<void> {
    this.status = 'Starting all streams...';
    const videoList = this.videoElements.toArray();

    for (let i = 0; i < this.cameras.length; i++) {
      const camera = this.cameras[i];
      const videoEl = videoList[i].nativeElement;

      try {
        await this.webrtcService.playStream(camera.id, videoEl);
        console.log(`✅ Started ${camera.id}`);
      } catch (error) {
        console.error(`❌ Failed to start ${camera.id}`, error);
      }
    }

    this.isPlaying = true;
    this.status = 'All streams started';
  }

  stopAll(): void {
    for (const camera of this.cameras) {
      this.webrtcService.stopStream(camera.id);
    }

    this.videoElements.forEach(v => (v.nativeElement.srcObject = null));
    this.isPlaying = false;
    this.status = 'All streams stopped';
  }

/*addCamera(cameraName: string, source: string) {
  return this.cameraService.addCamera(cameraName,source);
  
}*/

getAllCameras() {
  return this.cameraService.getAllCameras();
}


  ngOnDestroy(): void {
    this.stopAll();
  }
}
