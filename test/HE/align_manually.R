require(geometry2D);

let HE = read.csv("C:\Users\Administrator\Desktop\MLKL-5\HE_cells.csv", row.names = 1, check.names = FALSE);
let CD31_PAS = read.csv("C:\Users\Administrator\Desktop\MLKL-5\CD31+PAS_cells.csv", row.names = 1, check.names = FALSE);
let t = geometry2D::transform(theta = 0,
                              translate = [0,0],
                              scale = [0.85,1]);

