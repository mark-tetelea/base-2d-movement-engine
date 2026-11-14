// Door ground, open: floor you can walk on, framed by a plum rail so it reads as a door
// way that can close.
module.exports = {
  canvas: [16, 16],
  ramps: ['steel', 'plum'],
  floor: true,
  draw(b, K) {
    const [s0, s1, s2, s3] = K.P.ramps.steel;
    const [p0, p1] = K.P.ramps.plum;
    K.rect(b, 0, 0, 15, 15, s2);
    K.rect(b, 0, 0, 15, 1, p0); K.rect(b, 0, 14, 15, 15, p0);   // rails top and bottom
    K.span(b, 0, 15, 0, p1);
    for (const x of [3, 7, 11]) { b.set(x, 1, s1); b.set(x, 14, s1); }   // slots the bars sink into
    b.set(5, 6, s3); b.set(10, 10, s1);
  }
};
