import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PostsPlaceholderComponent } from './posts-placeholder.component';

describe('PostsPlaceholderComponent', () => {
  let component: PostsPlaceholderComponent;
  let fixture: ComponentFixture<PostsPlaceholderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PostsPlaceholderComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PostsPlaceholderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
