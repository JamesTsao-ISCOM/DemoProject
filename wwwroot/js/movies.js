$(document).ready(function () {
    // 分頁按鈕點擊事件
    $('.pagination').on('click', '.page-link', function (e) {
        e.preventDefault();
        var page = $(this).data('id');
        console.log(page);
        
        const url = new URL(window.location.href);
        url.searchParams.set("pageNumber", page);
        window.location.href = url; // 會重新載入
    });
});