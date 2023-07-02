require(ggplot);

options(strict = FALSE);
setwd(@dir);

let deconv = read.csv(file = "./HR2MSI mouse urinary bladder S096_top3.csv", row.names = 1, check.names = FALSE);
let topics = as.list(deconv, byrow = TRUE) |> sapply(r -> which.max(r));

bitmap(file = `${@dir}/UMAP2d.png`, size = [2400, 2000]) {
    
	let data = read.csv(`${@dir}/UMAP2D.csv`, row.names = 1);
	data[, "class"] = `class_${topics}`;
	
	# create ggplot layers and tweaks via ggplot style options
	ggplot(data, aes(x = "X", y = "Y"), padding = "padding:250px 500px 100px 100px;")
	+ geom_point(aes(color = "class"), color = "paper", shape = "triangle", size = 20)
	+ view_camera(angle = [31.5,65,125], fov = 100000)
	+ ggtitle("Scatter UMAP 2D")
	+ theme_default()
	;

}