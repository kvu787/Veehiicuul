// Render the local SVG chart as PNG for Markdown portability and visual review.
// Usage: node RenderChart.mjs [PathToNodeModules]
import { createRequire } from "node:module";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
const directory = dirname(fileURLToPath(import.meta.url));
const moduleDirectory = process.argv[2] || join(process.env.USERPROFILE,
  ".cache", "codex-runtimes", "codex-primary-runtime", "dependencies", "node", "node_modules");
const require = createRequire(join(resolve(moduleDirectory), "ArtifactLoader.cjs"));
const sharp = require("sharp");
await sharp(join(directory, "FrameRateTimeline.svg"), { density: 144 })
  .png().toFile(join(directory, "FrameRateTimeline.png"));
console.log("Saved FrameRateTimeline.png");
