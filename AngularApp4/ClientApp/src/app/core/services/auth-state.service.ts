import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { AuthUser } from '../models/api.model';

@Injectable({ providedIn: 'root' })
export class AuthStateService {
  private readonly KEY = 'nexus_user';
  readonly user$ = new BehaviorSubject<AuthUser | null>(this.load());

  setUser(user: AuthUser | null): void {
    this.user$.next(user);
    if (!user) {
      localStorage.removeItem(this.KEY);
      return;
    }
    localStorage.setItem(this.KEY, JSON.stringify(user));
  }

  private load(): AuthUser | null {
    const raw = localStorage.getItem(this.KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthUser;
    } catch {
      return null;
    }
  }
}
