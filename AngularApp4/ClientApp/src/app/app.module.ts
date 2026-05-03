import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

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
import { MatSortModule } from '@angular/material/sort';

import { MatDialogModule } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';

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
import { MasterSetupComponent } from './components/master-setup/master-setup.component';
import { UserPageComponent } from './components/user-page/user-page.component';
import { LoginComponent } from './components/auth/login/login.component';
import { RegisterComponent } from './components/auth/register/register.component';
import { ForgotPasswordComponent } from './components/auth/forgot-password/forgot-password.component';
import { DoctorDetailComponent } from './components/doctor-detail/doctor-detail.component';
import { DoctorListingComponent } from './components/doctor-listing/doctor-listing.component';
import { PatientRegistryComponent } from './components/patient-registry/patient-registry.component';
import { AppointmentControlComponent } from './components/appointment-control/appointment-control.component';
import { AdmissionControlComponent } from './components/admission-control/admission-control.component';
import { BillingFinanceComponent } from './components/billing-finance/billing-finance.component';
import { InventoryAdminComponent } from './components/inventory-admin/inventory-admin.component';
import { LabServiceAdminComponent } from './components/lab-service-admin/lab-service-admin.component';
import { MonitoringAdminComponent } from './components/monitoring-admin/monitoring-admin.component';
import { PatientProfileComponent } from './components/patient-profile/patient-profile.component';
import { DoctorDashboardComponent } from './components/doctor-dashboard/doctor-dashboard.component';
import { DoctorProfileComponent } from './components/doctor-profile/doctor-profile.component';
import { NotificationCenterComponent } from './components/notification-center/notification-center.component';
import { CareCommunicationComponent } from './components/care-communication/care-communication.component';
import { FloatingChatComponent } from './components/floating-chat/floating-chat.component';
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
    MasterSetupComponent,
    UserPageComponent,
    LoginComponent,
    RegisterComponent,
    ForgotPasswordComponent,
    DoctorDetailComponent,
    DoctorListingComponent,
    PatientRegistryComponent,
    AppointmentControlComponent,
    AdmissionControlComponent,
    BillingFinanceComponent,
    InventoryAdminComponent,
    LabServiceAdminComponent,
    MonitoringAdminComponent,
    PatientProfileComponent,
    DoctorDashboardComponent,
    DoctorProfileComponent,
    NotificationCenterComponent,
    CareCommunicationComponent,
    FloatingChatComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    CommonModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule.forRoot([
      { path: '', pathMatch: 'full', redirectTo: 'auth/login' },
      { path: 'auth/login', component: LoginComponent },
      { path: 'auth/register', component: RegisterComponent },
      { path: 'auth/forgot-password', component: ForgotPasswordComponent },
      { path: 'admin', pathMatch: 'full', redirectTo: 'admin/dashboard' },
      { path: 'admin/dashboard', component: HomeComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/patients', component: PatientRegistryComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/patients/create', component: PatientRegistryComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/patients/:id/edit', component: PatientRegistryComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/patients/:id', component: PatientRegistryComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/services', component: ServiceListComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/bookings', component: AppointmentControlComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/bookings/:id/edit', component: AppointmentControlComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/bookings/:id', component: AppointmentControlComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/admissions', component: AdmissionControlComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/admissions/create', component: AdmissionControlComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/admissions/:id/edit', component: AdmissionControlComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/admissions/:id', component: AdmissionControlComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/billing', pathMatch: 'full', redirectTo: 'admin/billing/bills' },
      { path: 'admin/billing/:section/create', component: BillingFinanceComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/billing/:section/:id/edit', component: BillingFinanceComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/billing/:section/:id', component: BillingFinanceComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/billing/:section', component: BillingFinanceComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/inventory', pathMatch: 'full', redirectTo: 'admin/inventory/dashboard' },
      { path: 'admin/inventory/:section/create', component: InventoryAdminComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/inventory/:section/:id/edit', component: InventoryAdminComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/inventory/:section/:id', component: InventoryAdminComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/inventory/:section', component: InventoryAdminComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/laboratory', pathMatch: 'full', redirectTo: 'admin/laboratory/labTests' },
      { path: 'admin/laboratory/:section/create', component: LabServiceAdminComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/laboratory/:section/:id/edit', component: LabServiceAdminComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/laboratory/:section/:id', component: LabServiceAdminComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/laboratory/:section', component: LabServiceAdminComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/monitoring', pathMatch: 'full', redirectTo: 'admin/monitoring/reports' },
      { path: 'admin/monitoring/:section/:reportKey', component: MonitoringAdminComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/monitoring/:section', component: MonitoringAdminComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/roles', component: UsersDashboardComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/users', pathMatch: 'full', redirectTo: 'admin/roles' },
      { path: 'admin/masters', pathMatch: 'full', redirectTo: 'admin/masters/departments' },
      { path: 'admin/masters/:section/create', component: MasterSetupComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/masters/:section/:id/edit', component: MasterSetupComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/masters/:section/:id', component: MasterSetupComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/masters/:section', component: MasterSetupComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/settings', pathMatch: 'full', redirectTo: 'admin/settings/organization' },
      { path: 'admin/settings/:section', component: AdminSettingsComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'admin/notifications', component: NotificationCenterComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Admin' } },
      { path: 'doctor', pathMatch: 'full', redirectTo: 'doctor/dashboard' },
      { path: 'doctor/dashboard', component: DoctorDashboardComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Doctor' } },
      { path: 'doctor/appointments', component: DoctorDashboardComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Doctor' } },
      { path: 'doctor/appointments/:appointmentId', component: DoctorDashboardComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Doctor' } },
      { path: 'doctor/availability', component: DoctorDashboardComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Doctor' } },
      { path: 'doctor/messages', component: CareCommunicationComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Doctor' } },
      { path: 'doctor/messages/:appointmentId', component: CareCommunicationComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Doctor' } },
      { path: 'doctor/reports', component: CareCommunicationComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Doctor' } },
      { path: 'doctor/reports/:appointmentId', component: CareCommunicationComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Doctor' } },
      { path: 'doctor/notifications', component: NotificationCenterComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Doctor' } },
      { path: 'doctor/profile', component: DoctorProfileComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'Doctor' } },
      { path: 'patient', pathMatch: 'full', redirectTo: 'patient/doctors' },
      { path: 'patient/dashboard', component: UserPageComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'User' } },
      { path: 'patient/doctors', component: DoctorListingComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'User' } },
      { path: 'patient/doctors/:doctorId', component: DoctorDetailComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'User' } },
      { path: 'patient/book', component: BookingComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'User' } },
      { path: 'patient/services', pathMatch: 'full', redirectTo: 'patient/doctors' },
      { path: 'patient/appointments', component: MyBookingsComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'User' } },
      { path: 'patient/appointments/:id', component: MyBookingsComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'User' } },
      { path: 'patient/messages', component: CareCommunicationComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'User' } },
      { path: 'patient/messages/:appointmentId', component: CareCommunicationComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'User' } },
      { path: 'patient/reports', component: CareCommunicationComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'User' } },
      { path: 'patient/reports/:appointmentId', component: CareCommunicationComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'User' } },
      { path: 'patient/notifications', component: NotificationCenterComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'User' } },
      { path: 'patient/profile', component: PatientProfileComponent, canActivate: [AuthGuard, RoleGuard], data: { role: 'User' } },
      { path: 'user', pathMatch: 'full', redirectTo: 'patient/dashboard' },
      { path: 'user/dashboard', pathMatch: 'full', redirectTo: 'patient/dashboard' },
      { path: 'user/services', pathMatch: 'full', redirectTo: 'patient/doctors' },
      { path: 'user/appointments', pathMatch: 'full', redirectTo: 'patient/appointments' },
      { path: 'patients', pathMatch: 'full', redirectTo: 'admin/patients' },
      { path: 'services', pathMatch: 'full', redirectTo: 'admin/services' },
      { path: 'admissions', pathMatch: 'full', redirectTo: 'admin/admissions' },
      { path: 'billing', pathMatch: 'full', redirectTo: 'admin/billing/bills' },
      { path: 'inventory', pathMatch: 'full', redirectTo: 'admin/inventory' },
      { path: 'laboratory', pathMatch: 'full', redirectTo: 'admin/laboratory' },
      { path: 'monitoring', pathMatch: 'full', redirectTo: 'admin/monitoring' },
      { path: 'settings', pathMatch: 'full', redirectTo: 'admin/settings' },
      { path: 'masters', pathMatch: 'full', redirectTo: 'admin/masters/departments' },
      { path: 'users', pathMatch: 'full', redirectTo: 'admin/roles' },
      { path: 'my-appointments', pathMatch: 'full', redirectTo: 'patient/appointments' },
      { path: 'book', pathMatch: 'full', redirectTo: 'patient/book' },
      { path: '**', redirectTo: 'auth/login' }
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
    MatSortModule,
    MatTooltipModule
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
