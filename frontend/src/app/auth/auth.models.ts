/** Response shape of POST /auth/register and POST /auth/login. */
export interface AuthResult {
  token: string;
  expiresAtUtc: string;
  businessId: number;
  businessName: string;
  email: string;
  /** "User" or "Admin" - decides whether the admin screens are offered. */
  role: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  businessName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

/** The signed-in business, as kept in browser storage between page loads. */
export interface SignedInBusiness {
  token: string;
  expiresAtUtc: string;
  businessId: number;
  businessName: string;
  email: string;
  role: string;
}
