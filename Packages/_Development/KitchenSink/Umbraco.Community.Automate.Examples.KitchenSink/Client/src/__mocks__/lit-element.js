import { LitElement } from "lit";

/**
 * Test stand-in for `@umbraco-cms/backoffice/lit-element`'s `UmbLitElement`. The real class
 * mixes in Umbraco's context system, which needs the whole backoffice running; a plain
 * LitElement is all a unit test of this editor needs.
 */
export class UmbLitElement extends LitElement {}
