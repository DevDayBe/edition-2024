import {Component, effect, inject, model} from '@angular/core';
import {MAT_DIALOG_DATA, MatDialogRef} from '@angular/material/dialog';
import {MatFormField, MatLabel} from '@angular/material/form-field';
import {MatButton} from '@angular/material/button';
import {MatInput} from '@angular/material/input';
import {FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {Sheep} from '../../models/sheep';
import {MatCard} from '@angular/material/card';
import {MatOption, MatSelect} from '@angular/material/select';

@Component({
  selector: 'app-sheep-popup',
  imports: [
    MatFormField,
    MatButton,
    MatInput,
    FormsModule,
    MatCard,
    ReactiveFormsModule,
    MatSelect,
    MatOption,
    MatLabel
  ],
  templateUrl: './sheep-popup.component.html',
  styleUrl: './sheep-popup.component.scss'
})
export class SheepPopupComponent {
  readonly dialogRef = inject(MatDialogRef<SheepPopupComponent>);
  readonly data = inject<Sheep>(MAT_DIALOG_DATA);
  readonly animal = model(this.data);
  private readonly fb: FormBuilder = inject(FormBuilder);



  sheepForm: FormGroup;

  constructor() {
    this.sheepForm = this.fb.group({
      name: ['', Validators.required],
      age: [0, [Validators.required, Validators.min(0)]],
      breed: ['', Validators.required],
      woolColor: ['', Validators.required],
      weight: [0, [Validators.required, Validators.min(0)]],
      healthStatus: [''],
      temperament: ['', Validators.required],
      imageUrl: ['']
    });

    effect(() => {
      this.sheepForm.reset(this.animal());
    });
  }

  onSubmit() {
    if (this.sheepForm.valid) {
      this.dialogRef.close(this.sheepForm.value); // Send form data back to the parent
    }
  }

  onCancel() {
    this.dialogRef.close(); // Close dialog without sending data
  }
  onNoClick(): void {
    this.dialogRef.close();
  }
}
