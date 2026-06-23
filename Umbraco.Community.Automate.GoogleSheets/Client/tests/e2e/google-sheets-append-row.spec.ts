import { expect } from '@playwright/test';
import { test } from '@umbraco-cms/acceptance-test-helpers';
import { randomUUID } from 'node:crypto';
import { AutomateApiHelper } from './helpers/AutomateApiHelper';
import { AutomationWorkspaceUiHelper } from './helpers/AutomationWorkspaceUiHelper';

const spreadsheetId = 'e2e-fake-spreadsheet-id';
const sheetName = 'Sheet1';
const columns = ['Page name', 'https://example.com/'];

test.describe('Google Sheets append row automation', () => {
    let workspaceId: string | undefined;
    let automationId: string | undefined;

    test.afterEach(async ({ umbracoApi }) => {
        const automateApi = new AutomateApiHelper(umbracoApi);
        if (automationId) await automateApi.deleteAutomation(automationId);
        if (workspaceId) await automateApi.deleteWorkspace(workspaceId);
        automationId = undefined;
        workspaceId = undefined;
    });

    test('configures and runs end-to-end against the stubbed Google Sheets API', async ({ umbracoApi, umbracoUi }) => {
        const automateApi = new AutomateApiHelper(umbracoApi);
        const automationWorkspace = new AutomationWorkspaceUiHelper(umbracoUi.page);

        // Arrange: seed a connection, a workspace allowed to use it, and a published automation,
        // entirely via the Management API so the test owns its own fixtures.
        const { connectionId } = await automateApi.seedGoogleSheetsConnection();
        const currentUser = await umbracoApi.user.getCurrentUser();

        const suffix = randomUUID().slice(0, 8);
        workspaceId = await automateApi.createWorkspace({
            alias: `e2e-google-sheets-ws-${suffix}`,
            name: `E2E Google Sheets Workspace ${suffix}`,
            serviceAccountKey: currentUser.id,
            allowedConnections: [connectionId],
        });

        const automationName = `E2E Append Row ${suffix}`;
        automationId = await automateApi.createAppendRowAutomation({
            alias: `e2e-append-row-${suffix}`,
            name: automationName,
            workspaceId,
            step: { id: randomUUID(), connectionId, spreadsheetId, sheetName, columns },
        });
        await automateApi.publishAutomation(automationId);

        // Act: open the real automation editor and exercise the rendered Append Row settings,
        // including the custom column-list property editor, before triggering a run via the UI.
        await umbracoUi.goToBackOffice();
        await automationWorkspace.gotoEditor(automationId);
        await automationWorkspace.waitForLoaded(automationName);

        await automationWorkspace.openSettings();
        await expect.poll(() => automationWorkspace.getSpreadsheetIdValue()).toBe(spreadsheetId);
        await expect.poll(() => automationWorkspace.getSheetNameValue()).toBe(sheetName);
        await expect(automationWorkspace.getColumnRows()).toHaveCount(columns.length);
        await automationWorkspace.closeModal();

        await automationWorkspace.triggerRunNow(automationName);

        // Assert: the real engine pipeline picks up the run and completes it against the stubbed API.
        let runId: string | undefined;
        await expect
            .poll(
                async () => {
                    const run = await automateApi.getLatestRun(automationId!);
                    runId = run?.id;
                    return run?.status;
                },
                { timeout: 20_000, message: 'Waiting for the triggered run to complete' },
            )
            .toBe('Completed');

        const run = await automateApi.getRunDetail(runId!);
        expect(run.stepRuns).toHaveLength(1);
        expect(run.stepRuns[0].actionAlias).toBe('googleSheets.appendRow');
        expect(run.stepRuns[0].status).toBe('Completed');

        // Also confirm the real Runs tab in the automation editor reflects the same outcome.
        await automationWorkspace.gotoRunsTab(automationId);
        await automationWorkspace.hasCompletedRunCellVisible();
    });
});
