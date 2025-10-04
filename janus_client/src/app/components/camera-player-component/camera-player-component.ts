import { Component, ElementRef, input, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CameraService } from '../../services/camera-service';
import { NgFor, NgIf } from '@angular/common';
import { DeviceInfo, SignalingInfo, SignalrStreamService } from '../../services/signalr-stream.service';

@Component({
  selector: 'app-camera-player-component',
  standalone: true,
  imports: [NgIf, NgFor],
  templateUrl: './camera-player-component.html',
  styleUrls: ['./camera-player-component.css']
})
export class CameraPlayerComponent implements OnInit, OnDestroy {
  accessibleDevices: any[] = [];
  selectedDevice?: DeviceInfo;
  streamInfo?: SignalingInfo;
  status = 'Preparing...';
  constructor(private signalService: SignalrStreamService) {}
  ngOnDestroy(): void {
    throw new Error('Method not implemented.');
  }

  async ngOnInit() {
    const connected = await this.signalService.startConnection();
    if (!connected) {
      this.status = 'Failed to connect to the server';
      return;
    }
     await this.loadAccessibleDevices();
  }
private async loadAccessibleDevices(): Promise<void> {
    const result = await this.signalService.getMyDevices();
    if (result.success) {
      this.accessibleDevices = result.data!;
      this.status = `Loaded device ${this.accessibleDevices.length}`;
    } else {
      this.status = `  Load devices failed: ${result.error}`;
    }
  }

 
  async startStream(device: DeviceInfo) {
    
    this.selectedDevice = device;
    const result = await this.signalService.requestStreamInfo(device.id);

    if (result.success && result.data) {
      this.streamInfo = result.data;
      this.playStream(result.data);
    } else {
      console.error('⚠️ Failed to get stream info:', result.error);
    }
  }

  playStream(info: SignalingInfo) {
    const video = document.getElementById('video') as HTMLVideoElement;
    if (!video) return;

    if (info.protocol === 'hls') {
      /*if (Hls.isSupported()) {
        const hls = new Hls();
        hls.loadSource(info.streamUrl);
        hls.attachMedia(video);
        hls.on(Hls.Events.MANIFEST_PARSED, () => {
          video.play();
        });
      } else {
        video.src = info.streamUrl;
        video.play();
      }*/
      console.log('HLS stream (use Hls.js):', info.streamUrl);
    } 
    else if (info.protocol === 'rtsp') {
      console.log('RTSP stream (needs external player):', info.streamUrl);
    }
    else if( info.protocol === 'webrtc') {
      console.log('WebRTC stream (needs WebRTC handling):', info.streamUrl);
    }
  }
}