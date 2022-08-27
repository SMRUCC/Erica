require(GCModeller);
require(Erica);

imports "singleCell" from "Erica";
imports "Bgee" from "Erica";
imports "background" from "gseakit";
imports "GSEA" from "gseakit";

raw = "K:\ST\10x_Visium_deal.h5ad"
|> read.h5ad()
;

geneList = expression_list(raw, q = 0.5);
bgee = read.backgroundPack("E:\Erica\test\bgee\mmu.bgee");

stages = development_stage(bgee);

print(stages);

write.csv(stages, file = `${@dir}/bgee_development_stage.csv`);

str(geneList);
print(names(geneList));

for(key in names(geneList)) {
    genes =  geneList[[key]];
    enrich = bgee |> bgee_calls(genes, development_stage = "MmusDv:0000153");
    enrich = as.data.frame(enrich);
    enrich[,"geneIDs"] = NULL;

    print(enrich, max.print = 13);

    write.csv(enrich, file = `${@dir}/bgee/${key}.csv`);
}