require(Erica);

imports "singleCell" from "Erica";

raw = "K:\Downloads\E16.5_E1S1.MOSTA.h5ad"
|> read.h5ad(loadExpr0 = FALSE)
;

spatial = raw 
|> spatialMap(useCellAnnotation = TRUE)
;

write.csv(spatial, file = `${@dir}/spatial.csv`, row.names = FALSE);

bitmap(file = `${@dir}/spatial_3.png`) {
	plot(spatial[, "x"], spatial[, "y"],
		 padding      = "padding:200px 500px 200px 250px;",
		 class        = spatial[, "class"],
		 title        = "Mouse Embryo Spatial 2D Map",
		 x.lab        = "X",
		 y.lab        = "Y",
		 legend.block = 100,
		 point.size   = 6,
		 colorSet     = "paper",  
		 grid.fill    = "transparent",
		 size         = [2800, 3200],
		 x.format     = "F0",
		 y.format     = "F0",
		 reverse      = TRUE
	);
}