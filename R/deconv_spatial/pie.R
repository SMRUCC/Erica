require(ggplot);

#' Plot the scatter pie data
#' 
#' @param cell_layers the cell class probablity matrix data
#' @param x the x axis for draw a pie, could be x scan location of the spatial data or x dimension of the single cells umap embedding result.
#' @param y the y axis for draw a pie, could be y scan location of the spatial data or y dimension of the single cells umap embedding result.
#' 
#' @details Display the cell/spot class distribution
#' 
const plot_pie = function(cell_layers, x = NULL, y = NULL) {
    let data = as.data.frame(cell_layers);
    let class_labels = colnames(data);

    if (isTRUE((length(x) == 0) || (length(y) == 0))) {
        # needs parsed from the spatial labels
        let spatial = rownames(data);
        let xy = strsplit(spatial, ",");
        
        names(xy) = spatial;
        xy = lapply(xy, a -> as.numeric(a));

        print("get spatial [x,y] information:");
        str(xy);

        data[, "x"] = xy@{1};
        data[, "y"] = xy@{2};
    } else {
        data[, "x"] = as.numeric(x);
        data[, "y"] = as.numeric(y);
    }    

    print("view of the scatter pie input data that tagged with the x,y axis data:");
    print(data, max.print = 13);

    ggplot(data, aes(x = "x", y = "y"), 
        padding = "padding: 250px 350px 200px 250px;")
    + geom_scatterpie(data = class_labels)
    ;
}