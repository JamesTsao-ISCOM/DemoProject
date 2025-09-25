$(document).ready(function() {
    let selectedPaymentMethod = null;

    // Toggle selection for Cash payment option
    $('#paymentCashOption').on('click', function() {
        $(this).toggleClass('selected');
        if ($(this).hasClass('selected')) {
            $(this).find('i').removeClass('bi-bank').addClass('bi-check-circle-fill');
            selectedPaymentMethod = 'cash';
        } else {
            $(this).find('i').removeClass('bi-check-circle-fill').addClass('bi-bank');
        }
    });

    $("#leaseStartDate").on("change", function() {
        const selectedDate = $(this).val();
        const selectedTime = $("#leaseStartTime").val();
        console.log(selectedDate, selectedTime);
        if (selectedDate && selectedTime) {
            const startDateTime = dayjs(`${selectedDate} ${selectedTime}`, 'YYYY-MM-DD HH:mm');
            const endDateTime = startDateTime.add(3, 'day');
            $("#selectedDateTime").text(startDateTime.format('YYYY-MM-DD HH:mm'));
            $("#endDateTime").text(endDateTime.format('YYYY-MM-DD HH:mm'));
        }
    });
    $("#leaseStartTime").on("change", function() {
        const selectedTime = $(this).val();
        const selectedDate = $("#leaseStartDate").val();
        console.log(selectedDate, selectedTime);
        if (selectedDate && selectedTime) {
            const startDateTime = dayjs(`${selectedDate} ${selectedTime}`, 'YYYY-MM-DD HH:mm');
            const endDateTime = startDateTime.add(3, 'day');
            $("#selectedDateTime").text(startDateTime.format('YYYY-MM-DD HH:mm'));
            $("#endDateTime").text(endDateTime.format('YYYY-MM-DD HH:mm'));
        }   
    });

    $('#leaseForm').on('submit', function(e) {
        e.preventDefault();
        const selectedTime = $("#leaseStartTime").val();
        const selectedDate = $("#leaseStartDate").val();

        if (!selectedPaymentMethod) {
            alert('請選擇付款方式');
            return;
        }

        if (!selectedDate || !selectedTime) {
            alert('請選擇租借開始日期和時間');
            return;
        }

        const formData = new FormData(this);
        formData.append("payment_method", selectedPaymentMethod);
        
        // 使用 dayjs 來獲取和格式化日期時間
        const leaseDateTime = dayjs(`${selectedDate} ${selectedTime}`).format('YYYY-MM-DD HH:mm:ss');
        const returnDateTime = dayjs(`${selectedDate} ${selectedTime}`).add(3, 'day').format('YYYY-MM-DD HH:mm:ss');
        
        formData.append("LeaseDate", leaseDateTime);
        formData.append("ReturnDate", returnDateTime);
        
        // 調試：顯示所有表單數據
        for(let pair of formData.entries()) {
            console.log(pair[0]+ ': ' + pair[1]); 
        }

        $.ajax({
            url: '/Leases/Create',
            type: 'POST',
            data: formData, // 直接傳送 FormData，不要 JSON.stringify
            processData: false, // 重要：不要處理數據
            contentType: false, // 重要：讓瀏覽器自動設定 Content-Type
            success: function(response) {
                if (response.success) {
                    alert('租借成功！');
                    // 重導向到首頁
                    window.location.href = '/';
                } else {
                    alert('租借失敗：' + response.message);
                    if(response.message == "請先登入"){
                        window.location.href = '/Account/Login';
                    }
                }
            },
            error: function(xhr, status, error) {
                console.log('Error details:', xhr.responseText);
                console.log('Status:', status);
                console.log('Error:', error);
                
                // 更詳細的錯誤處理
                if (xhr.status === 401) {
                    alert('請先登入才能租借電影');
                    window.location.href = '/Account/Login';
                } else if (xhr.status === 404) {
                    alert('找不到指定的電影');
                } else {
                    try {
                        const errorResponse = JSON.parse(xhr.responseText);
                        alert('租借失敗：' + errorResponse.message);
                    } catch (e) {
                        alert('租借失敗：請稍後再試');
                    }
                }
            }
        });
    });
});