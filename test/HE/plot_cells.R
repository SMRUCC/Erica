let cells_file = "C:\Users\Administrator\Desktop\MLKL-5\CD31+PAS_cells.csv";
let cells = read.csv(cells_file, row.names = 1, check.names = FALSE);
let y = as.numeric(cells$physical_y);

print(cells);

bitmap(file = file.path(dirname(cells_file), `${basename(cells_file)}.png`), size = [8000, 8000]) {
    plot(as.numeric(cells$physical_x), max(y) -y , fill="white");
}