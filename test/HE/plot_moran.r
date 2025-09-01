let data = read.csv("F:\cccccc\de1f4295b57d237928d08bd3c99c8fc2_files\cells.csv",  check.names = FALSE);
let density = data$density; # "moran-I";

print(density);
print(range(density));

bitmap(file = "F:\cccccc\de1f4295b57d237928d08bd3c99c8fc2_files\cells_moran.png") {
    plot(as.numeric(data$physical_x), as.numeric(data$physical_y), 
        point.size = 2, 
        heatmap = density, 
        grid.fill="white");
}
