# Generate a small test Seurat object and save as rda / rds
# so that the VB.NET reader can be validated against GNU R output.
library(Seurat)

set.seed(42)

# ---- build a tiny expression matrix: 100 genes x 30 cells ----
ncells <- 30
ngenes <- 100
expr.mat <- matrix(
  rpois(ncells * ngenes, lambda = 3),
  nrow = ngenes,
  ncol = ncells,
  dimnames = list(
    paste0("Gene", sprintf("%03d", seq_len(ngenes))),
    paste0("Cell", sprintf("%03d", seq_len(ncells)))
  )
)

# ---- create Seurat object with raw counts ----
seurat_obj <- CreateSeuratObject(
  counts = expr.mat,
  project = "TestProj",
  assay = "RNA"
)

# ---- add normalized data and scale data ----
seurat_obj <- NormalizeData(seurat_obj, verbose = FALSE)
seurat_obj <- FindVariableFeatures(seurat_obj, verbose = FALSE)
seurat_obj <- ScaleData(seurat_obj, verbose = FALSE)

# ---- meta.data: add a couple of cell-level annotations ----
seurat_obj$orig.ident <- ifelse(
  seq_len(ncells) <= 15,
  "GroupA",
  "GroupB"
)
seurat_obj$nCount_RNA <- Matrix::colSums(LayerData(seurat_obj, layer = "counts"))

# ---- dimensionality reduction: PCA + UMAP ----
seurat_obj <- RunPCA(seurat_obj, npcs = 10, verbose = FALSE)
seurat_obj <- RunUMAP(seurat_obj, dims = 1:10, verbose = FALSE)

# ---- NOTE: a spatial image is added in a separate script step if needed ----
# (Seurat 5 spatial image construction is non-trivial; core slots are tested here)

# ---- save to working directory ----
out_dir <- "G:/Erica/src/STRaid/test/data"
if (!dir.exists(out_dir)) {
  dir.create(out_dir, recursive = TRUE)
}

saveRDS(seurat_obj, file = file.path(out_dir, "test_seurat.rds"))
save(seurat_obj, file = file.path(out_dir, "test_seurat.rda"))

cat("Seurat object generated successfully.\n")
cat("  cells:", ncol(seurat_obj), "\n")
cat("  genes:", nrow(seurat_obj), "\n")
cat("  assays:", names(seurat_obj@assays), "\n")
cat("  reductions:", names(seurat_obj@reductions), "\n")
cat("  images:", names(seurat_obj@images), "\n")
