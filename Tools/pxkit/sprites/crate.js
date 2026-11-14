// Destructible crate: a wooden box filling one tile, planks with a cross brace. Pivot centre.
module.exports = {
  canvas: [16, 16],
  ramps: ['wood', 'steel'],
  margin: 0,
  draw(b, K) {
    const st = K.P.ramps.steel, w = K.P.ramps.wood;
    K.grid(b, [
      '.KKKKKKKKKKKKKK.',
      'KLLLLLLLLLLLLLmK',
      'KLmmmmmmmmmmmmdK',
      'KLdlLLLLLLLLLmdK',
      'KLmdlLLLLLLLmddK',
      'KLmLdlLLLLLmdmdK',
      'KLmLLdlLLLmdLmdK',
      'KLmLLLdlLmdLLmdK',
      'KLmLLLLdmdLLLmdK',
      'KLmLLLmddlLLLmdK',
      'KLmLLmdLLdlLLmdK',
      'KLmLmdLLLLdlLmdK',
      'KLmmdLLLLLLdlmdK',
      'KLmmmmmmmmmmmmdK',
      'KmdddddddddddddK',
      '.KKKKKKKKKKKKKK.',
    ], { K: st[0], d: w[0], m: w[1], L: w[2], l: w[3] });
  }
};
