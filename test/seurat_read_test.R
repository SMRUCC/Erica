require(Erica);

imports "STdata" from "Erica";

setwd(@dir);

let r = readRData("./SCA_CyTOF_TISSUE_WHOLE_BLOOD_SDY1389_Whole_Blood.RDS");

str(r);
stop();

let m = read.seurat("./SCA_CyTOF_TISSUE_WHOLE_BLOOD_SDY1389_Whole_Blood.RDS");

