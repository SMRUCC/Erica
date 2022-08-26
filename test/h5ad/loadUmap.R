require(Erica);

imports "singleCell" from "Erica";

raw = "F:\ST202208234793930\raw\10x_Visium_deal.h5ad"
|> read.h5ad()
;

umap = raw
|> umap_annotation()
;

write.csv(umap, file = `${@dir}/umap.csv`, row.names = FALSE);

bitmap(file = `${@dir}/umap.png`) {
	plot(umap[, "x"], umap[, "y"],
		 padding      = "padding:200px 400px 200px 250px;",
		 class        = umap[, "class"],
		 title        = "UMAP 2D Scatter",
		 x.lab        = "dimension 1",
		 y.lab        = "dimension 2",
		 legend.block = 13,
		 colorSet     = umap[, "color"], 
		 grid.fill    = "transparent",
		 size         = [2600, 1600]
	);
}

spatial = raw 
|> spatialMap()
;

write.csv(spatial, file = `${@dir}/spatial.csv`, row.names = FALSE);

bitmap(file = `${@dir}/spatial.png`) {
	plot(spatial[, "x"], spatial[, "y"],
		 padding      = "padding:200px 400px 200px 250px;",
		 class        = spatial[, "class"],
		 title        = "Spatial 2D Scatter Map",
		 x.lab        = "dimension 1",
		 y.lab        = "dimension 2",
		 legend.block = 13,
		 point.size   = 30,
		 colorSet     = spatial[, "color"],  
		 grid.fill    = "transparent",
		 size         = [2600, 1600]
	);
}