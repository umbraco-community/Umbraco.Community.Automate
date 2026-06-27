/**
 * Minimal stand-in for `@umbraco-cms/backoffice/modal`. Only the pieces
 * column-list.element.ts actually uses: a context token to look up the modal
 * manager, and `UmbModalToken` as an identity/alias carrier (mirrors the real
 * implementation's shape, which has no Umbraco-specific runtime dependencies).
 */
export const UMB_MODAL_MANAGER_CONTEXT = Symbol("UMB_MODAL_MANAGER_CONTEXT");

export class UmbModalToken {
    #alias;

    constructor(alias, defaults) {
        this.#alias = alias;
        this.defaults = defaults;
    }

    toString() {
        return this.#alias;
    }
}
