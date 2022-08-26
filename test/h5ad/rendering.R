require(Erica);
require(GCModeller);

imports "singleCell" from "Erica";
imports "st-imaging" from "Erica";
imports "geneExpression" from "phenotype_kit";

raw = "K:\ST\10x_Visium_deal.h5ad"
|> read.h5ad()
;

imaging = new_render(raw);

print([imaging]::geneIDs);

