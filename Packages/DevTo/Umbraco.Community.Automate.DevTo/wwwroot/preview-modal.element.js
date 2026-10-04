// Preview for Publish Content to DEV (registered by wwwroot/umbraco-package.json, opened from the
// Body Properties editor). Shows the Markdown and canonical URL the action would post, from
// DevToController.Preview; DEV itself isn't called.
import { css, html, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { devToApi } from "./api.js";

export class UaDevToPreviewModalElement extends UmbModalBaseElement {
    static properties = {
        _result: { state: true },
        _error: { state: true },
        _copied: { state: true },
    };

    constructor() {
        super();
        this._result = null;
        this._error = null;
        this._copied = false;
    }

    async firstUpdated() {
        const { data, error } = await devToApi(this, "post", "preview", { body: this.data });

        if (error) {
            const problem = error.problemDetails;
            this._error = { title: problem?.title ?? "The preview failed", detail: problem?.detail ?? error.message };
        } else {
            this._result = data;
        }
    }

    async #copy() {
        await navigator.clipboard.writeText(this._result.markdown);
        this._copied = true;
    }

    #renderResult() {
        if (this._error) {
            return html`
                <uui-box headline=${this._error.title}>
                    <p class="error">${this._error.detail}</p>
                </uui-box>
            `;
        }

        if (!this._result) return html`<uui-loader-bar></uui-loader-bar>`;

        const { canonicalUrl, culture, markdown, notes } = this._result;
        return html`
            ${notes.length
                ? html`<uui-box headline="Notes">
                      <ul class="notes">
                          ${notes.map((note) => html`<li>${note}</li>`)}
                      </ul>
                  </uui-box>`
                : nothing}
            <uui-box headline="Canonical URL">
                <a href=${canonicalUrl} target="_blank" rel="noopener">${canonicalUrl}</a>
                ${culture ? html`<p class="culture">Culture: ${culture}</p>` : nothing}
            </uui-box>
            <uui-box headline="Markdown">
                <uui-button slot="header-actions" compact look="secondary" label="Copy Markdown" @click=${this.#copy}>
                    <uui-icon name=${this._copied ? "icon-check" : "icon-documents"}></uui-icon> ${this._copied ? "Copied" : "Copy"}
                </uui-button>
                <pre>${markdown}</pre>
            </uui-box>
        `;
    }

    render() {
        return html`
            <umb-body-layout headline=${this._result?.name ? `Preview: ${this._result.name}` : "Preview"}>
                <div class="content">${this.#renderResult()}</div>
                <uui-button slot="actions" look="secondary" label="Close" @click=${this._rejectModal}>Close</uui-button>
            </umb-body-layout>
        `;
    }

    static styles = css`
        .content { display: flex; flex-direction: column; gap: var(--uui-size-layout-1); }
        pre {
            margin: 0;
            white-space: pre-wrap;
            word-break: break-word;
            font-family: var(--uui-font-monospace, monospace);
            font-size: var(--uui-type-small-size);
        }
        .notes { margin: 0; padding-left: var(--uui-size-space-5); }
        .culture { margin: var(--uui-size-space-2) 0 0; color: var(--uui-color-text-alt); }
        .error { margin: 0; color: var(--uui-color-danger); }
        a { word-break: break-all; }
    `;
}

customElements.define("ua-devto-preview-modal", UaDevToPreviewModalElement);

export { UaDevToPreviewModalElement as element };
