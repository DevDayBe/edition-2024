import {Component, effect, inject, input, signal} from '@angular/core';
import {MatCard, MatCardContent, MatCardFooter, MatCardHeader} from "@angular/material/card";
import {Post} from "../../models/post";
import {SheepService} from "../../services/sheep.service";

@Component({
  selector: 'app-posts',
  standalone: true,
  imports: [
    MatCard,
    MatCardContent,
    MatCardFooter,
    MatCardHeader
  ],
  templateUrl: './posts.component.html',
  styleUrl: './posts.component.scss'
})
export class PostsComponent {
  id = input<number>(0)
  service = inject(SheepService);
  posts = signal<Post[]>([]);

  constructor() {
    effect(() => this.service.getPosts(this.id()).subscribe(posts => this.posts.set(posts)))
  }

}
