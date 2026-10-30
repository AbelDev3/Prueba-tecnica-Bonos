
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  Bonus,
  BonusFilter,
  CreateBonusRequest,
  CreateBonusResponse,
  BonusEmployee
} from '../models/bonus';
import { PagedResponse } from '../models/paged-response';

@Injectable({ providedIn: 'root' })
export class BonusApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/Bonus`;

  getBonuses(filter: BonusFilter): Observable<PagedResponse<Bonus>> {
    let params = new HttpParams()
      .set('pageIndex', filter.pageIndex)
      .set('pageSize', filter.pageSize);

    if (filter.departmentCode) {
      params = params.set('departmentCode', filter.departmentCode);
    }

    if (filter.employeeNumber != null) {
      params = params.set('employeeNumber', filter.employeeNumber);
    }

    if (filter.period) {
      params = params.set('period', filter.period);
    }

    if (filter.stateCode) {
      params = params.set('stateCode', filter.stateCode);
    }

    return this.http.get<PagedResponse<Bonus>>(
      this.baseUrl,
      { params }
    );
  }

  createBonus(
    request: CreateBonusRequest,
    userCode: string
  ): Observable<CreateBonusResponse> {
    return this.http.post<CreateBonusResponse>(
      this.baseUrl,
      request,
      {
        headers: { UserCode: userCode }
      }
    );
  }

  cancelBonus(bonusId: number, userCode: string): Observable<void> {
    return this.http.put<void>(
      `${this.baseUrl}/${bonusId}/cancel`,
      null,
      {
        headers: { UserCode: userCode }
      }
    );
  }

  deleteBonus(bonusId: number, userCode: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/${bonusId}`,
      {
        headers: { UserCode: userCode }
      }
    );
  }

  getEmployee(
    departmentCode: string,
    employeeNumber: number
  ): Observable<BonusEmployee> {
    return this.http.get<BonusEmployee>(
      `${this.baseUrl}/employee/${encodeURIComponent(departmentCode)}/${employeeNumber}`
    );
  }

}
