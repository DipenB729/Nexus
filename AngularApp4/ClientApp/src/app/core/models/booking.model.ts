export interface Service {
  id: number;
  name: string;
  duration: number;
  price: number;
  description: string;
}

export interface TimeSlot {
  time: string;
  available: boolean;
}
