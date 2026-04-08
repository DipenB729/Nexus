import { Component, OnInit } from '@angular/core';
import { RoleDetails, RolePermission } from '../../core/models/hms/admin-ops.model';
import { AdminOpsService } from '../../core/services/hms/admin-ops.service';

const MODULE_CATALOG: ReadonlyArray<Pick<RolePermission, 'moduleKey' | 'moduleName'>> = [
  { moduleKey: 'dashboard', moduleName: 'Dashboard' },
  { moduleKey: 'patients', moduleName: 'Patients' },
  { moduleKey: 'appointments', moduleName: 'Appointments' },
  { moduleKey: 'pharmacy', moduleName: 'Pharmacy' },
  { moduleKey: 'laboratory', moduleName: 'Laboratory' },
  { moduleKey: 'inventory', moduleName: 'Inventory' },
  { moduleKey: 'billing', moduleName: 'Billing' },
  { moduleKey: 'masters', moduleName: 'Master Setup' },
  { moduleKey: 'branches', moduleName: 'Branches' },
  { moduleKey: 'settings', moduleName: 'Settings' }
];

@Component({
  selector: 'app-users-dashboard',
  templateUrl: './users-dashboard.component.html',
  styleUrls: ['./users-dashboard.component.scss']
})
export class UsersDashboardComponent implements OnInit {
  roles: RoleDetails[] = [];
  editingRole: RoleDetails | null = null;
  isLoading = true;
  isSaving = false;
  errorMessage = '';
  statusMessage = '';

  draftRoleName = '';
  draftRoleDescription = '';

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
        this.isLoading = false;
        if (this.roles.length) {
          this.selectRole(this.roles[0].roleId);
        }
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load role permissions.';
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
          ? { ...existing }
          : {
              moduleKey: module.moduleKey,
              moduleName: module.moduleName,
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
      canView: false,
      canAdd: false,
      canEdit: false,
      canDelete: false
    }));
  }
}
