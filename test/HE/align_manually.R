require(geometry2D);
require(Erica);

imports "singleCell" from "Erica";

let HE = read.csv("C:\Users\Administrator\Desktop\MLKL-5\HE_cells.csv", row.names = 1, check.names = FALSE);
let CD31_PAS = read.cells("C:\Users\Administrator\Desktop\MLKL-5\CD31+PAS_cells.csv");
let t = geometry2D::transform(theta = 0,
                              translate = [1260,-100],
                              scale = [0.95,0.98]);

CD31_PAS = CD31_PAS |> geo_transform(t) |> as.data.frame();

let cells_xy = data.frame(
    x = append(HE$physical_x, CD31_PAS$physical_x), 
    y= append(HE$physical_y, CD31_PAS$physical_y), 
    class = append(
        rep("HE", times = nrow(HE)), 
        rep("CD31+PAS", times = nrow(CD31_PAS)))
);

bitmap(file = "C:\Users\Administrator\Desktop\MLKL-5\HE_overlaps_CD31+PAS.png", size = [2400 3000]) {
    plot(as.numeric(cells_xy$x), 
        as.numeric(cells_xy$y), 
        class = cells_xy$class,
        point_size= 3,
        fill = "white"
    );
}