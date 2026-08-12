# Generate a minimal test Seurat object
# Use minimal dependencies to avoid complex serialization issues
library(Seurat)

set.seed(42)

# Minimal expression matrix: 50 genes x 10 cells
ncells <- 10
ngenes <- 50
expr.mat <- matrix(
  rpois(ncells * ngenes, lambda = 2),
  nrow = ngenes,
  ncol = ncells,
  dimnames = list(
    paste0("Gene", sprintf("%03d", seq_len(ngenes))),
    paste0("Cell", sprintf("%03d", seq_len(ncells)))
  )
)

# Create minimal Seurat object
seurat_obj <- CreateSeuratObject(
  counts = expr.mat,
  project = "TestMin",
  assay = "RNA"
)

# Only normalize, no ScaleData/FindVariableFeatures/PCA/UMAP
# to keep the serialization as simple as possible
seurat_obj <- NormalizeData(seurat_obj, verbose = FALSE)

# Add simple metadata
seurat_obj$group <- rep(c("A", "B"), each = 5)

out_dir <- "G:/Erica/src/STRaid/test/data"
if (!dir.exists(out_dir)) {
  dir.create(out_dir, recursive = TRUE)
}

saveRDS(seurat_obj, file = file.path(out_dir, "test_seurat_minimal.rds"))
save(seurat_obj, file = file.path(out_dir, "test_seurat_minimal.rda"))

cat("Minimal Seurat object generated.\n")
cat("  cells:", ncol(seurat_obj), "\n")
cat("  genes:", nrow(seurat_obj), "\n")
cat("  assays:", names(seurat_obj@assays), "\n")
