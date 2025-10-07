import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MultiLiveStreamComponent } from './multi-live-stream-component';

describe('MultiLiveStreamComponent', () => {
  let component: MultiLiveStreamComponent;
  let fixture: ComponentFixture<MultiLiveStreamComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MultiLiveStreamComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MultiLiveStreamComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
