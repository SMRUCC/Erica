require(HDS);
require(Erica);

setwd(@dir);

diskfile = "./hsa.bgee";
data = HDS::openStream(diskfile, readonly = TRUE);

print(HDS::tree(data));

close(data);

bgee = read.backgroundPack(diskfile);

data1 = development_stage(bgee);
data2 = anatomicalIDs(bgee);
data3 = geneIDs(bgee);

print("view of the development_stage:");
print(data1);

write.csv(data1, file = "./development_stage.csv", row.names = FALSE);

print("view of the anatomicalIDs:");
print(data2);

write.csv(data2, file = "./anatomicalIDs.csv", row.names = FALSE);

print("view of the geneID set:");
print(data3);

write.csv(data3, file = "./geneIDs.csv", row.names = FALSE);