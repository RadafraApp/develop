import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'radafra.admin';
const ADMIN_CODE = 'Radafra';

@Injectable({ providedIn: 'root' })
export class AdminAuthService {
  private readonly _authenticated = signal(this.readStored());

  readonly authenticated = this._authenticated.asReadonly();

  submit(code: string): boolean {
    if (code.trim() === ADMIN_CODE) {
      this._authenticated.set(true);
      this.persist(true);
      return true;
    }
    return false;
  }

  logout(): void {
    this._authenticated.set(false);
    this.persist(false);
  }

  private readStored(): boolean {
    try {
      return sessionStorage.getItem(STORAGE_KEY) === '1';
    } catch {
      return false;
    }
  }

  private persist(value: boolean): void {
    try {
      if (value) {
        sessionStorage.setItem(STORAGE_KEY, '1');
      } else {
        sessionStorage.removeItem(STORAGE_KEY);
      }
    } catch {
      /* almacenamiento no disponible: se mantiene solo en memoria */
    }
  }
}
