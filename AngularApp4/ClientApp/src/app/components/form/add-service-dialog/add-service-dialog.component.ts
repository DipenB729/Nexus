import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-add-service-dialog',
  templateUrl: './add-service-dialog.component.html',
  styleUrls: ['./add-service-dialog.component.css']
})
export class AddServiceDialogComponent implements OnInit {
  serviceForm: FormGroup;

  // List of icons for the user to pick from
  icons = ['medical_services', 'spa', 'psychology', 'history', 'bolt', 'self_improvement', 'fitness_center'];

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<AddServiceDialogComponent>
  ) {
    this.serviceForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      category: ['', Validators.required],
      description: ['', [Validators.required, Validators.maxLength(100)]],
      duration: ['', Validators.required],
      price: ['', [Validators.required, Validators.min(0)]],
      icon: ['medical_services', Validators.required],
      color: ['#3b82f6', Validators.required]
    });
  }

  ngOnInit(): void { }

  onSave() {
    if (this.serviceForm.valid) {
      this.dialogRef.close(this.serviceForm.value);
    }
  }

  onCancel() {
    this.dialogRef.close();
  }
}
