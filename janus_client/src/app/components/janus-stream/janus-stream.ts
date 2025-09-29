
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import Janus from 'janus-gateway';
@Component({
  selector: 'app-janus-stream',
  standalone: true,
  imports: [],
  templateUrl: './janus-stream.html',
  styleUrl: './janus-stream.css'
})
export class JanusStream implements OnInit {
 @ViewChild('remoteVideo') remoteVideo!: ElementRef<HTMLVideoElement>;

  private janus: any;
  private pluginHandle: any;

  ngOnInit() {
    Janus.init({
      debug: "all",
      callback: () => {
        this.janus = new Janus({
          server: "http://localhost:8088/janus",   // أو ws://localhost:8188
          success: () => {
            this.janus.attach({
              plugin: "janus.plugin.streaming",
              success: (pluginHandle: any) => {
                this.pluginHandle = pluginHandle;   // نحفظه هنا
                console.log("Attached to plugin:", this.pluginHandle.getPlugin());
                
                // نطلب الـ stream بالـ ID
                this.pluginHandle.send({
                  message: {
                    request: "watch",
                    id: 20
                  }
                });
              },
              onmessage: (msg: any, jsep: any) => {
                console.log("Got message:", msg);
                if (jsep) {
                  this.pluginHandle.createAnswer({
                    jsep,
                    media: { audioSend: false, videoSend: false }, // نستقبل بس
                    success: (jsepAnswer: any) => {
                      this.pluginHandle.send({
                        message: { request: "start" },
                        jsep: jsepAnswer
                      });
                    },
                    error: (err: any) => {
                      console.error("WebRTC error:", err);
                    }
                  });
                }
              },
              onremotestream: (stream: MediaStream) => {
                console.log("Remote stream received:", stream);
                this.remoteVideo.nativeElement.srcObject = stream;
              },
              oncleanup: () => {
                console.log("Cleaned up");
              }
            });
          },
          error: (err: any) => {
            console.error("Janus init error:", err);
          },
          destroyed: () => {
            console.log("Janus destroyed");
          }
        });
      }
    });
  }
}