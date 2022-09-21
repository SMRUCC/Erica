require(ggplot);
require(Erica);

setwd(@dir);

data = read.csv("./result/rawExpr0.csv", row.names = 1);
data = data[, c("topic_1","topic_2","topic_3","topic_4","topic_5",
"topic_6","topic_7","topic_8","topic_9","topic_10")];

layer = colnames(data);
spatial = rownames(data);
xy = strsplit( spatial, ",");
names(xy) = spatial;
xy = lapply(xy, a -> as.numeric(a));

str(xy);

data[, "x"] = sapply(xy, a -> a[1]);
data[, "y"] = sapply(xy, a -> a[2]);

print(data, max.print = 13);

for(id in layer) {
	
	layer = data[, c("x","y", id)];
	
	bitmap(file = `${@dir}/heatmap/${id}.png`, size = [3600, 2800]) {

		ggplot(data, aes(x = "x", y = "y"), padding = "padding: 250px 350px 200px 250px;")
		+ geom_scatterheatmap(data = id)
		;

	}

}