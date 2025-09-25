$(document).ready(function() {
    $.ajax({
        url: "/Leases/GetStatistics",
        method:"GET",
        success: function(data) {
            $("#pendingCount").text(data.data.pendingLeases);
            $("#activeCount").text(data.data.activeLeases);
            $("#completedCount").text(data.data.completedLeases);
            $("#cancelledCount").text(data.data.cancelledLeases);
        },
        error: function(err) {
            console.error("Error fetching lease statistics:", err);
        }
    });
    $("#resetFiltersBtn").click(function() {
        $("select[name='status']").val("-1");
        $("input[name='leaseId']").val("");
        $("input[name='memberName']").val("");
        $("input[name='leaseDate']").val("");
        $("#searchLeaseForm").submit();
    });
    $(document).find(".btn-approve").on("click", function() {
        const leaseId = $(this).data("lease-id");
        if(!leaseId) return;
        if(!confirm("確定要批准此租借嗎？")) return;
        $.ajax({
            url: "/Leases/UpdateStatus/" + leaseId,
            method: "PUT",
            data: { leaseId: leaseId, status: 1 },
            success: function(response) {
                if(response.success) {
                    alert("租借已批准");
                    location.reload();
                } else {
                    alert("操作失敗: " + response.message);
                }
            },
            error: function(err) {
                console.error("Error updating lease status:", err);
                alert("操作失敗，請稍後再試");
            }
        });

    });
    $(document).find(".btn-cancel").on("click", function() {
        const leaseId = $(this).data("lease-id");
        if(!leaseId) return;
        if(!confirm("確定要取消此租借嗎？")) return;
        $.ajax({
            url: "/Leases/UpdateStatus/" + leaseId,
            method: "PUT",
            data: { leaseId: leaseId, status: 3 },
            success: function(response) {
                if(response.success) {
                    alert("租借已取消");
                    location.reload();
                } else {
                    alert("操作失敗: " + response.message);
                }
            },
            error: function(err) {
                console.error("Error updating lease status:", err);
                alert("操作失敗，請稍後再試");
            }
        });
    });
    $(document).find(".btn-complete").on("click", function() {
        const leaseId = $(this).data("lease-id");
        if(!leaseId) return;
        if(!confirm("確定要將此租借標記為完成嗎？")) return;
        $.ajax({
            url: "/Leases/UpdateStatus/" + leaseId,
            method: "PUT",
            data: { leaseId: leaseId, status: 2 },
            success: function(response) {
                if(response.success) {
                    alert("租借已完成");
                    location.reload();
                } else {
                    alert("操作失敗: " + response.message);
                }
            },
            error: function(err) {
                console.error("Error updating lease status:", err);
                alert("操作失敗，請稍後再試");
            }
        });
    });
});