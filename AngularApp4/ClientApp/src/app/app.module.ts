import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';

// Material Imports
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatStepperModule } from '@angular/material/stepper';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatTableModule } from '@angular/material/table';

import { MatDialogModule } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';

// Component Imports
import { AppComponent } from './app.component';
import { BookingComponent } from './components/booking/booking.component';
import { NavbarComponent } from './components/navbar/navbar.component';
import { HomeComponent } from './components/home/home.component';
import { ServiceListComponent } from './components/service-list/service-list.component';
import { MyBookingsComponent } from './components/my-bookings/my-bookings.component';
import { AddServiceDialogComponent } from './components/form/add-service-dialog/add-service-dialog.component';
import { UsersDashboardComponent } from './components/users-dashboard/users-dashboard.component';
import { AdminSettingsComponent } from './components/admin-settings/admin-settings.component';
import { UserPageComponent } from './components/user-page/user-page.component';
import { LoginComponent } from './components/auth/login/login.component';
import { RegisterComponent } from './components/auth/register/register.component';
import { AuthGuard } from './core/guards/auth.guard';
import { RoleGuard } from './core/guards/role.guard';
import { AuthInterceptor } from './core/services/hms/auth.interceptor';

@NgModule({
  declarations: [
    AppComponent,
    BookingComponent,
    NavbarComponent,
    HomeComponent,
    ServiceListComponent,
    MyBookingsComponent,
    AddServiceDialogComponent,
    UsersDashboardComponent,
    AdminSettingsComponent,
    UserPageComponent,
    LoginComponent,
    RegisterComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    CommonModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule.forRoot([
      { path: '', component: HomeComponent },
      { path: 'auth/login', component: LoginComponent },
      { path: 'auth/register', component: RegisterComponent },
      { path: 'services', component: ServiceListComponent },
      { path: 'book', component: BookingComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'User' } },
      { path: 'my-appointments', component: MyBookingsComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'User' } },
      { path: 'users', component: UsersDashboardComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'User' } },
      { path: 'user', component: UserPageComponent, canActivate: [AuthGuard] },
      { path: 'settings', component: AdminSettingsComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: '**', redirectTo: '' }
    ]),
    // Material Modules
    MatToolbarModule,
    MatIconModule,
    MatStepperModule,
    MatCardModule,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatInputModule,
    MatFormFieldModule,
    MatBadgeModule,
    MatMenuModule,
    MatDividerModule,
    MatDialogModule,
    MatSelectModule,
    MatTableModule,
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
