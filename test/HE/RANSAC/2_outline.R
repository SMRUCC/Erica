imports "geometry2D" from "graphics";
imports "machineVision" from "signalKit";

let dir = "C:\Users\Administrator\Desktop\MLKL-5\raster";

let HE = read.csv(file.path(dir, "HE.csv"), row.names = NULL, check.names = FALSE);
let CD31_PAS = read.csv( file.path(dir, "CD31+PAS.csv") , row.names =NULL, check.names = FALSE);

print(HE, max.print = 13);

HE = polygon2D(HE$x, HE$y) |> concaveHull(as.polygon = TRUE) |> as.data.frame();
CD31_PAS = polygon2D(CD31_PAS$x, CD31_PAS$y ) |> concaveHull(as.polygon = TRUE) |> as.data.frame();

write.csv(HE, file.path(dir, "contour/HE.csv"), row.names = FALSE);
write.csv(CD31_PAS, file.path(dir, "contour/CD31+PAS.csv"), row.names = FALSE);

bitmap(file = file.path(dir, "contour/HE.png")) {
    plot(as.numeric(HE$x),as.numeric(HE$y), fill = "white",point_size= 3);
}

bitmap(file = file.path(dir, "contour/CD31+PAS.png")) {
    plot(as.numeric(CD31_PAS$x),as.numeric(CD31_PAS$y), fill = "white",point_size= 3);
}