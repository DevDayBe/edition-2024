import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SheepSummaryCardComponent } from './sheep-summary-card.component';

describe('SheepSummaryCardComponent', () => {
  let component: SheepSummaryCardComponent;
  let fixture: ComponentFixture<SheepSummaryCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SheepSummaryCardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SheepSummaryCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
