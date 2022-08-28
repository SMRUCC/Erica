require(Erica);

imports "singleCell" from "Erica";

raw = "K:\\E16.5_E2S3.MOSTA.h5ad"
|> read.h5ad(loadExpr0 = FALSE)
;

spatial = raw 
|> spatialMap(useCellAnnotation = TRUE)
;

write.csv(spatial, file = `${@dir}/spatial.csv`, row.names = FALSE);

bitmap(file = `${@dir}/spatial_2.png`) {
	plot(spatial[, "x"], spatial[, "y"],
		 padding      = "padding:200px 400px 200px 250px;",
		 class        = spatial[, "class"],
		 title        = "Spatial 2D Scatter Map",
		 x.lab        = "X",
		 y.lab        = "Y",
		 legend.block = 13,
		 point.size   = 36,
		 colorSet     = "paper",  
		 grid.fill    = "transparent",
		 size         = [2800, 2600],
		 x.format     = "F0",
		 y.format     = "F0"
	);
}