$(document).ready(function () {
    $(document).find("#button-cancel-lease").on("click", function() {
        const leaseId = $(this).data("lease-id");
        $.ajax({
            url:"/Leases/CancelLease/" + leaseId,
            method: "PUT",
            success: function(response) {
                if(response.success) {
                    alert("租借已取消");
                    location.reload();
                }
                else {
                    alert("操作失敗: " + response.message);
                }
            },
            error: function(err) {
                alert("系統錯誤，請稍後再試");
            }
        });
    });
});