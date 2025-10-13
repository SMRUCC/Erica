require(Erica);
require(HDS);

imports "singleCell" from "Erica";
imports "machineVision" from "signalKit";

let scan_pack = "C:\Users\Administrator\Desktop\MLKL-5\4 color (IHC1).hds";
let pack = read_stream(scan_pack);
let dzi = list.files(pack, pattern = "*.dzi", recursive =FALSE);
let dir_files = `${basename(dzi)}_files`;

dzi = parse_dziImage(pack |> getText(dzi));

let images = dir.open(`/${dir_files}/15/`, fs = pack);

let cells = dzi |> scan.dzi_cells(level =15, dir =images,
                                  ostu_factor  = 0.85,
                                noise  = 0.5,
                                moran_knn = 256);

write.csv(as.data.frame(cells), file = `${dirname(scan_pack)}/${basename(scan_pack)}_cells.csv`);              
