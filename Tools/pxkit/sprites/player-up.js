// The player, facing away from the camera (facing 2): the back of the head: idle, step A, step B side by side (16x24 frames), like
// player-down.js. The left-side facings are this drawing flipped in Unity.
const HEAD = [
  '................', '....KKKKKKKK....', '...KhhhhhhhhK...', '...KhHHhhhhhK...', '...KhhhhhhhhK...',
  '...KhhhhhhhhK...', '...KhhhhhhhhK...', '...KhhhhhhhhK...', '....KhhhhhhK....',
];
const TORSO = [
  '...KbbbbbbbbK...',
  '..KcbbbbbbbbBK..',
  '..KcbbbbbbbbBK..',
  '..KbbbbbbbbbBK..',
  '..KsBbbbbbbBsK..',
  '...KBBBBBBBBK...',
];
const LEGS = [
  ['....KllKKllK....', '....KllKKllK....', '....KLLKKLLK....', '....KKKKKKKK....'],
  ['....KllKKllK....', '....KLLKKllK....', '....KKKKKLLK....', '........KKKK....'],
  ['....KllKKllK....', '....KllKKLLK....', '....KLLKKKKK....', '....KKKK........'],
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
    LEGS.forEach((legs, f) => K.grid(b, HEAD.concat(TORSO, legs), map, 16 * f, 5));
  }
};
