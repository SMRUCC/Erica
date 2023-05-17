#' Load human genes for run single cells for ST data annotation
#'
const load.human_genes = function() {
    "data/HUMAN.tsv"
    |> system.file(package = "Erica")
    |> read.csv(row.names = NULL, check.names = FALSE, tsv = TRUE)
    ;
}

const human_genes.annotations = function(index = "Bgee") {
    let df = load.human_genes();
    let geneIDs = unique(df[, index]);
    let i = geneIDs != "";

    geneIDs = geneIDs[i];
    df = df[i, ];

    let annos = as.list(df, byrow = TRUE);
    let annoData = list();
    
    for(gene in annos) {
        geneIDs = strsplit(gene[[index]], ";");
        gene$"Gene Names (synonym)" = strsplit(gene$"Gene Names (synonym)");

        for(id in geneIDs) {
            annoData[[id]] = gene;
        }
    }

    annoData;
}