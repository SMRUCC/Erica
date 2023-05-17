require(Erica);
require(JSON);

# load.human_genes();

human_genes.annotations()
|> JSON::json_encode()
|> writeLines(con = `${@dir}/human.json`)
;