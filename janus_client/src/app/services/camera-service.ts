import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
export interface CameraInfo {
  janusWs: string;
  janusRest: string;
  mountId: number;
}
@Injectable({
  providedIn: 'root'
})
export class CameraService {
  constructor(private http: HttpClient) {}

  getCameraInfo(cameraId: number): Observable<CameraInfo> {
    return this.http.get<CameraInfo>(`https://localhost:7239/api/LiveStream/${cameraId}/info`);
  }
}
