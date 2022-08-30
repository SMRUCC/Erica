require(Erica);
require(GCModeller);

imports "singleCell" from "Erica";
imports "geneExpression" from "phenotype_kit";

raw ="K:\Downloads\E16.5_E1S1.MOSTA.h5ad"
|> read.h5ad()
;

mat = HTS_matrix(raw);

write.expr_matrix(mat, file = `${@dir}/rawExpr0.csv`);
write.expr_matrix(mat, file = `${@dir}/rawExpr0.dat`, binary = TRUE);