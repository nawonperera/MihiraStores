import axios, { AxiosError } from 'axios';
import type {
  Branch,
  Product,
  Stock,
  StockTransfer,
  CreateStockTransferRequest,
  ProblemDetails,
  TransferStatus,
} from '../types';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

export function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<ProblemDetails>;
    if (!axiosError.response) {
      return 'Unable to reach the backend API server. Please verify the ASP.NET Core API is running at http://localhost:5079.';
    }

    const data = axiosError.response.data;
    if (data?.detail) {
      return data.detail;
    }
    if (data?.title) {
      return data.title;
    }
    if (data?.errors) {
      const messages = Object.values(data.errors).flat();
      if (messages.length > 0) {
        return messages.join(', ');
      }
    }
    return `Server responded with error status ${axiosError.response.status}: ${axiosError.response.statusText}`;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'An unexpected error occurred. Please try again.';
}

export const api = {
  async getBranches(): Promise<Branch[]> {
    const response = await apiClient.get<Branch[]>('/branches');
    return response.data;
  },

  async getProducts(): Promise<Product[]> {
    const response = await apiClient.get<Product[]>('/products');
    return response.data;
  },

  async getBranchStock(branchId: number): Promise<Stock[]> {
    const response = await apiClient.get<Stock[]>(`/branches/${branchId}/stock`);
    return response.data;
  },

  async getTransfers(branchId?: number, status?: TransferStatus): Promise<StockTransfer[]> {
    const params = new URLSearchParams();
    if (branchId !== undefined) {
      params.append('branchId', branchId.toString());
    }
    if (status !== undefined) {
      params.append('status', status.toString());
    }
    const response = await apiClient.get<StockTransfer[]>('/transfers', { params });
    return response.data;
  },

  async createTransfer(request: CreateStockTransferRequest): Promise<StockTransfer> {
    const response = await apiClient.post<StockTransfer>('/transfers', request);
    return response.data;
  },

  async dispatchTransfer(transferId: number): Promise<StockTransfer> {
    const response = await apiClient.post<StockTransfer>(`/transfers/${transferId}/dispatch`);
    return response.data;
  },

  async receiveTransfer(transferId: number): Promise<StockTransfer> {
    const response = await apiClient.post<StockTransfer>(`/transfers/${transferId}/receive`);
    return response.data;
  },

  async cancelTransfer(transferId: number): Promise<StockTransfer> {
    const response = await apiClient.post<StockTransfer>(`/transfers/${transferId}/cancel`);
    return response.data;
  },
};
