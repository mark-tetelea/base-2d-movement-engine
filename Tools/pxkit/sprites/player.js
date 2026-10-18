// The player, one direction only (facing the camera) until the facing system decides
// 4 vs 8 directions. Blue tunic (the player's colour), dark hair, steel legs. Feet on
// the bottom row so the pivot is bottom-centre (ART.md rule 7).
module.exports = {
  canvas: [16, 24],
  ramps: ['blue', 'skin', 'steel'],
  anchorBottom: 23,
  margin: 1,
  offset: [0, 5],
  draw(b, K) {
    const st = K.P.ramps.steel, bl = K.P.ramps.blue, sk = K.P.ramps.skin;
    K.grid(b, [
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
      '....KllKKllK....',
      '....KllKKllK....',
      '....KLLKKLLK....',
      '....KKKKKKKK....',
    ], {
      K: st[0], h: st[1], H: st[3], l: st[3], L: st[1],
      s: sk[1], S: sk[0],
      b: bl[1], B: bl[0], c: bl[2],
    });
  }
};
