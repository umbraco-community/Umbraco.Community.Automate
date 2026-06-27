import { esbuildPlugin } from "@web/dev-server-esbuild";
import { importMapsPlugin } from "@web/dev-server-import-maps";
import { playwrightLauncher } from "@web/test-runner-playwright";

export default {
    files: "src/**/*.test.ts",
    nodeResolve: true,
    browsers: [playwrightLauncher({ product: "chromium" })],
    plugins: [
        importMapsPlugin({
            inject: {
                importMap: {
                    imports: {
                        // Pure re-exports with no Umbraco-specific runtime behaviour —
                        // safe to point straight at the real backoffice files.
                        "@umbraco-cms/backoffice/external/lit":
                            "/node_modules/@umbraco-cms/backoffice/dist-cms/external/lit/index.js",
                        "@umbraco-cms/backoffice/event":
                            "/node_modules/@umbraco-cms/backoffice/dist-cms/packages/core/event/index.js",
                        // Mocked: the real implementations pull in Umbraco's full
                        // context/controller-host system, which these tests don't need.
                        "@umbraco-cms/backoffice/lit-element": "/src/__mocks__/lit-element.js",
                        "@umbraco-cms/backoffice/modal": "/src/__mocks__/modal.js",
                    },
                },
            },
        }),
        esbuildPlugin({ ts: true, tsconfig: "./tsconfig.json", target: "auto" }),
    ],
};
