let data = read.csv("F:\cccccc\de1f4295b57d237928d08bd3c99c8fc2_files\cells_umap.csv" , row.names = 1, check.names = FALSE);
let cells = read.csv("F:\cccccc\de1f4295b57d237928d08bd3c99c8fc2_files\cells.csv",  check.names = FALSE);

cells[,"xy"] = `${cells$physical_x},${cells$physical_y}`;
data[,"xy"] = rownames(data);

data = data |> left_join(cells, by="xy");

bitmap(file = "F:\cccccc\de1f4295b57d237928d08bd3c99c8fc2_files\cells_umap.png") {
    plot(as.numeric(data$dimension_1), as.numeric(data$dimension_2), 
        point.size = 2, 
        heatmap = data[,"density"], 
        grid.fill="white");
}
