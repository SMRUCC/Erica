setwd(@dir);

require(JSON);

options(max.print = 20);
options(strict = FALSE);

data = "small_parts.json" |> readText |> json_decode;

tags = names(data);

i = tags like $".+\s+cell";


print(tags[i], max.print = 1000);


part = data$"cortex of kidney";
part = append(part, (data$"Leydig cell region of testis"));

 # str(part);

geneID = sapply(part, i -> i$geneID);
name = sapply(part, i -> i$gene_name);
quality = sapply(part, i -> i$call_quality); 
 developmental_stage = sapply(part, i-> i$ developmental_stage);
  developmental_stageID = sapply(part, i -> i$ developmental_stageID);
  expression_rank = sapply(part, i -> i$expression_rank);
anatomicalID = sapply(part, i-> i$anatomicalID);
anatomicalName = sapply(part, i -> i$anatomicalName);

cat("\n\n\n");

print(data.frame(
row.names = geneID, 
gene_name = name,
quality=quality,
expression_rank = as.numeric(expression_rank) ,
# developmental_stageID=developmental_stageID,
# developmental_stage = developmental_stage,
 anatomicalID=anatomicalID,
 anatomicalName =anatomicalName 
));