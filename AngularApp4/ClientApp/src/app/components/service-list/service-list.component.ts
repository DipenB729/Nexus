import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatTableDataSource } from '@angular/material/table';
import { AddServiceDialogComponent } from '../form/add-service-dialog/add-service-dialog.component';

interface Service {
  name: string;
  description: string;
  duration: string;
  price: number;
  icon: string;
  category: string;
  color: string;
}

@Component({
  selector: 'app-service-list',
  templateUrl: './service-list.component.html',
  styleUrls: ['./service-list.component.scss']
})
export class ServiceListComponent implements OnInit {
  // Define columns to display
  displayedColumns: string[] = ['name', 'category', 'duration', 'price', 'actions'];

  // Data Source for the table
  dataSource = new MatTableDataSource<Service>([
    { name: 'Initial Consultation', description: 'Comprehensive assessment', duration: '45 mins', price: 75, icon: 'medical_services', category: 'Health', color: '#3b82f6' },
    { name: 'Deep Tissue Massage', description: 'Intensive muscle therapy', duration: '60 mins', price: 120, icon: 'spa', category: 'Therapy', color: '#8b5cf6' },
    { name: 'Wellness Coaching', description: 'Lifestyle sessions', duration: '60 mins', price: 90, icon: 'psychology', category: 'Consulting', color: '#f59e0b' }
  ]);

  constructor(private dialog: MatDialog) { }

  ngOnInit() { }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
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
        const data = this.dataSource.data;
        data.unshift(result);
        this.dataSource.data = data; // Refresh table
      }
    });
  }
}
