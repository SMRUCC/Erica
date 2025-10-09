#' Ordinary Procrustes Analysis (OPA) for Matrix Alignment
#'
#' This function performs Ordinary Procrustes Analysis (OPA) to align two matrices by finding the optimal translation, rotation,
#' and scaling that minimizes the sum of squared differences between them. It is commonly used in shape analysis, morphometrics,
#' and image registration.
#'
#' @param X A numeric matrix representing the target/reference matrix. Each row is a sample, and each column is a dimension.
#' @param Y A numeric matrix representing the source matrix to be aligned to X. Must have the same dimensions as X.
#' @param scale Logical. If TRUE (default), scaling is applied to Y during alignment. If FALSE, only rotation and translation are used.
#'
#' @return A list containing the following components:
#' \item{aligned}{The aligned version of Y after applying Procrustes transformation.}
#' \item{rotation}{The rotation matrix (orthogonal) applied to Y.}
#' \item{angle}{The rotation angle in radians (if applicable).}
#' \item{scale}{The scaling factor applied to Y.}
#' \item{translation}{The translation vector applied to Y (centroid of X).}
#' \item{procrustes_ss}{The Procrustes sum of squared differences between X and the aligned Y.}
#' \item{correlation}{The correlation-like measure of alignment quality (higher values indicate better alignment).}
#'
#' @details
#' The Procrustes analysis involves three steps:
#' \enumerate{
#'   \item **Centering**: Remove the centroids of X and Y by column-wise mean subtraction.
#'   \item **Scaling and Rotation**: Compute the optimal scaling factor and rotation matrix using singular value decomposition (SVD) of the covariance matrix between X and Y.
#'   \item **Translation**: Shift the aligned Y to the centroid of X.
#' }
#' The function also computes a goodness-of-fit statistic (`correlation`) based on the sum of singular values from the SVD decomposition.
#'
#' @note
#' The input matrices X and Y must have the same dimensions. The function uses the Frobenius norm to compute the scaling factor
#' when `scale = TRUE`. If the norm of Y is too small, the function will stop with an error. The rotation matrix is computed using
#' SVD, ensuring orthogonality.
#'
#' @examples
#' # Create two simple 2D point sets
#' X <- matrix(c(1, 2, 3, 4, 5, 6), ncol = 2)
#' Y <- matrix(c(1.1, 2.1, 3.1, 4.1, 5.1, 6.1), ncol = 2)
#' result <- ordinary_procrustes(X, Y)
#' print(result$correlation) # Alignment quality
#' plot(result$aligned, col = "red", pch = 19)
#' points(X, col = "blue", pch = 17)
#' legend("topleft", legend = c("Aligned Y", "Target X"), col = c("red", "blue"), pch = c(19, 17))
#'
#' @references
#' For theoretical details, see:
#' \itemize{
#'   \item Procrustes analysis: https://en.wikipedia.org/wiki/Procrustes_analysis
#'   \item SVD-based alignment: https://en.wikipedia.org/wiki/Orthogonal_Procrustes_problem
#' }
#'
#' @seealso
#' Related functions: \code{\link[vegan]{procrustes}} (vegan package), \code{\link[shape]{procGPA}} (shape package).
#'
#' @export
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
    aligned = Y_aligned,
    rotation = rotation$rotation_matrix,
    angle = rotation$angle,
    scale = s,
    translation = colMeans(X),
    procrustes_ss = ss,
    correlation = correlation
  ))
}

