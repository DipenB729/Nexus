import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-todo',
  templateUrl: './todo.component.html'
    
  
})
export class TodoComponent implements OnInit {
  public todos: any[] = [];

  constructor(private http: HttpClient) { }

  ngOnInit() {
    this.http.get<any[]>('/api/todo').subscribe(result => {
      this.todos = result;
    });
  }
}
