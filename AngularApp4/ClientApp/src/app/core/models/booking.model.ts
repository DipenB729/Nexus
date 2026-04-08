export interface Service {
  id: number;
  name: string;
  category: string;
  durationMinutes: number; // Changed from 'duration' to match .NET
  price: number;
  icon: string;
  color: string;
  description: string;
}

export interface TimeSlot {
  time: string;
  available: boolean;
}
