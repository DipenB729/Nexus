export interface Appointment {
  id: number;              // Matches your [Key] int Id
  customerName: string;
  customerEmail: string;
  startTime: Date;         // Matches StartTime
  endTime: Date;           // Matches EndTime
  status: number;          // Enums come across as integers (0, 1, 2...)
  serviceId: number;
  service?: {
    name: string;
    color: string;
  };
}
