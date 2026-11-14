// The player, facing right (facing 0): side view, one eye, legs apart on the steps: idle, step A, step B side by side (16x24 frames), like
// player-down.js. The left-side facings are this drawing flipped in Unity.
const HEAD = [
  '................', '....KKKKKKK.....', '...KhhhhhhhK....', '...KhHHhhhhhK...', '...KhhhhhhhhK...',
  '...KhhhhssssK...', '...KhhhsssKsK...', '...KhhhssssSK...', '....KhssssssK...',
];
const SIDE = [
  '....KbbbbbbK....',
  '...KcbbbbbbBK...',
  '...KcbbbbbbBK...',
  '...KbbbbsbbBK...',
  '....KBBBBBBK....',
  '....KBBBBBBK....',
];
const LEGS = [
  ['.....KllllK.....', '.....KllllK.....', '.....KLLLLLK....', '.....KKKKKKK....'],
  ['....KLLKKllK....', '...KLLK..KllK...', '..KLLLK..KlllK..', '..KKKKK..KKKKK..'],
  ['....KllKKLLK....', '...KllK..KLLK...', '..KlllK..KLLLK..', '..KKKKK..KKKKK..'],
];

module.exports = {
  canvas: [48, 24],
  ramps: ['blue', 'skin', 'steel'],
  anchorBottom: 23,
  margin: 1,
  draw(b, K) {
    const st = K.P.ramps.steel, bl = K.P.ramps.blue, sk = K.P.ramps.skin;
    const map = {
      K: st[0], h: st[1], H: st[3], l: st[3], L: st[1],
      s: sk[1], S: sk[0],
      b: bl[1], B: bl[0], c: bl[2],
    };
    LEGS.forEach((legs, f) => K.grid(b, HEAD.concat(SIDE, legs), map, 16 * f, 5));
  }
};
