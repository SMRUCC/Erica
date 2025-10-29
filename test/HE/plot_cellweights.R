setwd(@dir);

let file = "C:\Users\Administrator\Desktop\MLKL-5\HE_cells.csv";
let cells = read.csv(file, row.names = 1, check.names = FALSE);

# cells = cells[as.numeric(cells$weight) > 0.7,];

bitmap(file = file.path(dirname(file), `${basename(file)}_cellWeights.png`)) {
    plot(as.numeric(cells$physical_x), as.numeric(cells$physical_y), point_size = 5, heatmap = cells$weight, fill = "white");
}