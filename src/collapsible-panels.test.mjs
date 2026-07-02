import assert from "node:assert/strict";
import test from "node:test";
import { toggleFormPanel } from "./collapsible-panels.ts";

test("toggleFormPanel opens the requested panel exclusively", () => {
    assert.equal(toggleFormPanel(null, "add"), "add");
    assert.equal(toggleFormPanel("add", "edit"), "edit");
    assert.equal(toggleFormPanel("edit", "add"), "add");
});

test("toggleFormPanel closes the active panel", () => {
    assert.equal(toggleFormPanel("add", "add"), null);
    assert.equal(toggleFormPanel("edit", "edit"), null);
});
