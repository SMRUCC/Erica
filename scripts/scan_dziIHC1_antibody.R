require(Erica);
require(HDS);

imports "singleCell" from "Erica";
imports "machineVision" from "signalKit";

let scan_pack = ?"--file" || stop("A mzkit virtual slide data pack file must be provided!");
let cells_table = ?"--outfile" || file.path(dirname(scan_pack), `${basename(scan_pack)}_antibody_cells.csv`);
let anitybody = list(
    CD11b = [0, 1,     0] * 255, 
    CD11c = [1, 0.647, 0] * 255, 
    CD8   = [1, 0,     0] * 255, 
    PanCK = [1, 0,     1] * 255, 
    Dapi  = [0, 0,     1] * 255
);
let level = Erica::max_dzi_level(scan_pack);
let pack = read_stream(scan_pack);
let dzi = list.files(pack, pattern = "*.dzi", recursive =FALSE);
let dir_files = `${basename(dzi)}_files`;
let dzimeta = parse_dziImage(pack |> getText(dzi));
let images = dir.open(`/${dir_files}/${level}/`, fs = pack);
let rgb = dzimeta |> scan.dzi_cells(level =level, dir =images,
                                  ostu_factor  = 0.85,
                                noise  = 0.2,
                                flip = FALSE,
                                moran_knn = 256,
                                IHC_antibody = anitybody);
let result = as.data.frame(rgb);

write.csv(result, file = file.path(dirname(scan_pack), `${basename(scan_pack)}_antibody_cells.csv`));
