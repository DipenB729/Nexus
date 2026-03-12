# Hospital Management System (Angular + ASP.NET Core Web API + SQL Server)

A complete, scalable, beginner-friendly implementation blueprint.

---

## 1) Recommended Project Architecture

## High-level architecture
- **Frontend:** Angular (feature modules + shared/core modules + route guards + interceptors).
- **Backend:** ASP.NET Core Web API using **Clean/Layered Architecture**.
- **Data:** SQL Server with Entity Framework Core (Code-First or Database-First).
- **Security:** JWT Authentication + Role-based Authorization (`Admin`, `User`).

## Suggested backend solution structure

```text
HospitalManagementSystem.sln
src/
  HospitalManagementSystem.Api/                # Presentation layer (Controllers, middleware, DI)
  HospitalManagementSystem.Application/        # Use cases, DTOs, interfaces, validation
  HospitalManagementSystem.Domain/             # Entities, enums, domain rules
  HospitalManagementSystem.Infrastructure/     # EF Core, repositories, auth, external services
tests/
  HospitalManagementSystem.UnitTests/
  HospitalManagementSystem.IntegrationTests/
```

## Suggested Angular structure

```text
hospital-management-ui/
  src/app/
    core/               # singleton services (auth, token, interceptors, guards)
    shared/             # reusable components, pipes, directives
    features/
      auth/
      admin/
        dashboard/
        doctors/
        schedules/
        staff/
        appointments/
        services/
        reports/
      user/
        home/
        doctors/
        appointments/
        profile/
    layouts/
      admin-layout/
      user-layout/
```

---

## 2) Database Design (Tables, PKs, FKs)

> Use `BIGINT IDENTITY` for PKs and UTC timestamps for auditing.

## Core entities and relationships
- `Users` belongs to one `Role`.
- `Patients` has one-to-one relation with `Users` (for user profile extensions).
- `Doctors` managed by admin.
- `DoctorSchedules` belongs to `Doctors`.
- `Appointments` links `Patients`, `Doctors`, and optional `DoctorSchedules`.
- `Staff` independent table managed by admin.
- `HospitalServices` catalog for display and booking context.

## Table blueprint

### Roles
- `RoleId` (PK)
- `Name` (Admin/User)
- `IsActive`, `CreatedAt`

### Users
- `UserId` (PK)
- `RoleId` (FK -> Roles)
- `FullName`, `Email` (unique), `Phone`
- `PasswordHash`, `PasswordSalt`
- `IsActive`, `CreatedAt`, `UpdatedAt`

### Patients
- `PatientId` (PK)
- `UserId` (FK -> Users, unique)
- `Gender`, `DateOfBirth`, `Address`, `BloodGroup`, `EmergencyContact`
- `CreatedAt`, `UpdatedAt`

### Doctors
- `DoctorId` (PK)
- `FullName`, `Specialization`, `Email` (unique), `Phone`
- `ExperienceYears`, `Qualification`, `ConsultationFee`
- `IsActive`, `CreatedAt`, `UpdatedAt`

### DoctorSchedules
- `ScheduleId` (PK)
- `DoctorId` (FK -> Doctors)
- `DayOfWeek` (1..7)
- `StartTime`, `EndTime`
- `SlotDurationMinutes`
- `MaxPatientsPerSlot`
- `IsActive`, `CreatedAt`, `UpdatedAt`

### Staff
- `StaffId` (PK)
- `FullName`, `Department`, `Designation`
- `Email` (unique), `Phone`, `JoinDate`
- `IsActive`, `CreatedAt`, `UpdatedAt`

### HospitalServices
- `ServiceId` (PK)
- `ServiceName`, `Description`
- `Price`, `EstimatedDurationMinutes`
- `IsActive`, `CreatedAt`, `UpdatedAt`

### Appointments
- `AppointmentId` (PK)
- `PatientId` (FK -> Patients)
- `DoctorId` (FK -> Doctors)
- `ScheduleId` (FK -> DoctorSchedules, nullable)
- `ServiceId` (FK -> HospitalServices, nullable)
- `AppointmentDate`
- `SlotStartTime`, `SlotEndTime`
- `Status` (Pending/Approved/Rescheduled/Cancelled/Completed)
- `Reason`, `AdminRemarks`
- `CreatedByUserId` (FK -> Users)
- `CreatedAt`, `UpdatedAt`

---

## 3) SQL Server Table Scripts

