import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach, beforeEach, vi } from 'vitest';
import { apiFetch } from '../services/api';

vi.mock('../services/api', () => ({
    API_BASE_URL: 'http://localhost:5282',
    apiFetch: vi.fn(),
}));

beforeEach(() => {
    // Any unconfigured request fails locally; no test can reach the backend.
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('Unexpected fetch')));
    apiFetch.mockRejectedValue(new Error('Unexpected apiFetch'));
});

afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
    vi.resetAllMocks();
    vi.unstubAllGlobals();
    vi.useRealTimers();
    localStorage.clear();
    sessionStorage.clear();
    window.history.replaceState(null, '', '/');
});
