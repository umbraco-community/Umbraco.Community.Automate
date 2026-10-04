// Editor for Publish Content to DEV's Body Properties (registered by wwwroot/umbraco-package.json).
// Hand-written: the package has no front-end build. The value is what BodyProperties.Parse reads:
// {"documentType":"…","aliases":[…]} once a document type is picked, otherwise "intro, contentRows".
import { css, html, nothing, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { umbOpenModal } from "@umbraco-cms/backoffice/modal";
import { UMB_PROPERTY_DATASET_CONTEXT } from "@umbraco-cms/backoffice/property";
import { UMB_DOCUMENT_PICKER_MODAL } from "@umbraco-cms/backoffice/document";
import { UMB_DOCUMENT_TYPE_PICKER_MODAL, UmbDocumentTypeDetailRepository } from "@umbraco-cms/backoffice/document-type";
import { UmbDataTypeItemRepository } from "@umbraco-cms/backoffice/data-type";

// Property editors the action can turn into an article body (see ContentMarkdownConverter).
const BODY_EDITORS = {
    "Umbraco.MarkdownEditor": "Markdown",
    "Umbraco.RichText": "Rich Text",
    "Umbraco.TinyMCE": "Rich Text",
    "Umbraco.BlockList": "Block List",
    "Umbraco.BlockGrid": "Block Grid",
    "Umbraco.TextArea": "Textarea",
};

// Matches the modal's alias in wwwroot/umbraco-package.json.
const PREVIEW_MODAL_ALIAS = "UmbracoCommunityAutomateDevTo.Modal.Preview";

// Step settings the preview sends along, by their camelCased PublishContentSettings names.
const PREVIEW_SETTINGS = ["culture", "siteUrl", "canonicalUrl"];

// Same separators as BodyProperties.Parse.
const parseAliases = (value) => (value ?? "").split(/[,;\s]+/).map((a) => a.trim()).filter(Boolean);

const parseValue = (value) => {
    const text = (value ?? "").trim();
    if (!text.startsWith("{")) return { documentType: null, aliases: parseAliases(text) };
    try {
        const stored = JSON.parse(text);
        return { documentType: stored.documentType ?? null, aliases: (stored.aliases ?? []).map((a) => String(a).trim()).filter(Boolean) };
    } catch {
        return { documentType: null, aliases: [] };
    }
};

export class UaDevToBodyPropertiesElement extends UmbLitElement {
    static properties = {
        value: { type: String },
        _documentType: { state: true },
        _available: { state: true },
        _hiddenCount: { state: true },
        _loading: { state: true },
        _manualAlias: { state: true },
    };

    #documentTypes = new UmbDocumentTypeDetailRepository(this);
    #dataTypes = new UmbDataTypeItemRepository(this);
    #settings = {};

    // The document type the aliases were picked from: { unique, name, aliases: Set } once loaded,
    // { unique, missing: true } if it has since been deleted.
    #loadedUnique = null;

    // Alias → name, from the document type, to label the selected aliases.
    #names = new Map();

    constructor() {
        super();
        this.value = "";
        this._documentType = null;
        this._available = [];
        this._hiddenCount = 0;
        this._loading = false;
        this._manualAlias = "";

        this.consumeContext(UMB_PROPERTY_DATASET_CONTEXT, async (dataset) => {
            for (const alias of PREVIEW_SETTINGS) {
                this.observe(await dataset?.propertyValueByAlias(alias), (value) => (this.#settings[alias] = value), `observe-${alias}`);
            }
        });
    }

    get #parsed() {
        return parseValue(this.value);
    }

    updated(changed) {
        if (!changed.has("value")) return;
        const { documentType } = this.#parsed;
        if (documentType && documentType !== this.#loadedUnique) this.#loadDocumentType(documentType);
    }

    #setValue(documentType, aliases) {
        const unique = [...new Set(aliases)];
        this.value = documentType ? JSON.stringify({ documentType, aliases: unique }) : unique.join(", ");
        this.dispatchEvent(new UmbChangeEvent());
    }

    #setAliases(aliases) {
        this.#setValue(this.#parsed.documentType, aliases);
    }

    #toggle(alias, checked) {
        const aliases = this.#parsed.aliases.filter((a) => a !== alias);
        this.#setAliases(checked ? [...aliases, alias] : aliases);
    }

    #move(index, delta) {
        const aliases = [...this.#parsed.aliases];
        const target = index + delta;
        if (target < 0 || target >= aliases.length) return;
        [aliases[index], aliases[target]] = [aliases[target], aliases[index]];
        this.#setAliases(aliases);
    }

    #remove(alias) {
        this.#setAliases(this.#parsed.aliases.filter((a) => a !== alias));
    }

    #addManual() {
        const typed = parseAliases(this._manualAlias);
        if (typed.length === 0) return;
        this.#setAliases([...this.#parsed.aliases, ...typed]);
        this._manualAlias = "";
    }

    async #chooseDocumentType() {
        let selection;
        try {
            ({ selection } = await umbOpenModal(this, UMB_DOCUMENT_TYPE_PICKER_MODAL, {
                data: { hideTreeRoot: true, multiple: false, pickableFilter: (item) => item.isElement === false },
            }));
        } catch {
            return; // Picker closed without choosing.
        }

        const unique = selection?.[0];
        if (!unique) return;

        await this.#loadDocumentType(unique);
        if (!this._documentType?.missing) this.#setValue(unique, this.#parsed.aliases);
    }

    async #loadDocumentType(unique) {
        this.#loadedUnique = unique;
        this._loading = true;
        try {
            const { name, properties } = await this.#collectProperties(unique, new Set());
            if (name === null) {
                this._documentType = { unique, missing: true };
                this._available = [];
                this._hiddenCount = 0;
                return;
            }

            const dataTypeIds = [...new Set(properties.map((p) => p.dataType?.unique).filter(Boolean))];
            const { data: dataTypes } = dataTypeIds.length ? await this.#dataTypes.requestItems(dataTypeIds) : { data: [] };
            const editorByDataType = new Map((dataTypes ?? []).map((d) => [d.unique, d.propertyEditorSchemaAlias]));

            const withEditors = properties.map((p) => ({
                alias: p.alias,
                name: p.name,
                kind: BODY_EDITORS[editorByDataType.get(p.dataType?.unique)],
            }));

            this.#names = new Map(withEditors.map((p) => [p.alias, p.name]));
            this._documentType = { unique, name, aliases: new Set(withEditors.map((p) => p.alias)) };
            this._available = withEditors.filter((p) => p.kind);
            this._hiddenCount = withEditors.length - this._available.length;
        } finally {
            this._loading = false;
        }
    }

    // A document type's own properties followed by those from its compositions.
    async #collectProperties(unique, seen) {
        if (seen.has(unique)) return { name: null, properties: [] };
        seen.add(unique);

        const { data } = await this.#documentTypes.requestByUnique(unique);
        if (!data) return { name: null, properties: [] };

        const inherited = await Promise.all(
            (data.compositions ?? [])
                .map((c) => c.contentType?.unique)
                .filter(Boolean)
                .map((composition) => this.#collectProperties(composition, seen)),
        );

        return { name: data.name, properties: [...(data.properties ?? []), ...inherited.flatMap((i) => i.properties)] };
    }

    async #preview() {
        const documentType = this._documentType && !this._documentType.missing ? this._documentType.unique : null;

        let selection;
        try {
            ({ selection } = await umbOpenModal(this, UMB_DOCUMENT_PICKER_MODAL, {
                data: {
                    multiple: false,
                    pickableFilter: documentType ? (item) => item.documentType?.unique === documentType : undefined,
                },
            }));
        } catch {
            return;
        }

        const contentKey = selection?.[0];
        if (!contentKey) return;

        await umbOpenModal(this, PREVIEW_MODAL_ALIAS, {
            modal: { type: "sidebar", size: "large" },
            data: {
                contentKey,
                bodyProperties: this.value,
                ...Object.fromEntries(PREVIEW_SETTINGS.map((alias) => [alias, this.#settings[alias] ?? null])),
            },
        }).catch(() => {});
    }

    #renderSelected() {
        const { aliases } = this.#parsed;
        if (aliases.length === 0) return html`<p class="empty">No body properties chosen yet.</p>`;

        const onType = this._documentType?.aliases;
        return html`
            <ol class="selected">
                ${repeat(
                    aliases,
                    (alias) => alias,
                    (alias, index) => html`
                        <li>
                            <span class="label">
                                ${this.#names.get(alias) ? html`<strong>${this.#names.get(alias)}</strong> ` : nothing}
                                <code>${alias}</code>
                                ${onType && !onType.has(alias)
                                    ? html`<uui-tag color="danger" look="secondary">Not on ${this._documentType.name}</uui-tag>`
                                    : nothing}
                            </span>
                            <uui-button compact look="secondary" label="Move up" ?disabled=${index === 0} @click=${() => this.#move(index, -1)}>
                                <uui-icon name="icon-arrow-up"></uui-icon>
                            </uui-button>
                            <uui-button compact look="secondary" label="Move down" ?disabled=${index === aliases.length - 1} @click=${() => this.#move(index, 1)}>
                                <uui-icon name="icon-arrow-down"></uui-icon>
                            </uui-button>
                            <uui-button compact look="secondary" label="Remove" @click=${() => this.#remove(alias)}>
                                <uui-icon name="icon-trash"></uui-icon>
                            </uui-button>
                        </li>
                    `,
                )}
            </ol>
        `;
    }

    #renderAvailable() {
        if (this._loading) return html`<uui-loader-bar></uui-loader-bar>`;
        if (!this._documentType) return nothing;

        if (this._documentType.missing) {
            return html`<p class="warning">The document type these properties were picked from no longer exists. Choose it again.</p>`;
        }

        const selected = new Set(this.#parsed.aliases);
        return html`
            <div class="available">
                <p class="heading">Body properties on <strong>${this._documentType.name}</strong></p>
                ${this._available.length === 0
                    ? html`<p class="empty">This document type has no Markdown, Rich Text, Block List, Block Grid or Textarea properties.</p>`
                    : this._available.map(
                          (p) => html`
                              <uui-checkbox
                                  .checked=${selected.has(p.alias)}
                                  label="${p.name} (${p.alias})"
                                  @change=${(e) => this.#toggle(p.alias, e.target.checked)}
                              >
                                  ${p.name} <code>${p.alias}</code> <span class="kind">${p.kind}</span>
                              </uui-checkbox>
                          `,
                      )}
                ${this._hiddenCount > 0
                    ? html`<p class="hint">${this._hiddenCount} other propert${this._hiddenCount === 1 ? "y is" : "ies are"} hidden: they can't hold an article body.</p>`
                    : nothing}
            </div>
        `;
    }

    render() {
        const hasAliases = this.#parsed.aliases.length > 0;
        return html`
            ${this.#renderSelected()}
            <div class="actions">
                <uui-button look="secondary" label="Choose from a document type" @click=${this.#chooseDocumentType}>
                    <uui-icon name="icon-document"></uui-icon> ${this._documentType && !this._documentType.missing ? "Change document type…" : "Choose from a document type…"}
                </uui-button>
                <uui-button look="secondary" label="Preview Markdown" ?disabled=${!hasAliases} @click=${this.#preview}>
                    <uui-icon name="icon-eye"></uui-icon> Preview…
                </uui-button>
            </div>
            ${this.#renderAvailable()}
            <div class="manual">
                <uui-input
                    label="Property alias"
                    placeholder="…or type an alias"
                    .value=${this._manualAlias}
                    @input=${(e) => (this._manualAlias = e.target.value)}
                    @keydown=${(e) => e.key === "Enter" && (e.preventDefault(), this.#addManual())}
                ></uui-input>
                <uui-button look="secondary" label="Add" ?disabled=${!this._manualAlias.trim()} @click=${this.#addManual}>Add</uui-button>
            </div>
        `;
    }

    static styles = css`
        :host { display: block; }
        .selected { margin: 0 0 var(--uui-size-space-3); padding-left: var(--uui-size-space-5); }
        .selected li { display: flex; align-items: center; gap: var(--uui-size-space-2); margin-bottom: var(--uui-size-space-2); }
        .selected .label { flex: 1; }
        .actions, .manual { display: flex; gap: var(--uui-size-space-2); margin-top: var(--uui-size-space-3); }
        .manual uui-input { flex: 1; }
        .available {
            margin-top: var(--uui-size-space-3);
            padding: var(--uui-size-space-4);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-2);
        }
        .heading, .empty, .hint, .warning { margin: 0; }
        .empty, .hint { color: var(--uui-color-text-alt); }
        .warning { margin-top: var(--uui-size-space-3); color: var(--uui-color-danger); }
        .kind { color: var(--uui-color-text-alt); font-size: var(--uui-type-small-size); }
    `;
}

customElements.define("ua-devto-body-properties", UaDevToBodyPropertiesElement);

export { UaDevToBodyPropertiesElement as element };
