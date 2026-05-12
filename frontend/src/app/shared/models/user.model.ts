export type GlobalRole = 'user' | 'admin' | 'super_admin';

export interface User {
  id: string;
  email: string;
  displayName: string;
  role: GlobalRole;
}
