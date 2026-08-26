// export R# package module type define for javascript/typescript language
//
//    imports "monocle3" from "Erica";
//
// ref=Erica.monocle3Tool@Erica, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

/**
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
     * @param num_HVgenes default value Is ``3000``.
   */
   function new(numPCA?: object, umapDim?: object, knnK?: object, resolution?: number, useLeiden?: boolean, useCache?: boolean, overwriteCache?: boolean, cacheDir?: string, pseudoVeloEnabled?: boolean, pseudoVeloWindow?: object, pseudoVeloSpan?: number, useVelocityProjection?: boolean, num_HVgenes?: object): object;
}
