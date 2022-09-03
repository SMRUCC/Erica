require(GCModeller);
require(Erica);
require(JSON);

imports "STdeconvolve" from "Erica";

docs = "E:\Erica\src\SingleCell\demo\HR2MSI mouse urinary bladder S096_top3.csv"
|> load.expr()
|> STdeconvolve::STCorpus()
;

model = STdeconvolve::fitLDA(k = 12, alpha = 2.0, beta = 0.5);

data = model |> getBetaTheta(corpus = docs);

write.expr_matrix([data]::theta, file = `${@dir}/result/rawExpr0.csv`);

i = 1;

for(topic in [data]::topicMap) {
    topic 
    |> json_encode()
    |> writeLines(
        con = `${@dir}/result/topic_${i = i + 1}.json`
    )
    ;
}