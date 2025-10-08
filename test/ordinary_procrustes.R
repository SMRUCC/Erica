# 普通普氏分析（Ordinary Procrustes Analysis）基础实现
# 输入：X和Y为两个矩阵，行数相同（样本数），列数相同（维度）
# 输出：对齐后的Y矩阵、旋转矩阵、缩放因子、平移向量和Procrustes统计量

ordinary_procrustes <- function(X, Y, scale = TRUE) {
    # 检查输入矩阵维度
    if (nrow(X) != nrow(Y)) {
        stop("X和Y必须具有相同的行数（样本数）")
    }
    if (ncol(X) != ncol(Y)) {
        stop("X和Y必须具有相同的列数（维度）")
    }

    n <- nrow(X)  # 样本数
    p <- ncol(X)  # 维度

    # 1. 中心化：平移至原点（去除位置差异）
    X_centered <- scale(X, center = TRUE, scale = FALSE)
    Y_centered <- scale(Y, center = TRUE, scale = FALSE)

    # 2. 可选缩放：归一化至单位Frobenius范数（去除尺度差异）
    if (scale) {
        norm_X <- norm(X_centered, type = "F")  # Frobenius范数
        norm_Y <- norm(Y_centered, type = "F")
        X_centered <- X_centered / norm_X
        Y_centered <- Y_centered / norm_Y
    }

    # 3. 计算旋转矩阵：通过SVD求解最优旋转（去除方向差异）
    C <- t(X_centered) %*% Y_centered  # 协方差矩阵
    svd_result <- svd(C)               # 奇异值分解
    U <- svd_result$u
    V <- svd_result$v
    D <- svd_result$d                   # 奇异值向量

    # 构建旋转矩阵（避免反射，确保为纯旋转）
    R <- U %*% t(V)
    if (det(R) < 0) {                   # 若行列式为负，调整反射
        V[, p] <- -V[, p]                 # 修改最后一列符号
        R <- U %*% t(V)
    }

    # 4. 计算缩放因子（最小二乘最优缩放）
    if (scale) {
        trace_XX <- sum(diag(t(X_centered) %*% X_centered))  # X的迹
        s <- sum(D) / trace_XX  # 缩放因子s = trace(SVD奇异值) / trace(X'X)
    } else {
        s <- 1
    }

    # 5. 变换Y：应用缩放、旋转和平移
    Y_aligned_centered <- s * Y_centered %*% R  # 缩放和旋转
    Y_aligned <- Y_aligned_centered + matrix(colMeans(X), n, p, byrow = TRUE)  # 平移至X的中心

    # 6. 计算Procrustes统计量（残差平方和，即M²值）
    ss <- sum((X_centered - Y_aligned_centered)^2)

    # 返回结果
    return(list(
        Y_aligned = Y_aligned,     # 对齐后的Y矩阵
        rotation = R,              # 旋转矩阵（p x p）
        scale = s,                 # 缩放因子
        translation = colMeans(X), # 平移向量（X的中心）
        procrustes_ss = ss,        # Procrustes平方和（M²）
        centered_X = X_centered,   # 中心化后的X（用于验证）
        centered_Y = Y_centered    # 中心化后的Y（用于验证）
    ))
}

# 示例使用：生成测试数据并验证
set.seed(123)  # 确保可重复性

# 创建示例数据：X为基准矩阵，Y为X的旋转缩放平移版本
X <- matrix(rnorm(15, mean=5), nrow=5, ncol=3)  # 5个样本，3维
rotation_true <- matrix(c(0.96, -0.28, 0.00,
                          0.28, 0.96, 0.00,
                          0.00, 0.00, 1.00), nrow=3)  # 约15度旋转矩阵
Y_raw <- scale(X, center=TRUE, scale=FALSE) %*% rotation_true * 1.5 + matrix(c(2,3,4), 5, 3, byrow=TRUE)

# 执行普氏分析
result <- ordinary_procrustes(X, Y_raw, scale = TRUE)

# 打印关键结果
cat("Procrustes统计量 (M²):", result$procrustes_ss, "\n")
cat("缩放因子:", result$scale, "\n")
cat("旋转矩阵:\n")
print(result$rotation)
cat("平移向量:", result$translation, "\n")

# 验证对齐效果：计算对齐后Y与X的均方误差
mse <- mean((result$Y_aligned - X)^2)
cat("对齐后Y与X的均方误差 (MSE):", mse, "\n")

# 可视化对比（适用于2D数据，若为3D需使用其他方法）
if (ncol(X) == 2) {
    plot(X[,1], X[,2], col = "red", pch = 16, xlim = range(X, result$Y_aligned),
         ylim = range(X, result$Y_aligned), main = "普氏分析结果（2D）",
         xlab = "维度1", ylab = "维度2")
    points(Y_raw[,1], Y_raw[,2], col = "blue", pch = 17)
    points(result$Y_aligned[,1], result$Y_aligned[,2], col = "green", pch = 18)
    legend("topright", legend = c("原始X", "原始Y", "对齐后Y"),
           col = c("red", "blue", "green"), pch = c(16,17,18))
    segments(X[,1], X[,2], result$Y_aligned[,1], result$Y_aligned[,2], lty = 2)  # 连接线
}
