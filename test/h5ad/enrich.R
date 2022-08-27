require(GCModeller);
require(Erica);

imports "singleCell" from "Erica";
imports "background" from "gseakit";
imports "GSEA" from "gseakit";

raw = "K:\ST\10x_Visium_deal.h5ad"
|> read.h5ad()
;

geneList = expression_list(raw, q = 0.5);

str(geneList);
print(names(geneList));

keywords = read.background("E:\\Erica\\test\\background\\uniprot_keywords.xml");

for(key in names(geneList)) {
    genes =  geneList[[key]];
    enrich = keywords |> enrichment(genes, outputAll = FALSE);
    enrich = as.data.frame(enrich);

    # print(enrich, max.print = 13);

    write.csv(enrich, file = `${@dir}/keywords/${key}.csv`);
}

locations = read.background("E:\\Erica\\test\\background\\subcellular_location.xml");

for(key in names(geneList)) {
    genes =  geneList[[key]];
    enrich = locations |> enrichment(genes, outputAll = FALSE);
    enrich = as.data.frame(enrich);

    # print(enrich, max.print = 13);

    write.csv(enrich, file = `${@dir}/locations/${key}.csv`);
}