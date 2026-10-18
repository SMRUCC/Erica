require(GCModeller);
require(Erica);
require(Matrix);

imports "WGCNA" from "TRNtoolkit";
imports "geneExpression" from "phenotype_kit";
imports "monocle3" from "Erica";
imports "GRN" from "Erica";
imports "bnlearn" from "biosystem";

let monocle3 = monocle3::new( cacheDir = "K:\\hsa_grn/GRN", num_HVgenes =2000);
let expr = load.expr0("K:\\hsa\\Homo_sapiens_expr_advanced_all_conditions.dat");

monocle3 <- monocle3::cell_rank(expr, opts = monocle3);

let m = dbn_sample(expr, hvgenes(monocle3) );
let sort_data = GRN::make_sample(monocle3, m, hvgenes(monocle3), method ="sliding",top_gene_fraction  = 1 );
