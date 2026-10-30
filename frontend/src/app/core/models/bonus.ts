
export interface Bonus {
  bonusId: number;
  departmentCode: string;
  employeeNumber: number;
  period: string;
  bonusTypeId: number;
  bonusValue: number;
  reasonCode: string;
  grossAmount: number;
  taxAmount: number;
  netAmount: number;
  stateCode: string;
  createdBy: string;
  createdAt: string;
  modifiedBy: string | null;
  modifiedAt: string | null;
}

export interface BonusFilter {
  departmentCode?: string;
  employeeNumber?: number;
  period?: string;
  stateCode?: string;
  pageIndex: number;
  pageSize: number;
}

export interface CreateBonusRequest {
  departmentCode: string;
  employeeNumber: number;
  period: string;
  bonusTypeId: number;
  bonusValue: number;
  reasonCode: string;
}

export interface CreateBonusResponse {
  bonusId: number;
  grossAmount: number;
  taxAmount: number;
  netAmount: number;
}
