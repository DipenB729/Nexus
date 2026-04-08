export interface Service {
  id: number;
  name: string;
  category: string;
  durationMinutes: number;  
  price: number;
  icon: string;
  color: string;
  description: string;
}

export interface TimeSlot {
  time: string;
  available: boolean;
}
