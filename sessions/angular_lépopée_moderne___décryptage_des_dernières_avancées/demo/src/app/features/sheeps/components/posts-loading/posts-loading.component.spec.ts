import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PostsLoadingComponent } from './posts-loading.component';

describe('PostsLoadingComponent', () => {
  let component: PostsLoadingComponent;
  let fixture: ComponentFixture<PostsLoadingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PostsLoadingComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PostsLoadingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
