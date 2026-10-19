#!/usr/bin/env Rscript
# =============================================================================
# filter_tf_edges.R
#
# 从超大网络边表（network-edges.csv，约 18.5 GB）中「流式」筛出
# 至少有一端（默认两端皆可）属于 Homo_sapiens_TF.txt 基因集合的边，
# 并按下列 5 列格式输出：
#
#     TF,TargetGene,RegulationType,Confidence,Evidence
#
#   RegulationType 由边的相关系数符号决定：> 0 -> Activator，< 0 -> Inhibitor
#   （= 0 或缺失 -> Neutral）；Confidence 为相关系数本身。
#
# 设计要点：
#   * 绝不整体读入文件。按行分块（默认 50 万行/块）读取 -> 解析 -> 过滤
#     -> 追加写出 -> 释放，峰值内存通常 < 500 MB。
#   * 不依赖 powershell / cmd 的文本处理，纯 R 实现，跨平台。
#   * 全部参数见 --help。
#
# 用法（Windows PowerShell）：
#   & "C:\Program Files\R\R-4.5.0\bin\Rscript.exe" .\filter_tf_edges.R
#
#   # 常用：指定输入输出、按 Ensembl 匹配
#   & "C:\Program Files\R\R-4.5.0\bin\Rscript.exe" .\filter_tf_edges.R `
#       --edges network-edges.csv --tf Homo_sapiens_TF.txt `
#       --out TF_network_edges.csv
#
#   # 先小规模试跑（只处理前 100 万行）验证字段映射
#   & "C:\Program Files\R\R-4.5.0\bin\Rscript.exe" .\filter_tf_edges.R `
#       --head-lines 1000000 --out TF_edges_sample.csv
#
# 依赖：data.table（若未安装：install.packages("data.table")）
# =============================================================================

suppressPackageStartupMessages(library(data.table))

# ------------------------------ 工具函数 -------------------------------------

script_dir <- function() {
  a <- commandArgs(trailingOnly = FALSE)
  f <- sub("^--file=", "", a[grepl("^--file=", a)])
  if (length(f) == 0L) return(getwd())
  d <- tryCatch(normalizePath(dirname(f[1]), mustWork = FALSE),
                error = function(e) getwd())
  if (length(d) == 0L || is.na(d)) getwd() else d
}

fmt_int <- function(x) formatC(x, format = "d", big.mark = ",")

resolve_input <- function(p) {
  if (file.exists(p)) return(normalizePath(p, mustWork = FALSE))
  alt <- file.path(script_dir(), p)
  if (file.exists(alt)) return(normalizePath(alt, mustWork = FALSE))
  stop("找不到输入文件: ", p, call. = FALSE)
}

