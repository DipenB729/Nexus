export interface Doctor {
  id?: number;
  fullName: string;
  specialty: string;
  experience: string;
  availableFrom: string;
  availableTo: string;
}

export interface ServiceItem {
  id?: number;
  name: string;
  description: string;
  category: string;
  duration: string;
  price: number;
  icon: string;
  color: string;
}

export interface BookingRecord {
  id?: number;
  bookingCode?: string;
  customerName: string;
  customerEmail?: string;
  doctorName?: string;
  serviceName: string;
  appointmentDate: string;
  time: string;
  status: string;
  price: number;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  role: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthUser {
  email: string;
  fullName: string;
  role: 'Admin' | 'Customer';
}
