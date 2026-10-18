// Test-arena floor tile. Mostly steel-2 (the ground colour every sprite is judged
// against), one stone per tile: grout on the right and bottom edge so tiles form a
// grid, a glint on the top-left corner (light from the upper left), a few worn specks.
// Kept low-contrast so anything standing on it reads first.
module.exports = {
  canvas: [16, 16],
  ramps: ['steel'],
  floor: true,
  draw(b, K) {
    const [s0, s1, s2, s3] = K.P.ramps.steel;
    K.rect(b, 0, 0, 15, 15, s2);
    K.span(b, 0, 15, 15, s1);         // bottom grout
    K.rect(b, 15, 0, 15, 15, s1);     // right grout
    K.span(b, 0, 5, 0, s3);           // glint along the top edge
    K.rect(b, 0, 1, 0, 3, s3);        // and down the left edge
    for (const [x, y] of [[9, 4], [4, 10], [12, 9]]) b.set(x, y, s1);
    b.set(10, 12, s3);
  }
};
