import { expect, fixture } from "@open-wc/testing";
import { html } from "lit";
import "./message-editor.element.js";
import type { UaKitchenSinkMessageEditorElement } from "./message-editor.element.js";

function count(element: UaKitchenSinkMessageEditorElement): string {
    return element.shadowRoot!.querySelector(".count")!.textContent!.trim();
}

describe("UaKitchenSinkMessageEditorElement", () => {
    let element: UaKitchenSinkMessageEditorElement;

    beforeEach(async () => {
        element = await fixture(html`<ua-kitchen-sink-message-editor></ua-kitchen-sink-message-editor>`);
    });

    it("counts the characters in the current value", async () => {
        element.value = "Hello";
        await element.updateComplete;

        expect(count(element)).to.equal("5 characters");
    });

    it("uses the singular for one character", async () => {
        element.value = "!";
        await element.updateComplete;

        expect(count(element)).to.equal("1 character");
    });

    it("updates the value and raises a change event when the user types", async () => {
        let changes = 0;
        element.addEventListener("change", () => changes++);

        const textarea = element.shadowRoot!.querySelector("uui-textarea") as HTMLElement & { value: string };
        textarea.value = "Typed";
        textarea.dispatchEvent(new Event("input"));
        await element.updateComplete;

        expect(element.value).to.equal("Typed");
        expect(changes).to.equal(1);
        expect(count(element)).to.equal("5 characters");
    });
});
