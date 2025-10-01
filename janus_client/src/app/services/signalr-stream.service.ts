import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';

export interface SignalingInfo {
  janusWebSocketUrl: string;
  mountpointId: number;
  deviceId: number;
  deviceName: string;
  iceServers: IceServer[];
}

export interface IceServer {
  urls: string;
  username?: string;
  credential?: string;
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
  private signalingInfo = new Subject<SignalingInfo>();
  private sessionCleanup = new Subject<void>();

  constructor() {
    this.initializeConnection();
  }

  private initializeConnection(): void {
    
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7239/streamhub'
        //, {
        //accessTokenFactory: () => this.getAccessToken(),
       // withCredentials: true // مهم للمصادقة بـ Cookies
      //}
    )
      .withAutomaticReconnect(
       // {
        //nextRetryDelayInMilliseconds: (retryContext: { previousRetryCount: number; }) => {
          // إعادة الاتصال بعد 1, 2, 4, 8, 16, 32 ثانية ثم كل 32 ثانية
         // return Math.min(32000, 1000 * Math.pow(2, retryContext.previousRetryCount));
       // }
      //}
    )
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.registerHubEvents();
  }
/*
  private getAccessToken(): string {
   
    const token = localStorage.getItem('access_token');
    
    return token ? token.replace('Bearer ', '') : '';
  }
*/
  private registerHubEvents(): void {
    this.hubConnection.on('accessibleDevices', (devices: DeviceInfo[]) => {
        this.accessibleDevices.next(devices);
    });

    this.hubConnection.on('viewerNotification', (notification: any) => {
        console.log('إشعار:', notification);
    });

    // إضافة استماع لـ heartbeatack
    this.hubConnection.on('heartbeatack', (response: any) => {
        console.log('✅ Signal approved ', response);
    });

    this.hubConnection.on('testEvent', (data) => {
        console.log('✅ Received event:', data);
    });
    this.hubConnection.onreconnected(() => {
        console.log('SignalR reconnect');
        this.connectionState.next(true);
    });

    this.hubConnection.onclose(() => {
        console.log('SignalR disconnect');
        this.connectionState.next(false);
    });
}

  // بدء الاتصال بـ SignalR
  public async startConnection(): Promise<boolean> {
    try {
      await this.hubConnection.start();
      this.connectionState.next(true);
      console.log('✅ Successful connected to SignalR');
      return true;
    } catch (err) {
      console.error('❌ Failed to connect to SignalR:', err);
      this.connectionState.next(false);
      return false;
    }
  }

  // طلب معلومات الإشارة لجهاز معين
  public async requestSignalingInfo(deviceId: number): Promise<HubResult<SignalingInfo>> {
    try {
      const result = await this.hubConnection.invoke<HubResult<SignalingInfo>>(
        'RequestSignalingInfo', 
        deviceId
      );
      return result;
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error'
      };
    }
  }

  // الحصول على الأجهزة المتاحة للمستخدم
  public async getMyDevices(): Promise<HubResult<DeviceInfo[]>> {
    try {
      return await this.hubConnection.invoke<HubResult<DeviceInfo[]>>('GetMyDevices');
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error'
      };
    }
  }

  // بدء مشاهدة جهاز معين
  public async startWatching(deviceId: number): Promise<HubResult<number>> {
    try {
      return await this.hubConnection.invoke<HubResult<number>>('StartWatching', deviceId);
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error'
      };
    }
  }

  // إيقاف مشاهدة جهاز
  public async stopWatching(deviceId: number): Promise<HubResult<void>> {
    try {
      return await this.hubConnection.invoke<HubResult<void>>('StopWatching', deviceId);
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error'
      };
    }
  }

  // نبض للحفاظ على الجلسة
  public async sendHeartbeat(): Promise<void> {
    if (this.hubConnection.state === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('Heartbeat');
      } catch (error) {
        console.warn('Failed to send signal: ', error);
      }
    }
  }

  // إغلاق الاتصال
  public async stopConnection(): Promise<void> {
    try {
      await this.hubConnection.stop();
      this.connectionState.next(false);
    } catch (error) {
      console.error('Error stopping connection:', error);
    }
  }

  // الحصول على حالة الاتصال كمشاهد observable
  public getConnectionState() {
    return this.connectionState.asObservable();
  }

  // الحصول على الأجهزة المتاحة كمشاهد observable
  public getAccessibleDevices() {
    return this.accessibleDevices.asObservable();
  }
}
