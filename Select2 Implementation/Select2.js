
//Apply on search bar
$('#searchitem').select2({
    ajax: {
       url: '@Url.Action("GetProductForSearch", "Requests")',
       delay: 250,
       data: function (params) {
         
           console.log(params);
           var query = {
                search: params.term,
                page: params.page || 1
           }
           SearchItem = params.term;

            // Query parameters will be ?search=[term]&type=public
            return query;
       },
       cache: true
   },
   minimumInputLength: 2,
   width: '100%',
   templateResult: formatRepo,

   templateSelection: formatRepoSelection
});


//for result
function formatRepo(repo) {

    if (repo.loading) {
        return repo.text;
    }

    var $container = $(
        `<div class="select2-result-repository clearfix d-flex">
            <div class="select2-result-repository__avatar mr-3" style="min-width: 66px; max-width: 66px;">
                <img src="${repo.imageSrc}" class="img-thumbnail" />
            </div>
            <div class="select2-result-repository__title"></div>
        </div>`
    );

    $container.find(".select2-result-repository__title").text(repo.text);

    return $container;
}


//After selection in search box appear
function formatRepoSelection(repo) {
    productId = repo.id;
    return SearchItem;
}


//Perform action on select drop down item from select2 result
$('#searchitem').on('select2:select', function (params) {
    
    $.ajax({
        url: '@Url.Action("FetchSearchedItemDetails", "Requests")',
        data: { productIds: productId },
        success: function (data) {
         
            $("#Searchitemlist").animate({ scrollTop: 0 }, "fast");
            $("#Searchitemlist").empty();
            $("#Searchitemlist").html(data);
            console.log(data);
            $('#dvSearchlist').removeClass("none");
            $("#searchlist_spiner").removeClass("none").addClass("none");
            $(".please_wait").removeClass("none").addClass("none");
            var TotalCount = $('#TotalSearchCount').val();
            var nextpage = $('#nextpagenumber').val();
            var previouspage = $('#prevpagenumber').val();
            var NoOfpages = 61;
            if (nextpage != null && nextpage!="") {
                $('.next_div').removeClass("none");
            }
            else {
                $('.next_div').removeClass("none").addClass("none");
            }
            if (previouspage != null && previouspage!="") {
                $('.previous_div').removeClass("none");

            }
            else {
                $('.previous_div').removeClass("none").addClass("none");
            }

            $("#selectedItem").click();
        },
        error: function (data) {
        }
    });
});