```sql
CREATE TABLE Roles (
    RoleId BIGINT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Users (
    UserId BIGINT IDENTITY(1,1) PRIMARY KEY,
    RoleId BIGINT NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    Phone NVARCHAR(20) NULL,
    PasswordHash VARBINARY(MAX) NOT NULL,
    PasswordSalt VARBINARY(MAX) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

CREATE TABLE Patients (
    PatientId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId BIGINT NOT NULL UNIQUE,
    Gender NVARCHAR(20) NULL,
    DateOfBirth DATE NULL,
    Address NVARCHAR(300) NULL,
    BloodGroup NVARCHAR(10) NULL,
    EmergencyContact NVARCHAR(20) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Patients_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE Doctors (
    DoctorId BIGINT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    Specialization NVARCHAR(120) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    Phone NVARCHAR(20) NULL,
    ExperienceYears INT NOT NULL DEFAULT 0,
    Qualification NVARCHAR(150) NULL,
    ConsultationFee DECIMAL(10,2) NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL
);

CREATE TABLE DoctorSchedules (
    ScheduleId BIGINT IDENTITY(1,1) PRIMARY KEY,
    DoctorId BIGINT NOT NULL,
    DayOfWeek TINYINT NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    SlotDurationMinutes INT NOT NULL,
    MaxPatientsPerSlot INT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT CK_DoctorSchedules_DayOfWeek CHECK (DayOfWeek BETWEEN 1 AND 7),
    CONSTRAINT CK_DoctorSchedules_TimeRange CHECK (StartTime < EndTime),
    CONSTRAINT FK_DoctorSchedules_Doctors FOREIGN KEY (DoctorId) REFERENCES Doctors(DoctorId)
);

CREATE TABLE Staff (
    StaffId BIGINT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    Department NVARCHAR(100) NOT NULL,
    Designation NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    Phone NVARCHAR(20) NULL,
    JoinDate DATE NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL
);

CREATE TABLE HospitalServices (
    ServiceId BIGINT IDENTITY(1,1) PRIMARY KEY,
    ServiceName NVARCHAR(150) NOT NULL UNIQUE,
    Description NVARCHAR(500) NULL,
    Price DECIMAL(10,2) NOT NULL DEFAULT 0,
    EstimatedDurationMinutes INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL
);

CREATE TABLE Appointments (
    AppointmentId BIGINT IDENTITY(1,1) PRIMARY KEY,
    PatientId BIGINT NOT NULL,
    DoctorId BIGINT NOT NULL,
    ScheduleId BIGINT NULL,
    ServiceId BIGINT NULL,
    AppointmentDate DATE NOT NULL,
    SlotStartTime TIME NOT NULL,
    SlotEndTime TIME NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    Reason NVARCHAR(500) NULL,
    AdminRemarks NVARCHAR(500) NULL,
    CreatedByUserId BIGINT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT CK_Appointments_Status CHECK (Status IN ('Pending','Approved','Rescheduled','Cancelled','Completed')),
    CONSTRAINT CK_Appointments_TimeRange CHECK (SlotStartTime < SlotEndTime),
    CONSTRAINT FK_Appointments_Patients FOREIGN KEY (PatientId) REFERENCES Patients(PatientId),
    CONSTRAINT FK_Appointments_Doctors FOREIGN KEY (DoctorId) REFERENCES Doctors(DoctorId),
    CONSTRAINT FK_Appointments_DoctorSchedules FOREIGN KEY (ScheduleId) REFERENCES DoctorSchedules(ScheduleId),
    CONSTRAINT FK_Appointments_Services FOREIGN KEY (ServiceId) REFERENCES HospitalServices(ServiceId),
    CONSTRAINT FK_Appointments_Users FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
);

CREATE INDEX IX_Appointments_DoctorDate ON Appointments(DoctorId, AppointmentDate);
CREATE INDEX IX_Appointments_PatientDate ON Appointments(PatientId, AppointmentDate);
CREATE INDEX IX_DoctorSchedules_DoctorDay ON DoctorSchedules(DoctorId, DayOfWeek);
```

---

## 4) Backend API Plan (ASP.NET Core)

## Domain models
- Create entities matching tables: `User`, `Role`, `Patient`, `Doctor`, `DoctorSchedule`, `Staff`, `HospitalService`, `Appointment`.
- Add enums: `UserRole`, `AppointmentStatus`.

## DTO examples
- Auth: `RegisterRequestDto`, `LoginRequestDto`, `AuthResponseDto`
- Doctors: `DoctorCreateDto`, `DoctorUpdateDto`, `DoctorReadDto`
- Appointments: `AppointmentCreateDto`, `AppointmentStatusUpdateDto`, `AppointmentReadDto`
- Common: `PagedRequestDto`, `PagedResponseDto<T>`, `ApiResponse<T>`

## DbContext setup
- `DbSet<Role> Roles`, `DbSet<User> Users`, etc.
- Use Fluent API for:
  - unique indexes (`Email`, `ServiceName`)
  - relationship constraints
  - enum/string conversions if needed

