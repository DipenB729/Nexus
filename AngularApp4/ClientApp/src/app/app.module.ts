import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common'; // Required for ngStyle/ngClass

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
import { MatBadgeModule } from '@angular/material/badge'; // Import this

import { AppComponent } from './app.component';
import { BookingComponent } from './components/booking/booking.component';
import { NavbarComponent } from './components/navbar/navbar.component';
import { HomeComponent } from './components/home/home.component';
import { ServiceListComponent } from './components/service-list/service-list.component';
import { MyBookingsComponent } from './components/my-bookings/my-bookings.component';

@NgModule({
  declarations: [
    AppComponent,
    BookingComponent,
    NavbarComponent,
    HomeComponent,
    ServiceListComponent,
    MyBookingsComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    CommonModule, // Ensure this is here
    FormsModule,
    ReactiveFormsModule,
    RouterModule.forRoot([
      { path: '', component: HomeComponent },
      { path: 'services', component: ServiceListComponent },
      { path: 'my-appointments', component: MyBookingsComponent },
      { path: '**', redirectTo: '' }
    ]),
    // Material Modules MUST be in imports, NOT declarations
    MatToolbarModule,
    MatIconModule,
    MatStepperModule,
    MatCardModule,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatInputModule,
    MatFormFieldModule,
    MatBadgeModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
