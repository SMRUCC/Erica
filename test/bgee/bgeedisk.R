require(GCModeller);
require(Erica);

setwd(@dir);

bgee = Bgee::parseTsv("./Homo_sapiens_expr_simple_all_conditions.tsv",pip.stream = TRUE);

write.backgroundPack(bgee, file = "./hsa.bgee");