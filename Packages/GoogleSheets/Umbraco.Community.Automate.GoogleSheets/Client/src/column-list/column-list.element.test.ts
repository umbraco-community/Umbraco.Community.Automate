import { expect, fixture } from "@open-wc/testing";
import { html } from "lit";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import "./column-list.element.js";
import type { UaGoogleSheetsColumnListElement } from "./column-list.element.js";

function getColumnLabels(element: UaGoogleSheetsColumnListElement): string[] {
    return Array.from(element.shadowRoot!.querySelectorAll(".col-label")).map((el) => el.textContent);
}

function getInputs(element: UaGoogleSheetsColumnListElement): HTMLElement[] {
    return Array.from(element.shadowRoot!.querySelectorAll("uui-input"));
}

function clickButton(element: UaGoogleSheetsColumnListElement, label: string, index = 0): void {
    const button = Array.from(element.shadowRoot!.querySelectorAll("uui-button")).find(
        (el) => el.getAttribute("label") === label,
    );
    const buttons = Array.from(element.shadowRoot!.querySelectorAll("uui-button")).filter(
        (el) => el.getAttribute("label") === label,
    );
    const target = index === 0 ? button : buttons[index];
    target!.dispatchEvent(new MouseEvent("click", { bubbles: true, composed: true }));
}

describe("UaGoogleSheetsColumnListElement", () => {
    let element: UaGoogleSheetsColumnListElement;

    beforeEach(async () => {
        element = await fixture(html`<ua-google-sheets-column-list></ua-google-sheets-column-list>`);
    });

    describe("column lettering", () => {
        it("labels the first 26 columns A through Z", async () => {
            element.value = Array.from({ length: 26 }, () => "");
            await element.updateComplete;

            const labels = getColumnLabels(element);
            expect(labels[0]).to.equal("A");
            expect(labels[25]).to.equal("Z");
        });

        it("continues into double letters after Z (AA, AB)", async () => {
            element.value = Array.from({ length: 28 }, () => "");
            await element.updateComplete;

            const labels = getColumnLabels(element);
            expect(labels[26]).to.equal("AA");
            expect(labels[27]).to.equal("AB");
        });
    });

    describe("add / remove", () => {
        it("adds an empty column and emits a change event", async () => {
            let changeCount = 0;
            element.addEventListener("change", () => changeCount++);

            clickButton(element, "Add column");
            await element.updateComplete;

            expect(element.value).to.deep.equal([""]);
            expect(changeCount).to.equal(1);
        });

        it("removes the targeted row, shifting the remaining rows down", async () => {
            element.value = ["alice", "bob", "carol"];
            await element.updateComplete;

            clickButton(element, "Remove", 1); // remove "bob"
            await element.updateComplete;

            expect(element.value).to.deep.equal(["alice", "carol"]);
        });
    });

    describe("editing a row", () => {
        it("updates the value array and emits a change event on input change", async () => {
            element.value = ["", ""];
            await element.updateComplete;

            let changeCount = 0;
            element.addEventListener("change", () => changeCount++);

            const input = getInputs(element)[1] as HTMLElement & { value: string };
            input.value = "alice@example.com";
            // composed: false — the `@change=` binding is on this exact input element, so it
            // doesn't need to cross the shadow boundary. Composing it would also make this same
            // event visible on the host listener below, double-counting alongside the UmbChangeEvent.
            input.dispatchEvent(new Event("change", { bubbles: true }));
            await element.updateComplete;

            expect(element.value).to.deep.equal(["", "alice@example.com"]);
            expect(changeCount).to.equal(1);
        });
    });

    describe("insert binding", () => {
        function stubModalManager(element: UaGoogleSheetsColumnListElement, expression: string) {
            const modalManager = {
                open: () => ({ onSubmit: () => Promise.resolve({ expression }) }),
            };
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            (element as any).__setMockContext(UMB_MODAL_MANAGER_CONTEXT, modalManager);
        }

        beforeEach(async () => {
            element.config = {
                getValueByAlias: () => [
                    { id: "trigger", label: "Trigger", icon: "icon-bolt", bindingPrefix: "trigger", leaves: [] },
                ],
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
            } as any;
            await element.updateComplete;
        });

        it("inserts the picked expression at the end when no caret position was recorded", async () => {
            element.value = ["hello "];
            await element.updateComplete;
            stubModalManager(element, "@trigger.email");

            clickButton(element, "Insert binding");
            // #insertBinding awaits modal.onSubmit() then this.updateComplete — flush both.
            await new Promise((resolve) => setTimeout(resolve, 0));
            await element.updateComplete;

            expect(element.value).to.deep.equal(["hello @trigger.email"]);
        });

        it("inserts at the last recorded caret position for that row, not always at the end", async () => {
            element.value = ["hello world"];
            await element.updateComplete;

            const input = getInputs(element)[0] as HTMLElement & {
                _input?: { selectionStart: number; selectionEnd: number };
            };
            // Simulate the caret having been at position 5 ("hello| world") when the row blurred.
            (input as unknown as { _input: unknown })._input = { selectionStart: 5, selectionEnd: 5 };
            input.dispatchEvent(new FocusEvent("blur", { bubbles: true, composed: true }));

            stubModalManager(element, "@trigger.name");
            clickButton(element, "Insert binding");
            await new Promise((resolve) => setTimeout(resolve, 0));
            await element.updateComplete;

            expect(element.value).to.deep.equal(["hello@trigger.name world"]);
        });

        it("re-indexes caret memory when an earlier row is removed", async () => {
            element.value = ["first", "second"];
            await element.updateComplete;

            // Record a caret position for row 1 ("second"), then remove row 0.
            const secondInput = getInputs(element)[1] as HTMLElement & {
                _input?: { selectionStart: number; selectionEnd: number };
            };
            (secondInput as unknown as { _input: unknown })._input = { selectionStart: 3, selectionEnd: 3 };
            secondInput.dispatchEvent(new FocusEvent("blur", { bubbles: true, composed: true }));

            clickButton(element, "Remove", 0); // remove "first" — "second" is now row 0
            await element.updateComplete;

            stubModalManager(element, "X");
            clickButton(element, "Insert binding");
            await new Promise((resolve) => setTimeout(resolve, 0));
            await element.updateComplete;

            // If caret memory weren't re-indexed, this would fall back to end-of-string (no caret
            // recorded for the new row 0) instead of splicing at the carried-over position 3.
            expect(element.value).to.deep.equal(["secXond"]);
        });
    });
});
