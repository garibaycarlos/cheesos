let dataTable;

$(document).ready(function ()
{
    loadDataTable();
});

function loadDataTable()
{
    dataTable = $('#tblData').DataTable({
        ajax: {
            url: "/admin/category/getall",
            type: "GET",
            datatype: "json"
        },
        dom: 'Blfrtip',
        columns: [
            { data: "name" },
            {
                data: "id",
                render: function (data, type, row)
                {
                    let detailsAction = `/Admin/Category/Details?id=${data}`;
                    let editAction = `/Admin/Category/Edit?id=${data}`;
                    let deleteAction = `deleteRecord('Cheesos - Category', 'Are you sure you want to delete this category?', '/Admin/Category/Delete?id=${data}');`;

                    return `<div class="text-center">
                                <a class="btn btn-primary text-white" href="${detailsAction}" style="text-decoration:none;" title="Details">
                                    <i class="fas fa-list-alt"></i>
                                </a>
                                &nbsp;
                                <a class="btn btn-success text-white" href="${editAction}" style="text-decoration:none;" title="Edit">
                                    <i class="fas fa-edit"></i>
                                </a>
                                &nbsp;
                                <a class="btn btn-danger text-white" style="cursor:pointer;" title="Delete" onclick="${deleteAction}">
                                    <i class="fas fa-trash-alt"></i>
                                </a>
                            </div>`;
                },
                orderable: false
            }
        ],
        language: {
            emptyTable: "No data to display"
        },
        width: "100%"
    });
}