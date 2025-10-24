import { Component } from '@angular/core';
import { MultiLiveStreamComponent } from './components/multi-live-stream-component/multi-live-stream-component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [MultiLiveStreamComponent],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class AppComponent {
  title = 'Camera Monitoring Dashboard';
new: any;
}
