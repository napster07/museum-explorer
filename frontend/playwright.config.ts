import { defineConfig } from "@playwright/test";
export default defineConfig({
  testDir: "./tests", fullyParallel: false,
  use: { baseURL: "http://127.0.0.1:3100", browserName: "chromium" },
  webServer: { command: "node node_modules/next/dist/bin/next dev --hostname 127.0.0.1 --port 3100", url: "http://127.0.0.1:3100", reuseExistingServer: false },
});
