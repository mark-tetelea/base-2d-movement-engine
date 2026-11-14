// Crumbling ground, whole: sandstone slabs in the floor's grid, one hairline crack, so it
// reads as "different floor" before anyone steps on it.
module.exports = {
  canvas: [16, 16],
  ramps: ['wood'],
  floor: true,
  draw(b, K) {
    const [w0, w1, w2, w3] = K.P.ramps.wood;
    K.rect(b, 0, 0, 15, 15, w2);
    K.span(b, 0, 15, 15, w1); K.rect(b, 15, 0, 15, 15, w1);   // grout
    K.span(b, 0, 5, 0, w3); K.rect(b, 0, 1, 0, 3, w3);         // glint
    K.line(b, [[5, 5], [7, 7], [7, 9]], w1);                   // hairline crack
  }
};
