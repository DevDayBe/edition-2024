import {Component, effect, inject, input, model, signal} from '@angular/core';
import {MatCard, MatCardActions, MatCardImage, MatCardTitle} from "@angular/material/card";
import {NgOptimizedImage} from "@angular/common";
import {Sheep} from "../../models/sheep";
import {MatIconAnchor, MatIconButton} from "@angular/material/button";
import {MatIcon} from "@angular/material/icon";
import {RouterLink} from "@angular/router";
import {MatBadge} from '@angular/material/badge';
import {MatSnackBar} from '@angular/material/snack-bar';

@Component({
  selector: 'app-sheep-summary-card',
  imports: [
    MatCardImage,
    MatCardTitle,
    MatCard,
    MatIcon,
    MatCardActions,
    MatIconButton,
    MatIconAnchor,
    RouterLink,
    NgOptimizedImage,
    MatBadge,
  ],
  templateUrl: './sheep-summary-card.component.html',
  styleUrl: './sheep-summary-card.component.scss'
})
export class SheepSummaryCardComponent {

  sheep = input.required<Sheep>()

  likeCount = model<number>(0);


  snackbar = inject(MatSnackBar);

  constructor() {
    /*
    effect(() => {
      const like = this.likeCount();
      if(like > 0){
        this.snackbar.open(`${this.sheep.name} has been liked ${like} times`);
      }
    });
    */
  }

  like() {
    this.likeCount.update(v => v+1);
  }
}
