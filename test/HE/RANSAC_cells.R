require(geometry2D);
require(Erica);

imports "singleCell" from "Erica";

let MSI = read.cells("C:\Users\Administrator\Desktop\MLKL-5\MSI_cells.csv");
let HE = read.cells("C:\Users\Administrator\Desktop\MLKL-5\HE_cells.csv");
let t = singleCell::RANSAC_cell_alignment(HE, MSI, distance_threshold = 10);

print(t);

HE = HE |> geo_transform(t);
HE = as.data.frame(HE);
HE[,"class"] = "HE";

MSI = as.data.frame(MSI);
MSI[,"class"] = "MSImaging";

let cells = rbind(HE, MSI);

bitmap(file = "C:\Users\Administrator\Desktop\MLKL-5_MSI+HE_cellWeights.png") {
    plot(as.numeric(cells$physical_x), as.numeric(cells$physical_y), 
        point_size = 5, 
        class = cells$class, 
        fill = "white");
}