// export R# package module type define for javascript/typescript language
//
//    imports "GRN" from "Erica";
//
// ref=Erica.GRN@Erica, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

/**
 * 
*/
declare namespace GRN {
   /**
     * @param maxIters default value Is ``500``.
   */
   function learn_grn(velocity_prior: object, prior: object, maxIters?: object): object;
   /**
    * 
    * 
     * @param monocle3 -
     * @param dbn_sample -
     * @param hvgenes -
     * @param method 分箱模式："bins"（等宽分箱，默认）或 "sliding"（滑动窗口）。
     * 
     * + default value Is ``'bins'``.
     * @param num_bins 等宽分箱数量（method="bins" 时有效）。默认 30。
     * 
     * + default value Is ``30``.
     * @param window_size 滑动窗口宽度（method="sliding" 时有效）。默认 5。
     * 
     * + default value Is ``5``.
     * @param step 滑动窗口步长（method="sliding" 时有效）。默认 1。
     * 
     * + default value Is ``1``.
     * @param gene_selection 基因筛选方式："top"（取速度幅度最高的 topGeneFraction 比例）或 "threshold"（速度幅度 > velocityThreshold）。默认 "top"。
     * 
     * + default value Is ``'top'``.
     * @param top_gene_fraction top 模式下保留的基因比例（0~1）。默认 0.3。
     * 
     * + default value Is ``0.3``.
     * @param velocity_thres threshold 模式的绝对阈值；设为 NaN 时自动取速度幅度中位数 × 2。
     * 
     * + default value Is ``NaN``.
     * @param discretize 是否对 bin 表达矩阵做分位数离散化（供离散 DBN）。默认 False。
     * 
     * + default value Is ``false``.
     * @param num_levels 离散化等级数（discretize=True 时有效）。默认 3。
     * 
     * + default value Is ``3``.
     * @param groupBy 分支标签（每细胞所属 group）；为 Nothing 时按整体单轨迹分箱。预留（本次不启用分支）。
     * 
     * + default value Is ``null``.
   */
   function make_sample(monocle3: object, dbn_sample: object, hvgenes: any, method?: string, num_bins?: object, window_size?: object, step?: object, gene_selection?: string, top_gene_fraction?: number, velocity_thres?: number, discretize?: boolean, num_levels?: object, groupBy?: object): object;
   /**
   */
   function merge_prior(velocity_prior: object, prior: object): object;
   /**
    * get time series expression matrix from processed DBN sample output, for GRN learning
    * 
    * 
     * @param x -
   */
   function time_series(x: object): object;
}
