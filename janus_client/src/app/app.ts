import { Component, signal } from '@angular/core';
import { CameraPlayerComponent } from './components/camera-player-component/camera-player-component';
import { JanusStream } from './components/janus-stream/janus-stream';
import { MultiLiveStreamComponent } from './components/multi-live-stream-component/multi-live-stream-component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    //CameraPlayerComponent, 
    MultiLiveStreamComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('janus_client');
}
