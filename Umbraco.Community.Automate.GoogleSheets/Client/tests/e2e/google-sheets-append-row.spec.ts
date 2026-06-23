import { expect } from '@playwright/test';
import { test } from '@umbraco-cms/acceptance-test-helpers';
import { randomUUID } from 'node:crypto';
import type { APIResponse } from '@playwright/test';

const AUTOMATE_API = '/umbraco/automate/management/api/v1';
const SEED_GOOGLE_SHEETS_ENDPOINT = '/umbraco-automate-e2e/seed-google-sheets';

const spreadsheetId = 'e2e-fake-spreadsheet-id';
const sheetName = 'Sheet1';
const columns = ['Page name', 'https://example.com/'];

// Create responses carry the new entity's id via the standard Umbraco `Location` header.
function locationId(response: APIResponse): string {
    const location = response.headers()['location'];
    if (!location) throw new Error(`Expected a Location header on the response from ${response.url()}.`);
    return location.split('/').filter(Boolean).pop()!;
}

test.describe('Google Sheets append row automation', () => {
    let workspaceId: string | undefined;
    let automationId: string | undefined;

    test.afterEach(async ({ umbracoApi }) => {
        if (automationId) await umbracoApi.delete(`${umbracoApi.baseUrl}${AUTOMATE_API}/automations/${automationId}`);
        if (workspaceId) await umbracoApi.delete(`${umbracoApi.baseUrl}${AUTOMATE_API}/workspaces/${workspaceId}`);
        automationId = undefined;
        workspaceId = undefined;
    });

    test('configures and runs end-to-end against the stubbed Google Sheets API', async ({ umbracoApi, umbracoUi }) => {
        // Arrange: seed a connection, a workspace allowed to use it, and a published automation,
        // entirely via the Management API so the test owns its own fixtures.
        const seedResponse = await umbracoApi.post(`${umbracoApi.baseUrl}${SEED_GOOGLE_SHEETS_ENDPOINT}`);
        expect(seedResponse.ok()).toBeTruthy();
        const { connectionId } = await seedResponse.json();

        const currentUser = await umbracoApi.user.getCurrentUser();

        const suffix = randomUUID().slice(0, 8);
        const workspaceResponse = await umbracoApi.post(`${umbracoApi.baseUrl}${AUTOMATE_API}/workspaces`, {
            alias: `e2e-google-sheets-ws-${suffix}`,
            name: `E2E Google Sheets Workspace ${suffix}`,
            serviceAccountKey: currentUser.id,
            userGroups: [],
            allowedConnections: [connectionId],
        });
        expect(workspaceResponse.status()).toBe(201);
        workspaceId = locationId(workspaceResponse);

        const appendRowStepId = randomUUID();
        const automationName = `E2E Append Row ${suffix}`;
        const automationResponse = await umbracoApi.post(`${umbracoApi.baseUrl}${AUTOMATE_API}/automations`, {
            alias: `e2e-append-row-${suffix}`,
            name: automationName,
            workspaceId,
            trigger: {
                triggerAlias: 'umbracoAutomate.manual',
                settings: {},
            },
            steps: [
                {
                    id: appendRowStepId,
                    actionAlias: 'googleSheets.appendRow',
                    name: 'Append Row to Google Sheet',
                    connectionId,
                    settings: {
                        SpreadsheetId: spreadsheetId,
                        SheetName: sheetName,
                        Columns: columns,
                    },
                    inputMappings: {},
                    position: { x: 0, y: 0 },
                    errorBehavior: 'Terminate',
                },
            ],
            // `connections` only models step-to-step edges; a step with no incoming connection
            // is implicitly wired to the trigger by the engine (the canvas's "__trigger__" node
            // is a frontend rendering construct only, not a real step id the API accepts).
            connections: [],
        });
        expect(automationResponse.status()).toBe(201);
        automationId = locationId(automationResponse);

        const publishResponse = await umbracoApi.post(`${umbracoApi.baseUrl}${AUTOMATE_API}/automations/${automationId}/publish`);
        expect(publishResponse.ok()).toBeTruthy();

        // Act: open the real automation editor and exercise the rendered Append Row settings,
        // including the custom column-list property editor, before triggering a run via the UI.
        await umbracoUi.goToBackOffice();
        await umbracoUi.page.goto(`/umbraco/section/automate/workspace/ua:automation/edit/${automationId}`);
        await expect(umbracoUi.page.getByTestId('workspace:footer').getByText(automationName)).toBeVisible();

        await umbracoUi.page.getByRole('button', { name: 'Settings' }).click();

        const spreadsheetIdInput = umbracoUi.page.getByRole('textbox', { name: /Spreadsheet/ });
        const sheetNameInput = umbracoUi.page.getByRole('textbox', { name: /Sheet \/ tab name/ });
        await expect.poll(() => spreadsheetIdInput.evaluate((el) => (el as HTMLInputElement).value)).toBe(spreadsheetId);
        await expect.poll(() => sheetNameInput.evaluate((el) => (el as HTMLInputElement).value)).toBe(sheetName);

        const columnRows = umbracoUi.page.locator('ua-google-sheets-column-list .row');
        await expect(columnRows).toHaveCount(columns.length);

        await umbracoUi.page.getByRole('button', { name: 'Close' }).click();

        const automationTreeLink = umbracoUi.page.getByRole('link', { name: automationName });
        await automationTreeLink.waitFor();
        const treeRow = umbracoUi.page.getByRole('menuitem').filter({ has: automationTreeLink });
        await treeRow.hover();
        await umbracoUi.page.getByTestId('entity-action:UmbracoAutomate.EntityAction.Automation.RunNow').click();

        // Assert: the real engine pipeline picks up the run and completes it against the stubbed API.
        // The list endpoint omits `stepRuns` (avoids N+1 loading on paged lists), so once the run
        // shows up we fetch it by id from the sibling `/runs/{id}` detail route for step-level detail.
        let runId: string | undefined;
        await expect
            .poll(
                async () => {
                    const runsResponse = await umbracoApi.get(`${umbracoApi.baseUrl}${AUTOMATE_API}/automations/${automationId}/runs`);
                    const { items } = await runsResponse.json();
                    runId = items[0]?.id;
                    return items[0]?.status;
                },
                { timeout: 20_000, message: 'Waiting for the triggered run to complete' },
            )
            .toBe('Completed');

        const runResponse = await umbracoApi.get(`${umbracoApi.baseUrl}${AUTOMATE_API}/runs/${runId}`);
        const run = await runResponse.json();
        expect(run.stepRuns).toHaveLength(1);
        expect(run.stepRuns[0].actionAlias).toBe('googleSheets.appendRow');
        expect(run.stepRuns[0].status).toBe('Completed');

        // Also confirm the real Runs tab in the automation editor reflects the same outcome.
        await umbracoUi.page.goto(`/umbraco/section/automate/workspace/ua:automation/edit/${automationId}/view/runs`);
        await expect(umbracoUi.page.getByRole('cell', { name: 'Completed' })).toBeVisible();
    });
});
