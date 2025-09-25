import { Component, ElementRef, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CameraService } from '../../services/camera-service';
import { NgIf } from '@angular/common';
declare const Janus: any;
@Component({
  selector: 'app-camera-player-component',
  standalone: true,
  imports: [NgIf],
  templateUrl: './camera-player-component.html',
  styleUrls: ['./camera-player-component.css']
})
export class CameraPlayerComponent implements OnInit, OnDestroy {
  @Input() cameraId!: number;
  @ViewChild('video') videoRef!: ElementRef<HTMLVideoElement>;
  private janus: any;
  private pluginHandle: any;
  status = 'WWW';

  constructor(private cameraService: CameraService) {}

  ngOnInit(): void {
    if (!this.cameraId) { this.status = 'No cameraId'; return; }
    // init Janus (optional debug)
    Janus.init({debug: false, callback: () => this.start()});
  }

  private async start() {
    this.status = 'Requesting camera info...';
    this.cameraService.getCameraInfo(this.cameraId).subscribe(info => {
      this.status = 'Connecting to Janus...';
      this.janus = new Janus({
        server: info.janusWs,
        success: () => this.attachToStreaming(info),
        error: (err: any) => { this.status = 'Janus connection error'; console.error(err); },
        destroyed: () => { this.status = 'Janus destroyed'; }
      });
    }, err => {
      console.error(err);
      this.status = 'Failed to get camera info';
    });
  }

  private attachToStreaming(info: { janusWs: string, mountId: number }) {
    this.janus.attach({
      plugin: "janus.plugin.streaming",
      success: (pluginHandle: any) => {
        this.pluginHandle = pluginHandle;
        this.status = 'Attached to streaming plugin, watching...';
        const body = { request: "watch", id: info.mountId };
        this.pluginHandle.send({ message: body });
      },
      error: (err: any) => { console.error("attach error", err); this.status = 'Attach error'; },
      onmessage: (msg: any, jsep: any) => {
        // handle Janus messages
        // Important: when Janus replies with a jsep (offer), create answer
        if (jsep) {
          this.pluginHandle.createAnswer({
            jsep: jsep,
            media: { audioSend: false, videoSend: false }, // viewer only
            success: (jsepAnswer: any) => {
              this.pluginHandle.send({ message: { request: "start" }, jsep: jsepAnswer });
            },
            error: (err: any) => {
              console.error("createAnswer error", err);
              this.status = 'WebRTC error';
            }
          });
        } else {
          // other plugin messages: e.g., "slow link", "starting", etc.
          console.log("plugin msg", msg);
        }
      },
      onremotestream: (stream: MediaStream) => {
        // janus.js provides onremotestream with the MediaStream
        const videoEl = this.videoRef.nativeElement;
        videoEl.srcObject = stream;
        videoEl.play().catch(()=>{});
        this.status = '';
      },
      oncleanup: () => {
        this.status = 'Cleaned up';
      }
    });
  }

  ngOnDestroy(): void {
    if (this.pluginHandle) {
      try { this.pluginHandle.send({ message: { request: "stop" } }); } catch {}
      try { this.pluginHandle.detach(); } catch {}
    }
    if (this.janus) try { this.janus.destroy(); } catch {}
  }
}