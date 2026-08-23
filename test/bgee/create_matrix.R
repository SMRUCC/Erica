require(GCModeller);
require(Erica);

imports "geneExpression" from "phenotype_kit";
imports "Bgee" from "Erica";

let bgee = Bgee::parseTsv(file ="C:\Users\Administrator\Downloads\Homo_sapiens_expr_advanced_all_conditions.tsv",
                             advance = TRUE,
                             quality = "*",
                             pip_stream =TRUE)
                             ;
let expr = Bgee::make_matrix(bgee);

write.expr_matrix(expr, file = "C:\Users\Administrator\Downloads\Homo_sapiens_expr_advanced_all_conditions.csv");
