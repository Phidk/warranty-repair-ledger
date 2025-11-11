const API_BASE = (import.meta.env.VITE_API_URL ?? 'http://localhost:8080').replace(/\/$/, '');

export class ApiError extends Error {
  status?: number;
  constructor(message: string, status?: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

export interface Product {
  id: number;
  name: string;
  brand?: string | null;
  serial: string;
  purchaseDate: string;
  warrantyMonths: number;
  retailer?: string | null;
  price?: number | null;
}

export interface CreateProductPayload {
  name: string;
  serial: string;
  purchaseDate: string;
  warrantyMonths?: number;
  brand?: string;
  retailer?: string;
  price?: number;
}

export interface ExpiringProductResponse {
  product: Product;
  daysRemaining: number;
}

export interface SummaryReport {
  countsByStatus: Record<string, number>;
  averageDaysOpen: number | null;
  expiringProducts: number;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const url = `${API_BASE}${path}`;
  const headers: HeadersInit = {
    Accept: 'application/json',
    ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
    ...init?.headers,
  };

  const response = await fetch(url, {
    ...init,
    headers,
  });

  if (!response.ok) {
    let message = `Request failed with status ${response.status}`;
    try {
      const problem = await response.json();
      if (problem?.title) {
        message = problem.title;
      } else if (problem?.errors) {
        const formatted = Object.entries(problem.errors)
          .map(([field, issues]) => `${field}: ${(issues as string[]).join(', ')}`)
          .join(' | ');
        if (formatted) {
          message = formatted;
        }
      }
    } catch {
      // ignore JSON parsing errors
    }

    throw new ApiError(message, response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export async function fetchProducts(): Promise<Product[]> {
  return await request<Product[]>('/products');
}

export async function fetchExpiringProducts(days: number): Promise<ExpiringProductResponse[]> {
  const params = new URLSearchParams({ days: String(days) });
  return await request<ExpiringProductResponse[]>(`/products/expiring?${params.toString()}`);
}

export async function fetchSummaryReport(): Promise<SummaryReport> {
  return await request<SummaryReport>('/reports/summary');
}

export async function createProduct(payload: CreateProductPayload): Promise<Product> {
  return await request<Product>('/products', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}
