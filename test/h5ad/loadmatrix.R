require(Erica);

imports "singleCell" from "Erica";

raw = "K:\ST\10x_Visium_deal.h5ad"
|> read.h5ad()
;

mat = HTS_matrix(raw);

