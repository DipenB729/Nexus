import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-root',
  templateUrl: './home.component.html'

})
export class HomeComponent implements OnInit {
  public todos: any[] = [];

  constructor(private http: HttpClient) { }

  ngOnInit() {
    // This calls the .NET Controller
    this.http.get<any[]>('/api/todo').subscribe(result => {
      this.todos = result;
      console.log('Data received:', result);
    }, error => {
      console.error(error);
    });
  }
}
 
