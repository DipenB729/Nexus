import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatTableDataSource } from '@angular/material/table';
import { AppointmentService } from '../../core/services/appointment.service';
import { AddServiceDialogComponent } from '../form/add-service-dialog/add-service-dialog.component';

@Component({
  selector: 'app-service-list',
  templateUrl: './service-list.component.html',
  styleUrls: ['./service-list.component.scss']
})
export class ServiceListComponent implements OnInit {
  displayedColumns: string[] = ['name', 'category', 'duration', 'price', 'actions'];
  dataSource = new MatTableDataSource<any>([]);

  constructor(
    private dialog: MatDialog,
    private appointmentService: AppointmentService // Inject our service
  ) { }

  ngOnInit() {
    this.loadServices();
  }

  loadServices() {
    this.appointmentService.getServices().subscribe(data => {
      this.dataSource.data = data;
    });
  }

  openAddDialog() {
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
