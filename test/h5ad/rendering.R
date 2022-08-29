require(GCModeller);
require(Erica);

imports "singleCell" from "Erica";
imports "st-imaging" from "Erica";
imports "geneExpression" from "phenotype_kit";

img = "K:\\ST\\10x_Visium_deal.h5ad"
|> read.h5ad()
|> new_render(raw)
;

print([img]::geneIDs);



for(id in ["ENSMUSG00000103810", "ENSMUSG00000047459",
"ENSMUSG00000029581","ENSMUSG00000066640",
"ENSMUSG00000045467"]) {
# id = "ENSMUSG00000047459";

cat("\n\n");

bitmap(file = `${@dir}/${id}.png`) {
    imaging(img, id);
}
}

