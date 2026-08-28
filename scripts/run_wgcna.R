#!/usr/bin/env Rscript
# =============================================================================
# run_wgcna.R
# WGCNA 加权基因共表达网络分析
# 输入: Homo_sapiens_expr_advanced_all_conditions.csv
#       行为基因(Ensembl ID), 列为样本(1888 个), 表达值为原始 counts 尺度
# 输出: WGCNA_output/ 目录下的图表、表格与 RData 对象
# 运行: "C:\Program Files\R\R-4.5.0\bin\Rscript.exe" run_wgcna.R
# =============================================================================

suppressPackageStartupMessages({
  library(WGCNA)
  library(data.table)
  library(matrixStats)
})

n_cores <- max(2L, parallel::detectCores() - 1L)
allowWGCNAThreads(nThreads = n_cores)  # 使用 (核心数-1) 个线程

# -----------------------------------------------------------------------------
# 配置区
# -----------------------------------------------------------------------------
CONFIG <- list(
  input_csv        = "Homo_sapiens_expr_advanced_all_conditions.csv",
  output_dir       = "WGCNA_output",

  # 预处理
  top_n_genes      = 61000L,     # 保留的高变基因数
  log2_transform   = TRUE,

  # 软阈值(固定值)
  soft_power       = 10,         # 固定软阈值(调高以切分更多模块)
  network_type     = "signed hybrid",

  # 网络构建
  cor_type         = "bicor",
  max_p_outliers   = 0.05,
  min_module_size  = 150L,       # 调低以切分更多模块
  deep_split       = 4L,         # 调高以切分更多模块
  merge_cut_height = 0.15,       # 调低以切分更多模块
  max_block_size   = 5000L,      # 分块计算, 控制峰值内存
  numeric_labels   = TRUE
)

# 已知的性状取值表(用于从样本名鲁棒解析)
CONDITION_SUFFIXES <- c(
  "wild-type",
  "white",
  "asian",
  "black or african american",
  "american indian or alaska native"
)

STAGE_PATTERNS <- c(
  "\\d+ decade stage \\(human\\)",      # e.g. "sixth decade stage (human)"
  "\\d+ lmp month stage \\(human\\)",   # e.g. "fifth lmp month stage (human)"
  "prime adult stage",
  "life cycle",
  "post-juvenile",
  "post\\-juvenile"
)

# 组织大类映射(基于细胞类型名称关键词)
TISSUE_KEYWORDS <- list(
  "Brain"            = c("cortex", "brain", "hippocamp", "ammon", "substantia nigra",
                         "cerebell", "neuron", "ganglion", "striatum", "thalam",
                         "hypothalam", "putamen", "caudate", "amygdala", "cerebral",
                         "frontal lobe", "parietal", "occipital", "temporal lobe",
                         "spinal cord", "nerve", "basal", "medulla", "pons", "white matter"),
  "Blood/Immune"     = c("blood", "leukocyte", "monocyte", "granulocyte", "lymph",
                         "mononuclear", "macrophage", "neutrophil", "basophil",
                         "eosinophil", "nk cell", "t cell", "b cell", "dendritic",
                         "immune", "bone marrow", "thymocyte", "spleen"),
  "Liver"            = c("liver", "hepatocyte", "hepatic"),
  "Lung"             = c("lung", "bronch", "alveol", "pneumo"),
  "Adipose"          = c("adipose", "adipocyte", "fat"),
  "Muscle"           = c("muscle", "skeletal", "myoblast", "myotube", "vastus",
                         "quadriceps", "gastrocnemius", "myocard", "cardiac",
                         "heart", "atrial", "ventricl"),
  "Pancreas"         = c("pancrea", "islet", "beta cell", "alpha cell"),
  "Reproductive"     = c("germ cell", "sperm", "oocyte", "testis", "testic",
                         "ovary", "ovarian", "endometri", "placenta", "uterus",
                         "prostate", "spermatogon", "follicle", "trophoblast",
                         "blastocyst", "embryo", "gonad"),
  "Kidney"           = c("kidney", "renal", "glomerul", "nephron", "tubul"),
  "Gastrointestinal" = c("colon", "intestin", "stomach", "gastric", "esophag",
                         "duoden", "ileum", "rectum", "gut", "colorect"),
  "Skin"             = c("skin", "dermal", "epiderm", "keratinocyte", "fibroblast",
                         "melanocyte"),
  "Endothelial/Stromal" = c("endothelial", "stromal", "mesenchym", "pericyte")
)

