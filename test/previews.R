setwd(@dir);

require(JSON);

options(strict = FALSE);

data = "small_parts.json" |> readText |> json_decode;

print(data);
