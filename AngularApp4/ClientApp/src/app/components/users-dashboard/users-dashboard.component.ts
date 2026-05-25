import { Component, OnInit } from '@angular/core';
import { AdminUser, RoleDetails, RolePermission } from '../../core/models/hms/admin-ops.model';
import { AdminOpsService } from '../../core/services/hms/admin-ops.service';

const MODULE_CATALOG: ReadonlyArray<Pick<RolePermission, 'moduleKey' | 'moduleName' | 'menuKey' | 'pageRoute'>> = [
  { moduleKey: 'dashboard', moduleName: 'Dashboard', menuKey: 'overview', pageRoute: '/admin/dashboard' },
  { moduleKey: 'patients', moduleName: 'Patients', menuKey: 'overview', pageRoute: '/admin/patients' },
  { moduleKey: 'services', moduleName: 'Services', menuKey: 'overview', pageRoute: '/admin/services' },
  { moduleKey: 'appointments', moduleName: 'Appointments', menuKey: 'overview', pageRoute: '/admin/bookings' },
  { moduleKey: 'admissions', moduleName: 'Admissions', menuKey: 'overview', pageRoute: '/admin/admissions' },
  { moduleKey: 'notifications', moduleName: 'Notifications', menuKey: 'overview', pageRoute: '/admin/notifications' },
  { moduleKey: 'masters', moduleName: 'Master Setup', menuKey: 'masters', pageRoute: '/admin/masters' },
  { moduleKey: 'pharmacy', moduleName: 'Pharmacy', menuKey: 'supply-chain', pageRoute: '/admin/inventory/medicines' },
  { moduleKey: 'laboratory', moduleName: 'Laboratory', menuKey: 'laboratory', pageRoute: '/admin/laboratory' },
  { moduleKey: 'inventory', moduleName: 'Inventory', menuKey: 'supply-chain', pageRoute: '/admin/inventory' },
  { moduleKey: 'billing', moduleName: 'Billing', menuKey: 'billing', pageRoute: '/admin/billing' },
  { moduleKey: 'monitoring', moduleName: 'Monitoring', menuKey: 'monitoring', pageRoute: '/admin/monitoring' },
  { moduleKey: 'branches', moduleName: 'Branches', menuKey: 'masters', pageRoute: '/admin/masters/departments' },
  { moduleKey: 'roles', moduleName: 'Roles & Access', menuKey: 'administration', pageRoute: '/admin/roles' },
  { moduleKey: 'settings', moduleName: 'Settings', menuKey: 'administration', pageRoute: '/admin/settings' }
];

@Component({
  selector: 'app-users-dashboard',
  templateUrl: './users-dashboard.component.html',
  styleUrls: ['./users-dashboard.component.scss']
})
export class UsersDashboardComponent implements OnInit {
  roles: RoleDetails[] = [];
  users: AdminUser[] = [];
  editingRole: RoleDetails | null = null;
  editingUser: AdminUser | null = null;
  activeTab: 'accounts' | 'access' | 'roleAccounts' = 'accounts';
  isLoading = true;
  isSaving = false;
  isSavingUser = false;
  errorMessage = '';
  statusMessage = '';

  draftRoleName = '';
  draftRoleDescription = '';
  draftUser = {
    roleId: 0,
    fullName: '',
    email: '',
    phone: '',
    password: ''
  };
  constructor(private readonly adminOps: AdminOpsService) {}

  ngOnInit(): void {
    this.loadRoles();
  }

  get activeRoles(): number {
    return this.roles.filter((role) => role.isActive).length;
  }

  get systemRoles(): number {
    return this.roles.filter((role) => role.isSystemRole).length;
  }

  get customRoles(): number {
    return this.roles.filter((role) => !role.isSystemRole).length;
  }

  get staffUsers(): number {
    return this.users.length;
  }

  get activeUsers(): number {
    return this.users.filter((user) => user.isActive).length;
  }

  get assignableRoles(): RoleDetails[] {
    return this.roles.filter((role) => role.isActive && role.name !== 'User');
  }

  setTab(tab: 'accounts' | 'access' | 'roleAccounts'): void {
    this.activeTab = tab;
    this.statusMessage = '';
    this.errorMessage = '';
  }

  selectRole(roleId: number): void {
    const selected = this.roles.find((role) => role.roleId === roleId);
    if (!selected) {
      return;
    }

    this.editingRole = this.cloneRole(selected);
    this.statusMessage = '';
    this.errorMessage = '';
  }

