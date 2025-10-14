require(Erica);
require(filter);

imports "singleCell" from "Erica";
imports "machineVision" from "signalKit";

let snapshot = readImage(relative_work("100_244.bmp")) |> filter::adjust_contrast(-10);
let bin = machineVision::ostu(snapshot, flip = FALSE,
                            factor =0.8);

print(snapshot);
bitmap(snapshot, file = relative_work("cells_grayscale3.bmp"));

let cells = bin |> singleCell::HE_cells(is.binarized = TRUE,
                            flip = FALSE,
                            ostu.factor = 0.7,
                            offset = NULL,
                            noise = 0.25,
                            moran.knn = 32);

print(as.data.frame(cells));

write.csv(as.data.frame(cells), file = relative_work("cells3.csv"));

bitmap(bin, file = relative_work("cells_bin3.bmp"));
bitmap(file = relative_work("cells3.png"), size = [6400,2700]) {
    plot(cells, scatter = TRUE);
}