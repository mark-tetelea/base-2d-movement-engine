// PNG read + write, RGBA8, no dependencies.
//
// Why read as well as write: lint.js has to be able to check a PNG that came
// out of Aseprite, not only one pxkit drew. A rule that only applies to
// generated art is half a rule.
const zlib = require('zlib');
const fs = require('fs');

let TABLE = null;
function crc32(buf) {
  if (!TABLE) {
    TABLE = new Int32Array(256);
    for (let n = 0; n < 256; n++) {
      let c = n;
      for (let k = 0; k < 8; k++) c = c & 1 ? 0xEDB88320 ^ (c >>> 1) : c >>> 1;
      TABLE[n] = c;
    }
  }
  let crc = -1;
  for (let i = 0; i < buf.length; i++) crc = TABLE[(crc ^ buf[i]) & 0xFF] ^ (crc >>> 8);
  return (crc ^ -1) >>> 0;
}
function chunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const td = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td));
  return Buffer.concat([len, td, crc]);
}

/** Write RGBA bytes (w*h*4) to a PNG file. */
function write(file, w, h, rgba) {
  const stride = w * 4;
  const raw = Buffer.alloc((stride + 1) * h);
  for (let y = 0; y < h; y++) {
    raw[y * (stride + 1)] = 0;                       // filter: none
    rgba.copy(raw, y * (stride + 1) + 1, y * stride, (y + 1) * stride);
  }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4);
  ihdr[8] = 8; ihdr[9] = 6;                          // 8-bit, RGBA
  fs.writeFileSync(file, Buffer.concat([
    Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    chunk('IHDR', ihdr),
    chunk('IDAT', zlib.deflateSync(raw, { level: 9 })),
    chunk('IEND', Buffer.alloc(0))
  ]));
}

const paeth = (a, b, c) => {
  const p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
  return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
};

/** Read a PNG into {w, h, rgba}. Bit depth 8, non-interlaced, colour types 0/2/3/6. */
function read(file) {
  const buf = fs.readFileSync(file);
  let p = 8, w = 0, h = 0, ct = 0, bd = 0, plte = null, trns = null;
  const idat = [];
  while (p < buf.length) {
    const len = buf.readUInt32BE(p);
    const type = buf.toString('ascii', p + 4, p + 8);
    const data = buf.slice(p + 8, p + 8 + len);
    if (type === 'IHDR') {
      w = data.readUInt32BE(0); h = data.readUInt32BE(4); bd = data[8]; ct = data[9];
      if (bd !== 8) throw new Error(`${file}: bit depth ${bd} unsupported (need 8)`);
      if (data[12] !== 0) throw new Error(`${file}: interlaced PNGs unsupported`);
    } else if (type === 'PLTE') plte = data;
    else if (type === 'tRNS') trns = data;
    else if (type === 'IDAT') idat.push(data);
    else if (type === 'IEND') break;
    p += 12 + len;
  }
  const CH = { 0: 1, 2: 3, 3: 1, 4: 2, 6: 4 }[ct];
  if (!CH) throw new Error(`${file}: colour type ${ct} unsupported`);
  const raw = zlib.inflateSync(Buffer.concat(idat));
  const stride = w * CH;
  const lines = Buffer.alloc(stride * h);
  for (let y = 0; y < h; y++) {
    const ft = raw[y * (stride + 1)];
    const src = y * (stride + 1) + 1, dst = y * stride, prev = dst - stride;
    for (let x = 0; x < stride; x++) {
      const a = x >= CH ? lines[dst + x - CH] : 0;
      const b = y > 0 ? lines[prev + x] : 0;
      const c = x >= CH && y > 0 ? lines[prev + x - CH] : 0;
      const v = raw[src + x];
      lines[dst + x] =
        ft === 0 ? v : ft === 1 ? v + a : ft === 2 ? v + b :
        ft === 3 ? v + ((a + b) >> 1) : v + paeth(a, b, c);
    }
  }
  const rgba = Buffer.alloc(w * h * 4);
  for (let i = 0; i < w * h; i++) {
    const s = i * CH, d = i * 4;
    if (ct === 6) { rgba[d] = lines[s]; rgba[d+1] = lines[s+1]; rgba[d+2] = lines[s+2]; rgba[d+3] = lines[s+3]; }
    else if (ct === 2) { rgba[d] = lines[s]; rgba[d+1] = lines[s+1]; rgba[d+2] = lines[s+2]; rgba[d+3] = 255; }
    else if (ct === 0) { rgba[d] = rgba[d+1] = rgba[d+2] = lines[s]; rgba[d+3] = 255; }
    else if (ct === 4) { rgba[d] = rgba[d+1] = rgba[d+2] = lines[s]; rgba[d+3] = lines[s+1]; }
    else { const k = lines[s]; rgba[d] = plte[k*3]; rgba[d+1] = plte[k*3+1]; rgba[d+2] = plte[k*3+2];
           rgba[d+3] = trns && k < trns.length ? trns[k] : 255; }
  }
  return { w, h, rgba };
}

module.exports = { read, write };
