import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CameraPlayerComponent } from './camera-player-component';

describe('CameraPlayerComponent', () => {
  let component: CameraPlayerComponent;
  let fixture: ComponentFixture<CameraPlayerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CameraPlayerComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CameraPlayerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