# -----------------------------------------------------------------------------
# 工具函数
# -----------------------------------------------------------------------------
log_msg <- function(...) {
  cat(sprintf("[%s] ", format(Sys.time(), "%Y-%m-%d %H:%M:%S")), ..., "\n", sep = "")
}

# -----------------------------------------------------------------------------
# 1. 数据加载与预处理
# -----------------------------------------------------------------------------
load_and_preprocess <- function(cfg) {
  log_msg("Step 1/5: 加载数据...")
  if (!file.exists(cfg$input_csv)) stop("输入文件不存在: ", cfg$input_csv)

  dt <- fread(cfg$input_csv, header = TRUE, data.table = TRUE, showProgress = TRUE)
  gene_ids <- dt[[1]]
  mat <- as.matrix(dt[, -1])
  rm(dt); gc()

  log_msg("  原始矩阵维度: ", nrow(mat), " 基因 x ", ncol(mat), " 样本")

  if (anyDuplicated(gene_ids)) {
    log_msg("  检测到重复基因 ID, 按行均值去重...")
    keep <- !duplicated(gene_ids)
    mat <- mat[keep, , drop = FALSE]
    gene_ids <- gene_ids[keep]
  }
  rownames(mat) <- gene_ids

  # log2 变换
  if (cfg$log2_transform) {
    log_msg("  执行 log2(x+1) 变换...")
    mat <- log2(mat + 1)
  }

  # 先按方差筛选 top N 高变基因(在全部基因上计算方差)
  if (nrow(mat) > cfg$top_n_genes) {
    log_msg("  计算基因方差并筛选 top ", cfg$top_n_genes, " 高变基因...")
    rv <- rowVars(mat)
    ord <- order(rv, decreasing = TRUE)
    top_idx <- ord[seq_len(cfg$top_n_genes)]
    mat <- mat[top_idx, , drop = FALSE]
  }

  log_msg("  预处理后矩阵: ", nrow(mat), " 基因 x ", ncol(mat), " 样本")

  # 转置为 样本 x 基因
  dat_expr <- t(mat)
  rm(mat); gc()

  # 质量检查
  gsg <- goodSamplesGenes(dat_expr, verbose = 3)
  if (!gsg$allOK) {
    log_msg("  警告: 存在异常样本/基因, 自动剔除...")
    dat_expr <- dat_expr[gsg$goodSamples, gsg$goodGenes]
  }

  list(datExpr = dat_expr, gene_ids = colnames(dat_expr),
       sample_names = rownames(dat_expr))
}

# -----------------------------------------------------------------------------
# 2. 软阈值选择(固定值)
# -----------------------------------------------------------------------------
select_soft_power <- function(datExpr, cfg) {
  log_msg("Step 2/5: 使用固定软阈值 power = ", cfg$soft_power)
  list(power = cfg$soft_power)
}

# -----------------------------------------------------------------------------
# 3. 网络构建与模块识别
# -----------------------------------------------------------------------------
build_network <- function(datExpr, power, cfg) {
  log_msg("Step 3/5: blockwiseModules 构建 signed hybrid 网络(分块, deepSplit=",
          cfg$deep_split, ")...")

  net <- blockwiseModules(
    datExpr,
    power          = power,
    networkType    = cfg$network_type,
    TOMType        = "signed",
    corType        = cfg$cor_type,
    maxPOutliers   = cfg$max_p_outliers,
    minModuleSize  = cfg$min_module_size,
    deepSplit      = cfg$deep_split,
    mergeCutHeight = cfg$merge_cut_height,
    numericLabels  = cfg$numeric_labels,
    maxBlockSize   = cfg$max_block_size,
    reassignThreshold = 1e-6,
    saveTOMs       = FALSE,
    saveTOMFileBase = "blockwiseTOM",
    verbose        = 3,
    nThreads       = n_cores
  )

  log_msg("  识别到 ", length(unique(net$colors)), " 个模块")
  net
}

