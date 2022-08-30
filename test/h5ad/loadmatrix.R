require(Erica);
require(GCModeller);

imports "singleCell" from "Erica";
imports "geneExpression" from "phenotype_kit";

options(memory.load = "max");

mat ="K:\Downloads\E16.5_E1S1.MOSTA.h5ad"
|> HTS_matrix()
;

write.expr_matrix(mat, file = `${@dir}/rawExpr0.csv`);
write.expr_matrix(mat, file = `${@dir}/rawExpr0.dat`, binary = TRUE);