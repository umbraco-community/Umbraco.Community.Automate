import { esbuildPlugin } from "@web/dev-server-esbuild";
import { importMapsPlugin } from "@web/dev-server-import-maps";
import { playwrightLauncher } from "@web/test-runner-playwright";

// Runs src/**/*.test.ts in a real browser. Umbraco's backoffice modules are mapped to the real
// files where they're plain re-exports, and to small mocks where the real ones need the whole
// backoffice running.
export default {
    files: "src/**/*.test.ts",
    nodeResolve: true,
    browsers: [playwrightLauncher({ product: "chromium" })],
    plugins: [
        importMapsPlugin({
            inject: {
                importMap: {
                    imports: {
                        "@umbraco-cms/backoffice/external/lit":
                            "/node_modules/@umbraco-cms/backoffice/dist-cms/external/lit/index.js",
                        "@umbraco-cms/backoffice/event":
                            "/node_modules/@umbraco-cms/backoffice/dist-cms/packages/core/event/index.js",
                        "@umbraco-cms/backoffice/lit-element": "/src/__mocks__/lit-element.js",
                    },
                },
            },
        }),
        esbuildPlugin({ ts: true, tsconfig: "./tsconfig.json", target: "auto" }),
    ],
};
