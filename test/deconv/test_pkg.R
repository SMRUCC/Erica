require(GCModeller);
require(Erica);
require(JSON);

imports "geneExpression" from "phenotype_kit";

let expr_mat = "/Erica/src/SingleCell/demo/HR2MSI mouse urinary bladder S096_top3.csv"
|> load.expr()
;

let [single_cells, 
        deconv_spatial,
        cell_layers,
        gibbs_LDA] = deconv_spatial(expr_mat, n_layers = 4, top_genes = 1000, alpha = 2.0, 
                                beta = 0.5,
                                iteration = 150,
                                prefix = "class");

setwd(@dir);

geneExpression::write.expr_matrix(single_cells, file = "./single_cells.csv");
geneExpression::write.expr_matrix(deconv_spatial, file = "./deconv_spatial.csv");
geneExpression::write.expr_matrix(cell_layers, file = "./cell_layers.csv");

gibbs_LDA
|> JSON::json_encode()
|> writeLines(
    con = "./gibbs_LDA.json"
);
