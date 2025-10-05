import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class WebRTCService {
  private peerConnection: RTCPeerConnection | null = null;

  async playStream(streamUrl: string, videoElement: HTMLVideoElement): Promise<void> {
    this.stopStream();

    console.log('Creating WebRTC connection to:', streamUrl);

    this.peerConnection = new RTCPeerConnection({
      iceServers: [
        { urls: 'stun:stun.l.google.com:19302' }
      ]
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

    this.peerConnection.oniceconnectionstatechange = () => {
      console.log('ICE connection state:', this.peerConnection?.iceConnectionState);
    };

    this.peerConnection.onconnectionstatechange = () => {
      console.log('Connection state:', this.peerConnection?.connectionState);
    };

    try {
      // Add transceivers for receiving
      this.peerConnection.addTransceiver('video', { direction: 'recvonly' });
      this.peerConnection.addTransceiver('audio', { direction: 'recvonly' });

      // Create offer
      const offer = await this.peerConnection.createOffer();
      console.log('Created offer:', offer.type);
      
      await this.peerConnection.setLocalDescription(offer);
      console.log('Set local description');

      // Send offer to WHEP endpoint
      console.log('Sending SDP offer to:', streamUrl);
      const response = await fetch(streamUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/sdp',
        },
        body: offer.sdp
      });

      console.log('Response status:', response.status, response.statusText);
      
      if (!response.ok) {
        const errorText = await response.text();
        console.error('Server response:', errorText);
        throw new Error(`HTTP ${response.status}: ${errorText}`);
      }

      const answerSdp = await response.text();
      console.log('Received SDP answer:', answerSdp.substring(0, 100) + '...');

      await this.peerConnection.setRemoteDescription({
        type: 'answer',
        sdp: answerSdp
      });

      console.log('WebRTC connection established successfully');

    } catch (error) {
      console.error('WebRTC connection failed:', error);
      this.stopStream();
      throw error;
    }
  }

  stopStream(): void {
    if (this.peerConnection) {
      this.peerConnection.close();
      this.peerConnection = null;
      console.log('WebRTC connection closed');
    }
  }
  

}