  createRole(): void {
    const name = this.draftRoleName.trim();
    if (!name) {
      this.errorMessage = 'Role name is required.';
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    this.statusMessage = '';

    this.adminOps.createRole({
      name,
      description: this.draftRoleDescription.trim(),
      permissions: this.createEmptyPermissions()
    }).subscribe({
      next: (role) => {
        this.roles = [...this.roles, role].sort((a, b) => a.name.localeCompare(b.name));
        this.draftRoleName = '';
        this.draftRoleDescription = '';
        this.isSaving = false;
        this.statusMessage = 'Role created. Configure module access below.';
        this.selectRole(role.roleId);
      },
      error: () => {
        this.isSaving = false;
        this.errorMessage = 'Unable to create role right now.';
      }
    });
  }

  createUser(): void {
    const roleId = Number(this.draftUser.roleId);
    const fullName = this.draftUser.fullName.trim();
    const email = this.draftUser.email.trim();
    const password = this.draftUser.password;
    if (!roleId || !fullName || !email || !password) {
      this.errorMessage = 'Role, full name, email, and password are required.';
      return;
    }

    this.isSavingUser = true;
    this.errorMessage = '';
    this.statusMessage = '';

    this.adminOps.createAdminUser({
      roleId,
      fullName,
      email,
      phone: this.draftUser.phone.trim(),
      password
    }).subscribe({
      next: (user) => {
        this.users = [...this.users, user].sort((a, b) => a.fullName.localeCompare(b.fullName));
        this.draftUser = { roleId, fullName: '', email: '', phone: '', password: '' };
        this.isSavingUser = false;
        this.statusMessage = 'Account created for selected role.';
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingUser = false;
        this.errorMessage = error?.error?.message || 'Unable to create account right now.';
      }
    });
  }

  selectUser(user: AdminUser): void {
    this.editingUser = { ...user };
    this.statusMessage = '';
    this.errorMessage = '';
  }

  saveUser(): void {
    if (!this.editingUser) {
      return;
    }

    this.isSavingUser = true;
    this.errorMessage = '';
    this.statusMessage = '';

    this.adminOps.updateAdminUser(this.editingUser.userId, {
      roleId: Number(this.editingUser.roleId),
      fullName: this.editingUser.fullName.trim(),
      phone: this.editingUser.phone?.trim() ?? '',
      isActive: this.editingUser.isActive
    }).subscribe({
      next: (user) => {
        this.users = this.users.map((item) => item.userId === user.userId ? user : item);
        this.editingUser = { ...user };
        this.isSavingUser = false;
        this.statusMessage = 'Account updated.';
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingUser = false;
        this.errorMessage = error?.error?.message || 'Unable to update account.';
      }
    });
  }

  saveRole(): void {
    if (!this.editingRole) {
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    this.statusMessage = '';

    this.adminOps.updateRole(this.editingRole.roleId, {
      name: this.editingRole.name.trim(),
      description: this.editingRole.description?.trim() ?? '',
      isActive: this.editingRole.isActive,
      permissions: this.editingRole.permissions
    }).subscribe({
      next: (role) => {
        this.roles = this.roles.map((item) => item.roleId === role.roleId ? role : item);
        this.editingRole = this.cloneRole(role);
        this.isSaving = false;
        this.statusMessage = 'Permissions updated successfully.';
      },
      error: () => {
        this.isSaving = false;
        this.errorMessage = 'Unable to save role changes.';
      }
    });
  }

  permissionLabel(permission: RolePermission): string {
    const activePermissions = [
      permission.canAccessMenu ? 'Menu' : '',
      permission.canAccessPage ? 'Page' : '',
      permission.canView ? 'View' : '',
      permission.canAdd ? 'Add' : '',
      permission.canEdit ? 'Edit' : '',
      permission.canDelete ? 'Delete' : ''
    ].filter(Boolean);

    return activePermissions.length ? activePermissions.join(', ') : 'No access';
  }

  private loadRoles(): void {
    this.isLoading = true;
    this.adminOps.getRoles().subscribe({
      next: (roles) => {
        this.roles = roles.map((role) => this.normalizeRole(role));
        this.loadUsers();
        this.isLoading = false;
        if (this.roles.length) {
          this.selectRole(this.roles[0].roleId);
          this.draftUser.roleId = this.assignableRoles[0]?.roleId ?? 0;
        }
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load role permissions.';
      }
    });
  }

  private loadUsers(): void {
    this.adminOps.getAdminUsers().subscribe({
      next: (users) => {
        this.users = users;
      },
      error: () => {
        this.errorMessage = 'Roles loaded, but accounts could not be loaded.';
      }
    });
  }

  private cloneRole(role: RoleDetails): RoleDetails {
    return {
      ...role,
      permissions: role.permissions.map((permission) => ({ ...permission }))
    };
  }

  private normalizeRole(role: RoleDetails): RoleDetails {
    const permissionMap = new Map(role.permissions.map((permission) => [permission.moduleKey, permission]));
    return {
      ...role,
      permissions: MODULE_CATALOG.map((module) => {
        const existing = permissionMap.get(module.moduleKey);
        return existing
          ? {
              ...existing,
              menuKey: existing.menuKey || module.menuKey,
              pageRoute: existing.pageRoute || module.pageRoute
            }
          : {
              moduleKey: module.moduleKey,
              moduleName: module.moduleName,
              menuKey: module.menuKey,
              pageRoute: module.pageRoute,
              canAccessMenu: false,
              canAccessPage: false,
              canView: false,
              canAdd: false,
              canEdit: false,
              canDelete: false
            };
      })
    };
  }

  private createEmptyPermissions(): RolePermission[] {
    return MODULE_CATALOG.map((module) => ({
      moduleKey: module.moduleKey,
      moduleName: module.moduleName,
      menuKey: module.menuKey,
      pageRoute: module.pageRoute,
      canAccessMenu: false,
      canAccessPage: false,
      canView: false,
      canAdd: false,
      canEdit: false,
      canDelete: false
    }));
  }
}
