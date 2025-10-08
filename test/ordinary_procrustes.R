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

# 创建2D形状示例：三角形和其变形版本
set.seed(123)

# 基准三角形（等边三角形）
triangle_X <- matrix(c(0, 0,    # 顶点1
                      1, 0,    # 顶点2
                      0.5, 0.866), # 顶点3
                    nrow = 3, ncol = 2, byrow = TRUE)

# 对三角形进行旋转、缩放和平移创建变形版本
rotation_angle <- 30 * pi / 180  # 30度旋转
rotation_matrix <- matrix(c(cos(rotation_angle), -sin(rotation_angle),
                         sin(rotation_angle), cos(rotation_angle)),
                       nrow = 2, ncol = 2)

# 应用变换：先缩放1.5倍，再旋转30度，最后平移(2,1)
triangle_Y_raw <- (1.5 * triangle_X) %*% rotation_matrix + matrix(c(2, 1), 3, 2, byrow = TRUE)

# 执行普氏分析
result <- ordinary_procrustes(triangle_X, triangle_Y_raw, scale = TRUE)

# 打印结果
cat("=== 2D形状普氏分析结果 ===\n")
cat("Procrustes统计量 (M²):", round(result$procrustes_ss, 4), "\n")
cat("缩放因子:", round(result$scale, 4), "\n")
cat("旋转矩阵:\n")
print(round(result$rotation, 4))
cat("平移向量:", round(result$translation, 4), "\n")

# 计算对齐误差
alignment_error <- sqrt(mean(rowSums((result$Y_aligned - triangle_X)^2)))
cat("平均对齐误差:", round(alignment_error, 4), "\n")

# 可视化结果
library(ggplot2)
library(ggforce)

# 准备绘图数据
shape_data <- data.frame(
  x = c(triangle_X[,1], triangle_Y_raw[,1], result$Y_aligned[,1]),
  y = c(triangle_X[,2], triangle_Y_raw[,2], result$Y_aligned[,2]),
  shape_type = rep(c("基准形状", "变形形状", "对齐后形状"), each = 3),
  point_id = rep(1:3, 3)
)

# 创建连线数据（用于显示三角形边）
line_data <- data.frame(
  x = c(triangle_X[,1], triangle_Y_raw[,1], result$Y_aligned[,1]),
  y = c(triangle_X[,2], triangle_Y_raw[,1], result$Y_aligned[,2]),
  group = rep(1:3, each = 3),
  shape_type = rep(c("基准形状", "变形形状", "对齐后形状"), each = 3)
)

# 绘制形状对比图
a = ggplot(shape_data, aes(x = x, y = y, color = shape_type, shape = shape_type)) +
  geom_point(size = 4) +
  geom_polygon(data = subset(shape_data, shape_type == "基准形状"),
               aes(group = 1), fill = "red", alpha = 0.2) +
  geom_polygon(data = subset(shape_data, shape_type == "变形形状"),
               aes(group = 1), fill = "blue", alpha = 0.2) +
  geom_polygon(data = subset(shape_data, shape_type == "对齐后形状"),
               aes(group = 1), fill = "green", alpha = 0.2) +
  geom_text(aes(label = point_id), nudge_y = 0.1, size = 3, color = "black") +
  scale_color_manual(values = c("基准形状" = "red", "变形形状" = "blue", "对齐后形状" = "green")) +
  scale_shape_manual(values = c("基准形状" = 16, "变形形状" = 17, "对齐后形状" = 18)) +
  labs(title = "2D形状普氏分析结果",
       subtitle = "展示基准三角形、变形三角形和对齐后三角形的对比",
       x = "X坐标", y = "Y坐标",
       color = "形状类型", shape = "形状类型") +
  theme_minimal() +
  theme(legend.position = "bottom")

# 创建更简单的点对点连接图（显示对应点关系）
b = ggplot(shape_data, aes(x = x, y = y, color = shape_type)) +
  geom_point(size = 3) +
  geom_path(data = subset(shape_data, shape_type == "基准形状"),
            aes(group = 1), color = "red", linetype = "dashed") +
  geom_path(data = subset(shape_data, shape_type == "对齐后形状"),
            aes(group = 1), color = "green", linetype = "dashed") +
  geom_segment(data = data.frame(x1 = triangle_X[,1], y1 = triangle_X[,2],
                                x2 = result$Y_aligned[,1], y2 = result$Y_aligned[,2]),
              aes(x = x1, y = y1, xend = x2, yend = y2),
              arrow = arrow(length = unit(0.1, "inches")), color = "gray") +
  geom_text(aes(label = point_id), nudge_y = 0.08, size = 3, color = "black") +
  scale_color_manual(values = c("red", "blue", "green")) +
  labs(title = "点对点对齐关系",
       subtitle = "箭头显示从基准形状到对齐后形状的变换",
       x = "X坐标", y = "Y坐标") +
  theme_minimal()

print(a)
print(b)

