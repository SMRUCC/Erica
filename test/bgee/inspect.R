require(HDS);
require(Erica);

setwd(@dir);

diskfile = "./hsa.bgee";
data = HDS::openStream(diskfile, readonly = TRUE);

print(HDS::tree(data));

close(data);

bgee = read.backgroundPack(diskfile);

print("view of the development_stage:");
print(development_stage(bgee));

print("view of the anatomicalIDs:");
print(anatomicalIDs(bgee));