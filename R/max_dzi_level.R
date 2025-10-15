const max_dzi_level = function(slide_file) {
    require(HDS);

    let s = HDS::read_stream(slide_file);
    let index = HDS::getText(s,"/index.txt") |> .Internal::trim() |> basename();
    let levels = list.dirs(`/${index[1]}_files/`, fs = s) 
    |> basename() 
    |> as.integer()
    ;

    close(s);

    return(max(levels));
}