import { css, html, customElement, property, repeat, nothing, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UMB_MODAL_MANAGER_CONTEXT, UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type {
    UmbPropertyEditorUiElement,
    UmbPropertyEditorConfigCollection,
} from "@umbraco-cms/backoffice/property-editor";
import type { UUIInputElement } from "@umbraco-cms/backoffice/external/uui";

/**
 * Re-declared locally rather than imported from the core Umbraco.Automate package's
 * frontend (`@umbraco-automate/core`) — that package is an internal npm workspace
 * alias in the core engine's monorepo and isn't published, so it can't be resolved
 * from this standalone repo. Only the shape this file actually reads (length, passed
 * through opaquely to the binding picker modal) matters here.
 */
interface BindingSource {
    id: string;
    label: string;
    subLabel?: string;
    icon: string;
    bindingPrefix: string;
    leaves: unknown[];
}

/**
 * Re-declared locally rather than imported from `@umbraco-automate/core` —
 * the monorepo's two `@umbraco-cms/backoffice` installs (core's and this
 * package's) aren't deduped to a single hoisted copy, so `UmbModalToken`
 * (a class with a private field) has two distinct type identities across
 * the package boundary even at the same version. Resolving the same
 * "Ua.Modal.BindingPicker" alias string against Umbraco's extension
 * registry at runtime is what actually matters, and that's unaffected.
 */
const UA_BINDING_PICKER_MODAL = new UmbModalToken<
    { sources: BindingSource[] },
    { expression: string }
>("Ua.Modal.BindingPicker", {
    modal: {
        type: "sidebar",
        size: "small",
    },
});

/**
 * Edits a `string[]` of spreadsheet column headers for the Google Sheets
 * "Append Row" action.
 *
 * Each row gets its own "Insert binding" button rather than relying on the
 * core `ua-binding-text-box` / property-action ("..." menu) flow. That flow
 * assumes one property editor maps to exactly one caret to insert into —
 * here, one property editor (this element) manages N dynamically-added row
 * inputs, so caret position is tracked per row index instead, mirroring
 * ua-binding-text-box's own blur-based caret capture per row.
 */
@customElement("ua-google-sheets-column-list")
export class UaGoogleSheetsColumnListElement extends UmbLitElement implements UmbPropertyEditorUiElement {
    @property({ attribute: false })
    value: string[] = [];

    @state()
    private _bindingSources: BindingSource[] = [];

    #lastSelection = new Map<number, { start: number; end: number }>();

    public set config(config: UmbPropertyEditorConfigCollection | undefined) {
        if (!config) return;
        this._bindingSources = config.getValueByAlias<BindingSource[]>("bindingSources") ?? [];
    }

    #clone(): string[] {
        return [...(this.value ?? [])];
    }

    #emit(next: string[]) {
        this.value = next;
        this.dispatchEvent(new UmbChangeEvent());
    }

    #onChange(index: number, e: Event) {
        const input = e.target as HTMLInputElement & { value: string };
        const next = this.#clone();
        next[index] = input.value;
        this.#emit(next);
    }

    #onRowBlur(index: number, e: FocusEvent) {
        const input = (e.target as unknown as { _input?: HTMLInputElement })._input;
        if (!input) return;
        this.#lastSelection.set(index, {
            start: input.selectionStart ?? input.value.length,
            end: input.selectionEnd ?? input.value.length,
        });
    }

    #add() {
        this.#emit([...this.#clone(), ""]);
    }

    #remove(index: number) {
        const next = this.#clone();
        next.splice(index, 1);
        this.#emit(next);

        // Shift caret memory down so it stays aligned with the rows that follow.
        const shifted = new Map<number, { start: number; end: number }>();
        for (const [rowIndex, selection] of this.#lastSelection) {
            if (rowIndex === index) continue;
            shifted.set(rowIndex > index ? rowIndex - 1 : rowIndex, selection);
        }
        this.#lastSelection = shifted;
    }

    async #insertBinding(index: number) {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager || this._bindingSources.length === 0) return;

        const modal = modalManager.open(this, UA_BINDING_PICKER_MODAL, {
            data: { sources: this._bindingSources },
        });

        try {
            const { expression } = await modal.onSubmit();

            const currentValue = this.value?.[index] ?? "";
            const selection = this.#lastSelection.get(index);
            const start = selection?.start ?? currentValue.length;
            const end = selection?.end ?? currentValue.length;
            const newValue = currentValue.slice(0, start) + expression + currentValue.slice(end);
            const newCaret = start + expression.length;

            const next = this.#clone();
            next[index] = newValue;
            this.#emit(next);
            this.#lastSelection.set(index, { start: newCaret, end: newCaret });

            await this.updateComplete;
            const input = this.shadowRoot
                ?.querySelectorAll<UUIInputElement>("uui-input")
                .item(index) as unknown as { _input?: HTMLInputElement } | undefined;
            input?._input?.focus();
            input?._input?.setSelectionRange(newCaret, newCaret);
        } catch {
            // Modal dismissed.
        }
    }

    #columnLabel(index: number): string {
        let n = index;
        let label = "";
        do {
            label = String.fromCharCode(65 + (n % 26)) + label;
            n = Math.floor(n / 26) - 1;
        } while (n >= 0);
        return label;
    }

    #renderRow(value: string, index: number) {
        return html`
            <div class="row">
                <span class="col-label">${this.#columnLabel(index)}</span>
                <uui-input
                    class="column-input"
                    .value=${value}
                    @change=${(e: Event) => this.#onChange(index, e)}
                    @blur=${(e: FocusEvent) => this.#onRowBlur(index, e)}
                ></uui-input>
                ${this._bindingSources.length > 0
                    ? html`
                          <uui-button
                              look="secondary"
                              compact
                              label="Insert binding"
                              @click=${() => this.#insertBinding(index)}
                          >
                              <uui-icon name="icon-code"></uui-icon>
                          </uui-button>
                      `
                    : nothing}
                <uui-button look="secondary" compact label="Remove" @click=${() => this.#remove(index)}>
                    <uui-icon name="icon-trash"></uui-icon>
                </uui-button>
            </div>
        `;
    }

    override render() {
        const rows = this.value ?? [];

        return html`
            <div class="column-list">
                ${rows.length > 0
                    ? repeat(
                          rows,
                          (_value, index) => index,
                          (value, index) => this.#renderRow(value, index),
                      )
                    : nothing}
                <uui-button look="placeholder" label="Add column" @click=${() => this.#add()}>
                    Add column
                </uui-button>
            </div>
        `;
    }

    static override styles = [
        css`
            :host {
                display: block;
            }

            .row {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-3);
                margin-bottom: var(--uui-size-space-2);
            }

            .col-label {
                width: 2ch;
                font-weight: bold;
                color: var(--uui-color-text-alt);
            }

            .column-input {
                flex: 1;
            }
        `,
    ];
}

export default UaGoogleSheetsColumnListElement;

declare global {
    interface HTMLElementTagNameMap {
        "ua-google-sheets-column-list": UaGoogleSheetsColumnListElement;
    }
}