usage <- function() {
  cat(
"流式筛选「至少一端是 TF」的网络边。

用法: Rscript filter_tf_edges.R [选项]

选项:
  --edges <file>       边表 CSV（默认 network-edges.csv）
  --tf <file>          TF 列表（默认 Homo_sapiens_TF.txt，制表符分隔）
  --out <file>         输出 CSV（默认 TF_network_edges.csv，存在则覆盖）
  --id-type <k>        TF 匹配依据: ensembl(默认) | symbol
  --match <k>          匹配方式: both(默认，source 或 target 任一) | from | to
  --tf-tf <k>          TF-TF 边的输出: expand(默认，正反各一行) | keep(仅一行)
  --dedupe <k>         去重: none(默认，保持原样) | unordered(无向去重，耗内存)
                       | from-lt-to(只保留 fromNode<toNode，零内存，适合对称表)
  --confidence <k>     Confidence 取值: value(默认，原始权重) | abs(绝对值)
                       (RegulationType 的符号判定始终基于原始权重)
  --regulation-type <k> RegulationType 取值:
                       direction(默认，按相关系数符号: >0 Activator / <0 Inhibitor)
                       | auto(用 interaction_type 列，为空填 coexpression)
                       | 其它任意固定字符串
  --evidence <str>     Evidence 列的固定文本（默认 WGCNA_coexpression）
  --chunk-lines <n>    每块行数（默认 500000，内存紧可降到 200000）
  --head-lines <n>     只处理前 n 行（调试用，默认不限制）
  --quiet              只输出最终统计
  -h, --help           显示本帮助

输入格式: fromNode,toNode,value,interaction_type,selfLoop（有表头自动识别）
输出格式: TF,TargetGene,RegulationType,Confidence,Evidence
")
}

parse_args <- function(argv) {
  opt <- list(
    edges = "network-edges.csv",
    tf = "Homo_sapiens_TF.txt",
    out = "TF_network_edges.csv",
    id_type = "ensembl",
    match = "both",
    tf_tf = "expand",
    dedupe = "none",
    confidence = "value",
    regulation_type = "direction",
    evidence = "WGCNA_coexpression",
    chunk_lines = 500000L,
    head_lines = Inf,
    quiet = FALSE,
    help = FALSE
  )
  i <- 1L
  while (i <= length(argv)) {
    a <- argv[i]
    if (a %in% c("-h", "--help")) {
      opt$help <- TRUE
      i <- i + 1L
      next
    }
    if (!startsWith(a, "--")) stop("无法解析的参数: ", a, call. = FALSE)
    body <- substring(a, 3L)
    eq <- regexpr("=", body, fixed = TRUE)
    if (eq > 0L) {
      key <- substring(body, 1L, eq - 1L)
      val <- substring(body, eq + 1L)
      i <- i + 1L
    } else {
      key <- body
      if (gsub("-", "_", key, fixed = TRUE) == "quiet") {
        val <- TRUE
        i <- i + 1L
      } else {
        if (i + 1L > length(argv)) stop("参数 --", key, " 缺少取值", call. = FALSE)
        val <- argv[i + 1L]
        i <- i + 2L
      }
    }
    key <- gsub("-", "_", key, fixed = TRUE)
    if (!key %in% names(opt)) stop("未知参数: --", gsub("_", "-", key), call. = FALSE)
    opt[[key]] <- val
  }
  opt$chunk_lines <- as.integer(opt$chunk_lines)
  opt$head_lines <- if (identical(as.character(opt$head_lines), "Inf")) {
    Inf
  } else {
    as.numeric(opt$head_lines)
  }
  opt$quiet <- isTRUE(opt$quiet) || identical(as.character(opt$quiet), "TRUE")
  if (is.na(opt$chunk_lines) || opt$chunk_lines < 1000L) {
    stop("--chunk-lines 至少为 1000", call. = FALSE)
  }
  opt$id_type <- match.arg(opt$id_type, c("ensembl", "symbol"))
  opt$match <- match.arg(opt$match, c("both", "from", "to"))
  opt$tf_tf <- match.arg(opt$tf_tf, c("expand", "keep"))
  opt$dedupe <- match.arg(opt$dedupe, c("none", "unordered", "from-lt-to"))
  opt$confidence <- match.arg(opt$confidence, c("value", "abs"))
  opt
}

# ------------------------------ 主流程 ---------------------------------------

main <- function() {
  opt <- parse_args(commandArgs(trailingOnly = TRUE))
  if (opt$help) { usage(); return(invisible(NULL)) }

  edges_path <- resolve_input(opt$edges)
  tf_path    <- resolve_input(opt$tf)
  out_path   <- opt$out

  # ---- 1. 读入 TF 集合 ------------------------------------------------------
  tf_tab <- fread(tf_path, sep = "\t", header = TRUE, colClasses = "character",
                  showProgress = FALSE, encoding = "UTF-8")
  tf_col <- if (opt$id_type == "ensembl") "Ensembl" else "Symbol"
  if (!tf_col %in% names(tf_tab)) {
    stop("TF 表中找不到列 '", tf_col, "'，实际列: ",
         paste(names(tf_tab), collapse = ", "), call. = FALSE)
  }
  tf_ids <- trimws(tf_tab[[tf_col]])
  tf_ids <- tf_ids[!is.na(tf_ids) & nzchar(tf_ids) & tf_ids != "NA"]
  if (opt$id_type == "symbol") tf_ids <- toupper(tf_ids)
  tf_ids <- unique(tf_ids)
  if (length(tf_ids) == 0L) stop("TF 集合为空，请检查 --id-type", call. = FALSE)

  # ---- 2. 探测表头与列位置 --------------------------------------------------
  con <- file(edges_path, "rb")
  on.exit(try(close(con), silent = TRUE), add = TRUE)
  total_bytes <- suppressWarnings(file.size(edges_path))
  if (is.na(total_bytes)) total_bytes <- 0

  first_line <- readLines(con, n = 1L, warn = FALSE)
  if (length(first_line) == 0L) stop("边表为空", call. = FALSE)
  seek(con, where = 0, origin = "start")   # 复位：表头行留给分块循环处理
  first_line <- sub("^\ufeff", "", first_line)
  first_line <- sub("\r$", "", first_line)
  fields <- trimws(gsub('"', "", strsplit(first_line, ",", fixed = TRUE)[[1]]))

  has_header <- !grepl("^ENSG", first_line) && !grepl("^[0-9]", first_line)
  if (has_header) {
    fi <- match("fromNode", fields); ti <- match("toNode", fields)
    vi <- match("value", fields);    ii <- match("interaction_type", fields)
    if (is.na(fi) || is.na(ti)) {
      stop("表头中找不到 fromNode / toNode 列，实际列: ",
           paste(fields, collapse = ", "), call. = FALSE)
    }
    if (is.na(vi)) vi <- max(fi, ti) + 1L
    if (is.na(ii)) ii <- 0L
  } else {
    fi <- 1L; ti <- 2L; vi <- 3L; ii <- 4L   # 无表头：按默认顺序
  }
  n_cols <- max(fi, ti, vi, ii, 5L)

  if (!opt$quiet) {
    cat("输入边表 : ", edges_path, " (",
        round(file.size(edges_path) / 1024^3, 2), " GB)\n", sep = "")
    cat("TF 列表  : ", tf_path, "  依据列=", tf_col,
        "  可用 TF 数=", length(tf_ids), "\n", sep = "")
    cat("表头     : ", if (has_header) "有" else "无",
        "  fromNode=", fi, " toNode=", ti, " value=", vi,
        " interaction_type=", ii, "\n", sep = "")
    cat("输出     : ", out_path, "\n", sep = "")
    cat("匹配     : ", opt$match, " / id=", opt$id_type,
        " / tf-tf=", opt$tf_tf, " / dedupe=", opt$dedupe, "\n\n", sep = "")
    flush.console()
  }

  # ---- 3. 分块扫描 ----------------------------------------------------------
  seen <- if (opt$dedupe == "unordered") new.env(hash = TRUE, parent = emptyenv()) else NULL

  n_lines <- 0            # 已读数据行（不含表头）
  n_kept  <- 0            # 已写出数据行
  n_tf_tf <- 0            # 命中的 TF-TF 边数（去重后）
  chunk_id <- 0L
  first_write <- TRUE
  t_start <- Sys.time()

  repeat {
    lines <- readLines(con, n = opt$chunk_lines, warn = FALSE)
    if (length(lines) == 0L) break

    chunk_id <- chunk_id + 1L
    if (chunk_id == 1L && has_header) lines <- lines[-1L]
    lines <- sub("\r$", "", lines)
    lines <- lines[nzchar(lines)]
    if (length(lines) == 0L) next

    # --head-lines 截断
    if (is.finite(opt$head_lines) && n_lines + length(lines) > opt$head_lines) {
      lines <- lines[seq_len(max(0, opt$head_lines - n_lines))]
      if (length(lines) == 0L) break
    }
    n_lines <- n_lines + length(lines)

    # ---- 解析当前块（全部按字符读入，避免类型猜测出错） ----
    dt <- tryCatch(
      fread(text = paste0(paste(lines, collapse = "\n"), "\n"),
            header = FALSE, sep = ",", fill = TRUE, colClasses = "character",
            showProgress = FALSE, data.table = TRUE),
      error = function(e) {
        warning("第 ", chunk_id, " 块解析失败，跳过: ", conditionMessage(e),
                call. = FALSE)
        NULL
      }
    )
    rm(lines)
    if (is.null(dt) || nrow(dt) == 0L) {
      if (is.finite(opt$head_lines) && n_lines >= opt$head_lines) break
      next
    }
    # 列数不足时补齐
    while (ncol(dt) < n_cols) dt[[ncol(dt) + 1L]] <- NA_character_

    from_raw <- dt[[fi]]
    to_raw   <- dt[[ti]]
    from_key <- if (opt$id_type == "symbol") toupper(from_raw) else from_raw
    to_key   <- if (opt$id_type == "symbol") toupper(to_raw) else to_raw

    # ---- 过滤 ----
    tf_f <- from_key %chin% tf_ids
    tf_t <- to_key %chin% tf_ids
    keep <- switch(opt$match,
                   both = tf_f | tf_t,
                   from = tf_f,
                   to   = tf_t)
    if (!any(keep)) {
      rm(dt); gc(FALSE)
      if (is.finite(opt$head_lines) && n_lines >= opt$head_lines) break
      if (!opt$quiet && chunk_id %% 20L == 0L) {
        cat("  ... 已扫描 ", fmt_int(n_lines), " 行，命中 ",
            fmt_int(n_kept), " 行\n", sep = "")
        flush.console()
      }
      next
    }

    f <- from_raw[keep]; t <- to_raw[keep]
    kf <- tf_f[keep];   kt <- tf_t[keep]

    # 反向重复（仅 --dedupe from-lt-to；零内存，适合对称无向表）
    ord_ok <- NULL
    if (opt$dedupe == "from-lt-to") {
      ord_ok <- f < t
      f <- f[ord_ok]; t <- t[ord_ok]; kf <- kf[ord_ok]; kt <- kt[ord_ok]
      if (length(f) == 0L) {
        rm(dt); gc(FALSE)
        if (is.finite(opt$head_lines) && n_lines >= opt$head_lines) break
        next
      }
    }

    tf_col_out <- ifelse(kf, f, t)
    tg_col_out <- ifelse(kf, t, f)

    is_tftf <- kf & kt
    n_tf_tf <- n_tf_tf + sum(is_tftf)

    # ---- 原始权重与 Confidence ----
    v <- suppressWarnings(as.numeric(dt[[vi]][keep]))
    if (!is.null(ord_ok)) v <- v[ord_ok]
    w <- if (opt$confidence == "abs") abs(v) else v

    # ---- RegulationType ----
    # direction(默认): 相关系数 > 0 为 Activator，< 0 为 Inhibitor，=0/NA 为 Neutral
    # auto          : 用 interaction_type 列，为空则填 coexpression
    # 其它任意字符串: 固定值
    if (identical(opt$regulation_type, "direction")) {
      rtype <- ifelse(is.na(v), "Neutral", ifelse(v > 0, "Activator", "Inhibitor"))
    } else if (identical(opt$regulation_type, "auto") && ii > 0L) {
      it <- trimws(dt[[ii]][keep])
      if (!is.null(ord_ok)) it <- it[ord_ok]
      it[is.na(it)] <- ""
      rtype <- ifelse(nzchar(it), it, "coexpression")
    } else {
      fixed <- if (identical(opt$regulation_type, "auto")) "coexpression" else opt$regulation_type
      rtype <- rep(fixed, length(tf_col_out))
    }

    res <- data.table(TF = tf_col_out,
                      TargetGene = tg_col_out,
                      RegulationType = rtype,
                      Confidence = w,
                      Evidence = rep(opt$evidence, length(tf_col_out)))

    # ---- TF-TF 双向展开 ----
    if (opt$tf_tf == "expand" && any(is_tftf)) {
      add <- data.table(TF = tg_col_out[is_tftf],
                        TargetGene = tf_col_out[is_tftf],
                        RegulationType = rtype[is_tftf],
                        Confidence = w[is_tftf],
                        Evidence = rep(opt$evidence, sum(is_tftf)))
      res <- rbind(res, add, use.names = FALSE)
    }

    # ---- 可选：无向去重（耗内存，仅对输出行） ----
    if (opt$dedupe == "unordered" && nrow(res) > 0L) {
      key <- paste(pmin(res$TF, res$TargetGene), pmax(res$TF, res$TargetGene),
                   sep = "\x1f")
      is_new <- !vapply(key, exists, logical(1L), envir = seen, inherits = FALSE)
      if (!all(is_new)) res <- res[is_new]
      new_keys <- unique(key[is_new])
      if (length(new_keys)) {
        list2env(as.list(setNames(rep(TRUE, length(new_keys)), new_keys)), envir = seen)
      }
      rm(key, is_new, new_keys)
    }

    if (nrow(res) > 0L) {
      fwrite(res, file = out_path, append = !first_write,
             col.names = first_write, quote = FALSE, na = "")
      first_write <- FALSE
      n_kept <- n_kept + nrow(res)
    }

    rm(dt, res, from_raw, to_raw, from_key, to_key, tf_f, tf_t, keep,
       f, t, kf, kt, tf_col_out, tg_col_out, is_tftf, v, w, rtype)
    gc(FALSE)

    if (!opt$quiet && (chunk_id %% 20L == 0L ||
                       (is.finite(opt$head_lines) && n_lines >= opt$head_lines))) {
      el <- as.numeric(difftime(Sys.time(), t_start, units = "secs"))
      pos <- tryCatch(seek(con, where = NA), error = function(e) NA_real_)
      pct <- if (is.finite(pos) && total_bytes > 0) 100 * pos / total_bytes else NA_real_
      cat("  ... 已扫描 ", fmt_int(n_lines), " 行 (",
          sprintf("%.1f", pct), "%)，命中 ", fmt_int(n_kept),
          " 行，用时 ", sprintf("%.0f", el), " s\n", sep = "")
      flush.console()
    }
    if (is.finite(opt$head_lines) && n_lines >= opt$head_lines) break
  }

  close(con)
  on.exit(NULL, add = FALSE)

  # 零命中时仍写出（仅有表头的）输出文件，便于下游脚本无差别读取
  if (first_write) {
    fwrite(data.table(TF = character(0), TargetGene = character(0),
                      RegulationType = character(0), Confidence = numeric(0),
                      Evidence = character(0)),
           file = out_path, col.names = TRUE, quote = FALSE)
    warning("没有任何边命中 TF 集合，输出文件只含表头。若输入节点是 Ensembl ID，" ,
            "请使用 --id-type ensembl（默认）。", call. = FALSE)
  }

  el <- as.numeric(difftime(Sys.time(), t_start, units = "secs"))
  cat("\n完成。\n")
  cat("  扫描数据行 : ", fmt_int(n_lines), "\n", sep = "")
  cat("  输出数据行 : ", fmt_int(n_kept), "\n", sep = "")
  cat("  其中 TF-TF : ", fmt_int(n_tf_tf), "\n", sep = "")
  cat(sprintf("  耗时       : %.1f 秒\n", el))
  if (file.exists(out_path)) {
    cat(sprintf("  输出文件   : %s (%.2f MB)\n", out_path,
                file.size(out_path) / 1024^2))
  }
  cat("\n输出列: TF,TargetGene,RegulationType,Confidence,Evidence\n")
  if (opt$id_type == "ensembl") {
    cat("提示: TF 与 TargetGene 均为 Ensembl gene ID（源自边表节点标识）。\n")
  }
  invisible(TRUE)
}

main()
