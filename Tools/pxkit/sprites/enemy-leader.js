// Group leader: the enemy blob with a small gold crown, so the one the others follow
// stands out. Same body as enemy.js (drawn here, not shared: used once). Pivot centre.
module.exports = {
  canvas: [16, 16],
  ramps: ['plum', 'steel', 'fire'],
  margin: 0,
  draw(b, K) {
    const r = K.P.ramps.plum, st = K.P.ramps.steel, fi = K.P.ramps.fire;
    K.grid(b, [
      '....K.KK.K......',
      '...KYKYYKYK.....',
      '...KyyyyyyK.....',
      '...KKKKKKKKK....',
      '...KrrRRRRrrK...',
      '..KrrRRRRRRrrK..',
      '..KrWWrRRrWWrK..',
      '..KrWKrrrrWKrK..',
      '..KrrrrrrrrrrK..',
      '..KdrrrrrrrrdK..',
      '..KddrrKKrrddK..',
      '...KddrrrrddK...',
      '....KKddddKK....',
      '......KKKK......',
      '................',
      '................',
    ], { K: st[0], d: r[0], r: r[1], R: r[2], W: st[4], y: fi[3], Y: fi[4] });
  }
};