#' Compute Rotation Angle Between Two Polygons
#'
#' This function calculates the rotation angle and rotation matrix between two polygons (or sets of 2D points) by comparing their point-wise orientations relative to their centroids or using Singular Value Decomposition (SVD).
#'
#' @param A A numeric matrix with dimensions n x 2 representing the original polygon coordinates. Each row corresponds to a point, with two columns for x and y coordinates.
#' @param B A numeric matrix with dimensions n x 2 representing the rotated polygon coordinates. It must have the same number of points as matrix A.
#'
#' @return A list containing the following components:
#' \itemize{
#'   \item \code{angle}: The estimated rotation angle in degrees from polygon A to polygon B.
#'   \item \code{rotation_matrix}: The 2x2 rotation matrix corresponding to the computed angle.
#'   \item \code{centroid_A}: The centroid (center of mass) of the original polygon A as a numeric vector of length 2.
#'   \item \code{centroid_B}: The centroid (center of mass) of the rotated polygon B as a numeric vector of length 2.
#' }
#'
#' @details
#' This function employs two independent methods to estimate the rotation angle between two aligned point sets:
#' \enumerate{
#'   \item \strong{Angle Difference Method}: Computes the angle for each point relative to the centroid of its polygon using \code{atan2}, then calculates the median difference of these angles. This approach is robust to outliers through the use of median aggregation.
#'   \item \strong{SVD-Based Method}: Uses Singular Value Decomposition (SVD) of the covariance matrix to derive the optimal rotation matrix. This method ensures a proper rotation by enforcing a determinant of 1, avoiding reflections.
#' }
#' The function automatically selects the most consistent angle estimate between the two methods. If the difference between their estimates exceeds 90 degrees, the SVD-based result is preferred due to its global optimization properties.
#'
#' @note
#' Important considerations for using this function:
#' \itemize{
#'   \item The two input matrices A and B must have the same number of points (rows) and correspond point-by-point.
#'   \item The function assumes that the polygons are already centered and that only rotation (not translation or scaling) differentiates them.
#'   \item For the SVD method, the rotation matrix is calculated as R = V * U^T after decomposing H = U * D * V^T, which is a standard approach for orthogonal Procrustes analysis.
#' }
#'
#' @examples
#' # Create a simple square polygon
#' square_A <- matrix(c(0, 0, 1, 0, 1, 1, 0, 1), ncol = 2, byrow = TRUE)
#'
#' # Define a 45-degree rotation matrix
#' theta <- 45 * pi / 180
#' R <- matrix(c(cos(theta), -sin(theta), sin(theta), cos(theta)), nrow = 2)
#'
#' # Apply rotation to create polygon B
#' square_B <- square_A %*% t(R)
#'
#' # Compute rotation angle between original and rotated square
#' result <- compute_rotation_angle(square_A, square_B)
#' print(paste("Rotation angle:", result$angle, "degrees"))
#'
#' # Verify the rotation matrix
#' print(result$rotation_matrix)
#'
#' @seealso
#' Related topics:
#' \itemize{
#'   \item \code{\link[base]{atan2}} for angle calculations
#'   \item \code{\link[base]{svd}} for singular value decomposition
#'   \item \code{\link[stats]{median}} for robust averaging
#' }
#'
#' @author Your Name <your.email@example.com>
#'
#' @export
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

#' Reverse rotation of a polygon
#'
#' This function restores a rotated polygon to its original position by applying an inverse rotation transformation.
#' The process involves three main steps: translating the rotated polygon to the origin (subtracting its centroid),
#' applying the inverse rotation matrix, and then translating it back to the original polygon's centroid position.
#'
#' @param B A numeric matrix representing the vertices of the rotated polygon. Each row corresponds to a vertex (x, y coordinates).
#' @param theta_deg The rotation angle in degrees (positive for counterclockwise rotation) that was originally applied.
#' @param centroid_A A numeric vector of length 2 specifying the centroid (x, y) of the original polygon before rotation.
#' @param centroid_B A numeric vector of length 2 specifying the centroid (x, y) of the rotated polygon.
#'
#' @return A numeric matrix with the same dimensions as `B`, containing the coordinates of the polygon vertices after restoring to the original position.
#'
#' @examples
#' # Create a simple square polygon
#' original_square <- matrix(c(0, 0, 1, 0, 1, 1, 0, 1), ncol = 2, byrow = TRUE)
#' centroid_orig <- c(0.5, 0.5) # Centroid of the original square
#'
#' # Suppose the square was rotated by 45 degrees around its centroid
#' theta <- 45
#' # In practice, the rotated polygon and its centroid would come from a rotation operation
#' # For demonstration, we'll use the original polygon as if it were rotated
#' rotated_square <- original_square
#' centroid_rot <- centroid_orig
#'
#' # Restore the polygon to its original position
#' restored_square <- rotate_polygon_back(rotated_square, theta, centroid_orig, centroid_rot)
#'
#' @export
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

