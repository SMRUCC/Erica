require(GCModeller);
require(Erica);

imports "background" from "gseakit";

setwd(@dir);

bgee = Bgee::parseTsv("K:\\ST\\Mus_musculus_expr_simple_all_conditions.tsv");
background = Bgee::tissue_background(bgee);

write.background(background, file = "./mouse_tissue.xml");