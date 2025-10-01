import { Component, ElementRef, input, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CameraService } from '../../services/camera-service';
import { NgFor, NgIf } from '@angular/common';
import { SignalingInfo, SignalrStreamService } from '../../services/signalr-stream.service';
declare const Janus: any;
@Component({
  selector: 'app-camera-player-component',
  standalone: true,
  imports: [NgIf, NgFor],
  templateUrl: './camera-player-component.html',
  styleUrls: ['./camera-player-component.css']
})
export class CameraPlayerComponent implements OnInit, OnDestroy {
  @ViewChild('videoElement') videoRef!: ElementRef<HTMLVideoElement>;
  @Input() cameraId!: number;
  accessibleDevices: any[] = [];
  selectedDeviceId: number | null = null;
  currentDevice: any = null;
  status = 'Preparing...';
  showPlayButton = false;
  isConnected = false;
  showDevicesPanel: boolean = true;
  private janus: any;
  private pluginHandle: any;
  private streamSessionId: number | null = null;
  private heartbeatInterval: any;

  constructor(
    private streamService: SignalrStreamService,
    //private authService: AuthService
  ) {}

  async ngOnInit(): Promise<void> {

    const connected = await this.streamService.startConnection();
    if (!connected) {
      this.status = 'Failed to connect to the server';
      return;
    }


    await this.loadAccessibleDevices();
    

    this.startHeartbeat();
    
    this.status = 'Select device to show stream';
  }

  private async loadAccessibleDevices(): Promise<void> {
    const result = await this.streamService.getMyDevices();
    if (result.success) {
      this.accessibleDevices = result.data!;
      this.status = `Loaded device ${this.accessibleDevices.length}`;
    } else {
      this.status = `  Load devices failed: ${result.error}`;
    }
  }

  async selectDevice(device: any): Promise<void> {
    if (!device.isOnline || !device.canView) {
      this.status = 'This device can not be shown';
      return;
    }

    this.selectedDeviceId = device.id;
    this.currentDevice = device;
    this.status = 'Stream connecting...';

    try {
      const signalingResult = await this.streamService.requestSignalingInfo(device.id);
      
      if (!signalingResult.success) {
        this.status = `Permissions wrong  : ${signalingResult.error}`;
        return;
      }

      const watchResult = await this.streamService.startWatching(device.id);
      if (!watchResult.success) {
        this.status = `Start watching failed  : ${watchResult.error}`;
        return;
      }

      this.streamSessionId = watchResult.data!;

      await this.initializeJanus(signalingResult.data!);

    } catch (error) {
      this.status = `Connection failure  : ${error}`;
      console.error('Device selection error:', error);
    }
  }

  private async initializeJanus(signalingInfo: SignalingInfo): Promise<void> {
    Janus.init({
      debug: false,
      callback: () => {
        this.janus = new Janus({
          server: signalingInfo.janusWebSocketUrl,
          iceServers: signalingInfo.iceServers,
          success: () => {
            this.isConnected = true;
            this.status = 'Plaing Stream...';
            this.attachToStreaming(signalingInfo.mountpointId);
          },
          error: (error: any) => {
            this.status = `Janus connection failure: ${error.message}`;
            console.error('Janus connection error:', error);
          },
          destroyed: () => {
            this.isConnected = false;
            this.status = 'Connection is closed';
          }
        });
      }
    });
  }

  private attachToStreaming(mountpointId: number): void {
    this.janus.attach({
      plugin: "janus.plugin.streaming",
      success: (pluginHandle: any) => {
        this.pluginHandle = pluginHandle;
        this.status = 'جاري طلب البث...';

        // طلب مشاهدة الـ mountpoint المحدد
        const watchRequest = { request: "watch", id: mountpointId };
        this.pluginHandle.send({ message: watchRequest });
      },
      error: (error: any) => {
        this.status = `فشل الاشتراك في البث: ${error}`;
        console.error('Plugin attachment error:', error);
      },
      onmessage: (msg: any, jsep: any) => {
        if (msg.error) {
          this.status = `خطأ في البث: ${msg.error}`;
          return;
        }

        if (jsep) {
          // إنشاء إجابة WebRTC
          this.pluginHandle.createAnswer({
            jsep: jsep,
            media: { 
              audioSend: false, 
              videoSend: false, 
              audioRecv: false, 
              videoRecv: true 
            },
            success: (jsepAnswer: any) => {
              this.pluginHandle.send({ 
                message: { request: "start" }, 
                jsep: jsepAnswer 
              });
              this.status = 'جاري تشغيل البث...';
            },
            error: (error: any) => {
              this.status = `خطأ في WebRTC: ${error}`;
            }
          });
        }
      },
      onremotetrack: (track: MediaStreamTrack, mid: string, on: boolean) => {
        this.handleRemoteTrack(track, mid, on);
      },
      iceState: (state: string) => {
        console.log('ICE connection state:', state);
      }
    });
  }

  private handleRemoteTrack(track: MediaStreamTrack, mid: string, on: boolean): void {
    const videoEl = this.videoRef.nativeElement;
    
    if (!on) {
      // تنظيف الـ track عند إيقافه
      if (videoEl.srcObject) {
        const stream = videoEl.srcObject as MediaStream;
        const existingTrack = stream.getTracks().find(t => t.id === track.id);
        if (existingTrack) {
          stream.removeTrack(existingTrack);
        }
      }
      return;
    }

    // إضافة الـ track الجديد
    let stream = videoEl.srcObject as MediaStream;
    if (!stream) {
      stream = new MediaStream();
      videoEl.srcObject = stream;
    }

    if (!stream.getTracks().some(t => t.id === track.id)) {
      console.log('تم إضافة track فيديو جديد');
      stream.addTrack(track);
      
      this.showPlayButton = true;
      this.status = 'البث جاهز - انقر للتشغيل';
    }
  }

  playVideo(): void {
    const videoEl = this.videoRef.nativeElement;
    videoEl.play().then(() => {
      this.showPlayButton = false;
      this.status = 'جاري التشغيل...';
    }).catch(err => {
      this.status = 'فشل التشغيل - انقر للمحاولة مرة أخرى';
      console.warn('فشل التشغيل التلقائي:', err);
    });
  }

  togglePlay(): void {
    const videoEl = this.videoRef.nativeElement;
    if (videoEl.paused) {
      videoEl.play();
    } else {
      videoEl.pause();
    }
  }

  private startHeartbeat(): void {
    // إرسال نبض كل 30 ثانية للحفاظ على الجلسة
    this.heartbeatInterval = setInterval(() => {
      this.streamService.sendHeartbeat();
    }, 30000);
  }

  getStatusClass(): string {
    if (this.status.includes('فشل') || this.status.includes('خطأ')) {
      return 'status-error';
    } else if (this.status.includes('جاهز') || this.status.includes('جاري التشغيل')) {
      return 'status-success';
    } else {
      return 'status-info';
    }
  }

  async ngOnDestroy(): Promise<void> {
    // تنظيف الموارد
    if (this.heartbeatInterval) {
      clearInterval(this.heartbeatInterval);
    }

    // إيقاف مشاهدة الجهاز الحالي
    if (this.selectedDeviceId) {
      await this.streamService.stopWatching(this.selectedDeviceId);
    }

    // إغلاق اتصال Janus
    if (this.pluginHandle) {
      try { this.pluginHandle.detach(); } catch (e) {}
    }
    if (this.janus) {
      try { this.janus.destroy(); } catch (e) {}
    }

    // إيقاف اتصال SignalR
    await this.streamService.stopConnection();
  }
}