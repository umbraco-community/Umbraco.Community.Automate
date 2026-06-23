import { defineConfig, devices } from '@playwright/test';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';
import 'dotenv/config';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

export const STORAGE_STATE = join(__dirname, 'tests/e2e/.auth/user.json');

// Testhelpers read auth tokens from this file.
process.env.STORAGE_STAGE_PATH = STORAGE_STATE;

export default defineConfig({
    testDir: './tests/e2e',
    timeout: 60 * 1000,
    expect: { timeout: 10 * 1000 },
    fullyParallel: false,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 2 : 0,
    workers: 1,
    reporter: process.env.CI ? 'line' : 'html',
    use: {
        // @umbraco/playwright-testhelpers reads its own base URL from `URL`, not a Playwright-specific var.
        baseURL: process.env.URL || 'https://localhost:44343',
        trace: 'retain-on-failure',
        ignoreHTTPSErrors: true,
        // Umbraco uses 'data-mark', not 'data-testid'.
        testIdAttribute: 'data-mark',
    },
    projects: [
        {
            name: 'setup',
            testMatch: '**/*.setup.ts',
        },
        {
            name: 'e2e',
            testMatch: '**/*.spec.ts',
            dependencies: ['setup'],
            use: {
                ...devices['Desktop Chrome'],
                ignoreHTTPSErrors: true,
                storageState: STORAGE_STATE,
            },
        },
    ],
});
