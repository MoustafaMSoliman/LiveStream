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

  peerConnection: RTCPeerConnection | null = null;
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
    console.log('🎬 Starting stream for:', device);
    this.selectedDevice = device;
    const result = await this.signalService.requestStreamInfo(device.id);
    console.log('📡 Stream info result:', result);
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
    // debugger;
    if (info.protocol.toLowerCase()  === 'hls') {
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
    else if (info.protocol.toLowerCase() === 'rtsp') {
      console.log('RTSP stream (needs external player):', info.streamUrl);
    }
    
    else if( info.protocol.toLowerCase()  === 'webrtc') {
      console.log('WebRTC stream (needs WebRTC handling):', info.streamUrl);
      this.playWebRTC(info.streamUrl, []);
    }
  }

  async playWebRTC(playUrl: string, iceServers: any[]) {
    console.log('🚀 Starting WebRTC stream from:', playUrl);
      const video = document.getElementById('video') as HTMLVideoElement;
      if (!video) {
           console.error('Video element not found');
           return;
      }

       // 1️⃣ Create RTCPeerConnection with ICE servers
       const pc = new RTCPeerConnection({ iceServers });
      console.log('🌐 RTCPeerConnection created with ICE servers:', iceServers);
       // 2️⃣ When remote track arrives, attach it to the video
       pc.ontrack = (event) => {
           console.log('🎥 Remote track received:', event);
           video.srcObject = event.streams[0];
      };

      // 3️⃣ Create an SDP offer
      const offer = await pc.createOffer();
      await pc.setLocalDescription(offer);

     // 4️⃣ Send the SDP offer to MediaMTX via HTTP POST
     const response = await fetch(playUrl, {
       method: 'POST',
       headers: { 'Content-Type': 'application/sdp' },
       body: offer.sdp!,
    });

     if (!response.ok) {
        console.error('Failed to connect to WebRTC stream:', response.statusText);
       return;
     }

     // 5️⃣ Receive the SDP answer and set it as the remote description
     const answer = await response.text();
     await pc.setRemoteDescription({ type: 'answer', sdp: answer });

     console.log('✅ WebRTC connection established');

     // 6️⃣ Auto-play the video when ready
     video.autoplay = true;
     video.playsInline = true;

     // 7️⃣ Handle cleanup when component is destroyed or stream ends
     this.peerConnection = pc;
    }

    stopWebRTC() {
      if (this.peerConnection) {
         this.peerConnection.close();
         this.peerConnection = null;
         console.log('🛑 WebRTC connection closed');
     }
    }

}