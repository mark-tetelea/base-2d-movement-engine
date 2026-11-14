// Shop item: a cut blue gem. Pivot centre (an icon / floor item).
module.exports = {
  canvas: [12, 12],
  ramps: ['blue', 'steel'],
  margin: 0,
  draw(b, K) {
    const st = K.P.ramps.steel, bl = K.P.ramps.blue;
    K.grid(b, [
      '............',
      '............',
      '...KKKKKK...',
      '..KbWBBbbK..',
      '.KbWBBBBbdK.',
      '.KKKKKKKKKK.',
      '..KbBBBbdK..',
      '...KbBbdK...',
      '....KbdK....',
      '.....KK.....',
      '............',
      '............',
    ], { K: st[0], d: bl[0], b: bl[1], B: bl[2], W: st[6] });
  }
};
