using System.Security.Cryptography;
using System.Text;
using AngularApp4.Model.Hms;
using Microsoft.EntityFrameworkCore;
using ServiceModel = AngularApp4.Model.Service;

namespace AngularApp4.Data;

public sealed class DatabaseInitializer
{
    private static readonly (string Name, string Description)[] SystemRoles =
    {
        ("Admin", "Full hospital administration access"),
        ("Receptionist", "Front desk scheduling and patient intake"),
        ("Doctor", "Clinical workflow and appointment access"),
        ("Pharmacist", "Medicine dispensing and pharmacy control"),
        ("Lab", "Laboratory workflow and results processing"),
        ("Storekeeper", "Inventory and store operations"),
        ("Accountant", "Billing, invoices, and payment oversight"),
        ("User", "Self-service patient portal access")
    };

    private static readonly (string Key, string Name)[] Modules =
    {
        ("dashboard", "Dashboard"),
        ("masters", "Master Setup"),
        ("patients", "Patients"),
        ("appointments", "Appointments"),
        ("pharmacy", "Pharmacy"),
        ("laboratory", "Laboratory"),
        ("inventory", "Inventory"),
        ("billing", "Billing"),
        ("branches", "Branches"),
        ("settings", "Settings")
    };

    private readonly AppDbContext _db;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext db, ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.MigrateAsync(cancellationToken);
        await EnsureRolesAsync(cancellationToken);
        await EnsureRolePermissionsAsync(cancellationToken);
        await EnsureDemoAccountsAsync(cancellationToken);
        await EnsureHospitalProfileAsync(cancellationToken);
        await EnsureBranchesAsync(cancellationToken);
        await EnsureDepartmentsAsync(cancellationToken);
        await EnsurePatientCategoriesAsync(cancellationToken);
        await EnsurePatientCategoryAssignmentsAsync(cancellationToken);
        await EnsureSeedServicesAsync(cancellationToken);
        await EnsureDoctorsAsync(cancellationToken);
        await EnsureStaffAsync(cancellationToken);
        await EnsureWardsAndBedsAsync(cancellationToken);
        await EnsureAppointmentsAsync(cancellationToken);
        await EnsureInventoryAsync(cancellationToken);
        await EnsureBillingAsync(cancellationToken);

