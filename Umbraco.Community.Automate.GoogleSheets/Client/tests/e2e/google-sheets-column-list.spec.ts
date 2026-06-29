import { expect } from '@playwright/test';
import { test } from '@umbraco-cms/acceptance-test-helpers';
import { randomUUID } from 'node:crypto';
import { AutomateApiHelper } from './helpers/AutomateApiHelper';
import { AutomationWorkspaceUiHelper } from './helpers/AutomationWorkspaceUiHelper';

const spreadsheetId = 'e2e-fake-spreadsheet-id';
const sheetName = 'Sheet1';

test.describe('Google Sheets column-list editor', () => {
    let workspaceId: string | undefined;
    let automationId: string | undefined;

    test.afterEach(async ({ umbracoApi }) => {
        const automateApi = new AutomateApiHelper(umbracoApi);
        if (automationId) await automateApi.deleteAutomation(automationId);
        if (workspaceId) await automateApi.deleteWorkspace(workspaceId);
        automationId = undefined;
        workspaceId = undefined;
    });

    test('adds, removes, and inserts a binding into a column via the real UI', async ({ umbracoApi, umbracoUi }) => {
        const automateApi = new AutomateApiHelper(umbracoApi);
        const automationWorkspace = new AutomationWorkspaceUiHelper(umbracoUi.page);

        const { connectionId } = await automateApi.seedGoogleSheetsConnection();
        const currentUser = await umbracoApi.user.getCurrentUser();

        const suffix = randomUUID().slice(0, 8);
        workspaceId = await automateApi.createWorkspace({
            alias: `e2e-google-sheets-ws-${suffix}`,
            name: `E2E Google Sheets Workspace ${suffix}`,
            serviceAccountKey: currentUser.id,
            allowedConnections: [connectionId],
        });

        // The second step exists purely so its Columns editor has a real predecessor to offer
        // as a binding source — this test only drives the editor UI and never runs the automation.
        const automationName = `E2E Column List Editor ${suffix}`;
        automationId = await automateApi.createTwoStepAppendRowAutomation({
            alias: `e2e-column-list-${suffix}`,
            name: automationName,
            workspaceId,
            firstStep: {
                id: randomUUID(),
                alias: 'first',
                connectionId,
                spreadsheetId,
                sheetName,
                columns: ['Page name'],
            },
            secondStep: {
                id: randomUUID(),
                alias: 'second',
                connectionId,
                spreadsheetId,
                sheetName,
                columns: [],
            },
        });

        await umbracoUi.goToBackOffice();
        await automationWorkspace.gotoEditor(automationId);
        await automationWorkspace.waitForLoaded(automationName);
        await automationWorkspace.openStepSettings('second');

        // Act/Assert: add a column, then insert a real binding via the picker UI (not by typing
        // the expression by hand), confirming the picker's own camelCase casing round-trips
        // correctly into the rendered row.
        await automationWorkspace.clickAddColumn();
        await expect(automationWorkspace.getColumnRows()).toHaveCount(1);

        await automationWorkspace.clickInsertBinding(0);
        await automationWorkspace.selectBindingLeaf('first', 'updatedRange');
        await expect.poll(() => automationWorkspace.getColumnInputValue(0)).toBe('${ steps.first.updatedRange }');

        // Add a second column so removal can be asserted unambiguously by index, then remove it
        // and confirm the first row (with its inserted binding) is untouched.
        await automationWorkspace.clickAddColumn();
        await expect(automationWorkspace.getColumnRows()).toHaveCount(2);

        await automationWorkspace.clickRemoveColumn(1);
        await expect(automationWorkspace.getColumnRows()).toHaveCount(1);
        await expect.poll(() => automationWorkspace.getColumnInputValue(0)).toBe('${ steps.first.updatedRange }');

        await automationWorkspace.closeModal();
    });
});
