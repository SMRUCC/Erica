require(geometry2D);
require(Erica);

imports "singleCell" from "Erica";

let MSI = read.cells("C:\Users\Administrator\Desktop\MLKL-5\MSI_cells.csv");
let HE = read.cells("C:\Users\Administrator\Desktop\MLKL-5\HE_cells.csv");
let t = singleCell::RANSAC_cell_alignment(HE, MSI, distance_threshold = 100);

print(t);
