imports "geometry2D" from "graphics";
imports "machineVision" from "signalKit";

let dir = "C:\Users\Administrator\Desktop\MLKL-5\raster";

let HE = read.csv(file.path(dir, "../HE_cells.csv"), row.names = NULL, check.names = FALSE);
let CD31_PAS = read.csv(file.path(dir,"../CD31+PAS_cells.csv"), row.names = NULL, check.names = FALSE);

HE = polygon2D(as.numeric(HE$physical_x), as.numeric(HE$physical_y));
CD31_PAS = polygon2D(as.numeric(CD31_PAS$physical_x), as.numeric(CD31_PAS$physical_y));

let t = RANSAC(CD31_PAS, HE, iterations= 100000, threshold  = 0.05);

str(t);

let aligned = as.data.frame( geo_transform(CD31_PAS, t));

HE = as.data.frame(HE);
HE[,"class"] = "HE";
CD31_PAS = as.data.frame(CD31_PAS );
CD31_PAS[,"class"] = "CD31+PAS"; 

aligned[,"class"] = "aligned(CD31+PAS)";
aligned = rbind(HE, aligned);

let un_aligned = rbind(HE, CD31_PAS);

write.csv(aligned, file = file.path(dir, "RANSAC", "aligned.csv"), row.names = FALSE);

bitmap(file = file.path(dir, "RANSAC", "unaligned.png")) {
    plot(as.numeric(un_aligned$x),as.numeric(un_aligned$y), class = un_aligned$class, fill = "white",point_size= 2, colors = "paper");
}

bitmap(file = file.path(dir, "RANSAC", "aligned.png")) {
    plot(as.numeric(aligned$x),as.numeric(aligned$y), class = aligned$class, fill = "white",point_size= 2, colors = "paper");
}