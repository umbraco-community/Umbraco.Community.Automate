import { LitElement } from "lit";

/**
 * Minimal stand-in for `@umbraco-cms/backoffice/lit-element`'s `UmbLitElement`.
 * The real class mixes in Umbraco's full context/controller-host system via
 * `UmbElementMixin` — unnecessary weight for unit tests that fully control
 * what `getContext()` resolves to. `__setMockContext` is test-only.
 */
export class UmbLitElement extends LitElement {
    #contexts = new Map();

    __setMockContext(token, value) {
        this.#contexts.set(token, value);
    }

    async getContext(token) {
        return this.#contexts.get(token);
    }
}
