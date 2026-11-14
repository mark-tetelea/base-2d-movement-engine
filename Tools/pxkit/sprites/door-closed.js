// Door ground, closed: plum bars risen out of the floor - a raised block with a lit top,
// a darker front and a steel-0 outline, like the wall tile, so it reads as solid.
module.exports = {
  canvas: [16, 16],
  ramps: ['steel', 'plum'],
  floor: true,
  draw(b, K) {
    const s0 = K.P.ramps.steel[0];
    const [p0, p1, p2] = K.P.ramps.plum;
    K.rect(b, 0, 0, 15, 15, s0);
    K.rect(b, 1, 1, 14, 10, p1);       // top face
    K.span(b, 1, 14, 1, p2);           // lit top edge
    K.rect(b, 1, 2, 1, 10, p2);        // lit left edge
    K.rect(b, 1, 11, 14, 14, p0);      // front face
    for (const x of [4, 8, 12]) K.rect(b, x, 3, x, 13, s0);   // gaps between the bars
  }
};
