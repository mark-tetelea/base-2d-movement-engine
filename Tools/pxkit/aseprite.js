// Run a Lua script in Aseprite, headless, and hand back what it wrote.
//
// Two things CLAUDE.md's Environment section found the hard way: `print()` from a script never reaches the
// calling shell on Windows, so a script reports by writing a file; and a script must never edit a file the
// user has open in the Aseprite window - their next Ctrl+S overwrites it without a word.
const fs = require('fs');
const os = require('os');
const path = require('path');
const { execFileSync } = require('child_process');

const EXE = process.env.ASEPRITE || 'D:\\Aseprite.v1.3.7\\Aseprite.exe';

/** Run `lua` (source text) with `params`; returns whatever the script wrote to app.params.result. */
function run(lua, params = {}) {
  if (!fs.existsSync(EXE)) throw new Error(`Aseprite not found at ${EXE} - set ASEPRITE to its path`);
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'pxkit-'));
  const script = path.join(dir, 'run.lua');
  const result = path.join(dir, 'result.txt');
  fs.writeFileSync(script, lua);
  const args = ['-b'];
  for (const [k, v] of Object.entries({ ...params, result })) args.push('--script-param', `${k}=${v}`);
  args.push('--script', script);
  execFileSync(EXE, args, { stdio: 'ignore' });
  const out = fs.existsSync(result) ? fs.readFileSync(result, 'utf8') : '';
  fs.rmSync(dir, { recursive: true, force: true });
  return out;
}

/** A scratch directory for PNGs handed to a script. */
const scratch = () => fs.mkdtempSync(path.join(os.tmpdir(), 'pxkit-png-'));

module.exports = { run, scratch, EXE };
