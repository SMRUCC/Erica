
#' plot single cells RNA velocity scatter
#'
#' @param x the x dimension value of single cell embedding result
#' @param y the y dimension value of single cell embedding result
#' @param vx the dimension x of the velocity embedding result
#' @param vy the dimension y of the velocity embedding result
#' @param class the cluster label of each corresponding single cells
#' 
const plot_velocity = function(x,y,vx,vy,class) {
    ggplot(data.frame(x,y,vx,vy,class), aes(x = "x", y = "y", color = "class")) + 
        geom_point() +  
        geom_segment(aes(xend = x + vx, yend = y + vy), color = "black") +  
        theme_minimal()
        ;
}