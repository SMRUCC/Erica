require(GCModeller);
require(Erica);
require(Matrix);

imports "WGCNA" from "TRNtoolkit";
imports "geneExpression" from "phenotype_kit";
imports "monocle3" from "Erica";
imports "GRN" from "Erica";
imports "bnlearn" from "biosystem";

let wgcna = read_wgcna_edges("K:\\hsa_grn\\network-edges.csv", cor_thres = 0.001);
let TF = read.table("K:\\hsa_grn\\Homo_sapiens_TF.txt", row.names = NULL, header = TRUE);
let adj = readBin("K:\hsa_grn\hsa_adj.dat", what = "data_mat");

print(TF, max.print = 6);
print("TF id list:");
print(TF$Ensembl);

let net_model = WGCNA::prior_network(wgcna, TF$Ensembl);
let monocle3 = monocle3::new( cacheDir = "K:\\hsa_grn/GRN", num_HVgenes =2000);
let expr = load.expr0("K:\\hsa\\Homo_sapiens_expr_advanced_all_conditions.dat");

monocle3 = monocle3::cell_rank(expr, opts = monocle3);

let hvgenes = dim_names(adj);

expr <- dbn_sample(expr , hvgenes);
expr <- GRN::make_sample(monocle3, expr, hvgenes, method = "sliding", num_bins = 300,  top_gene_fraction = 0.8);

net_model = GRN::merge_prior(expr, net_model);
net_model = GRN::learn_grn(expr, net_model, maxIters = 1000);

bnlearn::save_model(net_model, dir = "K:\\hsa_grn/GRN/bnlearn/" );

let permutation = c( net_model |> knockouts(hvgenes[as.integer(runif(30) * length(hvgenes))]),
                     net_model |> overexpress(hvgenes[as.integer(runif(30) * length(hvgenes))]),
                     net_model |> knockdown(hvgenes[as.integer(runif(30) * length(hvgenes))]) );

bnlearn::make_exports(permutation, dir = "K:\\hsa_grn/GRN/permutation_test/",top_n  = 1000);