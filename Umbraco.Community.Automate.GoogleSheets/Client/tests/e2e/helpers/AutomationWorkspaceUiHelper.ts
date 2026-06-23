import type { Locator, Page } from '@playwright/test';
import { BasePage } from '@umbraco-cms/acceptance-test-helpers';

// Page Object for the Automate section's custom automation workspace, following the testhelpers
// package's own `XxxUiHelper` convention (e.g. `DictionaryUiHelper`) of one class per UI area
// built on `BasePage`'s action/wait/assert vocabulary (`this.click`, `this.hover`, `this.isVisible`,
// ...). The package's own helpers extend the richer `UiBaseLocators` (a library of shared button
// locators like `saveBtn`/`createBtn`), but that class isn't part of the package's public exports
// (only `BasePage` is), and none of its generic locators apply to this custom workspace anyway.
export class AutomationWorkspaceUiHelper extends BasePage {
    private readonly settingsBtn: Locator;
    private readonly closeBtn: Locator;
    private readonly workspaceFooter: Locator;

    constructor(page: Page) {
        super(page);
        this.settingsBtn = page.getByRole('button', { name: 'Settings' });
        this.closeBtn = page.getByRole('button', { name: 'Close' });
        this.workspaceFooter = page.getByTestId('workspace:footer');
    }

    async gotoEditor(automationId: string): Promise<void> {
        await this.page.goto(`/umbraco/section/automate/workspace/ua:automation/edit/${automationId}`);
    }

    async gotoRunsTab(automationId: string): Promise<void> {
        await this.page.goto(`/umbraco/section/automate/workspace/ua:automation/edit/${automationId}/view/runs`);
    }

    async waitForLoaded(automationName: string): Promise<void> {
        await this.isVisible(this.workspaceFooter.getByText(automationName));
    }

    async openSettings(): Promise<void> {
        await this.click(this.settingsBtn);
    }

    async closeModal(): Promise<void> {
        await this.click(this.closeBtn);
    }

    async getSpreadsheetIdValue(): Promise<string> {
        return this.getValue(this.page.getByRole('textbox', { name: /Spreadsheet/ }));
    }

    async getSheetNameValue(): Promise<string> {
        return this.getValue(this.page.getByRole('textbox', { name: /Sheet \/ tab name/ }));
    }

    getColumnRows(): Locator {
        return this.page.locator('ua-google-sheets-column-list .row');
    }

    // The tree row's quick-action buttons (e.g. Run now) render as a hover-revealed popover that
    // is not a DOM descendant of the row despite appearing nested in the accessibility tree, so
    // the action button has to be queried unscoped rather than scoped under the row locator.
    async triggerRunNow(automationName: string): Promise<void> {
        const treeLink = this.page.getByRole('link', { name: automationName });
        await treeLink.waitFor();
        const treeRow = this.page.getByRole('menuitem').filter({ has: treeLink });
        await this.hover(treeRow);
        await this.click(this.page.getByTestId('entity-action:UmbracoAutomate.EntityAction.Automation.RunNow'));
    }

    async hasCompletedRunCellVisible(): Promise<void> {
        await this.isVisible(this.page.getByRole('cell', { name: 'Completed' }));
    }
}
