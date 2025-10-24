imports "geometry2D" from "graphics";
imports "machineVision" from "signalKit";

let dir = "C:\Users\Administrator\Desktop\MLKL-5";

let HE = read.csv(file.path(dir, "HE_cells.csv"), row.names = 1, check.names = FALSE);
let CD31_PAS = read.csv( file.path(dir, "CD31+PAS_cells.csv") , row.names =1, check.names = FALSE);

print(HE, max.print = 13);

HE = polygon2D(HE$physical_x, HE$physical_y) |> rasterize();
CD31_PAS = polygon2D(CD31_PAS$physical_x, CD31_PAS$physical_y) |> rasterize();

let HE_outline = polygon2D(HE,raster_n =3) |> concaveHull(as.polygon = TRUE);
let CD31_PAS_outline = polygon2D(CD31_PAS,raster_n =3) |> concaveHull(as.polygon = TRUE);

let HE_polygon =polygon2D(HE,raster_n =3);
let CD31_PAS_polygon = polygon2D(,raster_n =3);

HE = as.data.frame(HE_polygon);
HE[,"class"] = "HE";
CD31_PAS = as.data.frame(CD31_PAS_polygon);
CD31_PAS[,"class"] = "CD31+PAS";

print(HE_outline);
print(CD31_PAS_outline);

let t = RANSAC(CD31_PAS_outline, HE_outline);

str(t);

let aligned = as.data.frame( geo_transform(CD31_PAS_polygon, t));

aligned[,"class"] = "aligned(CD31+PAS)";
aligned = rbind(HE, aligned);

let un_aligned = rbind(HE, CD31_PAS);

bitmap(file = file.path(dir, "unaligned.png")) {
    plot(as.numeric(un_aligned$x),as.numeric(un_aligned$y), class = un_aligned$class, fill = "white");
}

bitmap(file = file.path(dir, "aligned.png")) {
    plot(as.numeric(aligned$x),as.numeric(aligned$y), class = aligned$class, fill = "white");
}