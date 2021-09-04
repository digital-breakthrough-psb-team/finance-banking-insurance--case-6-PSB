import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DoRecognitionFormComponent } from './do-recognition-form.component';

describe('DoRecognitionFormComponent', () => {
  let component: DoRecognitionFormComponent;
  let fixture: ComponentFixture<DoRecognitionFormComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DoRecognitionFormComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DoRecognitionFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
