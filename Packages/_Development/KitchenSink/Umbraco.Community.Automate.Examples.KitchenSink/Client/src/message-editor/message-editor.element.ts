import { css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type { UmbPropertyEditorUiElement } from "@umbraco-cms/backoffice/property-editor";

/**
 * A field editor for an action setting: a text area with a live character count. Shows the
 * smallest useful custom editor: take `value`, render it, and raise `UmbChangeEvent` when it
 * changes so the step's settings are updated.
 */
@customElement("ua-kitchen-sink-message-editor")
export class UaKitchenSinkMessageEditorElement extends UmbLitElement implements UmbPropertyEditorUiElement {
    @property({ type: String })
    value = "";

    #onInput(event: Event) {
        this.value = (event.target as HTMLTextAreaElement).value;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        const length = this.value?.length ?? 0;
        return html`
            <uui-textarea label="Message" .value=${this.value ?? ""} @input=${this.#onInput}></uui-textarea>
            <div class="count">${length} ${length === 1 ? "character" : "characters"}</div>
        `;
    }

    static override styles = css`
        :host {
            display: block;
        }
        .count {
            margin-top: var(--uui-size-space-2, 6px);
            color: var(--uui-color-text-alt, #68676b);
            font-size: var(--uui-type-small-size, 12px);
        }
    `;
}

export default UaKitchenSinkMessageEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "ua-kitchen-sink-message-editor": UaKitchenSinkMessageEditorElement;
    }
}
