require(Erica);

imports "singleCell" from "Erica";
imports "machineVision" from "signalKit";

let snapshot = readImage(relative_work("capture2.png"));
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

write.csv(as.data.frame(cells), file = relative_work("cells2.csv"));

bitmap(bin, file = relative_work("cells_bin2.bmp"));
bitmap(file = relative_work("cells2.png"), size = [6400,2700]) {
    plot(cells, scatter = TRUE);
}