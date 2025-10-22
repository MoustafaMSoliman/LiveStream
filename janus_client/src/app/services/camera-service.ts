import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface AddCameraRequest {
  source: string;
  sourceOnDemand: boolean;
  sourceOnDemandStartTimeout?: string;
}

export interface Camera {
  name: string;
  source: string;
  state: string;
  readers: number;
}

export interface CameraListResponse {
  itemCount: number;
  pageCount: number;
  items: Camera[];
}

@Injectable({
  providedIn: 'root'
})
export class CameraService {
  private apiUrl = '/api/cameras'; 

  constructor(private http: HttpClient) { }

  addCamera(cameraName: string, request: AddCameraRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/add/${cameraName}`, request);
  }

  removeCamera(cameraName: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/remove/${cameraName}`);
  }

  getAllCameras(): Observable<CameraListResponse> {
    return this.http.get<CameraListResponse>(`${this.apiUrl}/list`);
  }

  getCamera(cameraName: string): Observable<Camera> {
    return this.http.get<Camera>(`${this.apiUrl}/get/${cameraName}`);
  }
}
