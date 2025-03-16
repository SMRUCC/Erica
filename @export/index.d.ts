// export R# source type define for javascript/typescript language
//
// package_source=Erica

declare namespace Erica {
   module _ {
      /**
      */
      function onLoad(): object;
   }
   /**
     * @param color default value Is ``paper``.
   */
   function __spot_class(cell_layers: any, color?: any): object;
   /**
     * @param n_layers default value Is ``4``.
     * @param top_genes default value Is ``1000``.
     * @param alpha default value Is ``2``.
     * @param beta default value Is ``0.5``.
     * @param iteration default value Is ``150``.
     * @param prefix default value Is ``class``.
     * @param make_gene_filters default value Is ``true``.
     * @param filter_range default value Is ``[0.05, 0.95]``.
     * @param unify_scale default value Is ``10``.
     * @param log_norm default value Is ``true``.
     * @param n_threads default value Is ``null``.
   */
   function deconv_spatial(expr_mat: any, n_layers?: any, top_genes?: any, alpha?: any, beta?: any, iteration?: any, prefix?: any, make_gene_filters?: any, filter_range?: any, unify_scale?: any, log_norm?: any, n_threads?: any): object;
   module human_genes {
      /**
        * @param human_genes default value Is ``null``.
        * @param index default value Is ``Bgee``.
      */
      function annotations(human_genes?: any, index?: any): object;
   }
   module load {
      /**
      */
      function human_genes(): object;
   }
   /**
     * @param colors default value Is ``paper``.
     * @param title default value Is ``Spatial Spot Class``.
     * @param padding default value Is ``padding: 150px 350px 200px 200px;``.
   */
   function plot_class(spots_class: any, colors?: any, title?: any, padding?: any): object;
   /**
     * @param x default value Is ``null``.
     * @param y default value Is ``null``.
   */
   function plot_pie(cell_layers: any, x?: any, y?: any): object;
   /**
   */
   function plot_velocity(x: any, y: any, vx: any, vy: any, class: any): object;
   /**
     * @param dims default value Is ``9``.
     * @param max_iters default value Is ``1000``.
     * @param n_threads default value Is ``8``.
   */
   function single_nmf(x: any, dims?: any, max_iters?: any, n_threads?: any): object;
   /**
   */
   function UniProtKeywords(): object;
   /**
     * @param dims default value Is ``9``.
     * @param batch_size default value Is ``100``.
     * @param iterations default value Is ``1000``.
   */
   function vae_embedding(x: any, dims?: any, batch_size?: any, iterations?: any): object;
}
