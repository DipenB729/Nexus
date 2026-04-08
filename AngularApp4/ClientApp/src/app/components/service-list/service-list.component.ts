import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatTableDataSource } from '@angular/material/table';
import { AppointmentService } from '../../core/services/appointment.service';
import { AuthApiService } from '../../core/services/hms/auth-api.service';
import { AddServiceDialogComponent } from '../form/add-service-dialog/add-service-dialog.component';

@Component({
  selector: 'app-service-list',
  templateUrl: './service-list.component.html',
  styleUrls: ['./service-list.component.scss']
})
export class ServiceListComponent implements OnInit {
  displayedColumns: string[] = [];
  dataSource = new MatTableDataSource<any>([]);
  isAdmin = false;

  constructor(
    private dialog: MatDialog,
    private appointmentService: AppointmentService,
    private auth: AuthApiService
  ) { }

  ngOnInit() {
    this.isAdmin = this.auth.getRole() === 'Admin';
    this.displayedColumns = this.isAdmin
      ? ['name', 'category', 'duration', 'price', 'actions']
      : ['name', 'category', 'duration', 'price'];
    this.loadServices();
  }

  loadServices() {
    this.appointmentService.getServices().subscribe(data => {
      this.dataSource.data = data;
    });
  }

  openAddDialog() {
    if (!this.isAdmin) {
      return;
    }

    const dialogRef = this.dialog.open(AddServiceDialogComponent, {
      width: '500px',
      height: '100vh',
      position: { right: '0', top: '0' },
      panelClass: 'side-panel-dialog',
      enterAnimationDuration: '0ms',
      maxWidth: '30vw'   
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        // Call .NET API to save
        this.appointmentService.addService(result).subscribe(() => {
          this.loadServices(); // Refresh list after adding
        });
      }
    });
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }
}
