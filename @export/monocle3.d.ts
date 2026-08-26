// export R# package module type define for javascript/typescript language
//
//    imports "monocle3" from "Erica";
//
// ref=Erica.monocle3Tool@Erica, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

/**
 * 
*/
declare namespace monocle3 {
   /**
   */
   function cell_rank(x: object, opts: object): object;
   /**
   */
   function dbn_sample(matrix: object, hvgenes: string): object;
   /**
   */
   function hvgenes(x: object): string;
   /**
   */
   function learn_grn(velocity_prior: object, prior: object): object;
   /**
    * 
    * 
     * @param monocle3 -
     * @param dbn_sample -
     * @param hvgenes -
     * @param method 分箱模式："bins"（等宽分箱，默认）或 "sliding"（滑动窗口）。
     * 
     * + default value Is ``'bins'``.
     * @param numBins 等宽分箱数量（method="bins" 时有效）。默认 30。
     * 
     * + default value Is ``30``.
     * @param windowSize 滑动窗口宽度（method="sliding" 时有效）。默认 5。
     * 
     * + default value Is ``5``.
     * @param step 滑动窗口步长（method="sliding" 时有效）。默认 1。
     * 
     * + default value Is ``1``.
     * @param geneSelection 基因筛选方式："top"（取速度幅度最高的 topGeneFraction 比例）或 "threshold"（速度幅度 > velocityThreshold）。默认 "top"。
     * 
     * + default value Is ``'top'``.
     * @param topGeneFraction top 模式下保留的基因比例（0~1）。默认 0.3。
     * 
     * + default value Is ``0.3``.
     * @param velocityThreshold threshold 模式的绝对阈值；设为 NaN 时自动取速度幅度中位数 × 2。
     * 
     * + default value Is ``NaN``.
     * @param discretize 是否对 bin 表达矩阵做分位数离散化（供离散 DBN）。默认 False。
     * 
     * + default value Is ``false``.
     * @param numLevels 离散化等级数（discretize=True 时有效）。默认 3。
     * 
     * + default value Is ``3``.
     * @param groupBy 分支标签（每细胞所属 group）；为 Nothing 时按整体单轨迹分箱。预留（本次不启用分支）。
     * 
     * + default value Is ``null``.
   */
   function make_sample(monocle3: object, dbn_sample: object, hvgenes: object, method?: string, numBins?: object, windowSize?: object, step?: object, geneSelection?: string, topGeneFraction?: number, velocityThreshold?: number, discretize?: boolean, numLevels?: object, groupBy?: object): object;
   /**
   */
   function merge_prior(velocity_prior: object, prior: object): object;
   /**
     * @param numPCA default value Is ``10``.
     * @param umapDim default value Is ``3``.
     * @param knnK default value Is ``15``.
     * @param resolution default value Is ``1``.
     * @param useLeiden default value Is ``false``.
     * @param useCache default value Is ``true``.
     * @param overwriteCache default value Is ``false``.
     * @param cacheDir default value Is ``'./cache'``.
     * @param pseudoVeloEnabled default value Is ``true``.
     * @param pseudoVeloWindow default value Is ``2``.
     * @param pseudoVeloSpan default value Is ``0.3``.
     * @param useVelocityProjection default value Is ``true``.
     * @param numHVGenes default value Is ``3000``.
   */
   function new(numPCA?: object, umapDim?: object, knnK?: object, resolution?: number, useLeiden?: boolean, useCache?: boolean, overwriteCache?: boolean, cacheDir?: string, pseudoVeloEnabled?: boolean, pseudoVeloWindow?: object, pseudoVeloSpan?: number, useVelocityProjection?: boolean, numHVGenes?: object): object;
}
