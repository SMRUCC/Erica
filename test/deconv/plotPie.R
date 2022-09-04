require(ggplot);
require(Erica);

setwd(@dir);

data = read.csv("./result/rawExpr0.csv", row.names = 1);
data = data[, c("topic_1","topic_2","topic_3","topic_4","topic_5",
"topic_6","topic_7","topic_8","topic_9","topic_10")];

spatial = rownames(data);
xy = strsplit( spatial, ",");
names(xy) = spatial;
xy = lapply(xy, a -> as.numeric(a));

str(xy);

print(data, max.print = 13);
