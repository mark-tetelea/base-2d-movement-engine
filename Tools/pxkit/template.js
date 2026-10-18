#!/usr/bin/env node
// Make an Aseprite file for the user to draw in: the right canvas, the project palette loaded, Indexed, and
// guides on their own layer - the file set up correctly before the first pixel, which is what traps 56-65 each
// lacked.
//
//   node Tools/pxkit/template.js                    list the templates
//   node Tools/pxkit/template.js player             write templates/player.js's file
//   node Tools/pxkit/template.js player --force     overwrite it (it refuses otherwise - see below)
//
// A template file (templates/<id>.js) says:
//   canvas: [w, h]
//   out: 'Assets/Art/...aseprite'
//   hideGuides: true  - for a file that goes into the game as it is drawn: the guide and reference layers are
//                       saved hidden, because Unity's importer skips hidden layers and ships visible ones
//                       (trap 56). A plain template keeps its guide showing.
//   artLayers: ['far', 'near']  the art layers, bottom to top (default one, 'art'); frames go on the first
//   frames(K)    -> [Buf | null, ...]  the art layer, one entry a frame (default: one empty frame)
//   reference(K) -> Buf | null          a dim layer under the art, the same on every frame
//   guide(K)     -> Buf                 the guide layer, over the art
//
// It never overwrites a file without --force: once the user has drawn in it, the file is their drawing.
const fs = require('fs');
const path = require('path');
const K = require('./kit');
const P = require('./palette');
const A = require('./aseprite');

const ROOT = path.join(__dirname, '..', '..');

// The guide colours every template uses (ART.md, "Templates").
const G = {
  edge: P.ramps.blue[0],      // deep blue: the canvas edge
  zone: P.ramps.hot[0],       // hot red: the margin; a zone that matters
  line: P.ramps.fire[4],      // yellow: where the art must sit (the feet); dashed, the centre line
};

// Guide primitives, shared by the template files.
const guide = {
  G,
  edge(b) { K.rect(b, 0, 0, b.w - 1, 0, G.edge); K.rect(b, 0, b.h - 1, b.w - 1, b.h - 1, G.edge);
            K.rect(b, 0, 0, 0, b.h - 1, G.edge); K.rect(b, b.w - 1, 0, b.w - 1, b.h - 1, G.edge); },
  box(b, x0, y0, x1, y1, c = G.zone) {
    K.span(b, x0, x1, y0, c); K.span(b, x0, x1, y1, c);
    K.rect(b, x0, y0, x0, y1, c); K.rect(b, x1, y0, x1, y1, c);
  },
  row(b, y, c = G.line, x0 = 0, x1 = b.w - 1) { K.span(b, x0, x1, y, c); },
  dashedRow(b, y, c = G.line, x0 = 0, x1 = b.w - 1) { for (let x = x0; x <= x1; x++) if (x % 8 < 4) b.set(x, y, c); },
  centre(b, c = G.line) { const x = b.w / 2; for (let y = 0; y < b.h; y++) if (y % 8 < 4) { b.set(x - 1, y, c); b.set(x, y, c); } },
  // A tick at the left and right edges only, for a row that must not hide what is drawn across it.
  tick(b, y, c = G.line, len = 12) { K.span(b, 0, len - 1, y, c); K.span(b, b.w - len, b.w - 1, y, c); },
};

const LUA = `
local p = app.params
local w, h, n = tonumber(p.w), tonumber(p.h), tonumber(p.frames)
local spr = Sprite(w, h, ColorMode.RGB)
spr:setPalette(Palette{ fromFile = p.gpl })
for i = 2, n do spr:newEmptyFrame() end
local art = spr.layers[1]
local names = {}
for name in string.gmatch(p.layers, "[^,]+") do names[#names + 1] = name end
art.name = names[1]
for i = 2, #names do local l = spr:newLayer() l.name = names[i] end
for i = 1, n do
  local f = p["frame" .. i]
  if f ~= nil and f ~= "" then spr:newCel(art, spr.frames[i], Image{ fromFile = f }, Point(0, 0)) end
end
local ref = nil
if p.reference ~= "" then
  ref = spr:newLayer()
  ref.name = "REFERENCE - hide before import"
  ref.opacity = 96
  ref.stackIndex = 1
  local img = Image{ fromFile = p.reference }
  for i = 1, n do spr:newCel(ref, spr.frames[i], img, Point(0, 0)) end
end
local guide = spr:newLayer()
guide.name = "GUIDE - hide before import"
guide.opacity = 110
local gimg = Image{ fromFile = p.guide }
for i = 1, n do spr:newCel(guide, spr.frames[i], gimg, Point(0, 0)) end
app.command.ChangePixelFormat{ format = "indexed", dithering = "none" }
if p.hide == "1" then
  guide.isVisible = false
  if ref ~= nil then ref.isVisible = false end
end
spr:saveAs(p.out)
local out = io.open(p.result, "w")
out:write(spr.width .. "x" .. spr.height .. " " .. #spr.frames .. " frames " .. #spr.layers .. " layers indexed=" ..
  tostring(spr.colorMode == ColorMode.INDEXED))
out:close()
`;

function make(id, { force = false } = {}) {
  const spec = require(`./templates/${id}.js`);
  const [w, h] = spec.canvas;
  const out = path.resolve(ROOT, spec.out);
  if (fs.existsSync(out) && !force) {
    console.log(`${path.relative(ROOT, out)} exists - not overwritten (it may be the user's drawing). --force to replace.`);
    return false;
  }
  const dir = A.scratch();
  const save = (buf, name) => { const f = path.join(dir, name); buf.save(f); return f; };
  const frames = spec.frames ? spec.frames(K) : [null];
  const params = { w, h, frames: frames.length, gpl: P.source, out, hide: spec.hideGuides ? '1' : '0',
    layers: (spec.artLayers || ['art']).join(',') };
  frames.forEach((f, i) => { params[`frame${i + 1}`] = f ? save(f, `frame${i + 1}.png`) : ''; });
  const ref = spec.reference ? spec.reference(K) : null;
  params.reference = ref ? save(ref, 'reference.png') : '';
  const g = new K.Buf(w, h);
  guide.edge(g);
  if (spec.guide) spec.guide(g, guide, K);
  params.guide = save(g, 'guide.png');

  fs.mkdirSync(path.dirname(out), { recursive: true });
  const said = A.run(LUA, params);
  fs.rmSync(dir, { recursive: true, force: true });
  console.log(`${path.relative(ROOT, out)}  ${said || 'NO RESULT - Aseprite did not finish'}`);
  return !!said;
}

if (require.main === module) {
  const args = process.argv.slice(2);
  const ids = args.filter(a => !a.startsWith('-'));
  if (!ids.length) {
    for (const f of fs.readdirSync(path.join(__dirname, 'templates')).filter(f => f.endsWith('.js') && !f.startsWith('_')))
      console.log(f.slice(0, -3).padEnd(20), require(`./templates/${f}`).out);
  }
  for (const id of ids) make(id, { force: args.includes('--force') });
}

module.exports = { make, guide };
