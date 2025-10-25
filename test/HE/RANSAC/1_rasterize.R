imports "geometry2D" from "graphics";
imports "machineVision" from "signalKit";

let dir = "C:\Users\Administrator\Desktop\MLKL-5";

let HE = read.csv(file.path(dir, "HE_cells.csv"), row.names = 1, check.names = FALSE);
let CD31_PAS = read.csv( file.path(dir, "CD31+PAS_cells.csv") , row.names =1, check.names = FALSE);

print(HE, max.print = 13);

HE = polygon2D(HE$physical_x, HE$physical_y) |> rasterize() |> polygon2D(raster_n =3) |> as.data.frame();
CD31_PAS = polygon2D(CD31_PAS$physical_x, CD31_PAS$physical_y) |> rasterize() |> polygon2D(raster_n =3) |> as.data.frame();

write.csv(HE, file.path(dir, "raster/HE.csv"), row.names = FALSE);
write.csv(CD31_PAS, file.path(dir, "raster/CD31+PAS.csv"), row.names = FALSE);

bitmap(file = file.path(dir, "raster/HE.png")) {
    plot(as.numeric(HE$x),as.numeric(HE$y), fill = "white",point_size= 3);
}

bitmap(file = file.path(dir, "raster/CD31+PAS.png")) {
    plot(as.numeric(CD31_PAS$x),as.numeric(CD31_PAS$y), fill = "white",point_size= 3);
}