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
    
});