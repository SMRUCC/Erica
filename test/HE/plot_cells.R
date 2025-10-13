require(ggplot);

let cells_file = "C:\Users\Administrator\Desktop\MLKL-5\4 color (IHC1)_cells.csv";
let cells = read.csv(cells_file, row.names = 1, check.names = FALSE);

# cells = cells[as.numeric(cells$mean_distance) > 1, ];

let y = as.numeric(cells$physical_y);

cells[,"physical_y"] = max(y) -y;

print(cells);

bitmap(file = file.path(dirname(cells_file), `${basename(cells_file)}.png`), size = [8000, 8000]) {
    ggplot(cells, aes (x="physical_x",y="physical_y", size = "size"))
    + geom_point(size = [3,15])
    ;
}