        _logger.LogInformation("Database schema verified and initial seed data applied.");
    }

    private async Task EnsureRolesAsync(CancellationToken cancellationToken)
    {
        var hasChanges = false;

        foreach (var (name, description) in SystemRoles)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
            if (role is null)
            {
                _db.Roles.Add(new Role
                {
                    Name = name,
                    Description = description,
                    IsSystemRole = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                hasChanges = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(role.Description))
            {
                role.Description = description;
                hasChanges = true;
            }

            if (!role.IsSystemRole)
            {
                role.IsSystemRole = true;
                hasChanges = true;
            }

            if (!role.IsActive)
            {
                role.IsActive = true;
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureRolePermissionsAsync(CancellationToken cancellationToken)
    {
        var roles = await _db.Roles.AsNoTracking().ToListAsync(cancellationToken);
        var existingPermissions = await _db.RolePermissions.ToListAsync(cancellationToken);
        var hasChanges = false;

        foreach (var role in roles)
        {
            var defaults = GetDefaultPermissions(role.RoleId, role.Name).ToDictionary(x => x.ModuleKey, StringComparer.OrdinalIgnoreCase);
            foreach (var module in Modules)
            {
                if (existingPermissions.Any(x => x.RoleId == role.RoleId && x.ModuleKey == module.Key))
                {
                    continue;
                }

                if (!defaults.TryGetValue(module.Key, out var permission))
                {
                    permission = new RolePermission
                    {
                        RoleId = role.RoleId,
                        ModuleKey = module.Key,
                        ModuleName = module.Name,
                        UpdatedAt = DateTime.UtcNow
                    };
                }

                _db.RolePermissions.Add(permission);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureDemoAccountsAsync(CancellationToken cancellationToken)
    {
        await EnsureUserAsync("Nexus Admin", "admin@nexus.local", "Admin@123", "Admin", false, cancellationToken);
        await EnsureUserAsync("Nexus User", "user@nexus.local", "User@123", "User", true, cancellationToken);
        await EnsureUserAsync("Mira Adhikari", "mira.patient@nexus.local", "User@123", "User", true, cancellationToken);
        await EnsureUserAsync("Sudeep Khadka", "sudeep.patient@nexus.local", "User@123", "User", true, cancellationToken);
        await EnsureUserAsync("Anisha Shrestha", "anisha.patient@nexus.local", "User@123", "User", true, cancellationToken);
        await EnsureUserAsync("Rohan Gautam", "rohan.patient@nexus.local", "User@123", "User", true, cancellationToken);
    }

    private async Task EnsureHospitalProfileAsync(CancellationToken cancellationToken)
    {
        var profile = await _db.HospitalProfiles.OrderBy(x => x.HospitalProfileId).FirstOrDefaultAsync(cancellationToken);
        if (profile is not null)
        {
            return;
        }

        _db.HospitalProfiles.Add(new HospitalProfile
        {
            HospitalName = "Nexus Multi-Speciality Hospital",
            LogoUrl = "https://placehold.co/160x160?text=Nexus",
            AddressLine1 = "Putalisadak, Ward 28",
            City = "Kathmandu",
            StateOrProvince = "Bagmati",
            PostalCode = "44600",
            Country = "Nepal",
            ContactEmail = "info@nexushospital.local",
            ContactPhone = "+977-01-5550000",
            TaxLabel = "VAT",
            TaxRegistrationNumber = "VAT-490231",
            TaxPercentage = 13m,
            CurrencyCode = "NPR",
            InvoicePrefix = "NEX",
            InvoiceStartingNumber = 5001,
            InvoiceFooterNote = "Thank you for trusting Nexus Hospital.",
            MultiBranchEnabled = true,
            UpdatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBranchesAsync(CancellationToken cancellationToken)
    {
        if (await _db.Branches.AnyAsync(cancellationToken))
        {
            return;
        }

        _db.Branches.AddRange(
            new Branch
            {
                Name = "Kathmandu Central",
                Code = "KTM",
                Address = "Putalisadak, Kathmandu",
                ContactPhone = "+977-01-5550000",
                ContactEmail = "ktm@nexushospital.local",
                IsPrimary = true,
                IsActive = true,
                TotalBeds = 120,
                OccupiedBeds = 94,
                CreatedAt = DateTime.UtcNow
            },
            new Branch
            {
                Name = "Lalitpur Care Center",
                Code = "LTP",
                Address = "Jawalakhel, Lalitpur",
                ContactPhone = "+977-01-5551010",
                ContactEmail = "lalitpur@nexushospital.local",
                IsPrimary = false,
                IsActive = true,
                TotalBeds = 60,
                OccupiedBeds = 41,
                CreatedAt = DateTime.UtcNow
            },
            new Branch
            {
                Name = "Bhaktapur Diagnostic Hub",
                Code = "BKT",
                Address = "Suryabinayak, Bhaktapur",
                ContactPhone = "+977-01-5552020",
                ContactEmail = "bhaktapur@nexushospital.local",
                IsPrimary = false,
                IsActive = true,
                TotalBeds = 40,
                OccupiedBeds = 19,
                CreatedAt = DateTime.UtcNow
            });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDepartmentsAsync(CancellationToken cancellationToken)
    {
        var branches = await _db.Branches.OrderBy(x => x.BranchId).ToListAsync(cancellationToken);
        if (branches.Count == 0)
        {
            return;
        }

        foreach (var branch in branches)
        {
            await EnsureDepartmentAsync(branch.BranchId, "Outpatient Department", "OPD", "General outpatient consultation unit", cancellationToken);
            await EnsureDepartmentAsync(branch.BranchId, "Inpatient Department", "IPD", "Inpatient admissions and care", cancellationToken);
            await EnsureDepartmentAsync(branch.BranchId, "Pharmacy", "PHARM", "Medicine dispensing and pharmacy operations", cancellationToken);
            await EnsureDepartmentAsync(branch.BranchId, "Laboratory", "LAB", "Diagnostic laboratory and sample handling", cancellationToken);
            await EnsureDepartmentAsync(branch.BranchId, "Operation Theatre", "OT", "Surgical theatre services", cancellationToken);
            await EnsureDepartmentAsync(branch.BranchId, "Intensive Care Unit", "ICU", "Critical care and monitoring", cancellationToken);
        }
    }

    private async Task EnsurePatientCategoriesAsync(CancellationToken cancellationToken)
    {
        await EnsurePatientCategoryAsync("General", "Standard outpatient and inpatient patients", 1, cancellationToken);
        await EnsurePatientCategoryAsync("Emergency", "Emergency and urgent-care patients", 2, cancellationToken);
        await EnsurePatientCategoryAsync("Corporate", "Corporate or employer-sponsored patients", 3, cancellationToken);
        await EnsurePatientCategoryAsync("Insurance", "Patients billed via insurance partners", 4, cancellationToken);
        await EnsurePatientCategoryAsync("VIP", "VIP and priority-service patients", 5, cancellationToken);
    }

    private async Task EnsurePatientCategoryAssignmentsAsync(CancellationToken cancellationToken)
    {
        var categoryMap = await _db.PatientCategories
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Name, x => x.PatientCategoryId, cancellationToken);

        var assignments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["user@nexus.local"] = "General",
            ["mira.patient@nexus.local"] = "Emergency",
            ["sudeep.patient@nexus.local"] = "Corporate",
            ["anisha.patient@nexus.local"] = "Insurance",
            ["rohan.patient@nexus.local"] = "VIP"
        };

        var users = await _db.Users
            .Where(x => assignments.Keys.Contains(x.Email))
            .ToDictionaryAsync(x => x.Email, cancellationToken);

        var patients = await _db.Patients.ToListAsync(cancellationToken);
        var hasChanges = false;

        foreach (var (email, categoryName) in assignments)
        {
            if (!users.TryGetValue(email, out var user) || !categoryMap.TryGetValue(categoryName, out var categoryId))
            {
                continue;
            }

            var patient = patients.FirstOrDefault(x => x.UserId == user.UserId);
            if (patient is null || patient.PatientCategoryId == categoryId)
            {
                continue;
            }

            patient.PatientCategoryId = categoryId;
            patient.UpdatedAt = DateTime.UtcNow;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureDoctorsAsync(CancellationToken cancellationToken)
    {
        var branches = await _db.Branches.OrderBy(x => x.BranchId).ToListAsync(cancellationToken);
        if (branches.Count == 0)
        {
            return;
        }

        var branchMap = branches.ToDictionary(x => x.Code, x => x.BranchId, StringComparer.OrdinalIgnoreCase);
        var departments = await _db.Departments.ToListAsync(cancellationToken);
        var departmentMap = departments.ToDictionary(
            x => $"{x.BranchId}:{x.Code}",
            x => x.DepartmentId,
            StringComparer.OrdinalIgnoreCase);

        await EnsureDoctorAsync(
            "Dr. Aryan Shah",
            "Cardiology",
            "aryan.shah@nexushospital.local",
            "+977-9800000001",
            12,
            "MD, DM Cardiology",
            2500m,
            branchMap["KTM"],
            departmentMap[$"{branchMap["KTM"]}:OPD"],
            "sun,mon,tue,wed,thu",
            new TimeSpan(9, 0, 0),
            new TimeSpan(13, 0, 0),
            cancellationToken);

        await EnsureDoctorAsync(
            "Dr. Nisha Gurung",
            "Neurology",
            "nisha.gurung@nexushospital.local",
            "+977-9800000002",
            9,
            "MD Neurology",
            2800m,
            branchMap["LTP"],
            departmentMap[$"{branchMap["LTP"]}:OPD"],
            "sun,mon,tue,wed,fri",
            new TimeSpan(10, 0, 0),
            new TimeSpan(15, 0, 0),
            cancellationToken);

        await EnsureDoctorAsync(
            "Dr. Samir Rana",
            "General Medicine",
            "samir.rana@nexushospital.local",
            "+977-9800000003",
            7,
            "MD Internal Medicine",
            1800m,
            branchMap["BKT"],
            departmentMap[$"{branchMap["BKT"]}:OPD"],
            "sun,mon,wed,thu,fri",
            new TimeSpan(8, 30, 0),
            new TimeSpan(12, 30, 0),
            cancellationToken);
    }

    private async Task EnsureStaffAsync(CancellationToken cancellationToken)
    {
        var branches = await _db.Branches.OrderBy(x => x.BranchId).ToListAsync(cancellationToken);
        var departments = await _db.Departments.ToListAsync(cancellationToken);
        if (branches.Count == 0 || departments.Count == 0)
        {
            return;
        }

        var branchMap = branches.ToDictionary(x => x.Code, x => x.BranchId, StringComparer.OrdinalIgnoreCase);
        var departmentMap = departments.ToDictionary(
            x => $"{x.BranchId}:{x.Code}",
            x => x.DepartmentId,
            StringComparer.OrdinalIgnoreCase);

        await EnsureStaffMemberAsync(
            "Sanjana Karki",
            "STF-1001",
            "Nurse Supervisor",
            "Day",
            "sanjana.karki@nexushospital.local",
            "+977-9811111101",
            branchMap["KTM"],
            departmentMap[$"{branchMap["KTM"]}:ICU"],
            new DateTime(2024, 4, 1),
            cancellationToken);

        await EnsureStaffMemberAsync(
            "Prabesh KC",
            "STF-1002",
            "Lab Technician",
            "Morning",
            "prabesh.kc@nexushospital.local",
            "+977-9811111102",
            branchMap["LTP"],
            departmentMap[$"{branchMap["LTP"]}:LAB"],
            new DateTime(2024, 8, 15),
            cancellationToken);

        await EnsureStaffMemberAsync(
            "Nirmala Tamang",
            "STF-1003",
            "Pharmacy Officer",
            "Evening",
            "nirmala.tamang@nexushospital.local",
            "+977-9811111103",
            branchMap["BKT"],
            departmentMap[$"{branchMap["BKT"]}:PHARM"],
            new DateTime(2025, 1, 5),
            cancellationToken);
    }

    private async Task EnsureWardsAndBedsAsync(CancellationToken cancellationToken)
    {
        var branches = await _db.Branches.OrderBy(x => x.BranchId).ToListAsync(cancellationToken);
        var departments = await _db.Departments.ToListAsync(cancellationToken);
        if (branches.Count == 0 || departments.Count == 0)
        {
            return;
        }

        var branchMap = branches.ToDictionary(x => x.Code, x => x.BranchId, StringComparer.OrdinalIgnoreCase);
        var departmentMap = departments.ToDictionary(
            x => $"{x.BranchId}:{x.Code}",
            x => x.DepartmentId,
            StringComparer.OrdinalIgnoreCase);

        var ktmIcuWard = await EnsureWardAsync(
            branchMap["KTM"],
            departmentMap[$"{branchMap["KTM"]}:ICU"],
            "Critical Care Ward",
            "ICU",
            "Private",
            8500m,
            cancellationToken);

        var ltpOtWard = await EnsureWardAsync(
            branchMap["LTP"],
            departmentMap[$"{branchMap["LTP"]}:OT"],
            "Surgical Recovery Ward",
            "OT",
            "Semi-Private",
            6200m,
            cancellationToken);

        var bktIpdWard = await EnsureWardAsync(
            branchMap["BKT"],
            departmentMap[$"{branchMap["BKT"]}:IPD"],
            "General Admission Ward",
            "IPD",
            "General",
            3200m,
            cancellationToken);

        await EnsureBedAsync(ktmIcuWard.WardId, branchMap["KTM"], departmentMap[$"{branchMap["KTM"]}:ICU"], "ICU-01", 9000m, true, cancellationToken);
        await EnsureBedAsync(ktmIcuWard.WardId, branchMap["KTM"], departmentMap[$"{branchMap["KTM"]}:ICU"], "ICU-02", 9000m, false, cancellationToken);
        await EnsureBedAsync(ltpOtWard.WardId, branchMap["LTP"], departmentMap[$"{branchMap["LTP"]}:OT"], "REC-01", 6500m, false, cancellationToken);
        await EnsureBedAsync(bktIpdWard.WardId, branchMap["BKT"], departmentMap[$"{branchMap["BKT"]}:IPD"], "GEN-12", 3200m, true, cancellationToken);
    }

    private async Task EnsureAppointmentsAsync(CancellationToken cancellationToken)
    {
        if (await _db.Appointments.AnyAsync(cancellationToken))
        {
            return;
        }

        var patients = await _db.Patients.OrderBy(x => x.PatientId).ToListAsync(cancellationToken);
        var patientUsers = await _db.Users
            .Where(x => patients.Select(p => p.UserId).Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, cancellationToken);
        var doctors = await _db.Doctors.OrderBy(x => x.DoctorId).ToListAsync(cancellationToken);
        var services = await _db.Services.OrderBy(x => x.Id).ToListAsync(cancellationToken);

        if (patients.Count < 3 || doctors.Count < 3 || services.Count < 3)
        {
            return;
        }

        var today = DateTime.Today;
        var appointments = new[]
        {
            new Appointment
            {
                PatientId = patients[0].PatientId,
                DoctorId = doctors[0].DoctorId,
                ServiceId = services[0].Id,
                AppointmentDate = today,
                SlotStartTime = new TimeSpan(9, 0, 0),
                SlotEndTime = new TimeSpan(9, 45, 0),
                Status = AppointmentStatus.Pending,
                Reason = "Routine cardiac follow-up",
                CreatedByUserId = patientUsers[patients[0].UserId].UserId,
                CreatedAt = DateTime.UtcNow
            },
            new Appointment
            {
                PatientId = patients[1].PatientId,
                DoctorId = doctors[1].DoctorId,
                ServiceId = services[2].Id,
                AppointmentDate = today,
                SlotStartTime = new TimeSpan(11, 0, 0),
                SlotEndTime = new TimeSpan(12, 0, 0),
                Status = AppointmentStatus.Approved,
                Reason = "Migraine review",
                CreatedByUserId = patientUsers[patients[1].UserId].UserId,
                CreatedAt = DateTime.UtcNow
            },
            new Appointment
            {
                PatientId = patients[2].PatientId,
                DoctorId = doctors[2].DoctorId,
                ServiceId = services[1].Id,
                AppointmentDate = today.AddDays(1),
                SlotStartTime = new TimeSpan(14, 0, 0),
                SlotEndTime = new TimeSpan(15, 0, 0),
                Status = AppointmentStatus.Completed,
                Reason = "Post therapy evaluation",
                CreatedByUserId = patientUsers[patients[2].UserId].UserId,
                CreatedAt = DateTime.UtcNow
            },
            new Appointment
            {
                PatientId = patients[3].PatientId,
                DoctorId = doctors[2].DoctorId,
                ServiceId = services[3].Id,
                AppointmentDate = today.AddDays(2),
                SlotStartTime = new TimeSpan(16, 0, 0),
                SlotEndTime = new TimeSpan(17, 0, 0),
                Status = AppointmentStatus.Cancelled,
                Reason = "Cancelled by patient",
                CreatedByUserId = patientUsers[patients[3].UserId].UserId,
                CreatedAt = DateTime.UtcNow
            }
        };

        _db.Appointments.AddRange(appointments);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureInventoryAsync(CancellationToken cancellationToken)
    {
        if (await _db.MedicineInventoryItems.AnyAsync(cancellationToken))
        {
            return;
        }

        var branches = await _db.Branches.OrderBy(x => x.BranchId).ToListAsync(cancellationToken);
        if (branches.Count == 0)
        {
            return;
        }

        var today = DateTime.Today;
        _db.MedicineInventoryItems.AddRange(
            new MedicineInventoryItem
            {
                BranchId = branches[0].BranchId,
                Name = "Amoxicillin 500mg",
                Category = "Antibiotic",
                BatchNumber = "AMX-2401",
                QuantityInStock = 14,
                ReorderLevel = 25,
                ExpiryDate = today.AddMonths(5),
                UnitPrice = 24m,
                CreatedAt = DateTime.UtcNow
            },
            new MedicineInventoryItem
            {
                BranchId = branches[0].BranchId,
                Name = "Insulin Pen",
                Category = "Diabetes Care",
                BatchNumber = "INS-2402",
                QuantityInStock = 8,
                ReorderLevel = 15,
                ExpiryDate = today.AddMonths(2),
                UnitPrice = 650m,
                CreatedAt = DateTime.UtcNow
            },
            new MedicineInventoryItem
            {
                BranchId = branches[1].BranchId,
                Name = "Vitamin D Syrup",
                Category = "Supplements",
                BatchNumber = "VDS-2311",
                QuantityInStock = 22,
                ReorderLevel = 20,
                ExpiryDate = today.AddDays(-12),
                UnitPrice = 180m,
                CreatedAt = DateTime.UtcNow
            },
            new MedicineInventoryItem
            {
                BranchId = branches[2].BranchId,
                Name = "Cefixime 200mg",
                Category = "Antibiotic",
                BatchNumber = "CFX-2308",
                QuantityInStock = 6,
                ReorderLevel = 18,
                ExpiryDate = today.AddDays(-2),
                UnitPrice = 32m,
                CreatedAt = DateTime.UtcNow
            },
            new MedicineInventoryItem
            {
                BranchId = branches[1].BranchId,
                Name = "Paracetamol 500mg",
                Category = "Analgesic",
                BatchNumber = "PCM-2405",
                QuantityInStock = 56,
                ReorderLevel = 30,
                ExpiryDate = today.AddMonths(10),
                UnitPrice = 6m,
                CreatedAt = DateTime.UtcNow
            });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBillingAsync(CancellationToken cancellationToken)
    {
        if (await _db.BillingInvoices.AnyAsync(cancellationToken))
        {
            return;
        }

        var appointments = await _db.Appointments.OrderBy(x => x.AppointmentId).ToListAsync(cancellationToken);
        var patients = await _db.Patients.OrderBy(x => x.PatientId).ToListAsync(cancellationToken);
        var branches = await _db.Branches.OrderBy(x => x.BranchId).ToListAsync(cancellationToken);
        var today = DateTime.Today;

        if (patients.Count == 0 || branches.Count == 0)
        {
            return;
        }

        _db.BillingInvoices.AddRange(
            new BillingInvoice
            {
                InvoiceNumber = "NEX-5001",
                PatientId = patients[0].PatientId,
                AppointmentId = appointments.ElementAtOrDefault(0)?.AppointmentId,
                BranchId = branches[0].BranchId,
                TotalAmount = 7500m,
                AmountPaid = 7500m,
                Status = InvoiceStatus.Paid,
                InvoiceDate = today,
                LastPaymentDate = today,
                DueDate = today,
                Notes = "Paid at reception",
                CreatedAt = DateTime.UtcNow
            },
            new BillingInvoice
            {
                InvoiceNumber = "NEX-5002",
                PatientId = patients[1].PatientId,
                AppointmentId = appointments.ElementAtOrDefault(1)?.AppointmentId,
                BranchId = branches[1].BranchId,
                TotalAmount = 4200m,
                AmountPaid = 2000m,
                Status = InvoiceStatus.Partial,
                InvoiceDate = today,
                LastPaymentDate = today,
                DueDate = today.AddDays(5),
                Notes = "Balance due after lab test",
                CreatedAt = DateTime.UtcNow
            },
            new BillingInvoice
            {
                InvoiceNumber = "NEX-5003",
                PatientId = patients[2].PatientId,
                AppointmentId = appointments.ElementAtOrDefault(2)?.AppointmentId,
                BranchId = branches[0].BranchId,
                TotalAmount = 9800m,
                AmountPaid = 0m,
                Status = InvoiceStatus.Pending,
                InvoiceDate = today.AddDays(-1),
                DueDate = today.AddDays(3),
                Notes = "Pending insurance confirmation",
                CreatedAt = DateTime.UtcNow
            },
            new BillingInvoice
            {
                InvoiceNumber = "NEX-5004",
                PatientId = patients[3].PatientId,
                AppointmentId = appointments.ElementAtOrDefault(3)?.AppointmentId,
                BranchId = branches[2].BranchId,
                TotalAmount = 5600m,
                AmountPaid = 5600m,
                Status = InvoiceStatus.Paid,
                InvoiceDate = today.AddDays(-2),
                LastPaymentDate = today.AddDays(-2),
                DueDate = today.AddDays(-2),
                Notes = "Paid online",
                CreatedAt = DateTime.UtcNow
            });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDepartmentAsync(long branchId, string name, string code, string description, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var department = await _db.Departments.FirstOrDefaultAsync(
            x => x.BranchId == branchId && x.Code == normalizedCode,
            cancellationToken);

        if (department is null)
        {
            _db.Departments.Add(new Department
            {
                BranchId = branchId,
                Name = name,
                Code = normalizedCode,
                Description = description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var hasChanges = false;
        if (department.Name != name)
        {
            department.Name = name;
            hasChanges = true;
        }

        if (department.Description != description)
        {
            department.Description = description;
            hasChanges = true;
        }

        if (!department.IsActive)
        {
            department.IsActive = true;
            hasChanges = true;
        }

        if (hasChanges)
        {
            department.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsurePatientCategoryAsync(string name, string description, int priorityOrder, CancellationToken cancellationToken)
    {
        var item = await _db.PatientCategories.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
        if (item is null)
        {
            _db.PatientCategories.Add(new PatientCategory
            {
                Name = name,
                Description = description,
                PriorityOrder = priorityOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var hasChanges = false;
        if (item.Description != description)
        {
            item.Description = description;
            hasChanges = true;
        }

        if (item.PriorityOrder != priorityOrder)
        {
            item.PriorityOrder = priorityOrder;
            hasChanges = true;
        }

        if (!item.IsActive)
        {
            item.IsActive = true;
            hasChanges = true;
        }

        if (hasChanges)
        {
            item.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureDoctorAsync(
        string fullName,
        string specialization,
        string email,
        string phone,
        int experienceYears,
        string qualification,
        decimal consultationFee,
        long branchId,
        long departmentId,
        string opdDays,
        TimeSpan opdStartTime,
        TimeSpan opdEndTime,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var doctor = await _db.Doctors.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (doctor is null)
        {
            _db.Doctors.Add(new Doctor
            {
                BranchId = branchId,
                DepartmentId = departmentId,
                FullName = fullName,
                Specialization = specialization,
                Email = normalizedEmail,
                Phone = phone,
                ExperienceYears = experienceYears,
                Qualification = qualification,
                OpdDays = opdDays,
                OpdStartTime = opdStartTime,
                OpdEndTime = opdEndTime,
                ConsultationFee = consultationFee,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        doctor.BranchId = branchId;
        doctor.DepartmentId = departmentId;
        doctor.FullName = fullName;
        doctor.Specialization = specialization;
        doctor.Phone = phone;
        doctor.ExperienceYears = experienceYears;
        doctor.Qualification = qualification;
        doctor.OpdDays = opdDays;
        doctor.OpdStartTime = opdStartTime;
        doctor.OpdEndTime = opdEndTime;
        doctor.ConsultationFee = consultationFee;
        doctor.IsActive = true;
        doctor.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureStaffMemberAsync(
        string fullName,
        string employeeCode,
        string designation,
        string shift,
        string email,
        string phone,
        long branchId,
        long departmentId,
        DateTime joinDate,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var departmentName = await _db.Departments
            .Where(x => x.DepartmentId == departmentId)
            .Select(x => x.Name)
            .FirstAsync(cancellationToken);

        var item = await _db.Staff.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (item is null)
        {
            _db.Staff.Add(new Staff
            {
                BranchId = branchId,
                DepartmentId = departmentId,
                Department = departmentName,
                FullName = fullName,
                EmployeeCode = employeeCode,
                Designation = designation,
                Shift = shift,
                Email = normalizedEmail,
                Phone = phone,
                JoinDate = joinDate.Date,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        item.BranchId = branchId;
        item.DepartmentId = departmentId;
        item.Department = departmentName;
        item.FullName = fullName;
        item.EmployeeCode = employeeCode;
        item.Designation = designation;
        item.Shift = shift;
        item.Phone = phone;
        item.JoinDate = joinDate.Date;
        item.IsActive = true;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Ward> EnsureWardAsync(
        long branchId,
        long? departmentId,
        string name,
        string wardType,
        string roomType,
        decimal chargePerDay,
        CancellationToken cancellationToken)
    {
        var item = await _db.Wards.FirstOrDefaultAsync(x => x.BranchId == branchId && x.Name == name, cancellationToken);
        if (item is null)
        {
            item = new Ward
            {
                BranchId = branchId,
                DepartmentId = departmentId,
                Name = name,
                WardType = wardType,
                RoomType = roomType,
                ChargePerDay = chargePerDay,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Wards.Add(item);
            await _db.SaveChangesAsync(cancellationToken);
            return item;
        }

        item.DepartmentId = departmentId;
        item.WardType = wardType;
        item.RoomType = roomType;
        item.ChargePerDay = chargePerDay;
        item.IsActive = true;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return item;
    }

    private async Task EnsureBedAsync(
        long wardId,
        long branchId,
        long? departmentId,
        string bedNumber,
        decimal chargePerDay,
        bool isOccupied,
        CancellationToken cancellationToken)
    {
        var normalizedBedNumber = bedNumber.Trim().ToUpperInvariant();
        var item = await _db.Beds.FirstOrDefaultAsync(
            x => x.WardId == wardId && x.BedNumber == normalizedBedNumber,
            cancellationToken);

        if (item is null)
        {
            _db.Beds.Add(new Bed
            {
                WardId = wardId,
                BranchId = branchId,
                DepartmentId = departmentId,
                BedNumber = normalizedBedNumber,
                ChargePerDay = chargePerDay,
                IsOccupied = isOccupied,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        item.BranchId = branchId;
        item.DepartmentId = departmentId;
        item.ChargePerDay = chargePerDay;
        item.IsOccupied = isOccupied;
        item.IsActive = true;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUserAsync(
        string fullName,
        string email,
        string password,
        string roleName,
        bool ensurePatientProfile,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var role = await _db.Roles.FirstAsync(x => x.Name == roleName, cancellationToken);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            CreatePasswordHash(password, out var hash, out var salt);

            user = new User
            {
                FullName = fullName,
                Email = normalizedEmail,
                RoleId = role.RoleId,
                PasswordHash = hash,
                PasswordSalt = salt,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var hasChanges = false;

            if (user.RoleId != role.RoleId)
            {
                user.RoleId = role.RoleId;
                hasChanges = true;
            }

            if (!user.IsActive)
            {
                user.IsActive = true;
                hasChanges = true;
            }

            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                user.FullName = fullName;
                hasChanges = true;
            }

            if (hasChanges)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        if (ensurePatientProfile && !await _db.Patients.AnyAsync(x => x.UserId == user.UserId, cancellationToken))
        {
            _db.Patients.Add(new Patient
            {
                UserId = user.UserId,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureSeedServicesAsync(CancellationToken cancellationToken)
    {
        if (await _db.Services.AnyAsync(cancellationToken))
        {
            return;
        }

        _db.Services.AddRange(GetSeedServices());
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<RolePermission> GetDefaultPermissions(long roleId, string roleName)
    {
        return roleName switch
        {
            "Admin" => FullAccess(roleId),
            "Receptionist" => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>
            {
                ["dashboard"] = (true, false, false, false),
                ["masters"] = (true, false, false, false),
                ["patients"] = (true, true, true, false),
                ["appointments"] = (true, true, true, true),
                ["billing"] = (true, true, true, false),
                ["branches"] = (true, false, false, false)
            }),
            "Doctor" => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>
            {
                ["dashboard"] = (true, false, false, false),
                ["masters"] = (true, false, false, false),
                ["patients"] = (true, false, true, false),
                ["appointments"] = (true, false, true, false),
                ["laboratory"] = (true, true, true, false)
            }),
            "Pharmacist" => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>
            {
                ["dashboard"] = (true, false, false, false),
                ["masters"] = (true, false, false, false),
                ["pharmacy"] = (true, true, true, false),
                ["inventory"] = (true, true, true, false),
                ["billing"] = (true, false, false, false)
            }),
            "Lab" => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>
            {
                ["dashboard"] = (true, false, false, false),
                ["masters"] = (true, false, false, false),
                ["laboratory"] = (true, true, true, false),
                ["patients"] = (true, false, false, false)
            }),
            "Storekeeper" => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>
            {
                ["dashboard"] = (true, false, false, false),
                ["masters"] = (true, false, false, false),
                ["inventory"] = (true, true, true, true),
                ["branches"] = (true, false, false, false)
            }),
            "Accountant" => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>
            {
                ["dashboard"] = (true, false, false, false),
                ["masters"] = (true, false, false, false),
                ["billing"] = (true, true, true, false),
                ["patients"] = (true, false, false, false),
                ["branches"] = (true, false, false, false)
            }),
            "User" => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>
            {
                ["appointments"] = (true, true, false, false)
            }),
            _ => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>())
        };
    }

    private static IEnumerable<RolePermission> FullAccess(long roleId) =>
        Modules.Select(module => new RolePermission
        {
            RoleId = roleId,
            ModuleKey = module.Key,
            ModuleName = module.Name,
            CanView = true,
            CanAdd = true,
            CanEdit = true,
            CanDelete = true,
            UpdatedAt = DateTime.UtcNow
        });

    private static IEnumerable<RolePermission> ModuleSet(
        long roleId,
        IReadOnlyDictionary<string, (bool View, bool Add, bool Edit, bool Delete)> allowed)
    {
        return Modules.Select(module =>
        {
            allowed.TryGetValue(module.Key, out var permission);
            return new RolePermission
            {
                RoleId = roleId,
                ModuleKey = module.Key,
                ModuleName = module.Name,
                CanView = permission.View,
                CanAdd = permission.Add,
                CanEdit = permission.Edit,
                CanDelete = permission.Delete,
                UpdatedAt = DateTime.UtcNow
            };
        });
    }

    private static IEnumerable<ServiceModel> GetSeedServices()
    {
        return new[]
        {
            new ServiceModel
            {
                Name = "Initial Consultation",
                Category = "Health",
                DurationMinutes = 45,
                Price = 75.00m,
                Icon = "medical_services",
                Color = "#3b82f6",
                Description = "Comprehensive assessment",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ServiceModel
            {
                Name = "Deep Tissue Massage",
                Category = "Therapy",
                DurationMinutes = 60,
                Price = 120.00m,
                Icon = "spa",
                Color = "#8b5cf6",
                Description = "Muscle recovery",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ServiceModel
            {
                Name = "Wellness Coaching",
                Category = "Consulting",
                DurationMinutes = 60,
                Price = 90.00m,
                Icon = "psychology",
                Color = "#f59e0b",
                Description = "Lifestyle coaching",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ServiceModel
            {
                Name = "Physical Coaching",
                Category = "Checkup",
                DurationMinutes = 60,
                Price = 90.00m,
                Icon = "psychology",
                Color = "#10b981",
                Description = "Personalized physical assessment and coaching",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
    }

    private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        using var hmac = new HMACSHA512();
        salt = hmac.Key;
        hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }
}
