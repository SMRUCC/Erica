library(BgeeDB);

# Creating new Bgee class object
bgee <- Bgee$new(species = "Danio_rerio")
# Loading calls of expression
myTopAnatData <- loadTopAnatData(bgee)
# Look at the data
str(myTopAnatData)

background = names(myTopAnatData$gene2anatomy);

enrich = function(infile, outfile) {
	data = read.csv(infile, row.names = 1);
	geneList = background %in% rownames(data);
	geneList = as.integer(geneList);
	names(geneList) = background;
	
	# Prepare the topGO object
	myTopAnatObject <-  topAnat(myTopAnatData, geneList)
	results <- runTest(myTopAnatObject, algorithm = 'weight', statistic = 'fisher')
	# Display results sigificant at a 1% FDR threshold
	tableOver <- makeTable(myTopAnatData, myTopAnatObject, results, cutoff = 1);
	print(head(tableOver))
	
	terms = rownames(tableOver)
	hits_gene = lapply(terms, function(term) {
		stat = termStat(myTopAnatObject, term) 

		# 198 genes mapped to this term for Bgee 14.0 and Ensembl 84
		# genesInTerm(myTopAnatObject, term)
		# 48 significant genes mapped to this term for Bgee 14.0 and Ensembl 84
		annotated <- genesInTerm(myTopAnatObject, term)[[term]]
		hits = annotated[annotated %in% sigGenes(myTopAnatObject)]
		
		paste(hits, collapse = "; ");
	});
	
	hits_gene = unlist(hits_gene);
	tableOver = cbind(tableOver, hits = hits_gene);
	
	write.csv(tableOver, file = outfile);
}


enrich("./BMY_H1000_6h.csv", "./BMY_H1000_6h_topGO.csv");
enrich("./BMY_H1000_12h.csv", "./BMY_H1000_12h_topGO.csv");