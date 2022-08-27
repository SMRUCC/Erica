imports "UniProt" from "gseakit";

const UniProtKeywords = function() {
    "data/UniProt-Keywords.tsv"
    |> system.file(package = "Erica")
    |> read.csv(tsv = TRUE, check.names = FALSE)
    ;
}