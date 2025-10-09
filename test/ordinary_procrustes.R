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

    # 1. 中心化：平移至原点
    X_centered <- scale(X, center = TRUE, scale = FALSE)
    Y_centered <- scale(Y, center = TRUE, scale = FALSE)

    # 2. 计算协方差矩阵（使用未缩放的Y）
    C <- t(Y_centered) %*% X_centered

    # 3. SVD分解
    svd_result <- svd(C)
    U <- svd_result$u
    V <- svd_result$v
    d <- svd_result$d  # 奇异值

    # 4. 计算旋转矩阵
    R <- V %*% t(U)

    # 5. 处理反射问题
    if (det(R) < 0) {
        # 调整最后一个奇异值对应的向量
        V[, p] <- -V[, p]
        R <- V %*% t(U)
    }

    # 6. 计算最优缩放因子（关键修正点）
    if (scale) {
        norm_Y_sq <- sum(Y_centered^2)
        if (norm_Y_sq < .Machine$double.eps) {
            stop("Y矩阵的范数过小，无法计算缩放因子")
        }
        s <- sum(d) / norm_Y_sq  # 正确的缩放因子计算公式
    } else {
        s <- 1
    }

    # 7. 应用变换
    Y_scaled <- s * Y_centered
    # Y_aligned_centered <- Y_scaled %*% R
    Y_aligned <- Y_scaled + matrix(colMeans(X), n, p, byrow = TRUE)

    # 8. 计算Procrustes统计量
    ss <- sum((X_centered - Y_aligned)^2)

    # 计算拟合优度
    norm_X <- sqrt(sum(X_centered^2))
    correlation <- sum(d) / (norm_X * sqrt(sum(Y_scaled^2)))

    # 返回结果
    return(list(
        Y_aligned = Y_aligned,
        rotation = R,
        scale = s,
        translation = colMeans(X),
        procrustes_ss = ss,
        correlation = correlation
    ))
}



# 创建2D形状示例：飞机形状多边形
set.seed(123)

# 设置顶点数量（20-30个顶点）
n_vertices <- 25

# 定义飞机形状的基准多边形（近似飞机轮廓）
# 生成一个细长多边形模拟飞机机身，加上机翼和尾翼形状
theta <- seq(0, 2*pi, length.out = n_vertices)

# 创建飞机形状：细长机身加上机翼和尾翼的变形
x_base <- 0.6 * cos(theta) + 0.5  # 机身基础形状
y_base <- 0.2 * sin(theta) + 0.3  # 基本高度

# 添加机翼和尾翼特征使形状更像飞机
# 在特定角度区域扩大宽度模拟机翼
wing_indices <- which(theta > pi/4 & theta < 3*pi/4 | theta > 5*pi/4 & theta < 7*pi/4)
y_base[wing_indices] <- y_base[wing_indices] * 2.5  # 扩大机翼区域

# 添加尾翼特征
tail_indices <- which(theta > 3*pi/2 - 0.3 & theta < 3*pi/2 + 0.3)
y_base[tail_indices] <- y_base[tail_indices] * 1.8  # 尾翼稍微突出

# 确保多边形闭合（首尾点相同）
x_base <- c(x_base, x_base[1])
y_base <- c(y_base, y_base[1])
n_vertices <- n_vertices + 1  # 顶点数加1

# 创建基准飞机形状矩阵
airplane_X <- matrix(c(x_base, y_base), ncol = 2, byrow = FALSE)

# 对飞机形状进行旋转、缩放和平移创建变形版本
rotation_angle <- 5 * pi / 180  # 30度旋转
rotation_matrix <- matrix(c(cos(rotation_angle), -sin(rotation_angle),
                         sin(rotation_angle), cos(rotation_angle)),
                       nrow = 2, ncol = 2, byrow = TRUE)

# 应用变换：先缩放1.5倍，再旋转30度，最后平移(2,1)
airplane_Y_raw <- (1.5 * airplane_X) %*% rotation_matrix +
                  matrix(c(2, 1), nrow = n_vertices, ncol = 2, byrow = TRUE)

# 执行普氏分析
result <- ordinary_procrustes(airplane_X, airplane_Y_raw, scale = TRUE)

# 打印结果
cat("=== 飞机形状普氏分析结果 ===\n")
cat("顶点数量:", n_vertices, "\n")
cat("Procrustes统计量 (M²):", round(result$procrustes_ss, 4), "\n")
cat("缩放因子:", round(result$scale, 4), "\n")
cat("旋转矩阵:\n")
print(round(result$rotation, 4))
cat("平移向量:", round(result$translation, 4), "\n")

# 计算对齐误差
alignment_error <- sqrt(mean(rowSums((result$Y_aligned - airplane_X)^2)))
cat("平均对齐误差:", round(alignment_error, 4), "\n")

# 可视化结果
library(ggplot2)
library(ggforce)

# 准备绘图数据
shape_data <- data.frame(
  x = c(airplane_X[,1], airplane_Y_raw[,1], result$Y_aligned[,1]),
  y = c(airplane_X[,2], airplane_Y_raw[,2], result$Y_aligned[,2]),
  shape_type = rep(c("基准形状", "变形形状", "对齐后形状"), each = n_vertices),
  point_id = rep(1:n_vertices, 3)
)

# 创建连线数据（用于显示多边形边）
line_data <- data.frame(
  x = c(airplane_X[,1], airplane_Y_raw[,1], result$Y_aligned[,1]),
  y = c(airplane_X[,2], airplane_Y_raw[,2], result$Y_aligned[,2]),
  group = rep(1:3, each = n_vertices),
  shape_type = rep(c("基准形状", "变形形状", "对齐后形状"), each = n_vertices)
)

# 绘制形状对比图
plt = ggplot(shape_data, aes(x = x, y = y, color = shape_type, shape = shape_type)) +
  geom_point(size = 3) +
  geom_polygon(data = subset(shape_data, shape_type == "基准形状"),
               aes(group = 1), fill = "red", alpha = 0.2, linetype = "solid") +
  geom_polygon(data = subset(shape_data, shape_type == "变形形状"),
               aes(group = 1), fill = "blue", alpha = 0.2, linetype = "solid") +
  geom_polygon(data = subset(shape_data, shape_type == "对齐后形状"),
               aes(group = 1), fill = "green", alpha = 0.2, linetype = "solid") +
  geom_text(aes(label = point_id), nudge_y = 0.05, size = 2.5, color = "black") +
  scale_color_manual(values = c("基准形状" = "red", "变形形状" = "blue",
                               "对齐后形状" = "green")) +
  scale_shape_manual(values = c("基准形状" = 16, "变形形状" = 17,
                               "对齐后形状" = 18)) +
  labs(title = "飞机形状普氏分析结果",
       subtitle = paste("展示基准飞机形状（", n_vertices, "个顶点）、变形形状和对齐后形状的对比"),
       x = "X坐标", y = "Y坐标",
       color = "形状类型", shape = "形状类型") +
  theme_minimal() +
  theme(legend.position = "bottom") +
  coord_equal()  # 确保比例一致，保持形状不变形

print(plt)
