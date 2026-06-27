import { defineConfig } from "vite";
import { resolve } from "path";

export default defineConfig({
    build: {
        lib: {
            entry: {
                "umbraco-community-automate-google-sheets-manifests": resolve(__dirname, "src/manifests.ts"),
            },
            formats: ["es"],
        },
        outDir: "../wwwroot",
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            external: [/^@umbraco/],
        },
    },
});
