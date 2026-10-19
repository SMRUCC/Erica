#!/usr/bin/env Rscript
# =============================================================================
# top_confidence_edges.R
#
# 从 filter_tf_edges.R 的输出（TF_network_edges.csv）中，
# 按 |Confidence| 降序取 Top-N（默认 1,000,000 条），供后续分析使用。
#
# 算法（两遍流式扫描，内存与输入文件大小无关）：
#   第 1 遍：每块压出 Top-N 候选并合并 -> 得到全局第 N 大的 |Confidence| 阈值
#            （数学上精确：全局 Top-N 的任一条必然出现在其所在块的 Top-N 中）
#   第 2 遍：输出 |Confidence| > 阈值 的全部记录；若不足 N 条，用 == 阈值的
#            并列记录补齐（并列截断，保证结果恰好 N 条）
#   最后：对 N 条结果按键 (-|Confidence|, TF, TargetGene) 排序后保存
#
# 用法（Windows PowerShell）：
#   & "C:\Program Files\R\R-4.5.0\bin\Rscript.exe" .\top_confidence_edges.R
#
#   # 只取 50 万条，输出到指定文件
#   & "C:\Program Files\R\R-4.5.0\bin\Rscript.exe" .\top_confidence_edges.R `
#       --in TF_network_edges.csv --out TF_top500k.csv --top 500000
#
# 依赖：data.table
# =============================================================================

suppressPackageStartupMessages(library(data.table))

# ------------------------------ 工具函数 -------------------------------------

fmt_int <- function(x) formatC(x, format = "d", big.mark = ",")

script_dir <- function() {
  a <- commandArgs(trailingOnly = FALSE)
  f <- sub("^--file=", "", a[grepl("^--file=", a)])
  if (length(f) == 0L) return(getwd())
  d <- tryCatch(normalizePath(dirname(f[1]), mustWork = FALSE),
                error = function(e) getwd())
  if (length(d) == 0L || is.na(d)) getwd() else d
}

resolve_input <- function(p) {
  if (file.exists(p)) return(normalizePath(p, mustWork = FALSE))
  alt <- file.path(script_dir(), p)
  if (file.exists(alt)) return(normalizePath(alt, mustWork = FALSE))
  stop("找不到输入文件: ", p, call. = FALSE)
}

