require(Erica);

for(file in list.files("K:\zebrafish\readsCounts", pattern = "*.csv")) {
	cat(`process ${basename(file)}...`);
	
	binfile = `${dirname(file)}/${basename(file)}.HTS`;
	file 
	|> load.expr()
	|> write.expr_matrix(file = binfile, binary = TRUE)
	;
	
	cat(" ~done!\n");
}