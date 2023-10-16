require(Erica);

setwd(@dir);

let layers = read.csv("./cell_layers.csv", row.names = 1, check.names = FALSE);
let spots = __spot_class(layers);

print(as.data.frame(spots));