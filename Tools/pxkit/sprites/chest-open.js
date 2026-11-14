// Chest, open (lid up, dark inside). Same canvas and pivot as chest.js.
module.exports = {
  canvas: [16, 16],
  ramps: ['wood', 'fire', 'steel'],
  margin: 0,
  draw(b, K) {
    const st = K.P.ramps.steel, w = K.P.ramps.wood, fi = K.P.ramps.fire;
    K.grid(b, [
      '..KKKKKKKKKKKK..',
      '.KlLLLLLLLLLLmK.',
      '.KlLLLLLLLLLLmK.',
      '.KmmmmmmmmmmmdK.',
      '.KKKKKKKKKKKKKK.',
      '.KKKKKKKKKKKKKK.',
      '.KdKKKKKKKKKKdK.',
      '.KKKKKKKKKKKKKK.',
      '.KlLLLKyyKLLLmK.',
      '.KlLLLKyoKLLLmK.',
      '.KlLLLLKKLLLLmK.',
      '.KlLLLLLLLLLLmK.',
      '.KmmmmmmmmmmmdK.',
      '.KddddddddddddK.',
      '..KKKKKKKKKKKK..',
      '................',
    ], { K: st[0], d: w[0], m: w[1], l: w[3], L: w[2], o: fi[2], y: fi[3] });
  }
};
