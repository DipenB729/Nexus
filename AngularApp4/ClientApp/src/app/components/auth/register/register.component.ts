import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit {
  fullName = '';
  email = '';
  password = '';
  role: 'Customer' | 'Admin' = 'Customer';
  message = '';

  constructor(private api: ApiService, private route: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    const role = (this.route.snapshot.data['role'] as 'Customer' | 'Admin' | undefined) ?? 'Customer';
    this.role = role;
  }

  submit(): void {
    this.message = '';
    this.api.register({ fullName: this.fullName, email: this.email, password: this.password, role: this.role }).subscribe({
      next: () => {
        this.message = `${this.role} account created. Please login.`;
        setTimeout(() => this.router.navigateByUrl('/login'), 800);
      },
      error: () => {
        this.message = 'Registration failed.';
      }
    });
  }
}
