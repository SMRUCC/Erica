require(GCModeller);
require(Erica);

imports "GPS" from "biosystem";
imports "bnlearn" from "biosystem";
imports "GRN" from "Erica";

let sort_data = readBin("K:\hsa_grn\hsa_rank\cell_rank.zip", what = "cell_rank");
let gps = GRN::time_series(sort_data) 
|> build_gps(NULL, config = gps_config(
    maxGenes = 0,
    cpmNormalize = TRUE,
    logTransform = TRUE,
    zScore = TRUE,
    peDim = 8,
    spdMaxDist = 4,
    coexpressionTopK = 0,
    minCoexpression = 0.7,
    dModel = 32,
    nLayers = 3,
    nHeads = 4,
    ffnDim = 64,
    dropout = 0.05,
    geneEmbedDim = 32,
    useSpdBias = TRUE,
    useGlobalAttention = TRUE,
    useLocalMpnn = TRUE,
    window = 4,
    gruHidden = 128,
    useUncertainty = FALSE,
    pretrainEpochs = 20,
    finetuneEpochs = 10,
    learningRate = 0.01,
    autofocusGamma = 1,
    lambdaDir = 0.5,
    lambdaL1 = 0.0001,
    lambdaPrior = 0,
    gradientClip = 1,
    earlyStopPatience = 4,
    maskRatio = 0.15,
    printEvery = 5,
    trainRatio = 0.6,
    validationRatio = 0.2,
    seed = 2024,
    significanceZScore = 0.5,
    cascadeSteps = 5    
))
|> train(checkpoint = "K:\hsa_grn\hsa_rank\grn_model.gps-checkpoiint.zip")
;

cat(attr(gps, "report"));

str(as.list(attr(gps,"metrics")));
str(as.list(attr(gps,"baseline")));
str(as.list(attr(gps,"training")));

writeBin(gps, con = "K:\hsa_grn\hsa_rank\grn_model.gps.zip");