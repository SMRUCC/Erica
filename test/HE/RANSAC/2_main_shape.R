imports "geometry2D" from "graphics";
imports "machineVision" from "signalKit";
imports "clustering" from "MLkit";

let dir = "C:\Users\Administrator\Desktop\MLKL-5\raster";

let HE = read.csv(file.path(dir, "HE.csv"), row.names = NULL, check.names = FALSE);
let CD31_PAS = read.csv( file.path(dir, "CD31+PAS.csv") , row.names =NULL, check.names = FALSE);

print(HE, max.print = 13);

let get_mainshape = function(multishapes, name) {
    multishapes = knn_cluster(multishapes, knn = 32, p = 0.5);
    multishapes = as.data.frame(multishapes);

    print(multishapes, max.print = 16);

    # show object detection result
    bitmap(file = file.path(dir, `shapes_${name}.png`)) {
        plot(as.numeric(multishapes[, "x"]), as.numeric(multishapes[, "y"]), 
            class     = multishapes[, "Cluster"], 
            grid.fill = "white",
            padding   = "padding: 125px 300px 200px 200px;",
            colorSet  = "paper"
        );
    }

    write.csv(multishapes, file = file.path(dir,`shapes_${name}.csv` ));
}

get_mainshape(HE,"HE");
get_mainshape(CD31_PAS,"CD31+PAS");
