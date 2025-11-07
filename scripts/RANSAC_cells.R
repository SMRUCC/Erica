require(geometry2D);
require(Erica);

imports "singleCell" from "Erica";

let target = ?"--target" || stop("missing target slice for make the RANSAC alignment!");
let reference = ?"--subject" || stop("missing the reference slice for the target alignment.");
let grids = ?"--grid" ||  900;
let output_dir = ?"--output" || file.path(dirname(target), `${basename(target)}_RANSAC_cell`);

let target_slice = read.cells(target);
let subject_slice = read.cells(reference);
let target_raster = cell_rasterized(target_slice, grid = grids);
let subject_raster = cell_rasterized(subject_slice, grid = grids);

let t = singleCell::RANSAC_cell_alignment(target_raster, subject_raster, distance_threshold = 10);

print(t);

let unaligned = as.data.frame(target_slice);

target_slice = target_slice |> geo_transform(t);
target_slice = as.data.frame(target_slice);
target_slice[,"class"] = basename(target);
subject_slice = as.data.frame(subject_slice );
subject_slice [,"class"] = basename(reference);

let cells = rbind(target_slice, subject_slice);

unaligned[,"class"] = `${basename(target)}_unaligned`;
unaligned = rbind(unaligned, subject_slice);

bitmap(file = file.path(output_dir,"RANSAC_cell.png"), size = [6000,4800]) {
    plot(as.numeric(cells$physical_x), as.numeric(cells$physical_y), 
        point_size = 2, 
        class = cells$class, 
        colors = "paper",
        fill = "white");
}

bitmap(file = file.path(output_dir,"unaligned_cell.png"), size = [6000,4800]) {
    plot(as.numeric(unaligned$physical_x), as.numeric(unaligned$physical_y), 
        point_size = 2, 
        class = unaligned$class, 
        colors = "paper",
        fill = "white");
}