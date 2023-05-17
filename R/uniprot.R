#' Load human genes for run single cells for ST data annotation
#'
const load.human_genes = function() {
    const df = "data/HUMAN.tsv"
    |> system.file(package = "Erica")
    |> read.csv(row.names = NULL, check.names = FALSE, tsv = TRUE)
    ;

    str(df);
}