## Suggested Application layer services
- `IAuthService`, `IDoctorService`, `IScheduleService`, `IAppointmentService`, `IStaffService`, `IHospitalServiceService`, `IReportService`
- Service methods must enforce business rules:
  - No overlapping schedule slots
  - No double booking for same doctor/date/time
  - Only admins can approve/reschedule/cancel globally
  - Users can only view their own appointment history

## Controllers (REST)
- `AuthController`
- `UsersController`
- `DoctorsController`
- `DoctorSchedulesController`
- `StaffController`
- `HospitalServicesController`
- `AppointmentsController`
- `ReportsController`

## Cross-cutting best practices
- Global exception middleware
- FluentValidation for DTO validation
- JWT bearer auth + policy-based authorization
- Response wrapper:
  - success flag
  - message
  - data
  - errors

---

## 5) Angular Frontend Plan

## Core technical setup
- Use **Reactive Forms** for all forms.
- Use `HttpInterceptor` to attach JWT token and handle 401/403 globally.
- Use route guards:
  - `AuthGuard` for authenticated routes
  - `RoleGuard` for admin-only routes

## Angular route map (sample)

```text
/auth/login
/auth/register
/
/doctors
/services
/user/appointments
/user/profile
/admin/dashboard
/admin/doctors
/admin/schedules
/admin/staff
/admin/appointments
/admin/services
/admin/reports
```

## Angular services
- `auth.service.ts`
- `doctor.service.ts`
- `schedule.service.ts`
- `appointment.service.ts`
- `staff.service.ts`
- `hospital-service.service.ts`
- `report.service.ts`

## Page list (UI)

### Admin pages
1. Login
2. Dashboard (summary cards + charts)
3. Doctors CRUD
4. Doctor Schedules CRUD
5. Staff CRUD
6. Appointments management (approve/reschedule/cancel)
7. Services CRUD
8. Reports page

### User pages
1. Register/Login
2. Home + Hospital Info
3. Services list
4. Doctors list + specialization filter + schedule view
5. Book appointment
6. Appointment history and status
7. Profile update

---

## 6) Authentication and Authorization

## JWT flow
1. User/Admin logs in via `/api/auth/login`.
2. API validates credentials and returns JWT.
3. Angular stores token securely (prefer HttpOnly cookies in production; local/session storage only for beginner setups).
4. Interceptor attaches token to subsequent API calls.
5. Backend validates token and role claims for protected endpoints.

## Role rules
- `Admin`:
  - Full CRUD for doctors/schedules/staff/services
  - Manage all appointments
  - Access reports
- `User`:
  - Manage own profile
  - View doctors/services
  - Create appointment
  - View own appointment history

---

## 7) Appointment Booking Workflow

1. User logs in.
2. User selects doctor and date.
3. Frontend calls `GET /api/doctors/{id}/available-slots?date=...`.
4. User selects slot and optionally service/reason.
5. Frontend submits `POST /api/appointments`.
6. Backend validates:
   - user is active
   - doctor exists and active
   - selected slot belongs to doctor schedule
   - slot is not already booked
7. Appointment created with `Pending` status.
8. Admin reviews and marks `Approved` or `Rescheduled` or `Cancelled`.
9. User sees status updates in appointment history.

---

## 8) Sample API Endpoints (Module-wise)

