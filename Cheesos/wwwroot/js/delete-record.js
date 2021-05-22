function deleteRecord(title, msg, url)
{
    swal({
        title: title,
        text: msg,
        icon: "warning",
        dangerMode: true,
        buttons: true
    }).then((willDelete) =>
    {
        if (willDelete)
        {
            $.ajax({
                type: "DELETE",
                url: url,
                success: function (data)
                {
                    if (data.success)
                    {
                        toastr.success(data.message);
                        dataTable.ajax.reload();
                    }
                    else
                    {
                        toastr.error(data.message);
                    }
                }
            });
        }
    });
}