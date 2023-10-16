imports "UniProt" from "gseakit";

#' Get uniprot keyword background
#' 
const UniProtKeywords = function() {
    "data/UniProt-Keywords.tsv"
    |> system.file(package = "Erica")
    |> read.csv(tsv = TRUE, check.names = FALSE)
    ;
}