import { Component, OnInit } from '@angular/core';
import { Doctor } from '../../core/models/api.model';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-doctors-list',
  templateUrl: './doctors-list.component.html',
  styleUrls: ['./doctors-list.component.scss']
})
export class DoctorsListComponent implements OnInit {
  doctors: Doctor[] = [];

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getDoctors().subscribe(d => this.doctors = d);
  }
}