# -----------------------------------------------------------------------------
# 4. 性状解析与模块-性状关联
# -----------------------------------------------------------------------------
parse_traits <- function(sample_names) {
  log_msg("  解析样本名性状...")
  s <- tolower(trimws(sample_names))
  n <- length(s)

  # 4.1 条件: 从末尾匹配已知后缀
  condition <- rep(NA_character_, n)
  for (suffix in CONDITION_SUFFIXES) {
    hit <- grepl(paste0(suffix, "$"), s)
    condition[hit] <- suffix
    s[hit] <- sub(paste0("-", suffix, "$"), "", s[hit])
  }

  # 4.2 性别: 此时字符串末尾应为性别 token
  sex <- rep(NA_character_, n)
  for (g in c("male", "female", "any")) {
    hit <- grepl(paste0("-", g, "$"), s)
    sex[hit] <- g
    s[hit] <- sub(paste0("-", g, "$"), "", s[hit])
  }

  # 4.3 阶段: 在剩余前缀中匹配已知 stage 模式
  stage <- rep(NA_character_, n)
  for (pat in STAGE_PATTERNS) {
    hit <- grepl(pat, s) & is.na(stage)
    stage[hit] <- regmatches(s[hit], regexpr(pat, s[hit]))
    s[hit] <- gsub(pat, "", s[hit])
  }
  s <- gsub("^-+|-+$", "", s)

  # 4.4 细胞类型 = 剩余部分
  cell_type <- s
  cell_type[cell_type == ""] <- NA_character_

  # 4.5 组织大类映射
  tissue <- rep("Other", n)
  for (tname in names(TISSUE_KEYWORDS)) {
    hit <- sapply(cell_type, function(ct) {
      if (is.na(ct)) return(FALSE)
      any(sapply(TISSUE_KEYWORDS[[tname]], function(kw) grepl(kw, ct, fixed = TRUE)))
    })
    tissue[hit] <- tname
  }

  log_msg("    条件分布: ", paste(names(table(condition)), table(condition),
          collapse = ", ", sep = "="))
  log_msg("    性别分布: ", paste(names(table(sex)), table(sex), collapse = ", ", sep = "="))

  data.frame(sample = sample_names, cell_type = cell_type, tissue = tissue,
             stage = stage, sex = sex, condition = condition,
             stringsAsFactors = FALSE)
}

traits_to_matrix <- function(traits) {
  # 将分类性状编码为二进制指示变量矩阵
  df <- traits[, c("tissue", "sex", "condition")]
  rownames(df) <- traits$sample          # 保留样本名, 供后续与模块特征基因对齐
  df <- df[, sapply(df, function(x) sum(!is.na(x)) > 0), drop = FALSE]
  mm <- model.matrix(~ . - 1, data = df)
  colnames(mm) <- sub("^tissue", "", colnames(mm))
  mm <- mm[, colSums(mm) >= 3, drop = FALSE]  # 剔除过小的类别
  as.data.frame(mm)
}

associate_traits <- function(MEs, trait_matrix, cfg) {
  log_msg("Step 4/5: 计算模块-性状相关性...")
  common <- intersect(rownames(MEs), rownames(trait_matrix))
  ME <- as.matrix(MEs[common, , drop = FALSE])
  tr <- as.matrix(trait_matrix[common, , drop = FALSE])

  cor_res <- cor(ME, tr, use = "pairwise.complete.obs")
  p_res <- corPvalueStudent(cor_res, nSamples = length(common))

  list(cor = cor_res, p = p_res, nSamples = length(common))
}

# -----------------------------------------------------------------------------
# 模块间 eigengene 相关矩阵
# -----------------------------------------------------------------------------
module_eigengene_correlation <- function(MEs, cfg) {
  log_msg("  计算模块间 eigengene 相关矩阵...")

  me_mat <- as.matrix(MEs)
  # 各模块的 eigengene 即列向量; 计算模块两两相关(第一主成分之间的相关)
  cor_mat <- cor(me_mat, use = "pairwise.complete.obs")
  colnames(cor_mat) <- rownames(cor_mat) <- colnames(me_mat)

  out_path <- file.path(cfg$output_dir, "module_eigengene_correlation.csv")
  cor_dt <- as.data.table(cor_mat)
  cor_dt <- cbind(module = rownames(cor_mat), cor_dt)
  fwrite(cor_dt, out_path)
  log_msg("    模块间相关矩阵: ", nrow(cor_mat), " x ", ncol(cor_mat),
          " -> ", out_path)

  cor_mat
}

