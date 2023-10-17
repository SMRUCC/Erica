imports "charts" from "graphics";

#' plot spot class map
#' 
#' @param spots_class should be a collection of the spot class 
#'    annotation data or a dataframe object that contains the 
#'    spot annotation data.
#' 
const plot_class = function(spots_class, colors = "paper", 
                            title = "Spatial Spot Class", 
                            padding = "padding: 150px 350px 200px 200px;") {
    # [x,y,class,color]
    spots_class = as.data.frame(spots_class);

    plot(as.numeric(spots_class$x), y = as.numeric(spots_class$y), 
        point.size = 12, 
        class = spots_class$class,
        colorSet = colors,
        reverse = TRUE,
        title = title,
        padding = padding,
        grid.fill = "white"
    );
}