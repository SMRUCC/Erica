// export R# package module type define for javascript/typescript language
//
//    imports "singleCell" from "Erica";
//
// ref=Erica.singleCells@Erica, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

/**
 * single cell data toolkit
 * 
*/
declare namespace singleCell {
   /**
   */
   function dzi_unmix(dzi: object, level: object, dir: object, IHC_antibody: object, export_dir: string): any;
   /**
    * Extract the gene id set with non-missing expression status under 
    *  the given threshold value **`q`**.
    * 
    * 
     * @param raw -
     * @param q the percentage threshold value for assign a gene a missing feature
     * 
     * + default value Is ``0.2``.
   */
   function expression_list(raw: object, q?: number): object;
   /**
   */
   function geo_transform(cells: object, transform: object): object;
   /**
     * @param knn default value Is ``16``.
     * @param maxDistance default value Is ``500``.
     * @param distanceWeight default value Is ``0.7``.
     * @param morphologyWeight default value Is ``0.3``.
   */
   function greedy_matches(sliceA: object, sliceB: object, knn?: object, maxDistance?: number, distanceWeight?: number, morphologyWeight?: number): object;
   /**
    * scan the cells from a given HE image
    * 
    * 
     * @param HEstain the HE image, should be a grayscale bitmap image.
     * @param flip -
     * 
     * + default value Is ``false``.
     * @param ostu_factor -
     * 
     * + default value Is ``0.7``.
     * @param offset -
     * 
     * + default value Is ``null``.
     * @param noise -
     * 
     * + default value Is ``0.25``.
     * @param moran_knn -
     * 
     * + default value Is ``32``.
     * @param split_blocks 
     * + default value Is ``false``.
     * @param env -
     * 
     * + default value Is ``null``.
   */
   function HE_cells(HEstain: any, flip?: boolean, ostu_factor?: number, offset?: any, noise?: number, moran_knn?: object, split_blocks?: boolean, env?: object): object;
   /**
    * extract the raw expression data matrix from the h5ad object
    * 
    * 
     * @param h5ad -
     * @param env -
     * 
     * + default value Is ``null``.
   */
   function HTS_matrix(h5ad: any, env?: object): object;
   /**
   */
   function hungarian_assignment(phase1: object, phase2: object): object;
   /**
    * unmix IHC stained pixel color into the corresponding antibody expression values
    * 
    * 
     * @param pixel [r,g,b] color value, in range [0,1]
     * @param IHC_antibody a tuple list of the antibody reference colors
     * 
     * + default value Is ``null``.
     * @param env 
     * + default value Is ``null``.
   */
   function ihc_unmixing(pixel: any, IHC_antibody?: object, env?: object): any;
   /**
    * Parse the dzi image metadata from a given xml document data
    * 
    * 
     * @param xml -
   */
   function parse_dziImage(xml: string): object;
   /**
    * Extract the PCA matrix from h5ad
    * 
    * 
     * @param h5ad -
   */
   function pca_annotation(h5ad: object): object;
   /**
     * @param iterations default value Is ``1000``.
     * @param distance_threshold default value Is ``0.1``.
   */
   function RANSAC_cell_alignment(sliceA: object, sliceB: object, iterations?: object, distance_threshold?: number): object;
   module read {
      /**
       * read the csv table file as cells data matrix
       * 
       * 
        * @param file -
        * @param env -
        * 
        * + default value Is ``null``.
      */
      function cells(file: any, env?: object): object|object;
      /**
       * read the deepzoom image metadata xml file
       * 
       * 
        * @param file -
      */
      function dziImage(file: string): object;
      /**
       * read h5ad object from a specific hdf5 file
       * 
       * > this function only works on Windows platform.
       * 
        * @param h5adfile The file path to the h5ad rawdata file
        * @param loadExpr0 -
        * 
        * + default value Is ``true``.
      */
      function h5ad(h5adfile: string, loadExpr0?: boolean): object;
      /**
       * Read expression matrix from a TCGA MTX dataset folder
       * 
       * 
        * @param dataset a folder path to the TCGA MTX dataset files, this folder should includes the data files:
        *  1. barcodes.tsv
        *  2. features.tsv
        *  3. matrix.mtx
        * @return a clr @``T:Microsoft.VisualBasic.Data.Framework.DataFrame`` object that contains all information that read from the given TCGA dataset folder.
      */
      function TCGA_mtx(dataset: string): object;
   }
   module scan {
      /**
       * Scan all single cell shapes from the given dzi slide data
       * 
       * 
        * @param dzi metadata of the dzi image
        * @param level usually be the max zoom level
        * @param dir A directory path that contains the image files in current **`level`**.
        * @param ostu_factor -
        * 
        * + default value Is ``0.7``.
        * @param noise quantile level for filter the polygon shape points. all cell shapes which has its
        *  shape points less than this quantile level will be treated as noise
        * 
        * + default value Is ``0.25``.
        * @param moran_knn -
        * 
        * + default value Is ``32``.
        * @param flip 
        * + default value Is ``false``.
        * @param split_blocks -
        * 
        * + default value Is ``false``.
        * @param split_rgb 
        * + default value Is ``false``.
        * @param IHC_antibody 
        * + default value Is ``null``.
        * @param env 
        * + default value Is ``null``.
        * @return if scanning of the IHC1 channels, then this function will returns a tuple list that contains
        *  the rgb channels single cell detections result of the IHC1 image.
        *  if scanning of the IHC2 channels, then this function will returns a tuple list that contains
        *  the cmyk channels single cell detection result of the IHC2 image.
      */
      function dzi_cells(dzi: object, level: object, dir: object, ostu_factor?: number, noise?: number, moran_knn?: object, flip?: boolean, split_blocks?: boolean, split_rgb?: boolean, IHC_antibody?: object, env?: object): object|object;
   }
   /**
    * Create spatial annotations data set for each spot data
    * 
    * 
     * @param x X of the spot dataset, a numeric vector
     * @param y Y of the spot dataset, a numeric vector
     * @param label A character vector that assign the class label of each
     *  spatial spot, the vector size of this parameter must be equals to the 
     *  vector size of x and y.
     * @param colors A character value of the spatial class color 
     *  palette name or a vector of color code character for assign each 
     *  spatial spot.
     * 
     * + default value Is ``'paper'``.
     * @param barcode the raw reference barcode to the spots
     * 
     * + default value Is ``null``.
     * @param env -
     * 
     * + default value Is ``null``.
   */
   function spatial_annotations(x: any, y: any, label: any, colors?: any, barcode?: any, env?: object): object;
   /**
    * export the spatial maps data
    * 
    * 
     * @param h5ad -
     * @param useCellAnnotation -
     * 
     * + default value Is ``null``.
     * @param env 
     * + default value Is ``null``.
   */
   function spatialMap(h5ad: object, useCellAnnotation?: object, env?: object): object;
   /**
    * Extract the UMAP matrix from h5ad
    * 
    * 
     * @param h5ad -
   */
   function umap_annotation(h5ad: object): object;
   module write {
      /**
       * write the cell matrix data into a bson file
       * 
       * 
        * @param cells a vector of the cell objects
        * @param file file path to the bson file for save the cell matrix data
      */
      function cells_bson(cells: object, file: string): ;
   }
}
