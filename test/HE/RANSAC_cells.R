require(geometry2D);
require(Erica);

imports "singleCell" from "Erica";

let MSI = read.cells("C:\Users\Administrator\Desktop\MLKL-5\MSI_cells.csv");
let HE = read.cells("C:\Users\Administrator\Desktop\MLKL-5\HE_cells.csv");
let HE_raster = cell_rasterized(HE, grid = 1000);
let t = singleCell::RANSAC_cell_alignment(HE_raster, MSI, distance_threshold = 10);

print(t);

HE = HE |> geo_transform(t);
HE = as.data.frame(HE);
HE[,"class"] = "HE";

MSI = as.data.frame(MSI);
MSI[,"class"] = "MSImaging";

let cells = rbind(HE, MSI);

bitmap(file = "C:\Users\Administrator\Desktop\MLKL-5_MSI+HE_cellWeights.png", size = [3000,3600]) {
    plot(as.numeric(cells$physical_x), as.numeric(cells$physical_y), 
        point_size = 4, 
        class = cells$class, 
        fill = "white");
}