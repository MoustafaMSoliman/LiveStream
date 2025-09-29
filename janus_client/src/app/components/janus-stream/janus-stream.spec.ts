import { ComponentFixture, TestBed } from '@angular/core/testing';

import { JanusStream } from './janus-stream';

describe('JanusStream', () => {
  let component: JanusStream;
  let fixture: ComponentFixture<JanusStream>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [JanusStream]
    })
    .compileComponents();

    fixture = TestBed.createComponent(JanusStream);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
