setwd(@dir);

let file = "C:\Users\Administrator\Desktop\MLKL-5\HE_cells.csv";
let cells = read.csv(file, row.names = 1, check.names = FALSE);

bitmap(file = file.path(dirname(file), `${basename(file)}_cellWeights.png`)) {
    plot(as.numeric(cells$physical_x), as.numeric(cells$physical_y), point_size = 5, heatmap = cells$weight, fill = "white");
}