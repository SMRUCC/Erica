#' Load human genes for run single cells for ST data annotation
#'
const load.human_genes = function() {
    "data/HUMAN.tsv"
    |> system.file(package = "Erica")
    |> read.csv(row.names = NULL, check.names = FALSE, tsv = TRUE)
    ;
}

const human_genes.annotations = function(human_genes = NULL, index = "Bgee") {
    let df = {
        if (is.null(human_genes)) {
            load.human_genes();
        } else {
            human_genes
        }
    };
    let geneIDs = unique(df[, index]);
    let i = geneIDs != "";

    print("removes missing genes...");
    geneIDs = geneIDs[i];
    df = df[i, ];

    print(`get ${nrow(df)} which has gene id assigned!`);

    let annos = as.list(df, byrow = TRUE);
    let annoData = list();
    
    print("create gene annotation json...");

    for(gene in annos) {
        geneIDs = strsplit(gene[[index]], ";");
        gene$"Gene Names (synonym)" = strsplit(gene$"Gene Names (synonym)");

        for(id in geneIDs) {
            annoData[[id]] = gene;
        }
    }

    annoData;
}