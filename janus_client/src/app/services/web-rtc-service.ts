import { Injectable } from '@angular/core';
import { StreamInfo, StreamTokenService } from './stream-token-service';

@Injectable({
  providedIn: 'root'
})
export class WebRTCService {
  private peerConnections = new Map<string, RTCPeerConnection>();

  constructor(private tokenService: StreamTokenService) {}

  async playStream(cameraId: string, videoElement: HTMLVideoElement): Promise<void> {
    // Clean up existing connection
    this.stopStream(cameraId);

    // Subscribe to token updates
    this.tokenService.getStreamInfo(cameraId).subscribe(async streamInfo => {
      if (streamInfo) {
        await this.startWebRTCConnection(cameraId, streamInfo, videoElement);
      }
    });
  }

  private async startWebRTCConnection(cameraId: string, streamInfo: StreamInfo, videoElement: HTMLVideoElement): Promise<void> {
    const peerConnection = new RTCPeerConnection({
        iceServers: [
            // STUN servers - للاتصالات المباشرة
            { urls: 'stun:62.241.148.115:3478' },
            //{ urls: 'stun:stun.l.google.com:19302' }, // احتياطي
            
            // TURN servers - للشبكات المقيدة
            { 
                urls: 'turn:62.241.148.115:3478',
                username: 'mediamtx',
                credential: 'your-strong-password-123'
            },
            { 
                urls: 'turns:62.241.148.115:5349',
                username: 'mediamtx',
                credential: 'your-strong-password-123'
            }
        ],
        iceTransportPolicy: 'all', // أو 'relay' للأمان الأعلى
        bundlePolicy: 'max-bundle',
        rtcpMuxPolicy: 'require'
    });

    this.peerConnections.set(cameraId, peerConnection);

    // Handle incoming video track
    peerConnection.ontrack = (event) => {
      if (event.streams && event.streams[0]) {
        videoElement.srcObject = event.streams[0];
        videoElement.play().catch(console.error);
      }
    };

    peerConnection.onconnectionstatechange = () => {
      console.log(`WebRTC connection state for ${cameraId}:`, peerConnection.connectionState);
    };

    try {
      // Add transceivers for receiving
      peerConnection.addTransceiver('video', { direction: 'recvonly' });
      peerConnection.addTransceiver('audio', { direction: 'recvonly' });

      // Create and send offer
      const offer = await peerConnection.createOffer();
      await peerConnection.setLocalDescription(offer);

      const whepUrl = this.tokenService.getSecureWebRTCUrl(cameraId, streamInfo.accessToken);
      
      const response = await fetch(whepUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/sdp',
        },
        body: offer.sdp
      });

      if (!response.ok) {
        throw new Error(`WebRTC connection failed: ${response.status}`);
      }

      const answerSdp = await response.text();
      await peerConnection.setRemoteDescription({
        type: 'answer',
        sdp: answerSdp
      });

      console.log(`WebRTC connected for ${cameraId}`);
    } catch (error) {
      console.error('WebRTC connection failed:', error);
      this.stopStream(cameraId);
    }
  }

  stopStream(cameraId: string): void {
    const peerConnection = this.peerConnections.get(cameraId);
    if (peerConnection) {
      peerConnection.close();
      this.peerConnections.delete(cameraId);
    }
    this.tokenService.cleanup(cameraId);
  }

}
