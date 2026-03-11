export type AccountRole = 'Admin' | 'User';

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  role: AccountRole;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthUser {
  id: number;
  fullName: string;
  email: string;
  role: AccountRole;
}