## Auth
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/logout` (optional if token blacklist/cookies used)

## Users/Profiles
- `GET /api/users/me`
- `PUT /api/users/me`

## Doctors
- `GET /api/doctors?page=1&pageSize=10&search=cardio`
- `GET /api/doctors/{doctorId}`
- `POST /api/doctors` (Admin)
- `PUT /api/doctors/{doctorId}` (Admin)
- `DELETE /api/doctors/{doctorId}` (Admin)

## DoctorSchedules
- `GET /api/doctors/{doctorId}/schedules`
- `POST /api/doctors/{doctorId}/schedules` (Admin)
- `PUT /api/schedules/{scheduleId}` (Admin)
- `DELETE /api/schedules/{scheduleId}` (Admin)
- `GET /api/doctors/{doctorId}/available-slots?date=2026-05-15`

## Staff
- `GET /api/staff?page=1&pageSize=10&search=nurse`
- `POST /api/staff` (Admin)
- `PUT /api/staff/{staffId}` (Admin)
- `DELETE /api/staff/{staffId}` (Admin)

## HospitalServices
- `GET /api/services`
- `GET /api/services/{serviceId}`
- `POST /api/services` (Admin)
- `PUT /api/services/{serviceId}` (Admin)
- `DELETE /api/services/{serviceId}` (Admin)

## Appointments
- `POST /api/appointments` (User)
- `GET /api/appointments/my?page=1&pageSize=10&status=Pending` (User)
- `GET /api/appointments?page=1&pageSize=10&status=Pending` (Admin)
- `PUT /api/appointments/{appointmentId}/status` (Admin)
- `PUT /api/appointments/{appointmentId}/reschedule` (Admin)

## Reports (Admin)
- `GET /api/reports/dashboard-summary`
- `GET /api/reports/appointments-by-status`
- `GET /api/reports/appointments-by-doctor`

---

## 9) Dashboard Features

## Admin dashboard cards
- Total Doctors
- Total Staff
- Total Users
- Today’s Appointments
- Pending Appointments
- Cancelled/Completed this month

## Charts/tables
- Appointments by status (pie)
- Appointments trend by week/month (line)
- Top doctors by appointment count (bar)
- Recent appointments table

## User dashboard cards
- Upcoming appointments
- Last appointment status
- Quick links (book appointment, edit profile)

---

## 10) Step-by-Step Development Order

## Phase 1: Database design
1. Finalize ERD and constraints.
2. Create SQL scripts and run in SQL Server.
3. Seed roles/admin/sample data.

## Phase 2: Backend API
1. Set up Clean Architecture projects.
2. Add entities, DbContext, migrations.
3. Implement repository/service pattern.
4. Build CRUD modules: Doctors, Schedules, Staff, Services.
5. Build Appointments module with business validations.

## Phase 3: Angular frontend
1. Create Angular app with core/shared/features layout.
2. Set up routing, auth pages, shared layout.
3. Implement admin modules.
4. Implement user modules.
5. Add pagination/search UI for list pages.

## Phase 4: Authentication & authorization
1. Implement register/login APIs + JWT.
2. Add role claims and policy checks.
3. Add Angular interceptor + route guards.
4. Restrict admin and user feature access.

## Phase 5: Appointment workflow
1. Implement available slots endpoint.
2. Add booking UI and appointment creation.
3. Add admin approval/reschedule/cancel actions.
4. Add user appointment history and status timeline.

## Phase 6: Final improvements
1. Global error handling and standardized API responses.
2. Logging and audit fields.
3. Unit/integration tests.
4. Performance tuning (indexes, query optimization).
5. Deployment pipeline and environment config.

---

## Naming Conventions

- **SQL tables:** PascalCase plural (`Doctors`, `Appointments`)
- **Columns:** PascalCase (`CreatedAt`, `FullName`)
- **C# classes:** PascalCase (`DoctorSchedule`)
- **C# private fields:** `_camelCase`
- **DTOs:** suffix with `Dto` (`DoctorCreateDto`)
- **Interfaces:** prefix `I` (`IAppointmentService`)
- **Angular files:** kebab-case (`appointment-history.component.ts`)
- **Angular classes:** PascalCase (`AppointmentHistoryComponent`)

---

## Sample Seed Data

```sql
INSERT INTO Roles (Name) VALUES ('Admin'), ('User');

-- Example: hashes are placeholders
INSERT INTO Users (RoleId, FullName, Email, Phone, PasswordHash, PasswordSalt)
VALUES (1, 'System Admin', 'admin@hospital.com', '9999999999', 0x01, 0x02);

INSERT INTO HospitalServices (ServiceName, Description, Price, EstimatedDurationMinutes)
VALUES ('General Consultation', 'Basic doctor consultation', 500, 30),
       ('Cardiology Checkup', 'Heart specialist checkup', 1200, 45);

INSERT INTO Doctors (FullName, Specialization, Email, Phone, ExperienceYears, Qualification, ConsultationFee)
VALUES ('Dr. A. Kumar', 'Cardiology', 'akumar@hospital.com', '8888888888', 10, 'MD Cardiology', 1200),
       ('Dr. S. Rao', 'Dermatology', 'srao@hospital.com', '8777777777', 7, 'MD Dermatology', 900);
```

---

## Future Enhancements

1. Online payment gateway for appointment confirmation.
2. E-prescriptions and downloadable prescription history.
3. Lab test booking + lab report upload/download.
4. SMS/Email/WhatsApp notifications.
5. Video consultation (telemedicine).
6. Multi-branch hospital support.
7. Audit log and activity tracking.
8. Advanced analytics dashboard with filters/export.

---

## Practical Notes for Real Project Readiness
- Use environment-specific configs (`appsettings.Development.json`, production secrets).
- Enforce HTTPS and CORS policy.
- Add refresh tokens for secure session management.
- Add soft-delete strategy where needed (staff/services/doctors).
- Document APIs with Swagger + Postman collection.
- Add CI/CD and SQL migration strategy.
