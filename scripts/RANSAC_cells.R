require(geometry2D);
require(Erica);

imports "singleCell" from "Erica";

let target = ?"--target" || stop("");
let reference = ?"--subject" || stop("");
let grids = ?"--grid" ||  900;
let output_dir = ?"--output" || file.path(dirname(target), `${basename(target)}_RANSAC_cell`);

let target_slice = read.cells(target);
let subject_slice = read.cells(reference);
let target_raster = cell_rasterized(target_slice, grid = grids);
let subject_raster = cell_rasterized(subject_slice, grid = grids);

let t = singleCell::RANSAC_cell_alignment(target_raster, subject_raster, distance_threshold = 10);

print(t);

target_slice = target_slice |> geo_transform(t);
target_slice = as.data.frame(target_slice);
target_slice[,"class"] = basename(target);
subject_slice = as.data.frame(subject_slice );
subject_slice [,"class"] = basename(reference);

let cells = rbind(target_slice, subject_slice);

bitmap(file = file.path(output_dir,"RANSAC_cell.png"), size = [6000,4800]) {
    plot(as.numeric(cells$physical_x), as.numeric(cells$physical_y), 
        point_size = 2, 
        class = cells$class, 
        colors = "paper",
        fill = "white");
}