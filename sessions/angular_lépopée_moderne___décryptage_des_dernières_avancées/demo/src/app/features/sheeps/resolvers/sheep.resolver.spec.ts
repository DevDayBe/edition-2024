import { TestBed } from '@angular/core/testing';
import { ResolveFn } from '@angular/router';

import { sheepResolver } from './sheep.resolver';

describe('sheepResolver', () => {
  const executeResolver: ResolveFn<boolean> = (...resolverParameters) => 
      TestBed.runInInjectionContext(() => sheepResolver(...resolverParameters));

  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('should be created', () => {
    expect(executeResolver).toBeTruthy();
  });
});
