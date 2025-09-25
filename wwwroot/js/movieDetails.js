$(document).ready(function () {
    // 星級監聽 - hover 效果
    $(document).on("mouseenter", ".rating-star", function () {
        const value = $(this).data("value");
        // 先清除所有星星的 active 狀態
        $(this).parent().find('.rating-star').removeClass("active");
        
        // 然後為當前星星及其之前的星星添加 active 狀態
        for (let i = 1; i <= value; i++) {
            $(this).parent().find(`.rating-star[data-value="${i}"]`).addClass("active");
        }
    });

    $(document).on("mouseleave", ".star-rating", function () {
        // 當滑鼠離開星級容器時，移除所有 active 狀態
        $(this).find('.rating-star').removeClass("active");
    });
    // 星級監聽 - 點擊評分
    $(document).on("click", ".rating-star", function () {
        $(document).off("mouseenter", ".rating-star");
        $(document).off("mouseleave", ".star-rating");
        // 取得點擊的星星數值
        const value = $(this).data("value");
        console.log("Selected rating:", value);
        $(this).parent().find('.rating-star').removeClass("bi bi-star-fill active").addClass("bi-star");
        // 然後為當前星星及其之前的星星添加 active 狀態
        for (let i = 1; i <= value; i++) {
            $(this).parent().find(`.rating-star[data-value="${i}"]`).removeClass("bi-star").addClass("bi bi-star-fill active");
        }
    });
});