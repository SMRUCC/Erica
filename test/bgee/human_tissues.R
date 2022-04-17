require(Erica);

bgee = Bgee::parseTsv("F:\human\Homo_sapiens_expr_simple_all_conditions.tsv");
background = Bgee::tissue_background(bgee);

