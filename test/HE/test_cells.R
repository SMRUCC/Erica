require(Erica);

imports "singleCell" from "Erica";
imports "machineVision" from "signalKit";

let snapshot = readImage(relative_work("38_55.jpeg"));
let bin = machineVision::ostu(snapshot, flip = FALSE,
                            factor = 1.125);

print(snapshot);
# print(bin);

let cells = bin |> singleCell::HE_cells(is.binarized = TRUE,
                            flip = FALSE,
                            ostu.factor = 0.7,
                            offset = NULL,
                            noise = 0.25,
                            moran.knn = 32);

print(as.data.frame(cells));

write.csv(as.data.frame(cells), file = relative_work("cells.csv"));

bitmap(bin, file = relative_work("cells_bin.bmp"));
bitmap(file = relative_work("cells.png"), size = [6400,2700]) {
    plot(cells, scatter = TRUE);
}