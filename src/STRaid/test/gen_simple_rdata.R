# Generate simple R objects for testing the RData parser
# without Seurat dependencies

out_dir <- "G:/Erica/src/STRaid/test/data"

# Test 1: Simple list
simple_list <- list(a = 1:5, b = c("hello", "world"), c = TRUE)
saveRDS(simple_list, file = file.path(out_dir, "simple_list.rds"))
save(simple_list, file = file.path(out_dir, "simple_list.rda"))

# Test 2: Data frame
simple_df <- data.frame(
  x = 1:10,
  y = rnorm(10),
  z = letters[1:10],
  stringsAsFactors = FALSE
)
saveRDS(simple_df, file = file.path(out_dir, "simple_df.rds"))
save(simple_df, file = file.path(out_dir, "simple_df.rda"))

cat("Simple test data generated.\n")
cat("Files created in:", out_dir, "\n")
