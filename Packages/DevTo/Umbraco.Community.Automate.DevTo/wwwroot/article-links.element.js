// "DEV" box on a document's Info tab (registered by wwwroot/umbraco-package.json): the DEV articles
// Publish Content to DEV last posted this item as, from DevToController. Renders
// nothing until the item has been posted. DEV is only called when someone clicks Check on DEV.
import { css, html, nothing, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { observeMultiple } from "@umbraco-cms/backoffice/observable-api";
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/document";
import { UMB_ACTION_EVENT_CONTEXT } from "@umbraco-cms/backoffice/action";
import { UmbRequestReloadStructureForEntityEvent } from "@umbraco-cms/backoffice/entity-action";
import { devToApi } from "./api.js";

export class UaDevToArticleLinksElement extends UmbLitElement {
    static properties = {
        _links: { state: true },
        _problems: { state: true },
        _checking: { state: true },
    };

    #unique = null;
    #actionEvents = null;

    // Publishing reloads the workspace; the automation posts shortly after, so check again then.
    #onReload = (event) => {
        if (event.getUnique() !== this.#unique) return;
        this.#load();
        setTimeout(() => this.#load(), 5000);
    };

    constructor() {
        super();
        this._links = [];
        this._problems = [];
        this._checking = false;

        this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (workspace) => {
            if (!workspace) return;
            this.observe(
                observeMultiple([workspace.isNew, workspace.unique]),
                ([isNew, unique]) => {
                    if (isNew || !unique || unique === this.#unique) return;
                    this.#unique = unique;
                    this.#load();
                },
                "observeWorkspaceState",
            );
        });

        this.consumeContext(UMB_ACTION_EVENT_CONTEXT, (actionEvents) => {
            this.#actionEvents?.removeEventListener(UmbRequestReloadStructureForEntityEvent.TYPE, this.#onReload);
            this.#actionEvents = actionEvents;
            actionEvents?.addEventListener(UmbRequestReloadStructureForEntityEvent.TYPE, this.#onReload);
        });
    }

    disconnectedCallback() {
        super.disconnectedCallback();
        this.#actionEvents?.removeEventListener(UmbRequestReloadStructureForEntityEvent.TYPE, this.#onReload);
    }

    #request(method, suffix = "", query) {
        return devToApi(this, method, `article-links/${this.#unique}${suffix}`, { query });
    }

    async #load() {
        const unique = this.#unique;
        const { data } = await this.#request("get");
        if (unique === this.#unique) this._links = data ?? [];
    }

    async #check() {
        this._checking = true;
        try {
            const { data, error } = await this.#request("post", "/check");
            if (data) {
                this._links = data.links;
                this._problems = data.problems;
            } else {
                this._problems = [error?.problemDetails?.title ?? "The check failed."];
            }
        } finally {
            this._checking = false;
        }
    }

    async #remove(link) {
        const { data, error } = await this.#request("delete", "", link.culture ? { culture: link.culture } : undefined);
        if (data) this._links = data;
        else this._problems = [error?.problemDetails?.status === 403 ? "You don't have permission to update this page." : "The link couldn't be removed."];
    }

    #renderLink(link) {
        const date = (value) => html`<umb-localize-date .date=${value} .options=${{ dateStyle: "medium", timeStyle: "short" }}></umb-localize-date>`;
        return html`
            <li class=${link.deleted ? "deleted" : ""}>
                <a href=${link.url} target="_blank" rel="noopener">
                    <uui-icon name="icon-out"></uui-icon>
                    ${link.culture ? html`<span class="culture">${link.culture}</span>` : nothing}
                    <span class="url">${link.url}</span>
                </a>
                <p class="meta">
                    ${link.deleted
                        ? html`<uui-tag look="secondary" color="danger">Deleted on DEV</uui-tag>`
                        : html`<uui-tag look="secondary" color=${link.published ? "positive" : "default"}>${link.published ? "Published" : "Draft"}</uui-tag>`}
                    <span>Last posted ${date(link.postedUtc)}${link.checkedUtc ? html` · checked ${date(link.checkedUtc)}` : nothing}</span>
                </p>
                ${link.deleted
                    ? html`<p class="deleted-hint">
                          Publishing this page again will post a new article.
                          <uui-button compact look="secondary" color="danger" label="Remove link" @click=${() => this.#remove(link)}>Remove link</uui-button>
                      </p>`
                    : nothing}
            </li>
        `;
    }

    render() {
        if (this._links.length === 0) return nothing;

        return html`
            <umb-workspace-info-app-layout headline="DEV">
                <uui-button
                    slot="header-actions"
                    compact
                    look="secondary"
                    label="Check on DEV"
                    .state=${this._checking ? "waiting" : undefined}
                    ?disabled=${this._checking}
                    @click=${this.#check}
                >
                    <uui-icon name="icon-refresh"></uui-icon> Check on DEV
                </uui-button>
                <ul>
                    ${repeat(this._links, (link) => link.culture ?? "", (link) => this.#renderLink(link))}
                </ul>
                ${this._problems.length
                    ? html`<ul class="problems">${this._problems.map((problem) => html`<li>${problem}</li>`)}</ul>`
                    : nothing}
            </umb-workspace-info-app-layout>
        `;
    }

    static styles = css`
        ul { list-style: none; margin: 0; padding: var(--uui-size-space-4) var(--uui-size-space-5); display: flex; flex-direction: column; gap: var(--uui-size-space-4); }
        a { display: flex; align-items: center; gap: var(--uui-size-space-2); color: var(--uui-color-interactive); text-decoration: none; }
        a:hover { color: var(--uui-color-interactive-emphasis); text-decoration: underline; }
        .url { word-break: break-all; }
        .culture { font-weight: 700; }
        .meta { margin: var(--uui-size-space-2) 0 0; display: flex; flex-wrap: wrap; align-items: center; gap: var(--uui-size-space-2); color: var(--uui-color-text-alt); font-size: var(--uui-type-small-size); }
        .deleted a { color: var(--uui-color-text-alt); text-decoration: line-through; }
        .deleted-hint { margin: var(--uui-size-space-2) 0 0; display: flex; align-items: center; gap: var(--uui-size-space-3); font-size: var(--uui-type-small-size); }
        .problems { padding-top: 0; color: var(--uui-color-danger); font-size: var(--uui-type-small-size); }
    `;
}

customElements.define("ua-devto-article-links", UaDevToArticleLinksElement);

export { UaDevToArticleLinksElement as element };
