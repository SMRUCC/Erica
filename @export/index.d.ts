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
   */
   function deconv_spatial(expr_mat: any, n_layers?: any, top_genes?: any, alpha?: any, beta?: any, iteration?: any, prefix?: any): object;
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
   */
   function plot_class(spots_class: any, colors?: any): object;
   /**
   */
   function plot_pie(cell_layers: any): object;
   /**
   */
   function UniProtKeywords(): object;
}
