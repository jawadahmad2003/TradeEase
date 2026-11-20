import { cpSync, existsSync, mkdirSync, readdirSync, statSync } from "fs";
import { join } from "path";

const src = String.raw`C:\Users\Abdul\OneDrive\Desktop\TradeEase-Frontend\TradeEase-Frontend\tradeease`;
const dest = String.raw`D:\Trade Ease\Trade Ease\tradeease-client`;
const skip = new Set(["node_modules", ".vite", "dist"]);

function copyDir(from, to) {
  mkdirSync(to, { recursive: true });
  for (const name of readdirSync(from)) {
    if (skip.has(name)) continue;
    const s = join(from, name);
    const d = join(to, name);
    if (statSync(s).isDirectory()) copyDir(s, d);
    else cpSync(s, d);
  }
}

copyDir(src, dest);
const srcDir = join(dest, "src");
let count = 0;
function countFiles(dir) {
  for (const name of readdirSync(dir)) {
    const p = join(dir, name);
    if (statSync(p).isDirectory()) countFiles(p);
    else count++;
  }
}
if (existsSync(srcDir)) countFiles(srcDir);
console.log(JSON.stringify({
  ok: existsSync(join(dest, "package.json")),
  srcFiles: count,
}));
