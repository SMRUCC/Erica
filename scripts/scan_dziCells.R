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

cells = as.data.frame(cells);
# cells = cells[as.numeric(cells$mean_distance) > 0,];
cells = cells[as.numeric(cells$size) > 6, ];

write.csv(cells, file = cells_table);              

bitmap(file = file.path(dirname(scan_pack), `${basename(cells_table)}.png`)) {
    plot(as.numeric(cells$physical_x), as.numeric(cells$physical_y), point_size = 5, fill = "white");
}