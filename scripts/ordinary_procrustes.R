# 普通普氏分析（Ordinary Procrustes Analysis）基础实现
# 输入：X和Y为两个矩阵，行数相同（样本数），列数相同（维度）
# 输出：对齐后的Y矩阵、旋转矩阵、缩放因子、平移向量和Procrustes统计量
ordinary_procrustes <- function(X, Y, scale = TRUE) {
  # 检查输入矩阵维度
  if (nrow(X) != nrow(Y)) {
    stop("The X and Y matrices must have the same number of rows (number of samples)!");
  } else if (ncol(X) != ncol(Y)) {
    stop("The X and Y matrices must have the same number of columns (dimensions)!");
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
  d <- svd_result$d  # 奇异值

  # 6. 计算最优缩放因子（关键修正点）
  if (scale) {
    norm_Y_sq <- sum(Y_centered^2)
    if (norm_Y_sq < .Machine$double.eps) {
      stop("The norm of the Y matrix is too small to calculate the scaling factor.")
    }
    s <- sum(d) / norm_Y_sq  # 正确的缩放因子计算公式
  } else {
    s <- 1
  }

  # 7. 应用变换
  Y_scaled <- s * Y_centered
  Y_aligned <- Y_scaled + matrix(colMeans(X), n, p, byrow = TRUE)

  rotation = compute_rotation_angle(X,Y_aligned);
  Y_aligned = rotate_polygon_back(Y_aligned, rotation$angle, rotation$centroid_A, rotation$centroid_B)

  # 8. 计算Procrustes统计量
  ss <- sum((X_centered - Y_aligned)^2)

  # 计算拟合优度
  norm_X <- sqrt(sum(X_centered^2))
  correlation <- sum(d) / (norm_X * sqrt(sum(Y_scaled^2)))

  # 返回结果
  return(list(
    Y_aligned = Y_aligned,
    rotation = rotation$rotation_matrix,
    angle = rotation$angle,
    scale = s,
    translation = colMeans(X),
    procrustes_ss = ss,
    correlation = correlation
  ))
}

# 旋转角度计算函数
# A: 原多边形矩阵（n x 2）
# B: 旋转后多边形矩阵（n x 2）
compute_rotation_angle <- function(A, B) {


  # 计算重心
  centroid_A <- colMeans(A)
  centroid_B <- colMeans(B)

  # 中心化点集
  A_centered <- A - matrix(centroid_A, nrow = nrow(A), ncol = 2, byrow = TRUE)
  B_centered <- B - matrix(centroid_B, nrow = nrow(B), ncol = 2, byrow = TRUE)

  # 方法1：使用更稳健的角度计算方法
  # 计算每个点相对于重心的角度差
  angles_A <- atan2(A_centered[,2], A_centered[,1])
  angles_B <- atan2(B_centered[,2], B_centered[,1])

  # 计算角度差异（考虑周期性问题）
  angle_diffs <- angles_B - angles_A
  angle_diffs <- (angle_diffs + pi) %% (2 * pi) - pi  # 归一化到[-pi, pi]

  # 使用中位数减少异常值影响
  theta_rad <- median(angle_diffs)
  theta_deg <- theta_rad * 180 / pi

  # 方法2：备用的协方差矩阵方法（更稳健的SVD处理）
  H <- t(A_centered) %*% B_centered
  svd_H <- svd(H)

  # 计算旋转矩阵（确保纯旋转）
  R <- svd_H$v %*% t(svd_H$u)

  # 强制行列式为1（确保是纯旋转，无反射）
  if (det(R) < 0) {
    # 如果行列式为负，调整以消除反射
    svd_H$v[,2] <- -svd_H$v[,2]
    R <- svd_H$v %*% t(svd_H$u)
  }

  # 从旋转矩阵提取角度（更稳健的方法）
  theta_from_matrix <- atan2(R[2,1], R[1,1])
  theta_deg_from_matrix <- theta_from_matrix * 180 / pi

  # 选择更一致的角度估计
  if (abs(theta_deg - theta_deg_from_matrix) > 90) {
    theta_deg <- theta_deg_from_matrix
  }

  return(list(angle = theta_deg,
              rotation_matrix = R,
              centroid_A = centroid_A,
              centroid_B = centroid_B))
}

# 多边形旋转还原函数
# B: 旋转后多边形
# theta_deg: 旋转角度（度）
# centroid_A: 原多边形重心
# centroid_B: 旋转后多边形重心
rotate_polygon_back <- function(B, theta_deg, centroid_A, centroid_B) {


  theta_rad <- theta_deg * pi / 180  # 逆旋转角度

  # 创建逆旋转矩阵
  R_inv <- matrix(c(cos(theta_rad), sin(theta_rad),
                    -sin(theta_rad), cos(theta_rad)), nrow = 2, byrow = TRUE)

  # 正确的还原步骤：
  # 1. 将B平移到原点（减去B的重心）
  # 2. 应用逆旋转
  # 3. 平移到A的重心位置
  B_centered <- B - matrix(centroid_B, nrow = nrow(B), ncol = 2, byrow = TRUE)
  A_centered_restored <- t(R_inv %*% t(B_centered))
  A_restored <- A_centered_restored + matrix(centroid_A, nrow = nrow(B), ncol = 2, byrow = TRUE)

  return(A_restored)
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
rotation_angle <- 45 * pi / 180  # 30度旋转
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
