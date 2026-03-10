import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatTableDataSource } from '@angular/material/table';
import { ServiceItem } from '../../core/models/api.model';
import { ApiService } from '../../core/services/api.service';
import { AddServiceDialogComponent } from '../form/add-service-dialog/add-service-dialog.component';

@Component({
  selector: 'app-service-list',
  templateUrl: './service-list.component.html',
  styleUrls: ['./service-list.component.scss']
})
export class ServiceListComponent implements OnInit {
  displayedColumns: string[] = ['name', 'category', 'duration', 'price', 'actions'];
  dataSource = new MatTableDataSource<ServiceItem>([]);

  constructor(private dialog: MatDialog, private api: ApiService) { }

  ngOnInit(): void {
    this.loadServices();
  }

  loadServices(): void {
    this.api.getServices().subscribe(data => this.dataSource.data = data);
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  openAddDialog(): void {
    const dialogRef = this.dialog.open(AddServiceDialogComponent, {
      width: '500px',
      height: '100vh',
      position: { right: '0', top: '0' },
      panelClass: 'side-panel-dialog',
      enterAnimationDuration: '0ms',
      maxWidth: '30vw'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) return;
      this.api.createService(result).subscribe(() => this.loadServices());
    });
  }

  deleteService(id?: number): void {
    if (!id) return;
    this.api.deleteService(id).subscribe(() => this.loadServices());
  }
}
