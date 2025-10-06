import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';


export interface StreamInfo {
  webRTCUrl: string;
  hlsUrl: string;
  rtspUrl: string;
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface TokenRefreshResponse {
  accessToken: string;
}

export interface TokenRequest {
  cameraId: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
  cameraId: string;
}
@Injectable({
  providedIn: 'root'
}) 
export class StreamTokenService {
  private baseUrl = 'http://localhost:5000/api/stream';
  private activeStreams = new Map<string, BehaviorSubject<StreamInfo>>();
  private refreshTimers = new Map<string, any>();

  constructor(private http: HttpClient){}

  getStreamInfo(cameraId:string):Observable<StreamInfo>{
    if(!this.activeStreams.has(cameraId)){
      this.activeStreams.set(cameraId, new BehaviorSubject<StreamInfo>(null!));
      this.initializeStream(cameraId);
    }
    return this.activeStreams.get(cameraId)!.asObservable();
  }

  private initializeStream(cameraId:string): void {
    const request : TokenRequest = {cameraId};
    this.http.post<StreamInfo>(`${this.baseUrl}/token`, request)
       .pipe(
        tap(streamInfo => {
          this.activeStreams.get(cameraId)!.next(streamInfo);
          this.scheduleTokenRefresh( cameraId, streamInfo);
        }),
        catchError( error => {
          console.error('Failed to get stream token:', error);
          throw error;
        })
       )
       .subscribe();
  }
  private scheduleTokenRefresh(cameraId: string, streamInfo: StreamInfo): void {
    const expiresAt = new Date(streamInfo.expiresAt).getTime();
    const now = Date.now();
    const timeUntilExpiry = expiresAt - now;
    
    // Refresh 1 minute before expiry
    const refreshTime = Math.max(timeUntilExpiry - 60000, 10000); // Minimum 10 seconds
    
    console.log(`Scheduling token refresh for ${cameraId} in ${refreshTime}ms`);
    
    if (this.refreshTimers.has(cameraId)) {
      clearTimeout(this.refreshTimers.get(cameraId));
    }
    
    const timerRef = setTimeout(() => {
      this.refreshToken(cameraId, streamInfo.refreshToken);
    }, refreshTime);
    
    this.refreshTimers.set(cameraId, timerRef);
  }

  private refreshToken(cameraId: string, refreshToken: string): void {
    console.log(`Refreshing token for ${cameraId}`);
    const request: RefreshTokenRequest = {
      refreshToken,
      cameraId
    };
    
    this.http.post<TokenRefreshResponse>(`${this.baseUrl}/refresh`, {
      request
    })
    .pipe(
      tap((response: TokenRefreshResponse) => {
        // Update the stream info with new token
        const currentStream = this.activeStreams.get(cameraId)!.value;
        const updatedStream: StreamInfo = {
          ...currentStream,
          accessToken: response.accessToken,
          expiresAt: new Date(Date.now() + 5 * 60 * 1000).toISOString() // 5 minutes from now
        };
        
        this.activeStreams.get(cameraId)!.next(updatedStream);
        this.scheduleTokenRefresh(cameraId, updatedStream);
        
        console.log(`Token refreshed for ${cameraId}`);
      }),
      catchError(error => {
        console.error('Token refresh failed:', error);
        // Reinitialize the stream (will force re-authentication)
        this.initializeStream(cameraId);
        throw error;
      })
    )
    .subscribe();
  }

  getSecureWebRTCUrl(cameraId: string, token: string): string {
    return `http://localhost:8889/whep/${cameraId}?token=${token}`;
  }

  getSecureHLSUrl(cameraId: string, token: string): string {
    return `http://localhost:8888/${cameraId}/index.m3u8?token=${token}`;
  }

  getSecureRTSPUrl(cameraId: string, token: string): string {
    return `rtsp://${token}@localhost:8554/${cameraId}`;
  }

  cleanup(cameraId: string): void {
    if (this.refreshTimers.has(cameraId)) {
      clearTimeout(this.refreshTimers.get(cameraId));
      this.refreshTimers.delete(cameraId);
    }
    if (this.activeStreams.has(cameraId)) {
      this.activeStreams.delete(cameraId);
    }
  }
}
