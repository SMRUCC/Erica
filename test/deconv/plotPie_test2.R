require(Erica);

setwd(@dir);

bitmap(file = "./stdeconv_scatterpie.png") {
    plot_pie(read.csv("./cell_layers.csv", row.names = 1, check.names = FALSE));
}

bitmap(file = "./spatial_class.png") {
    plot_class(read.csv("./spots_class.csv", row.names = NULL, check.names = FALSE));
}