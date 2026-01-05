import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
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

// Add to @NgModule imports:
// MatTableModule, MatSortModule,
// NEW MATERIAL IMPORTS FOR DIALOG & FORM
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

@NgModule({
  declarations: [
    AppComponent,
    BookingComponent,
    NavbarComponent,
    HomeComponent,
    ServiceListComponent,
    MyBookingsComponent,
    AddServiceDialogComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule.forRoot([
      { path: '', component: HomeComponent },
      { path: 'services', component: ServiceListComponent },
      { path: 'book', component: BookingComponent }, // Added back
      { path: 'my-appointments', component: MyBookingsComponent },
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
    MatDialogModule, // Added this
    MatSelectModule,
    MatTableModule,
    HttpClientModule,// Added this
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
