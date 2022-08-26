require(Erica);
require(GCModeller);

imports "singleCell" from "Erica";
imports "st-imaging" from "Erica";
imports "geneExpression" from "phenotype_kit";

raw = "K:\ST\10x_Visium_deal.h5ad"
|> read.h5ad()
;

img = new_render(raw);

print([img]::geneIDs);

for(id in ["ENSMUSG00000103810", "ENSMUSG00000047459"]) {
# id = "ENSMUSG00000047459";

bitmap(file = `${@dir}/${id}.png`) {
    imaging(img, id);
}
}

