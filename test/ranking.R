require(GCModeller);
require(Erica);
require(Matrix);

imports "WGCNA" from "TRNtoolkit";
imports "geneExpression" from "phenotype_kit";
imports "monocle3" from "Erica";
imports "GRN" from "Erica";
imports "bnlearn" from "biosystem";

let output = "K:\hsa_grn\20261020-1\hsa_rank";
let monocle3 = monocle3::new( cacheDir = output, num_HVgenes =60273);
let expr = load.expr("K:\hsa_grn\20261020-1\hsa_sub_expression.csv");

monocle3 <- monocle3::cell_rank(expr, opts = monocle3);

let m = dbn_sample(expr, hvgenes(monocle3) );
let sort_data = GRN::make_sample(monocle3, m, hvgenes(monocle3), method ="sliding",top_gene_fraction  = 1 );

writeBin(sort_data, con = file.path(output, "cell_rank.zip"));