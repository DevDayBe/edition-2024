import {Component, Input, resource} from '@angular/core';
import {CommonModule, NgIf} from "@angular/common";
import {MatCardModule} from "@angular/material/card";
import {Sheep} from "../../models/sheep";
import {PostsComponent} from "../posts/posts.component";
import {PostsPlaceholderComponent} from "../posts-placeholder/posts-placeholder.component";
import {PostsLoadingComponent} from "../posts-loading/posts-loading.component";
import {MatIcon} from "@angular/material/icon";
import {NgxSkeletonLoaderModule} from 'ngx-skeleton-loader';

@Component({
  selector: 'app-sheep-details',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    PostsComponent,
    PostsPlaceholderComponent,
    PostsLoadingComponent,
    MatIcon,
    NgIf,
    NgxSkeletonLoaderModule
  ],
  templateUrl: './sheep-details.component.html',
  styleUrl: './sheep-details.component.scss'
})
export class SheepDetailsComponent {
  @Input({required:true}) sheep!: Sheep;

  image = resource({
      loader: () => fetch('http://localhost:8080/sheep', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(this.sheep),
      }).then(body => body.text())
    });
}
