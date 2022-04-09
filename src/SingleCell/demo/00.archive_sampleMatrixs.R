require(Erica);

binfile = `${@dir}/HR2MSI mouse urinary bladder S096_top3.HTS`;
`${@dir}/HR2MSI mouse urinary bladder S096_top3.csv`
|> load.expr()
|> write.expr_matrix(file = binfile, binary = TRUE)
;

cat(" ~done!\n");