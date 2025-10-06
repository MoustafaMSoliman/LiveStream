import { Component, ElementRef, input, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CameraService } from '../../services/camera-service';
import { NgFor, NgIf } from '@angular/common';
import { DeviceInfo, SignalingInfo, SignalrStreamService } from '../../services/signalr-stream.service';
import { WebRTCService } from '../../services/web-rtc-service';

@Component({
  selector: 'app-camera-player-component',
  standalone: true,
  imports: [NgIf, NgFor],
  templateUrl: './camera-player-component.html',
  styleUrls: ['./camera-player-component.css']
})
export class CameraPlayerComponent implements OnInit, OnDestroy {
  @ViewChild('videoElement') videoElement!: ElementRef<HTMLVideoElement>;
  @Input() cameraId: string = 'cam1';
  isPlaying = false;
  error = '';

  accessibleDevices: any[] = [];
  selectedDevice?: DeviceInfo;
  streamInfo?: SignalingInfo;
  status = 'Preparing...';

  peerConnection: RTCPeerConnection | null = null;
  constructor(private webrtcService: WebRTCService, private signalService: SignalrStreamService) {}
 

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

  async play(): Promise<void> {
    try {
      this.status = 'Connecting...';
      await this.webrtcService.playStream(this.cameraId, this.videoElement.nativeElement);
      this.isPlaying = true;
      this.status = 'Connected';
      
      // Clear status after 3 seconds
      setTimeout(() => this.status = '', 3000);
    } catch (error) {
      this.status = 'Connection failed';
      this.isPlaying = false;
    }
  }
  stop(): void {
    this.webrtcService.stopStream(this.cameraId);
    this.isPlaying = false;
    this.status = 'Stopped';
    
    if (this.videoElement.nativeElement.srcObject) {
      this.videoElement.nativeElement.srcObject = null;
    }
  }
async playVideo(): Promise<void> {
  try {
    this.error = '';
    await this.webrtcService.playStream('http://localhost:8889/cam1/whep', this.videoElement.nativeElement);
    this.isPlaying = true;
  } catch (err) {
    this.error = 'Failed to play WebRTC stream: ' + (err as Error).message;
    this.isPlaying = false;
  }
}

  stopVideo(): void {
    this.webrtcService.stopStream(this.cameraId);
    this.videoElement.nativeElement.srcObject = null;
    this.isPlaying = false;
    this.error = '';
  }

  ngOnDestroy(): void {
    this.stopVideo();
  }
 
  async startStream(device: DeviceInfo) {
    console.log('🎬 Starting stream for:', device);
    this.selectedDevice = device;
    const result = await this.signalService.requestStreamInfo(device.id);
    console.log('📡 Stream info result:', result);
    if (result.success && result.data) {
      this.streamInfo = result.data;
      this.webrtcService.playStream('http://localhost:8889/cam1/whep', this.videoElement.nativeElement);
    } else {
      console.error('⚠️ Failed to get stream info:', result.error);
    }
  }
/*
 async playStream(videoElement: HTMLVideoElement): Promise<void> {
  this.stopStream();

  // Use the correct WHEP URL
  const whepUrl = 'http://localhost:8889/whep/cam1';
  console.log('Creating WebRTC connection to:', whepUrl);

  this.peerConnection = new RTCPeerConnection({
    iceServers: [{ urls: 'stun:stun.l.google.com:19302' }]
  });

  // Handle incoming tracks
  this.peerConnection.ontrack = (event) => {
    console.log('WebRTC: Received track:', event.track.kind);
    if (event.streams && event.streams[0]) {
      videoElement.srcObject = event.streams[0];
      videoElement.play().catch(e => {
        console.error('Video play failed:', e);
      });
    }
  };

  // Add connection state monitoring
  this.peerConnection.onconnectionstatechange = () => {
    console.log('Connection state:', this.peerConnection?.connectionState);
  };

  try {
    // Add transceivers for receiving video/audio
    this.peerConnection.addTransceiver('video', { direction: 'recvonly' });
    this.peerConnection.addTransceiver('audio', { direction: 'recvonly' });

    // Create and send offer
    const offer = await this.peerConnection.createOffer();
    await this.peerConnection.setLocalDescription(offer);

    console.log('Sending SDP offer to WHEP endpoint...');
    const response = await fetch(whepUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/sdp',
      },
      body: offer.sdp
    });

    console.log('WHEP Response status:', response.status);
    
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`WHEP server returned ${response.status}: ${errorText}`);
    }

    const answerSdp = await response.text();
    await this.peerConnection.setRemoteDescription({
      type: 'answer',
      sdp: answerSdp
    });

    console.log('WebRTC WHEP connection established!');

  } catch (error) {
    console.error('WebRTC WHEP connection failed:', error);
    //this.stopStream();
    throw error;
  }
}
*/
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