require(geometry2D);
require(Erica);

imports "singleCell" from "Erica";

let MSI = read.cells("C:\Users\Administrator\Desktop\MLKL-5\MSI_cells.csv");
let HE = read.cells("C:\Users\Administrator\Desktop\MLKL-5\HE-Clean_cells.csv");
let HE_raster = cell_rasterized(HE, grid = 900);
let t = singleCell::RANSAC_cell_alignment(HE_raster, MSI, distance_threshold = 10);
# let t = new affine2d_transform(a = 1.0008326741016038,
#   b = -0.00015948061166604077,
#   c = -153.68834016556684,
#   d = 0.0004911713864022005,
#   e = 0.9992597512341472,
#   f = -18.15537030347332);

print(t);

HE = HE_raster |> geo_transform(t);
HE = as.data.frame(HE);
HE[,"class"] = "HE";

MSI = as.data.frame(MSI);
MSI[,"class"] = "MSImaging";

let cells = rbind(HE, MSI);

bitmap(file = "C:\Users\Administrator\Desktop\MLKL-5_MSI+HE_cellWeights.png", size = [6000,4800]) {
    plot(as.numeric(cells$physical_x), as.numeric(cells$physical_y), 
        point_size = 2, 
        class = cells$class, 
        colors = "paper",
        fill = "white");
}