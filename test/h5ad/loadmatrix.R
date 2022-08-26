require(Erica);
require(GCModeller);

imports "singleCell" from "Erica";
imports "geneExpression" from "phenotype_kit";

raw = "K:\ST\10x_Visium_deal.h5ad"
|> read.h5ad()
;

mat = HTS_matrix(raw);

write.expr_matrix(mat, file = `${@dir}/rawExpr0.csv`);
write.expr_matrix(mat, file = `${@dir}/rawExpr0.dat`, binary = TRUE);