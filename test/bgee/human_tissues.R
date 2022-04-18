require(Erica);

setwd(@dir);

bgee = Bgee::parseTsv("./Homo_sapiens_expr_simple_all_conditions.tsv");
background = Bgee::tissue_background(bgee);

write.backgroundPack(background, file = "./human_tissues.XML");