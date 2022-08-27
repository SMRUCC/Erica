require(GCModeller);

imports "UniProt" from "gseakit";
imports "uniprot" from "seqtoolkit";
imports "background" from "gseakit";

uniprot = open.uniprot("K:\\ST\\uniprot-compressed_true_download_true_format_xml_query__28_28taxonom-2022.08.27-02.03.36.85.xml") |> as.vector();

subcellular_location(uniprot)
|> write.background(file = `${@dir}/subcellular_location.xml`)
;

uniprot_keywords(uniprot)
|> write.background(file = `${@dir}/uniprot_keywords.xml`)
;