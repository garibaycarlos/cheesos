$(document).ready(function ()
{
    updateSubCategoryList();
});

function updateSubCategoryList()
{
    let categorySelected = document.getElementById("ddlCategoryList").value;

    $list = $('#SubCategoryList');

    $.ajax({
        url: '/Admin/SubCategory/GetSubCategory/' + categorySelected,
        type: 'GET',
        dataType: 'text',
        success: function (data)
        {
            results = JSON.parse(data);

            $list.html('');

            $list.append('<ul class="list-group">');

            for (let i in results)
            {
                $list.append('<li class="list-group-item">' + results[i].text + '</li>')
            }

            $list.append('</ul>')
        }
    });
}