require(Erica);
require(HDS);

imports "singleCell" from "Erica";
imports "machineVision" from "signalKit";

let scan_pack = "C:\Users\Administrator\Desktop\MLKL-5\4 color (IHC1).hds";
let pack = read_stream(scan_pack);
let dzi = list.files(pack, pattern = "*.dzi", recursive =FALSE);
let dir_files = `${basename(dzi)}_files`;
let level = 18;

dzi = parse_dziImage(pack |> getText(dzi));

let images = dir.open(`/${dir_files}/${level}/`, fs = pack);
let rgb = dzi |> scan.dzi_cells(level =level, dir =images,
                                  ostu_factor  = 0.85,
                                noise  = 0.5,
                                flip = FALSE,
                                moran_knn = 256,
                                split.IHC1.channels = TRUE);
let r = as.data.frame(rgb$r);
let g = as.data.frame(rgb$g);
let b = as.data.frame(rgb$b);

write.csv(r, file = `${dirname(scan_pack)}/${basename(scan_pack)}_red.csv`);    
write.csv(g, file = `${dirname(scan_pack)}/${basename(scan_pack)}_green.csv`);    
write.csv(b, file = `${dirname(scan_pack)}/${basename(scan_pack)}_blue.csv`);              
