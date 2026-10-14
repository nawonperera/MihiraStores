export interface Branch {
  id: number;
  name: string;
  code: string;
  isWarehouse: boolean;
}

export interface Product {
  id: number;
  name: string;
  sku: string;
}

export interface Stock {
  id: number;
  branchId: number;
  branchName: string;
  productId: number;
  productName: string;
  productSku: string;
  quantity: number;
  lastUpdatedUtc: string;
}

export type TransferStatus = 'Requested' | 'Dispatched' | 'Received' | 'Cancelled' | 1 | 2 | 3 | 4;

export interface StockTransferLine {
  id: number;
  productId: number;
  productName: string;
  productSku: string;
  quantity: number;
}

export interface StockTransfer {
  id: number;
  fromBranchId: number;
  fromBranchName: string;
  toBranchId: number;
  toBranchName: string;
  status: TransferStatus;
  createdAtUtc: string;
  dispatchedAtUtc: string | null;
  receivedAtUtc: string | null;
  cancelledAtUtc: string | null;
  lines: StockTransferLine[];
}

export interface CreateStockTransferLineRequest {
  productId: number;
  quantity: number;
}

export interface CreateStockTransferRequest {
  fromBranchId: number;
  toBranchId: number;
  lines: CreateStockTransferLineRequest[];
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

export function normalizeStatus(status: TransferStatus): 'Requested' | 'Dispatched' | 'Received' | 'Cancelled' {
  if (status === 'Requested' || status === 1) return 'Requested';
  if (status === 'Dispatched' || status === 2) return 'Dispatched';
  if (status === 'Received' || status === 3) return 'Received';
  if (status === 'Cancelled' || status === 4) return 'Cancelled';
  return 'Requested';
}
