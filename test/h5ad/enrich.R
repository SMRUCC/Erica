require(GCModeller);
require(Erica);

imports "singleCell" from "Erica";
imports "background" from "gseakit";
imports "GSEA" from "gseakit";
imports "UniProt" from "gseakit";
imports "visualPlot" from "visualkit";

raw = "K:\ST\10x_Visium_deal.h5ad"
|> read.h5ad()
;

keywords_list = UniProtKeywords();

geneList = expression_list(raw, q = 0.5);

str(geneList);
print(names(geneList));

keywords = read.background("E:\\Erica\\test\\background\\uniprot_keywords.xml");

for(key in names(geneList)) {
    genes =  geneList[[key]];
    enrich = keywords |> enrichment(genes, outputAll = FALSE);
    profiles = keyword_profiles(enrich,keywords_list, top = 3);
    enrich = as.data.frame(enrich);

    # print(enrich, max.print = 13);
    bitmap(file = `${@dir}/keywords/${key}.png`) {
        category_profiles.plot(
            profiles ,
             colors = "paper",
             title = "UniProt Keywords",
             axis.title = "-log10(p-value)",
             size = [1200,1600],
             dpi = 100
             );
    }

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