import { TestBed } from '@angular/core/testing';

import { SheepService } from './sheep.service';

describe('SheepService', () => {
  let service: SheepService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SheepService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
