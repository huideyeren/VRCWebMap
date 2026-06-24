import { defineConfig } from "vite";

export default defineConfig({
  build: {
    emptyOutDir: true,
    outDir: "wwwroot/assets",
    rollupOptions: {
      input: "src/main.tsx",
      output: {
        entryFileNames: "app.js",
        chunkFileNames: "[name].js",
        // Keep the HTML references stable while still allowing Leaflet image
        // assets to keep their package filenames under the same directory.
        assetFileNames: (assetInfo) =>
          assetInfo.names.some((name) => name.endsWith(".css"))
            ? "app.css"
            : "[name][extname]"
      }
    }
  }
});
