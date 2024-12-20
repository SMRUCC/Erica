require(ggplot);
require(Erica);

setwd(@dir);

let data = read.csv("./HR2MSI mouse urinary bladder S096_top3-cell_layers.csv", row.names = 1, check.names = FALSE);
let layer = colnames(data);
let spatial = rownames(data);
let xy = strsplit( spatial, ",");

data[, "x"] = as.numeric(xy@{1});
data[, "y"] = as.numeric(xy@{2});

print(data, max.print = 13);

for(id in layer) {
	
	layer = data[, c("x","y", id)];
	
	bitmap(file = `cell_layers/${id}.png`, size = [3600, 2800]) {

		ggplot(data, aes(x = "x", y = "y"), colorSet = "viridis:turbo", padding = "padding: 250px 350px 200px 250px;")
		+ geom_scatterheatmap(data = id)
		;
	}
}