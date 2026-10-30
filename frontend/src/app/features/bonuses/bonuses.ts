
import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';

import { Bonus } from '../../core/models/bonus';
import { MasterStateCodes } from '../../core/models/catalog';
import { BonusApi } from '../../core/services/bonus-api';
import { CatalogApi } from '../../core/services/catalog-api';

import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { BonusEmployee } from '../../core/models/bonus';

@Component({
  selector: 'app-bonuses',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './bonuses.html',
  styleUrl: './bonuses.css',
})
export class Bonuses {
  private readonly catalogApi = inject(CatalogApi);
  private readonly bonusApi = inject(BonusApi);
  
  protected readonly selectedDepartment = signal('');
  protected readonly selectedState = signal('');

  protected readonly selectedEmployee = signal('');
  protected readonly selectedPeriod = signal('');

  protected readonly departments = toSignal(
    this.catalogApi.getDepartments(),
    { initialValue: [] }
  );

  protected readonly bonusStates = toSignal(
    this.catalogApi.getStates(MasterStateCodes.Bonus),
    { initialValue: [] }
  );

  protected readonly bonuses = signal<Bonus[]>([]);
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal('');

  protected readonly pageNumber = signal(1);
  protected readonly pageSize = 10;
  protected readonly totalPages = signal(0);
  protected readonly totalRecords = signal(0);
  
  private readonly fb = inject(FormBuilder);

  protected readonly showModal = signal(false);
  protected readonly searchingEmployee = signal(false);
  protected readonly employee = signal<BonusEmployee | null>(null);
  protected readonly employeeError = signal('');

  protected readonly bonusTypes = toSignal(
    this.catalogApi.getBonusTypes(),
    { initialValue: [] }
  );

  protected readonly bonusReasons = toSignal(
    this.catalogApi.getBonusReasons(),
    { initialValue: [] }
  );

  protected readonly bonusForm = this.fb.nonNullable.group({
    departmentCode: ['', Validators.required],
    employeeNumber: [0, [Validators.required, Validators.min(1)]],
    period: ['', Validators.required],
    bonusTypeId: [0, [Validators.required, Validators.min(1)]],
    bonusValue: [0, [Validators.required, Validators.min(0.01)]],
    reasonCode: ['', Validators.required]
  });


  constructor() {
    this.loadBonuses();
  }

  protected loadBonuses(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    const employeeText = this.selectedEmployee().trim();

    this.bonusApi.getBonuses({
      departmentCode: this.selectedDepartment(),
      stateCode: this.selectedState(),
      employeeNumber: employeeText ? Number(employeeText) : undefined,
      period: this.selectedPeriod(),
      pageIndex: this.pageNumber(),
      pageSize: this.pageSize
    }).subscribe({
      next: response => {
        this.bonuses.set(response.data);
        this.totalPages.set(response.totalPages);
        this.totalRecords.set(response.totalRecords);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo cargar el listado de bonos.');
        this.loading.set(false);
      }
    });
  }

  protected nextPage(): void {
    if (this.pageNumber() < this.totalPages()) {
      this.pageNumber.update(page => page + 1);
      this.loadBonuses();
    }
  }

  protected previousPage(): void {
    if (this.pageNumber() > 1) {
      this.pageNumber.update(page => page - 1);
      this.loadBonuses();
    }
  }

  protected onFilterChange(): void {
    this.pageNumber.set(1);
    this.loadBonuses();
  }

  protected openModal(): void {
    this.bonusForm.reset();
    this.employee.set(null);
    this.employeeError.set('');
    this.showModal.set(true);
  }

  protected closeModal(): void {
    this.showModal.set(false);
    this.employee.set(null);
    this.employeeError.set('');
  }

  protected searchEmployee(): void {
    const departmentCode =
      this.bonusForm.controls.departmentCode.value.trim();

    const employeeNumber =
      this.bonusForm.controls.employeeNumber.value;

    this.employee.set(null);
    this.employeeError.set('');

    if (!departmentCode || employeeNumber <= 0) {
      this.employeeError.set(
        'Debe seleccionar un área e ingresar un número de empleado válido.'
      );
      return;
    }

    this.searchingEmployee.set(true);

    this.bonusApi.getEmployee(
      departmentCode,
      employeeNumber
    ).subscribe({
      next: response => {
        this.employee.set(response);
        this.searchingEmployee.set(false);
      },
      error: error => {
        this.employeeError.set(
          error.error?.description ?? 'No se pudo consultar el empleado.'
        );
        this.searchingEmployee.set(false);
      }
    });
  }

}
