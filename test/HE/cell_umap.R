require(umap);

let cells = read.csv("F:\cccccc\de1f4295b57d237928d08bd3c99c8fc2_files\cells.csv", row.names = 1, check.names = FALSE);

# x	y	physical_x	physical_y
rownames(cells) = `${cells$physical_x},${cells$physical_y}`;

cells[,"x"] = NULL;
cells[,"y"] = NULL;
cells[,"physical_x"] = NULL;
cells[,"physical_y"] = NULL;
cells[,"p-value"] = -log10(cells[,"p-value"]);

for(let col in colnames(cells)) {
    cells[, col] = z( as.numeric( cells[,col]));
}

let manifold = umap(cells, dimension= 2, numberOfNeighbors = 15);

manifold = as.data.frame(manifold$umap, labels = manifold$labels);

write.csv(manifold, file ="F:\cccccc\de1f4295b57d237928d08bd3c99c8fc2_files\cells_umap.csv" );
