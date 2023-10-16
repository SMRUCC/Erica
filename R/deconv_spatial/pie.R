require(ggplot);

#' Plot the scatter pie data
#' 
#' Display the cell distribution
#' 
const plot_pie = function(cell_layers) {
    let data = as.data.frame(cell_layers);
    let spatial = rownames(data);
    let xy = strsplit(spatial, ",");
    let class_labels = colnames(data);

    names(xy) = spatial;
    xy = lapply(xy, a -> as.numeric(a));

    print("get spatial [x,y] information:");
    str(xy);

    data[, "x"] = sapply(xy, a -> a[1]);
    data[, "y"] = sapply(xy, a -> a[2]);

    print(data, max.print = 13);

    ggplot(data, aes(x = "x", y = "y"), 
        padding = "padding: 250px 350px 200px 250px;")
    + geom_scatterpie(data = class_labels)
    ;
}