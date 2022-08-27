require(GCModeller);
require(Erica);

bgee = Bgee::parseTsv("K:\\ST\\Mus_musculus_expr_simple_all_conditions.tsv",pip.stream = TRUE);

write.backgroundPack(bgee, file = `${@dir}/mmu.bgee`);