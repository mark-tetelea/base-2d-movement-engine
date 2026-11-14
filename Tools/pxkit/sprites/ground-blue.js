// Blue ground: cannot be stepped on, but bullets and sight pass over it - a low block of
// water. A darker bottom row and right edge so a patch reads as one sunken pool.
module.exports = {
  canvas: [16, 16],
  ramps: ['blue', 'steel'],
  floor: true,
  draw(b, K) {
    const [b0, b1, b2] = K.P.ramps.blue;
    K.rect(b, 0, 0, 15, 15, b0);
    K.span(b, 0, 15, 15, K.P.ramps.steel[1]);       // sunken edge
    K.rect(b, 15, 0, 15, 15, K.P.ramps.steel[1]);
    K.span(b, 2, 6, 4, b1); K.span(b, 3, 5, 3, b1);  // ripples
    K.span(b, 8, 13, 9, b1); K.span(b, 9, 12, 8, b1);
    b.set(4, 3, b2); b.set(10, 8, b2);               // glints, light from the upper left
  }
};
