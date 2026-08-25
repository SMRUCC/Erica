require(GCModeller);
require(Erica);

imports "WGCNA" from "TRNtoolkit";
imports "geneExpression" from "phenotype_kit";

let wgcna = read_wgcna_edges("K:\\hsa_grn\\network-edges.csv", cor_thres = 0.65);
let TF = read.table("K:\\hsa_grn\\Homo_sapiens_TF.txt", row.names = NULL, header = TRUE);

print(TF, max.print = 6);
print("TF id list:");
print(TF$Ensembl);

let net_model = WGCNA::bnnet(wgcna, TF$Ensembl);