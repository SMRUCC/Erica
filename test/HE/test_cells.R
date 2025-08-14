require(Erica);

imports "singleCell" from "Erica";

let snapshot = readImage(relative_work("capture.png"));

print(snapshot);

let cells = snapshot |> singleCell::HE_cells(is.binarized = FALSE,
                            flip = FALSE,
                            ostu.factor = 0.7,
                            offset = NULL,
                            noise = 0.25,
                            moran.knn = 32);

print(as.data.frame(cells));

write.csv(as.data.frame(cells), file = relative_work("cells.csv"));