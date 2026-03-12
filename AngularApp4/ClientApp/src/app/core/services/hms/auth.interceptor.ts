import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthApiService } from './auth-api.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private readonly auth: AuthApiService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.auth.getToken();
    if (!token) {
      return next.handle(req);
    }

    const cloned = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });

    return next.handle(cloned);
  }
}
