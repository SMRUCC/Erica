require(Erica);
require(HDS);

imports "singleCell" from "Erica";
imports "machineVision" from "signalKit";

let scan_pack = "C:\Users\Administrator\Desktop\MLKL-5\CD31+PAS.hds";
let pack = read_stream(scan_pack);
let dzi = list.files(pack, pattern = "*.dzi", recursive =FALSE);
let dir_files = `${basename(dzi)}_files`;
let level = 15;

dzi = parse_dziImage(pack |> getText(dzi));

let images = dir.open(`/${dir_files}/${level}/`, fs = pack);

let cells = dzi |> scan.dzi_cells(level =level, dir =images,
                                  ostu_factor  = 0.85,
                                noise  = 0.5,
                                flip = FALSE,
                                moran_knn = 256);

write.csv(as.data.frame(cells), file = `${dirname(scan_pack)}/${basename(scan_pack)}_cells.csv`);              
