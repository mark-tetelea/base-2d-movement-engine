// The player facing the camera (facing 6, down): a sheet of three 16x24 frames side by
// side - idle, step A (left foot lifted), step B (right foot lifted). Unity slices it into
// three sprites. Blue tunic (the player's colour), dark hair, steel legs. Feet on the
// bottom row so the pivot is bottom-centre (ART.md rule 7).
const BODY = [
  '................',
  '....KKKKKKKK....',
  '...KhhhhhhhhK...',
  '...KhHHhhhhhK...',
  '...KhhhhhhhhK...',
  '...KhsssssshK...',
  '...KssKssKssK...',
  '...KsssssssSK...',
  '....KssssssK....',
  '...KbbbbbbbbK...',
  '..KcbbbbbbbbBK..',
  '..KcbbbbbbbbBK..',
  '..KbbbbbbbbbBK..',
  '..KsBbbbbbbBsK..',
  '...KBBBBBBBBK...',
];
const LEGS = [
  ['....KllKKllK....',
   '....KllKKllK....',
   '....KLLKKLLK....',
   '....KKKKKKKK....'],
  ['....KllKKllK....',
   '....KLLKKllK....',
   '....KKKKKLLK....',
   '........KKKK....'],
  ['....KllKKllK....',
   '....KllKKLLK....',
   '....KLLKKKKK....',
   '....KKKK........'],
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
    LEGS.forEach((legs, f) => K.grid(b, BODY.concat(legs), map, 16 * f, 5));
  }
};