#' Debug Print for Ordinary Procrustes Analysis Results
#'
#' This function provides a detailed debug printout of the results from an ordinary Procrustes analysis.
#' It displays key alignment parameters such as rotation matrix, scale factor, translation vector,
#' and Procrustes statistic, along with calculated metrics like the average alignment error between
#' the aligned shape and the base polygon.
#'
#' @param X A numeric matrix representing the base polygon vertices for alignment.
#'   Each row should correspond to a vertex coordinate.
#' @param result A list containing the output of an ordinary Procrustes analysis. The list should
#'   include the following components:
#'   \describe{
#'     \item{aligned}{Numeric matrix of the aligned polygon vertices}
#'     \item{rotation}{Numeric matrix representing the rotation applied}
#'     \item{angle}{Numeric value of the rotation angle in radians (optional)}
#'     \item{scale}{Numeric value of the scale factor applied}
#'     \item{translation}{Numeric vector [x, y] of the translation applied}
#'     \item{procrustes_ss}{Numeric value of the Procrustes sum of squares statistic}
#'     \item{correlation}{Numeric value of the correlation coefficient (optional)}
#'   }
#'
#' @return This function does not return a value explicitly. It prints the analysis results to the
#'   console and invisibly returns `NULL`.
#'
#' @examples
#' # Create a base polygon matrix (e.g., a square)
#' X <- matrix(c(0, 0, 1, 0, 1, 1, 0, 1), ncol = 2, byrow = TRUE)
#'
#' # Simulate a Procrustes analysis result list
#' result_sim <- list(
#'   aligned = matrix(c(0.1, 0.1, 1.1, 0.1, 1.1, 1.1, 0.1, 1.1), ncol = 2, byrow = TRUE),
#'   rotation = matrix(c(0.99, -0.01, 0.01, 0.99), ncol = 2),
#'   scale = 1.02,
#'   translation = c(0.1, 0.1),
#'   procrustes_ss = 0.05,
#'   correlation = 0.98
#' )
#'
#' # Print the debug information
#' debug_print(X, result_sim)
#'
#' @export
debug_print = function(X, result = list(aligned = NULL,       # the aligned polygon matrix
                                        rotation = NULL,      # rotation matrix
                                        angle = NULL,         # rotation angle
                                        scale = NULL,         # scale factor
                                        translation = NULL,   # translation [x,y]
                                        procrustes_ss = NULL, # procrustes stat score
                                        correlation = NULL    # correlation
)) {
  n_vertices = nrow(result$aligned);

  # 打印结果
  cat("=== 多边形形状对齐，普氏分析结果 ===\n")
  cat("顶点数量:", n_vertices, "\n")
  cat("Procrustes统计量 (M²):", round(result$procrustes_ss, 4), "\n")
  cat("缩放因子:", round(result$scale, 4), "\n")
  cat("旋转矩阵:\n")
  print(round(result$rotation, 4))
  cat("平移向量:", round(result$translation, 4), "\n")

  # 计算对齐误差
  alignment_error <- sqrt(mean(rowSums((result$aligned - X)^2)))
  cat("平均对齐误差:", round(alignment_error, 4), "\n")
}

#' Visualization of Ordinary Procrustes Analysis for 2D Polygon Alignment
#'
#' This function creates a comparative visualization of polygon alignment using Ordinary Procrustes Analysis (OPA).
#' It plots three polygons: the base reference polygon, the target polygon before alignment, and the target polygon after alignment.
#' The visualization includes points representing vertices, polygon fills with transparency, and vertex labels for clear comparison.
#'
#' @param X A numeric matrix or data frame with two columns representing the base reference polygon's vertex coordinates (x, y).
#'           It serves as the fixed reference for alignment.
#' @param target A numeric matrix or data frame with two columns representing the target polygon's vertex coordinates (x, y) before alignment.
#'               Must have the same number of vertices as `X`.
#' @param aligned A numeric matrix or data frame with two columns representing the aligned polygon's vertex coordinates (x, y) after Procrustes analysis.
#'                Must have the same number of vertices as `X` and `target`.
#'
#' @return A ggplot object displaying the alignment results. The plot includes:
#' \itemize{
#'   \item Points for each vertex, colored and shaped by polygon type
#'   \item Polygon outlines and semi-transparent fills
#'   \item Vertex labels showing the point IDs
#'   \item Equal coordinate aspect ratio to preserve shape proportions
#' }
#' The function also prints the plot to the active graphics device.
#'
#' @examples
#' \dontrun{
#' # Create example polygons: a triangle and a transformed version
#' n_vertices <- 3
#' X <- matrix(c(0,0, 1,0, 0.5,1), ncol=2, byrow=TRUE)
#' target <- matrix(c(0.1,0.1, 1.1,0.1, 0.6,1.1), ncol=2, byrow=TRUE)
#' aligned <- matrix(c(0.05,0.05, 0.95,0.05, 0.5,0.95), ncol=2, byrow=TRUE)
#'
#' # Generate visualization
#' polygon_alignment_visual(X, target, aligned)
#' }
#'
#' @import ggplot2
#' @import ggforce
#' @export
polygon_alignment_visual = function(X, target, aligned) {
  library(ggplot2)
  library(ggforce)

  # 准备绘图数据
  shape_data <- data.frame(
    x = c(X[,1], target[,1], aligned[,1]),
    y = c(X[,2], target[,2], aligned[,2]),
    shape_type = rep(c("基准形状", "变形形状", "对齐后形状"), each = n_vertices),
    point_id = rep(1:n_vertices, 3)
  )

  # 创建连线数据（用于显示多边形边）
  line_data <- data.frame(
    x = c(X[,1], target[,1], aligned[,1]),
    y = c(X[,2], target[,2], aligned[,2]),
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
    labs(title = "多边形形状对齐，普氏分析结果",
         subtitle = paste("展示基准多边形形状（", n_vertices, "个顶点）、变形形状和对齐后形状的对比"),
         x = "X坐标", y = "Y坐标",
         color = "形状类型", shape = "形状类型") +
    theme_minimal() +
    theme(legend.position = "bottom") +
    coord_equal()  # 确保比例一致，保持形状不变形

  print(plt)
}

demo = function() {
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

  debug_print(airplane_X, result);
  polygon_alignment_visual(airplane_X, airplane_Y_raw, result$aligned);
}

demo();
