imports "charts" from "graphics";

#' plot spot class map
#' 
#' @param spots_class should be a collection of the spot class 
#'    annotation data or a dataframe object that contains the 
#'    spot annotation data.
#' 
const plot_class = function(spots_class, colors = "paper") {
    # [x,y,class,color]
    spots_class = as.data.frame(spots_class);

    plot(spots_class$x, y = spots_class$y, 
        point.size = 12, 
        class = spots_class$class,
        colorSet = colors
    );
}