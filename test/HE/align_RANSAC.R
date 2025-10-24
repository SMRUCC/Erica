imports "geometry2D" from "graphics";
imports "machineVision" from "signalKit";

let HE = read.csv("C:\Users\Administrator\Desktop\MLKL-5\HE_cells.csv", row.names = 1, check.names = FALSE);
let CD31_PAS = read.csv("C:\Users\Administrator\Desktop\MLKL-5\CD31+PAS_cells.csv", row.names =1, check.names = FALSE);

print(HE, max.print = 13);

HE = polygon2D(HE$physical_x, HE$physical_y) |> rasterize() |> polygon2D(raster_n =3) |> concaveHull(as.polygon = TRUE);
CD31_PAS = polygon2D(CD31_PAS$physical_x, CD31_PAS$physical_y) |> rasterize() |> polygon2D(raster_n =3) |> concaveHull(as.polygon = TRUE);

print(HE);
print(CD31_PAS);

let t = RANSAC(CD31_PAS, HE);

str(t);