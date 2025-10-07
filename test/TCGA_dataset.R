require(Erica);

imports "singleCell" from "Erica";
imports "geneExpression" from "phenotype_kit";

let data = singleCell::read.TCGA_mtx("/marker/test/aa55ac5e-7a48-45fd-9fb5-7e013805b247/");

data = data 
|> geneExpression::load.expr() 
|> geneExpression::tr()
|> geneExpression::impute_missing(by_features=TRUE)
|> geneExpression::totalSumNorm()
|> geneExpression::relative(median = TRUE)
|> geneExpression::sort_mad(top = 5000)
|> geneExpression::tr()
;

write.expr_matrix(data, file = "/marker/test/aa55ac5e-7a48-45fd-9fb5-7e013805b247.csv");
