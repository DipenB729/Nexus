import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';
import { TodoComponent } from './todo/todo.component'; // 1. Import it

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    TodoComponent  
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    RouterModule.forRoot([
      // Route for Home Button
      { path: '', component: HomeComponent, pathMatch: 'full' },

      // Route for Todo Button (Loads TodoComponent, NOT AppComponent)
      { path: 'todo', component: TodoComponent }
    ])
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
