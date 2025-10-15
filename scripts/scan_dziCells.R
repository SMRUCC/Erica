require(Erica);
require(HDS);

imports "singleCell" from "Erica";
imports "machineVision" from "signalKit";

let scan_pack = ?"--file" || stop("A mzkit virtual slide data pack file must be provided!");
let cells_table = ?"--outfile" || file.path(dirname(scan_pack), `${basename(scan_pack)}_cells.csv`);
let level = Erica::max_dzi_level(scan_pack);
let pack = read_stream(scan_pack);
let dzi = list.files(pack, pattern = "*.dzi", recursive =FALSE);
let dir_files = `${basename(dzi)}_files`;
let dzi_meta = parse_dziImage(pack |> getText(dzi));
let images = dir.open(`/${dir_files}/${level}/`, fs = pack);
let cells = dzi_meta |> scan.dzi_cells(
    level =level, dir =images,
    ostu_factor = 0.85,
    noise  = 0.2,
    flip = FALSE,
    moran_knn = 256
);

write.csv(as.data.frame(cells), file = cells_table);              
