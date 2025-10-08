# 加载所需包
library(ggplot2)

# 修正后的旋转角度计算函数
compute_rotation_angle_corrected <- function(A, B) {
  # A: 原多边形矩阵（n x 2）
  # B: 旋转后多边形矩阵（n x 2）

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

# 修正后的多边形还原函数
restore_polygon_corrected <- function(B, theta_deg, centroid_A, centroid_B) {
  # B: 旋转后多边形
  # theta_deg: 旋转角度（度）
  # centroid_A: 原多边形重心
  # centroid_B: 旋转后多边形重心

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

# 生成更复杂的测试数据（不规则多边形）
set.seed(123)
A <- matrix(c(0,0, 2,0, 3,1, 2,2, 1,2, 0,1), ncol = 2, byrow = TRUE)

# 添加随机扰动使多边形更真实
A <- A + matrix(rnorm(nrow(A)*2, sd=0.05), ncol=2)

# 定义旋转角度
theta_true <- 45  # 真实旋转角度（度）
theta_rad <- theta_true * pi / 180

# 创建旋转矩阵
R_true <- matrix(c(cos(theta_rad), -sin(theta_rad),
                 sin(theta_rad), cos(theta_rad)), nrow = 2, byrow = TRUE)

# 应用旋转
centroid_A <- colMeans(A)
A_centered <- A - matrix(centroid_A, nrow = nrow(A), ncol = 2, byrow = TRUE)
B_centered <- t(R_true %*% t(A_centered))
B <- B_centered + matrix(centroid_A, nrow = nrow(A), ncol = 2, byrow = TRUE)

# 计算旋转角度
result <- compute_rotation_angle_corrected(A, B)
theta_computed <- result$angle
cat("真实旋转角度:", theta_true, "度\n")
cat("计算出的旋转角度:", round(theta_computed, 2), "度\n")
cat("角度误差:", round(abs(theta_computed - theta_true), 2), "度\n")

# 还原多边形
A_restored <- restore_polygon_corrected(B, theta_computed, result$centroid_A, result$centroid_B)

# 计算还原误差
restoration_error <- mean(sqrt(rowSums((A - A_restored)^2)))
cat("平均还原误差:", round(restoration_error, 6), "\n")

# 可视化验证
df_original <- data.frame(x = A[,1], y = A[,2], group = "Original")
df_rotated <- data.frame(x = B[,1], y = B[,2], group = "Rotated")
df_restored <- data.frame(x = A_restored[,1], y = A_restored[,2], group = "Restored")

df <- rbind(df_original, df_rotated, df_restored)
df$group <- factor(df$group, levels = c("Original", "Rotated", "Restored"))

# 创建可视化
p <- ggplot(df, aes(x = x, y = y, color = group, linetype = group)) +
  geom_polygon(fill = NA, linewidth = 1.2, alpha = 0.7) +
  geom_point(size = 3) +
  labs(title = "多边形旋转验证（修正版）",
       subtitle = paste("真实角度:", theta_true, "度 | 计算角度:", round(theta_computed, 2), "度 | 误差:", round(restoration_error, 6)),
       x = "X", y = "Y") +
  scale_color_manual(values = c("Original" = "black", "Rotated" = "red", "Restored" = "blue")) +
  scale_linetype_manual(values = c("Original" = "solid", "Rotated" = "dashed", "Restored" = "dotted")) +
  theme_minimal() +
  coord_fixed() +
  theme(legend.position = "bottom")

print(p)

# 额外验证：检查顶点间距离
cat("\n顶点间距离验证:\n")
for (i in 1:nrow(A)) {
  dist_orig <- sqrt(sum((A[i,] - A_restored[i,])^2))
  cat(paste0("顶点", i, " 还原误差: ", round(dist_orig, 6), "\n"))
}
