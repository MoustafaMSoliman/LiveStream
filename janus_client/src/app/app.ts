import { Component, signal } from '@angular/core';
import { CameraPlayerComponent } from './components/camera-player-component/camera-player-component';
import { JanusStream } from './components/janus-stream/janus-stream';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CameraPlayerComponent, JanusStream],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('janus_client');
}
