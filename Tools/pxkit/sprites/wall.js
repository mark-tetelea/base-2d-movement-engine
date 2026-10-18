// Test-arena wall tile: a stone block seen from above at a slight tilt - a lit top
// face and a short darker front face, outlined so it stands clear of the floor.
module.exports = {
  canvas: [16, 16],
  ramps: ['steel'],
  draw(b, K) {
    const [s0, s1, , s3, s4, s5] = K.P.ramps.steel;
    K.rect(b, 0, 0, 15, 15, s0);      // outline
    K.rect(b, 1, 1, 14, 10, s4);      // top face
    K.span(b, 1, 14, 1, s5);          // lit top edge
    K.rect(b, 1, 2, 1, 10, s5);       // lit left edge
    K.rect(b, 14, 2, 14, 10, s3);     // shaded right edge
    K.span(b, 1, 14, 11, s1);         // where the top meets the front
    K.rect(b, 1, 12, 14, 14, s3);     // front face
    K.rect(b, 1, 12, 1, 14, s4);
  }
};
