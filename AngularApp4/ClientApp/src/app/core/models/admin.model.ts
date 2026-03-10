export type UserRole = 'Admin' | 'Manager' | 'Staff';
export type UserStatus = 'Active' | 'Invited' | 'Suspended';

export interface AdminUser {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  status: UserStatus;
  lastLogin: string;
}

export type BookingStatus = 'Confirmed' | 'Pending' | 'Cancelled' | 'Completed';

export interface Appointment {
  id: string;
  customerName: string;
  serviceName: string;
  appointmentDate: Date;
  time: string;
  status: BookingStatus;
  price: number;
}

export interface AdminSettings {
  smtpHost: string;
  smtpPort: number;
  senderEmail: string;
  senderName: string;
  enableTls: boolean;
  bookingNotifications: boolean;
  reminderNotifications: boolean;
}
