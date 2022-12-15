imports "package_utils" from "devkit";

package_utils::attach("E:\Erica");

# require(Erica);
require(igraph);
require(charts);

imports "phenograph" from "Erica";
imports "geneExpression" from "phenotype_kit";

setwd(@dir);

x = "../../demo/HR2MSI mouse urinary bladder S096_top3.HTS"
|> load.expr0()
;

print(geneExpression::dims(x));

x
|> phenograph(k = 32)
|> save.network(file = "HR2MSI mouse urinary bladder S096_graph")
;

bitmap(file = `Rphenograph.png`) {
	const data     = "HR2MSI mouse urinary bladder S096_graph/nodes.csv" 
	|> read.csv(
		row.names   = NULL, 
		check.names = FALSE
	);

	# print(data[, 1]);

	const ID = lapply(data[, 1], function(px) as.numeric(strsplit(px, ",", fixed = TRUE)));

	# print(ID);

	const clusters = data[, "NodeType"];
	const x        = sapply(ID, px -> px[1]);
    const y        = sapply(ID, px -> px[2]);
	
	print(data, max.print = 13);
	print("[x,y]:");
	print(x);
	print(y);

	plot(x, y, 
	    class      = clusters, 
	    shape      = "Square", 
	    colorSet   = "Set1:c8", 
	    grid.fill  = "white", 
	    point_size = 25, 
	    reverse    = TRUE
    );
}