import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

export default defineConfig({
    plugins: [react()],
    test: {
        environment: 'jsdom',
        environmentOptions: { jsdom: { url: 'http://localhost/' } },
        include: ['src/components/**/*.component.test.jsx'],
        setupFiles: ['./src/test/setup.js'],
    },
});
