// Assembles the Characters tab page (public/character-maker.html) from the character
// maker's sources in tools/minifig-viewer. Unlike the standalone page it carries no game
// files: manager.js reads the body and everything else from the player's game.
import { readFileSync, writeFileSync, mkdirSync, existsSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const src = join(here, "../../../tools/minifig-viewer");
const out = join(here, "../public/character-maker.html");
// Builds that only see apps/mod-manager (the Linux container) use the page built
// before they started.
if (!existsSync(src) && existsSync(out)) {
  console.log(`character maker: sources not here, keeping ${out}`);
  process.exit(0);
}
const read = (name) => readFileSync(join(src, name), "utf8");
const strip = (text) => text.replace(/if\(typeof module!='undefined'\)module\.exports=[^;]*;/g, "");
const scripts = (...names) => strip(names.map(read).join("\n"));

const block = read("data_block.html")
  .replace("/*__DECODERS__*/", () => scripts("tables.js", "decode.js"))
  .replace("/*__MODEL__*/", () => scripts("model.js", "prefab.js", "tex_template.js", "skeleton.js", "gamedat.js"))
  .replace("/*__ASSETS__*/", () => read("manager.js"));

const page =
  '<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">' +
  read("head.html").replace("<title>Minifig Part Viewer</title>", "<title>Character maker</title>") +
  block +
  read("app_script.js") +
  "</body></html>\n";

mkdirSync(dirname(out), { recursive: true });
writeFileSync(out, page);
console.log(`character maker: ${out} (${(page.length / 1e6).toFixed(2)} MB)`);