usage <- function() {
  cat(
"按 |Confidence| 降序提取 Top-N 边。

用法: Rscript top_confidence_edges.R [选项]

选项:
  --in <file>        输入 CSV（默认 TF_network_edges.csv）
  --out <file>       输出 CSV（默认 TF_network_edges_top1M.csv，存在则覆盖）
  --top <n>          保留条数（默认 1000000）
  --chunk-lines <n>  每块行数（默认 1000000）
  --quiet            只输出最终统计
  -h, --help         显示本帮助

输入/输出列均为: TF,TargetGene,RegulationType,Confidence,Evidence
排序键: |Confidence| 降序；并列时按 TF、TargetGene 升序，保证结果可复现。
")
}

parse_args <- function(argv) {
  opt <- list(
    in_file = "TF_network_edges.csv",
    out = "TF_network_edges_top1M.csv",
    top = 1000000L,
    chunk_lines = 1000000L,
    quiet = FALSE,
    help = FALSE
  )
  i <- 1L
  while (i <= length(argv)) {
    a <- argv[i]
    if (a %in% c("-h", "--help")) { opt$help <- TRUE; i <- i + 1L; next }
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
    if (key == "in") key <- "in_file"          # --in 是 R 的保留用法，改名
    if (!key %in% names(opt)) stop("未知参数: --", gsub("_", "-", key), call. = FALSE)
    opt[[key]] <- val
  }
  opt$top <- as.integer(opt$top)
  opt$chunk_lines <- as.integer(opt$chunk_lines)
  opt$quiet <- isTRUE(opt$quiet) || identical(as.character(opt$quiet), "TRUE")
  if (is.na(opt$top) || opt$top < 1L) stop("--top 必须是正整数", call. = FALSE)
  if (is.na(opt$chunk_lines) || opt$chunk_lines < 1000L) {
    stop("--chunk-lines 至少为 1000", call. = FALSE)
  }
  opt
}

# 读取一块并解析为 5 列 data.table（列序统一为输出顺序）
read_chunk <- function(con, n) {
  lines <- readLines(con, n = n, warn = FALSE)
  if (length(lines) == 0L) return(NULL)
  lines <- sub("\r$", "", lines)
  lines <- lines[nzchar(lines)]
  if (length(lines) == 0L) return(NULL)
  fread(text = paste0(paste(lines, collapse = "\n"), "\n"),
        header = FALSE, sep = ",", fill = TRUE, colClasses = "character",
        showProgress = FALSE, data.table = TRUE)
}

# ------------------------------ 主流程 ---------------------------------------

main <- function() {
  opt <- parse_args(commandArgs(trailingOnly = TRUE))
  if (opt$help) { usage(); return(invisible(NULL)) }

  in_path <- resolve_input(opt$in_file)
  out_path <- opt$out
  K <- opt$top

  # ---- 表头探测 -------------------------------------------------------------
  con <- file(in_path, "rb")
  on.exit(try(close(con), silent = TRUE), add = TRUE)
  total_bytes <- suppressWarnings(file.size(in_path))
  if (is.na(total_bytes)) total_bytes <- 0

  first_line <- readLines(con, n = 1L, warn = FALSE)
  if (length(first_line) == 0L) stop("输入文件为空", call. = FALSE)
  seek(con, where = 0, origin = "start")   # 复位：表头行留给分块循环处理
  first_line <- sub("^\ufeff", "", first_line)
  first_line <- sub("\r$", "", first_line)
  fields <- trimws(gsub('"', "", strsplit(first_line, ",", fixed = TRUE)[[1]]))

  has_header <- "Confidence" %in% fields
  if (has_header) {
    fi <- match("TF", fields);           ti <- match("TargetGene", fields)
    ri <- match("RegulationType", fields); ci <- match("Confidence", fields)
    ei <- match("Evidence", fields)
    if (any(is.na(c(fi, ti, ci)))) {
      stop("表头中缺少 TF / TargetGene / Confidence 列，实际列: ",
           paste(fields, collapse = ", "), call. = FALSE)
    }
    n_needed <- max(c(fi, ti, ci), na.rm = TRUE)
  } else {
    fi <- 1L; ti <- 2L; ri <- 3L; ci <- 4L; ei <- 5L; n_needed <- 5L
  }

  if (!opt$quiet) {
    cat("输入文件 : ", in_path, " (", round(total_bytes / 1024^2, 1), " MB)\n",
        sep = "")
    cat("输出文件 : ", out_path, "\n", sep = "")
    cat("保留条数 : ", fmt_int(K), "\n", sep = "")
    cat("表头     : ", if (has_header) "有" else "无",
        "  TF=", fi, " TargetGene=", ti, " Confidence=", ci, "\n\n", sep = "")
    flush.console()
  }

  # ===========================================================================
  # 第 1 遍：求全局第 K 大的 |Confidence|
  # ===========================================================================
  t_start <- Sys.time()
  best <- NULL          # 至多 K 行：TF / TargetGene / RegulationType / Confidence / abs_conf
  thr <- -Inf
  n_total <- 0
  chunk_id <- 0L

  repeat {
    chunk_id <- chunk_id + 1L
    dt <- read_chunk(con, opt$chunk_lines)
    if (is.null(dt)) break
    if (chunk_id == 1L && has_header) dt <- dt[-1L, ]
    n_total <- n_total + nrow(dt)
    if (nrow(dt) == 0L) next
    while (ncol(dt) < n_needed) dt[[ncol(dt) + 1L]] <- NA_character_

    cf <- suppressWarnings(as.numeric(dt[[ci]]))
    ok <- is.finite(cf)
    if (!any(ok)) { rm(dt); next }

    cand <- data.table(TF = dt[[fi]][ok],
                       TargetGene = dt[[ti]][ok],
                       RegulationType = if (ri %in% seq_len(ncol(dt))) dt[[ri]][ok] else "Activator",
                       Confidence = cf[ok],
                       abs_conf = abs(cf[ok]))
    rm(dt)

    # 阈值剪枝：池已满时只留 >= 当前阈值的候选
    if (!is.null(best) && nrow(best) >= K) cand <- cand[abs_conf >= thr]
    if (nrow(cand) > 0L) {
      setorder(cand, -abs_conf)
      if (nrow(cand) > K) cand <- cand[seq_len(K)]
      best <- if (is.null(best)) cand else rbind(best, cand)
      if (nrow(best) > K) {
        setorder(best, -abs_conf)
        best <- best[seq_len(K)]
      }
      if (nrow(best) >= K) thr <- best$abs_conf[K]
    }
    rm(cand)
    gc(FALSE)

    if (!opt$quiet && chunk_id %% 5L == 0L) {
      cat("  [扫描1] 已读 ", fmt_int(n_total), " 行，当前阈值 |r| >= ",
          sprintf("%.6f", if (is.finite(thr)) thr else NA_real_), "\n", sep = "")
      flush.console()
    }
  }

  # ===========================================================================
  # 第 2 遍：按阈值输出（不足 K 时用并列记录补齐）
  # ===========================================================================
  if (is.null(best) || nrow(best) == 0L) {
    close(con)
    fwrite(data.table(TF = character(0), TargetGene = character(0),
                      RegulationType = character(0), Confidence = numeric(0),
                      Evidence = character(0)),
           file = out_path, col.names = TRUE, quote = FALSE)
    warning("输入文件中没有可用的 Confidence 数值，已输出空表。", call. = FALSE)
    return(invisible(NULL))
  }

  if (nrow(best) < K) {
    warning("输入文件仅有 ", nrow(best), " 条记录，少于要求的 ",
            K, " 条，将全部输出。", call. = FALSE)
    thr <- -Inf
  }

  seek(con, where = 0, origin = "start")
  tmp_gt <- tempfile("topgt_", tmpdir = dirname(out_path), fileext = ".csv")
  on.exit(if (file.exists(tmp_gt)) unlink(tmp_gt), add = TRUE)

  n_gt <- 0
  eq_collected <- 0L
  eq_buf <- list()
  chunk_id <- 0L

  repeat {
    chunk_id <- chunk_id + 1L
    dt <- read_chunk(con, opt$chunk_lines)
    if (is.null(dt)) break
    if (chunk_id == 1L && has_header) dt <- dt[-1L, ]
    if (nrow(dt) == 0L) next
    while (ncol(dt) < n_needed) dt[[ncol(dt) + 1L]] <- NA_character_

    cf <- suppressWarnings(as.numeric(dt[[ci]]))
    a <- abs(cf)
    valid <- is.finite(a)

    out <- data.table(TF = dt[[fi]],
                      TargetGene = dt[[ti]],
                      RegulationType = if (ri %in% seq_len(ncol(dt))) dt[[ri]] else "Activator",
                      Confidence = cf,
                      Evidence = if (ei %in% seq_len(ncol(dt))) dt[[ei]] else NA_character_)
    rm(dt)

    # (a) 严格大于阈值：全部保留
    gt <- valid & (a > thr)
    if (any(gt)) {
      fwrite(out[gt], file = tmp_gt, append = n_gt > 0L,
             col.names = FALSE, quote = FALSE, na = "")
      n_gt <- n_gt + sum(gt)
    }
    # (b) 等于阈值：按需补齐（收集上限 K 条，need <= K 故足够）
    eq <- valid & (a == thr) & !gt
    if (any(eq) && eq_collected < K) {
      take <- min(sum(eq), K - eq_collected)
      eq_buf[[length(eq_buf) + 1L]] <- out[eq][seq_len(take)]
      eq_collected <- eq_collected + take
    }
    rm(out, cf, a, valid, gt, eq)
    gc(FALSE)

    if (!opt$quiet && chunk_id %% 5L == 0L) {
      cat("  [扫描2] 已写出 ", fmt_int(n_gt), " 条 (> 阈值)\n", sep = "")
      flush.console()
    }
  }

  close(con)

  # ---- 汇总结果：大于阈值的 + 补齐的并列记录 ----
  need <- max(0L, K - n_gt)
  res <- NULL
  if (n_gt > 0L) {
    res <- fread(tmp_gt, header = FALSE, sep = ",", colClasses = "character",
                 showProgress = FALSE, data.table = TRUE)
    setnames(res, c("TF", "TargetGene", "RegulationType", "Confidence", "Evidence"))
  }
  n_tie <- 0L
  if (need > 0L && length(eq_buf) > 0L) {
    eq_dt <- rbindlist(eq_buf, use.names = FALSE)
    eq_dt <- eq_dt[seq_len(min(need, nrow(eq_dt)))]
    n_tie <- nrow(eq_dt)
    res <- if (is.null(res)) eq_dt else rbind(res, eq_dt, use.names = FALSE)
  }
  rm(eq_buf)

  # ---- 统一排序：|Confidence| 降序，并列按 TF/TargetGene 升序（结果可复现） ----
  if (is.null(res) || nrow(res) == 0L) {
    fwrite(data.table(TF = character(0), TargetGene = character(0),
                      RegulationType = character(0), Confidence = numeric(0),
                      Evidence = character(0)),
           file = out_path, col.names = TRUE, quote = FALSE)
    cat("\n没有可用记录，已输出空表。\n")
    return(invisible(NULL))
  }

  res[, Conf_num := abs(suppressWarnings(as.numeric(Confidence)))]
  setorder(res, -Conf_num, TF, TargetGene)
  res[, Conf_num := NULL]
  res[, Confidence := as.numeric(Confidence)]

  fwrite(res, file = out_path, col.names = TRUE, quote = FALSE, na = "")

  # ---- 统计输出 -------------------------------------------------------------
  el <- as.numeric(difftime(Sys.time(), t_start, units = "secs"))
  cat("\n完成。\n")
  cat("  输入数据行 : ", fmt_int(n_total), "\n", sep = "")
  cat("  输出数据行 : ", fmt_int(nrow(res)), "\n", sep = "")
  cat("  阈值 |r|   : ", if (is.finite(thr)) sprintf("%.10f", thr) else "全部（不足 Top-N）",
      "\n", sep = "")
  cat("  且 > 阈值  : ", fmt_int(n_gt), "\n", sep = "")
  cat("  并列补齐   : ", fmt_int(n_tie), "\n", sep = "")
  cat("  |r| 范围   : ",
      sprintf("%.6f ~ %.6f", min(abs(res$Confidence)), max(abs(res$Confidence))),
      "\n", sep = "")
  cat("  Activator  : ", fmt_int(sum(res$RegulationType == "Activator")), "\n", sep = "")
  cat("  Inhibitor  : ", fmt_int(sum(res$RegulationType == "Inhibitor")), "\n", sep = "")
  cat(sprintf("  耗时       : %.1f 秒\n", el))
  cat(sprintf("  输出文件   : %s (%.2f MB)\n", out_path,
              file.size(out_path) / 1024^2))
  cat("\n输出列: TF,TargetGene,RegulationType,Confidence,Evidence（按 |Confidence| 降序）\n")
  invisible(TRUE)
}

main()
