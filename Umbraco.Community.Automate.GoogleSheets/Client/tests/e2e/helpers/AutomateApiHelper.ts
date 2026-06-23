import type { ApiHelpers } from '@umbraco-cms/acceptance-test-helpers';
import type { APIResponse } from '@playwright/test';

const AUTOMATE_API = '/umbraco/automate/management/api/v1';
const SEED_GOOGLE_SHEETS_ENDPOINT = '/umbraco-automate-e2e/seed-google-sheets';

export interface AppendRowStep {
    id: string;
    connectionId: string;
    spreadsheetId: string;
    sheetName: string;
    columns: string[];
}

export interface RunSummary {
    id: string;
    status: string;
}

export interface StepRun {
    actionAlias: string;
    status: string;
}

export interface RunDetail {
    stepRuns: StepRun[];
}

// Create responses carry the new entity's id via the standard Umbraco `Location` header.
function locationId(response: APIResponse): string {
    const location = response.headers()['location'];
    if (!location) throw new Error(`Expected a Location header on the response from ${response.url()}.`);
    return location.split('/').filter(Boolean).pop()!;
}

// Thin wrapper around the Automate Management API, mirroring the testhelpers package's own
// `XxxApiHelper` convention (e.g. `DictionaryApiHelper`) of one class per domain area.
export class AutomateApiHelper {
    constructor(private readonly api: ApiHelpers) {}

    async seedGoogleSheetsConnection(): Promise<{ connectionId: string }> {
        const response = await this.api.post(`${this.api.baseUrl}${SEED_GOOGLE_SHEETS_ENDPOINT}`);
        if (!response.ok()) throw new Error(`Failed to seed the Google Sheets connection: ${response.status()}`);
        return response.json();
    }

    async createWorkspace(options: {
        alias: string;
        name: string;
        serviceAccountKey: string;
        allowedConnections: string[];
    }): Promise<string> {
        const response = await this.api.post(`${this.api.baseUrl}${AUTOMATE_API}/workspaces`, {
            alias: options.alias,
            name: options.name,
            serviceAccountKey: options.serviceAccountKey,
            userGroups: [],
            allowedConnections: options.allowedConnections,
        });
        if (response.status() !== 201) throw new Error(`Failed to create workspace: ${response.status()}`);
        return locationId(response);
    }

    async createAppendRowAutomation(options: {
        alias: string;
        name: string;
        workspaceId: string;
        step: AppendRowStep;
    }): Promise<string> {
        const response = await this.api.post(`${this.api.baseUrl}${AUTOMATE_API}/automations`, {
            alias: options.alias,
            name: options.name,
            workspaceId: options.workspaceId,
            trigger: { triggerAlias: 'umbracoAutomate.manual', settings: {} },
            steps: [
                {
                    id: options.step.id,
                    actionAlias: 'googleSheets.appendRow',
                    name: 'Append Row to Google Sheet',
                    connectionId: options.step.connectionId,
                    settings: {
                        SpreadsheetId: options.step.spreadsheetId,
                        SheetName: options.step.sheetName,
                        Columns: options.step.columns,
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
        if (response.status() !== 201) throw new Error(`Failed to create automation: ${response.status()}`);
        return locationId(response);
    }

    async publishAutomation(automationId: string): Promise<void> {
        const response = await this.api.post(`${this.api.baseUrl}${AUTOMATE_API}/automations/${automationId}/publish`);
        if (!response.ok()) throw new Error(`Failed to publish automation: ${response.status()}`);
    }

    async deleteAutomation(automationId: string): Promise<void> {
        await this.api.delete(`${this.api.baseUrl}${AUTOMATE_API}/automations/${automationId}`);
    }

    async deleteWorkspace(workspaceId: string): Promise<void> {
        await this.api.delete(`${this.api.baseUrl}${AUTOMATE_API}/workspaces/${workspaceId}`);
    }

    async getLatestRun(automationId: string): Promise<RunSummary | undefined> {
        const response = await this.api.get(`${this.api.baseUrl}${AUTOMATE_API}/automations/${automationId}/runs`);
        const { items } = await response.json();
        return items[0];
    }

    // The list endpoint above deliberately omits `stepRuns` (it avoids N+1 loading on paged
    // lists), so step-level detail has to come from the sibling `/runs/{id}` detail route.
    async getRunDetail(runId: string): Promise<RunDetail> {
        const response = await this.api.get(`${this.api.baseUrl}${AUTOMATE_API}/runs/${runId}`);
        return response.json();
    }
}
