import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';

export interface SignalingInfo {
  streamUrl: string;
  protocol: string; // "hls" | "rtsp" | "webrtc"
  deviceId: number;
  deviceName: string;
}

export interface DeviceInfo {
  id: number;
  name: string;
  description: string;
  isOnline: boolean;
  canView: boolean;
  location: string;
  viewerCount: number;
  status: string;
}

export interface HubResult<T> {
  success: boolean;
  error?: string;
  data?: T;
}

@Injectable({
  providedIn: 'root'
})
export class SignalrStreamService {
  private hubConnection!: signalR.HubConnection;
  private connectionState = new BehaviorSubject<boolean>(false);
  private accessibleDevices = new BehaviorSubject<DeviceInfo[]>([]);

  constructor() {
    this.initializeConnection();
  }

  private initializeConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7239/streamhub')
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.registerHubEvents();
  }

  private registerHubEvents(): void {
    this.hubConnection.on('accessibleDevices', (devices: DeviceInfo[]) => {
      this.accessibleDevices.next(devices);
    });

    this.hubConnection.on('viewerNotification', (notification: any) => {
      console.log('📢 إشعار:', notification);
    });

    this.hubConnection.on('heartbeatack', (response: any) => {
      console.log('✅ Heartbeat acknowledged:', response);
    });

    this.hubConnection.onreconnected(() => {
      console.log('🔄 SignalR reconnected');
      this.connectionState.next(true);
    });

    this.hubConnection.onclose(() => {
      console.log('❌ SignalR disconnected');
      this.connectionState.next(false);
    });
  }

  public async startConnection(): Promise<boolean> {
    try {
      await this.hubConnection.start();
      this.connectionState.next(true);
      console.log('✅ Connected to SignalR');
      return true;
    } catch (err) {
      console.error('❌ Failed to connect:', err);
      this.connectionState.next(false);
      return false;
    }
  }

  public async getMyDevices(): Promise<HubResult<DeviceInfo[]>> {
    try {
      return await this.hubConnection.invoke<HubResult<DeviceInfo[]>>('GetMyDevices');
    } catch (error) {
      return { success: false, error: (error as Error).message };
    }
  }

  public async requestStreamInfo(deviceId: number): Promise<HubResult<SignalingInfo>> {
    try {
      return await this.hubConnection.invoke<HubResult<SignalingInfo>>('RequestSignalingInfo', deviceId);
    } catch (error) {
      return { success: false, error: (error as Error).message };
    }
  }

  public async sendHeartbeat(): Promise<void> {
    if (this.hubConnection.state === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('Heartbeat');
      } catch (error) {
        console.warn('⚠️ Heartbeat failed:', error);
      }
    }
  }

  public getAccessibleDevices() {
    return this.accessibleDevices.asObservable();
  }
}
