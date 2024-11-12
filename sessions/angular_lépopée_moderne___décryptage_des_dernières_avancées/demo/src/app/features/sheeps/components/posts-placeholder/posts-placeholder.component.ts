import {Component} from '@angular/core';
import {MatCard, MatCardContent, MatCardFooter, MatCardHeader} from "@angular/material/card";

@Component({
  selector: 'app-posts-placeholder',
  standalone: true,
  imports: [
    MatCardFooter,
    MatCardContent,
    MatCard,
    MatCardHeader
  ],
  templateUrl: './posts-placeholder.component.html',
  styleUrl: './posts-placeholder.component.scss'
})
export class PostsPlaceholderComponent {

}
