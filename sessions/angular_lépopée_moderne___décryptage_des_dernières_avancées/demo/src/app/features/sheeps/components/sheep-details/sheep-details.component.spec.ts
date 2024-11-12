import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SheepDetailsComponent } from './sheep-details.component';

describe('SheepDetailsComponent', () => {
  let component: SheepDetailsComponent;
  let fixture: ComponentFixture<SheepDetailsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SheepDetailsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SheepDetailsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
