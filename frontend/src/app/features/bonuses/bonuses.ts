
import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DecimalPipe } from '@angular/common';

import { Bonus } from '../../core/models/bonus';
import { MasterStateCodes } from '../../core/models/catalog';
import { BonusApi } from '../../core/services/bonus-api';
import { CatalogApi } from '../../core/services/catalog-api';

@Component({
  selector: 'app-bonuses',
  imports: [DecimalPipe],
  templateUrl: './bonuses.html',
  styleUrl: './bonuses.css',
})
export class Bonuses {
  private readonly catalogApi = inject(CatalogApi);
  private readonly bonusApi = inject(BonusApi);

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

  constructor() {
    this.loadBonuses();
  }

  protected loadBonuses(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.bonusApi.getBonuses({
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
}
