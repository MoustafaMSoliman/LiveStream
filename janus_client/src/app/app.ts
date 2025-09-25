import { Component, signal } from '@angular/core';
import { CameraPlayerComponent } from './components/camera-player-component/camera-player-component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CameraPlayerComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('janus_client');
}
