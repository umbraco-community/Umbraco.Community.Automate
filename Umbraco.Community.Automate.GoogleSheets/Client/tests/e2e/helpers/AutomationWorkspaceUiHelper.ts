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

    // Each canvas node renders its own icon-only "Settings" button (accessible name comes from
    // its `title` attribute, not visible text), so with more than one step on the canvas a bare
    // role locator is ambiguous. Scope to the node carrying the step's alias chip instead.
    async openStepSettings(stepAlias: string): Promise<void> {
        const node = this.page.locator('.ua-node--action').filter({ hasText: stepAlias });
        await this.click(node.getByTitle('Settings'));
    }

    async clickAddColumn(): Promise<void> {
        await this.click(this.page.getByRole('button', { name: 'Add column' }));
    }

    async clickRemoveColumn(rowIndex: number): Promise<void> {
        await this.click(this.getColumnRows().nth(rowIndex).getByRole('button', { name: 'Remove' }));
    }

    async getColumnInputValue(rowIndex: number): Promise<string> {
        return this.getValue(this.getColumnRows().nth(rowIndex).getByRole('textbox'));
    }

    // The picker only offers binding sources once `ua-node-settings-modal` resolves the step's
    // predecessor output schemas asynchronously, so this button isn't necessarily present yet
    // the instant the modal opens — waitForVisible covers that load.
    async clickInsertBinding(rowIndex: number): Promise<void> {
        const button = this.getColumnRows().nth(rowIndex).getByRole('button', { name: 'Insert binding' });
        await this.waitForVisible(button);
        await this.click(button);
    }

    // Selects a leaf in the real `ua-binding-picker` modal by source step alias and output
    // property path (e.g. "first" / "updatedRange") — this both submits the modal and splices
    // the binding expression into the row that opened it. Scoping by source alias matters: a
    // step with exactly one predecessor gets both a "steps.<alias>" source AND a "previous"
    // pseudo-source offering the *same* leaf paths, so an unscoped `name=` lookup is ambiguous.
    async selectBindingLeaf(sourceAlias: string, path: string): Promise<void> {
        const box = this.page.locator('ua-binding-picker uui-box').filter({ hasText: sourceAlias });
        await this.click(box.locator(`uui-ref-node[name="${path}"]`));
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
