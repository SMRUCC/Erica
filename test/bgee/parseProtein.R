require(GCModeller);

imports "uniprot" from "seqtoolkit";

"D:\Erica\test\bgee\P51451.xml"
|> readText()
|> parseUniProt()
;