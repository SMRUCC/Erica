require(Erica);

imports "singleCell" from "Erica";
imports "machineVision" from "signalKit";

let dzi = read.dziImage("F:\cccccc\de1f4295b57d237928d08bd3c99c8fc2_files\de1f4295b57d237928d08bd3c99c8fc2.xml");
let cells = dzi |> scan.dzi_cells(level =15, dir ="F:\cccccc\de1f4295b57d237928d08bd3c99c8fc2_files\15",
                                  ostu_factor  = 1.125,
                                noise  = 0.5,
                                moran_knn = 64);

write.csv(as.data.frame(cells), file = "F:\cccccc\de1f4295b57d237928d08bd3c99c8fc2_files\cells.csv");              
