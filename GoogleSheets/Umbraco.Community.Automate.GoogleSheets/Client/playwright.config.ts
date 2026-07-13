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

    webServer: {
        cwd: join(__dirname, '../../../Umbraco.Community.Automate.Demo'),
        command: 'dotnet run --urls "https://localhost:44343;http://localhost:52012"',
        // Polled over plain HTTP because Node's built-in TCP/HTTP checker can't validate the
        // self-signed dev cert on the HTTPS port — browser tests still go over HTTPS via `baseURL`.
        // /umbraco/api/health/ready returns 503 during unattended install/migrations and 200 once
        // Umbraco reaches RuntimeLevel.Run, so Playwright only proceeds once it's fully booted.
        url: 'http://localhost:52012/umbraco/api/health/ready',
        stdout: 'pipe',
        stderr: 'pipe',
        ignoreHTTPSErrors: true,
        reuseExistingServer: !process.env.CI,
        timeout: 120 * 1000,
        env: {
            ASPNETCORE_ENVIRONMENT: 'Development',
            AUTOMATE_E2E_MODE: '1',
        },
    },
});