# -----------------------------------------------------------------------------
# 5. 结果导出
# -----------------------------------------------------------------------------
export_results <- function(pre, power, net, traits, assoc, cfg) {
  log_msg("Step 5/5: 导出结果...")
  dir.create(cfg$output_dir, showWarnings = FALSE, recursive = TRUE)

  datExpr <- pre$datExpr
  gene_ids <- pre$gene_ids

  # 模块颜色
  module_colors <- labels2colors(net$colors)
  names(module_colors) <- gene_ids

  # 模块特征基因
  MEs0 <- moduleEigengenes(datExpr, module_colors)$eigengenes
  MEs <- orderMEs(MEs0)
  colnames(MEs) <- sub("^ME", "", colnames(MEs))

  # kME (模块内连通性)
  kME <- signedKME(datExpr, MEs)

  # ---- 表格 ----
  # 基因-模块分配表
  assign_df <- data.frame(
    geneID      = gene_ids,
    moduleColor = module_colors,
    stringsAsFactors = FALSE
  )
  # 合并 kME(每基因取最大模块连通性)
  assign_df$kME <- apply(kME, 1, max, na.rm = TRUE)
  fwrite(as.data.table(assign_df),
         file.path(cfg$output_dir, "gene_module_assignment.csv"))

  # 模块特征基因
  me_out <- as.data.frame(MEs)
  me_out$sample <- rownames(MEs)
  me_out <- me_out[, c("sample", setdiff(colnames(me_out), "sample"))]
  fwrite(as.data.table(me_out),
         file.path(cfg$output_dir, "module_eigengenes.csv"))

  # 模块间 eigengene 相关矩阵
  module_eigengene_correlation(MEs, cfg)

  # 模块-性状关系
  rel_df <- data.frame(
    module   = rownames(assoc$cor),
    assoc$cor,
    stringsAsFactors = FALSE
  )
  p_df <- data.frame(module = rownames(assoc$p), assoc$p, stringsAsFactors = FALSE)
  rel_long <- data.frame()
  for (i in seq_len(nrow(rel_df))) {
    for (j in 2:ncol(rel_df)) {
      rel_long <- rbind(rel_long, data.frame(
        module    = rel_df$module[i],
        trait     = colnames(rel_df)[j],
        cor       = rel_df[i, j],
        p_value   = p_df[i, j],
        stringsAsFactors = FALSE
      ))
    }
  }
  fwrite(as.data.table(rel_long),
         file.path(cfg$output_dir, "module_trait_relationship.csv"))

  # ---- 共表达网络 ----
  log_msg("  导出共表达网络(带符号 signed hybrid TOM)...")
  # 在 signed hybrid 网络下计算加权邻接矩阵(保留符号, 含负值),
  # 再通过 TOMsimilarity 得到带符号的拓扑重叠矩阵(TOM)。
  adj <- adjacency(datExpr, power = power,
                   type = cfg$network_type,
                   corFnc = if (cfg$cor_type == "bicor") bicor else cor,
                   corOptions = list(maxPOutliers = cfg$max_p_outliers))
  tom <- TOMsimilarity(adj, TOMType = "signed")
  adj_dt <- as.data.table(tom)
  adj_dt <- cbind(gene = gene_ids, adj_dt)
  fwrite(adj_dt, file.path(cfg$output_dir, "adjacency_matrix.csv"))
  log_msg("    带符号 TOM 矩阵范围: [",
          round(min(tom), 3), ", ", round(max(tom), 3), "]")

  # 边列表(仅在同模块内、权重高于阈值时保留), 便于网络可视化工具使用
  # 以模块为单位, 取模块内连通性较高的基因对
  # gene_module <- setNames(module_colors, gene_ids)
  # edge_list <- data.frame(
  #   from     = character(), to = character(),
  #   weight   = numeric(),     module = character(),
  #   stringsAsFactors = FALSE
  # )
  # for (mod in unique(module_colors)) {
  #   if (mod == "grey") next                 # 灰模块为未分配, 跳过
  #   idx <- which(module_colors == mod)
  #   if (length(idx) < 2) next
  #   sub_adj <- adj[idx, idx, drop = FALSE]
  #   # 取下三角(无向边, 去除自环), 仅保留权重 >= edge_weight_cut 的边
  #   edge_weight_cut <- 0.1
  #   for (i in seq_along(idx)) {
  #     for (j in seq_len(i - 1)) {
  #       w <- sub_adj[i, j]
  #       if (!is.na(w) && w >= edge_weight_cut) {
  #         edge_list <- rbind(edge_list, data.frame(
  #           from   = gene_ids[idx[i]],
  #           to     = gene_ids[idx[j]],
  #           weight = w,
  #           module = mod,
  #           stringsAsFactors = FALSE
  #         ))
  #       }
  #     }
  #   }
  # }
  # fwrite(as.data.table(edge_list),
  #        file.path(cfg$output_dir, "network_edges.csv"))
  # log_msg("    邻接矩阵: ", nrow(adj_dt), " x ", ncol(adj_dt) - 1,
  #         "; 边数(权重>=0.1): ", nrow(edge_list))

  # ---- 图形 ----
  log_msg("  生成图形...")

  # 聚类树
  png(file.path(cfg$output_dir, "cluster_dendrogram.png"),
      width = 1600, height = 900, res = 120)
  plotDendroAndColors(
    net$dendrograms[[1]],
    colors = labels2colors(net$unmergedColors)[net$blockGenes[[1]]],
    groupLabels = "Module colors",
    main = "Gene dendrogram and module colors",
    dendroLabels = FALSE,
    addGuide = TRUE, guideHang = 0.05
  )
  dev.off()

  # 模块-性状热图
  png(file.path(cfg$output_dir, "module_trait_heatmap.png"),
      width = 1400, height = 900, res = 120)
  text_matrix <- paste(signif(assoc$cor, 2), "\n(", signif(assoc$p, 1), ")", sep = "")
  dim(text_matrix) <- dim(assoc$cor)
  labeledHeatmap(
    Matrix = assoc$cor,
    xLabels = colnames(assoc$cor),
    yLabels = rownames(assoc$cor),
    xSymbols = colnames(assoc$cor),
    ySymbols = rownames(assoc$cor),
    colorLabels = FALSE,
    colors = blueWhiteRed(50),
    textMatrix = text_matrix,
    setStdMargins = FALSE,
    cex.text = 0.6,
    zlim = c(-1, 1),
    main = "Module-trait relationships"
  )
  dev.off()

  # ---- RData ----
  save(datExpr, gene_ids, power, net, module_colors, MEs, kME,
       traits, assoc,
       file = file.path(cfg$output_dir, "WGCNA_results.RData"))

  log_msg("  结果已导出到: ", cfg$output_dir)
  log_msg("  模块统计:")
  print(table(module_colors))
}

# -----------------------------------------------------------------------------
# 主流程
# -----------------------------------------------------------------------------
main <- function() {
  log_msg("========== WGCNA 分析开始 ==========")
  t0 <- Sys.time()

  pre     <- load_and_preprocess(CONFIG)
  sft     <- select_soft_power(pre$datExpr, CONFIG)
  net     <- build_network(pre$datExpr, sft$power, CONFIG)
  traits  <- parse_traits(pre$sample_names)
  tr_mat  <- traits_to_matrix(traits)
  assoc   <- associate_traits(
    moduleEigengenes(pre$datExpr, labels2colors(net$colors))$eigengenes,
    tr_mat, CONFIG
  )
  export_results(pre, sft$power, net, traits, assoc, CONFIG)

  log_msg("========== 完成! 总耗时: ",
          round(difftime(Sys.time(), t0, units = "mins"), 1), " 分钟 ==========")
}

if (sys.nframe() == 0L) {
  main()
}
