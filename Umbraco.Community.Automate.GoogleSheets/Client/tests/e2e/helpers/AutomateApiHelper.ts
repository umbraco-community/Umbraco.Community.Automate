import type { ApiHelpers } from '@umbraco-cms/acceptance-test-helpers';
import type { APIResponse } from '@playwright/test';

const AUTOMATE_API = '/umbraco/automate/management/api/v1';
const SEED_GOOGLE_SHEETS_ENDPOINT = '/umbraco-automate-e2e/seed-google-sheets';
const GOOGLE_SHEETS_REQUESTS_ENDPOINT = '/umbraco-automate-e2e/google-sheets-requests';

// The engine's `TopologicalSort` (WorkflowCompiler.cs) treats a step-to-step `connections` array as
// authoritative the moment it's non-empty: it BFS's reachability starting from this sentinel ID, and
// any step with no path from it is silently dropped from compilation. Only when `connections` is
// fully empty does the engine fall back to wiring every step sequentially.
const TRIGGER_STEP_ID = '00000000-0000-0000-0000-000000000000';

export interface AppendRowStep {
    id: string;
    alias?: string;
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

function appendRowStepConfig(step: AppendRowStep) {
    return {
        id: step.id,
        alias: step.alias,
        actionAlias: 'googleSheets.appendRow',
        name: 'Append Row to Google Sheet',
        connectionId: step.connectionId,
        settings: {
            SpreadsheetId: step.spreadsheetId,
            SheetName: step.sheetName,
            Columns: step.columns,
        },
        inputMappings: {},
        position: { x: 0, y: 0 },
        errorBehavior: 'Terminate',
    };
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

    // Read-and-clear: every request the stub has observed since the last call, so a test only
    // sees the calls its own run produced rather than leftovers from earlier tests.
    async drainGoogleSheetsRequests(): Promise<string[]> {
        const response = await this.api.get(`${this.api.baseUrl}${GOOGLE_SHEETS_REQUESTS_ENDPOINT}`);
        if (!response.ok()) throw new Error(`Failed to read captured Google Sheets requests: ${response.status()}`);
        const { requests } = await response.json();
        return requests;
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
            steps: [appendRowStepConfig(options.step)],
            // An empty `connections` array puts the engine in its sequential-fallback mode, where
            // every step implicitly runs after the previous one (and the first step after the
            // trigger) — fine for a single step, see TRIGGER_STEP_ID for why this doesn't extend
            // to the multi-step case below.
            connections: [],
        });
        if (response.status() !== 201) throw new Error(`Failed to create automation: ${response.status()}`);
        return locationId(response);
    }

    // Two Append Row steps chained together, with the second wired to depend on the first via
    // `connections` so its settings can bind to the first step's real output at runtime — see
    // `firstStep`/`secondStep`'s callers for how a `${steps.<id>.PropertyName}` expression gets
    // passed as a plain settings string. Because `connections` is non-empty, the trigger→firstStep
    // edge must also be explicit (see TRIGGER_STEP_ID) or the engine treats firstStep as
    // unreachable and silently compiles an empty workflow.
    async createTwoStepAppendRowAutomation(options: {
        alias: string;
        name: string;
        workspaceId: string;
        firstStep: AppendRowStep;
        secondStep: AppendRowStep;
    }): Promise<string> {
        const response = await this.api.post(`${this.api.baseUrl}${AUTOMATE_API}/automations`, {
            alias: options.alias,
            name: options.name,
            workspaceId: options.workspaceId,
            trigger: { triggerAlias: 'umbracoAutomate.manual', settings: {} },
            steps: [appendRowStepConfig(options.firstStep), appendRowStepConfig(options.secondStep)],
            connections: [
                { sourceStepId: TRIGGER_STEP_ID, targetStepId: options.firstStep.id },
                { sourceStepId: options.firstStep.id, targetStepId: options.secondStep.id },
            ],
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
