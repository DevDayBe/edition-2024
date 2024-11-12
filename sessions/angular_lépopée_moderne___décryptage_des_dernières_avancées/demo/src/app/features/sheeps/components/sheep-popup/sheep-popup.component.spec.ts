import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SheepPopupComponent } from './sheep-popup.component';

describe('SheepPopupComponent', () => {
  let component: SheepPopupComponent;
  let fixture: ComponentFixture<SheepPopupComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SheepPopupComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SheepPopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
