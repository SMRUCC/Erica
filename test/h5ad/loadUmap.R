require(Erica);

imports "singleCell" from "Erica";

umap = "F:\ST202208234793930\raw\10x_Visium_deal.h5ad"
|> read.h5ad()
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
		 colorSet     = "paper", 
		 grid.fill    = "transparent",
		 size         = [2600, 1600]
	);
}