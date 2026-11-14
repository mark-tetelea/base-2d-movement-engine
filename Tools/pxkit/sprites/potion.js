// Shop item: a small red potion bottle with a cork. Pivot centre (an icon / floor item).
module.exports = {
  canvas: [12, 12],
  ramps: ['plum', 'wood', 'steel'],
  margin: 0,
  draw(b, K) {
    const st = K.P.ramps.steel, r = K.P.ramps.plum, w = K.P.ramps.wood;
    K.grid(b, [
      '....KKK.....',
      '....KcK.....',
      '...KKKKK....',
      '....KgK.....',
      '...KgggK....',
      '..KgrrrgK...',
      '..KrWRrdK...',
      '..KrRrrdK...',
      '..KrrrrdK...',
      '...KddddK...',
      '....KKKK....',
      '............',
    ], { K: st[0], c: w[2], g: st[4], d: r[0], r: r[1], R: r[2], W: st[6] });
  }
};
