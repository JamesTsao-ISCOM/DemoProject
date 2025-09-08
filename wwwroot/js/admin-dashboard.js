$(document).ready(() => {
  // CSRF Token 管理函數
  function getLatestToken() {
    return $("input[name=__RequestVerificationToken]").val() || 
           $("meta[name=__RequestVerificationToken]").attr("content");
  }

  function updateAjaxHeaders() {
    var token = getLatestToken();
    if (token) {
      $.ajaxSetup({
        beforeSend: function(xhr) {
          xhr.setRequestHeader("RequestVerificationToken", token);
        }
      });
    }
  }

  function ajaxWithToken(options) {
    updateAjaxHeaders();
    return $.ajax(options);
  }

  // 搜索種類變數
  let searchType = "all"; // 預設為名稱搜尋
  let pageNumber = 1; // 預設頁碼
  // 新增電影按鈕
  $("#addMovieButton").on("click", () => {
      // 清空表單和圖片預覽
      $("#createMovieForm")[0].reset();
      $("#addMovieImagePreview").hide();
      $("#editMovieImagePreview").hide();
      $("#AddYTURLPreview").attr("src", "#");
      $("#AddYTURLPreview").hide();
      $("#EditYTURLPreview").attr("src", "#");
      $("#EditYTURLPreview").hide();
      $("#addMovieModal").modal("show");
  });
  // 處理圖片預覽 - 新增電影表單
  $("#addMovieImage").on("change", function() {
      const file = this.files[0];
      if (file) {
          const reader = new FileReader();
          reader.onload = function(e) {
              $("#addMovieImagePreview").attr("src", e.target.result).show();
          }
          reader.readAsDataURL(file);
      } else {
          $("#addMovieImagePreview").hide();
      }
  });
  // 處理圖片預覽 - 編輯電影表單
  $("#editMovieImage").on("change", function() {
      const file = this.files[0];
      if (!file) {
          $("#error-dialog-message").text("請上傳電影圖片。");
          $("#errorMessageModal").modal("show");
          return;
      }
      if(file.size == 0){
         $("#error-dialog-message").text("圖片大小不可為0KB");
         $("#errorMessageModal").modal("show");
          return;
      }
      if (file) {
          const reader = new FileReader();
          reader.onload = function(e) {
              $("#editMovieImagePreview").attr("src", e.target.result).show();
          }
          reader.readAsDataURL(file);
      } else {
          $("#editMovieImagePreview").hide();
      }
  });
  // 處理 YouTube URL 解析和預覽更新的共通函數
  function updateYouTubePreview(url, previewSelector) {

    let videoId = "";
    try {
        const urlObj = new URL(url);
        // YouTube 正常 URL
        if (urlObj.hostname.includes("youtube.com")) {
            videoId = urlObj.searchParams.get("v");
        }
        // YouTube 短網址 youtu.be
        else if (urlObj.hostname.includes("youtu.be")) {
            videoId = urlObj.pathname.substring(1);
        }
    } catch (e) {
        console.warn("無效的網址", e);
    }
    
    if (videoId) {
        $(previewSelector)
            .attr("src", `https://www.youtube.com/embed/${videoId}`)
            .show();
    } else {
        $(previewSelector).hide();
    }
    return videoId;
  }

  // 新增預告片YT URL
  $("#addMovieYTUrl").on("change", function() {
    const url = $(this).val();
    updateYouTubePreview(url, "#AddYTURLPreview");
  });
  
  // 綁定編輯表單中的事件處理函數
  function bindEditFormEvents() {
    // 編輯預告片YT URL
    $("#editMovieYTUrl").off("change").on("change", function() {
      const url = $(this).val();
      updateYouTubePreview(url, "#EditYTURLPreview");
    });
    
    // 處理編輯表單中的圖片預覽
    $("#editMovieImage").off("change").on("change", function() {
      const file = this.files[0];
      if (file) {
          const reader = new FileReader();
          reader.onload = function(e) {
              $("#editMovieImagePreview").attr("src", e.target.result).show();
          }
          reader.readAsDataURL(file);
      }
    });
    
    // 處理編輯表單提交
    $("#updateMovieForm").off("submit").on("submit", function(e) {
      e.preventDefault();
      const movieId = $(this).data("movie-id");
      console.log("更新電影提交", movieId);
      var formData = new FormData(this);
      ajaxWithToken({
        url: `/Movies/Edit/${movieId}`,
        type: "POST",
        data: formData,
        processData: false,
        contentType: false,
        success: function(html) {
          $("#editMovieModal").modal("hide");
          // 更新畫面上的電影卡片
          $(`[data-id="${movieId}"]`).closest(".col").replaceWith(html);
        },
        error: function(xhr) {
          alert(xhr.responseJSON?.message || "更新失敗");
        }
      });
    });
  }
  // 新增電影表單提交
  $("#createMovieForm").on("submit", function(e) {
      e.preventDefault(); // 阻止預設行為

      let file = $("#addMovieImage")[0].files[0];
      if (!file) {
          $("#error-dialog-message").text("請上傳電影圖片。");
          $("#errorMessageModal").modal("show");
          return;
      }
      if(file.size == 0){
         $("#error-dialog-message").text("圖片大小不可為0KB");
         $("#errorMessageModal").modal("show");
          return;
      }
      var formData = new FormData(this);
      ajaxWithToken({
        url:"/Movies/Create",
        type:"POST",
        data:formData,
        processData: false, // 不處理資料
        contentType: false, // 不設置 contentType
        success: function (html) {
          console.log("新增電影成功", html);
          $("#addMovieModal").modal("hide");
          $("#movieCards .col").last().remove();
          $("#movieCards").prepend(html); // 直接加到列表
          $("#createMovieForm")[0].reset(); // 清空表單
          $("#addMovieImagePreview").hide(); // 清空預覽圖片
        },
        error: function (xhr) {
            alert(xhr.responseJSON?.message || "新增失敗");
        }
      });
   });
  // 編輯電影按鈕點擊，載入電影資料
  $("#movieList").on("click", ".btn-edit", function() {
    const movieId = $(this).data("id");
    $.ajax({
        url: `/Movies/Edit/${movieId}`,
        type: "GET",
        success: function (data) {
          // 將電影資料填入編輯表單
          $("#editMovieModal .modal-content").html(data);
          
          // 初始化 YouTube URL 和圖片預覽
          const ytUrl = $("#editMovieYTUrl").val();
          if (ytUrl) {
            updateYouTubePreview(ytUrl, "#EditYTURLPreview");
          }
          
          // 如果有圖片，顯示圖片預覽
          const imgSrc = $("#editMovieImagePreview").attr("src");
          if (imgSrc && imgSrc !== "#") {
            $("#editMovieImagePreview").show();
          }
          
          // 重新綁定事件處理
          bindEditFormEvents();
        },
        error: function (xhr) {
          alert(xhr.responseJSON?.message || "載入失敗");
        }
    });
    
    // 顯示編輯對話框
    $("#editMovieModal").modal("show");
   });
     // 綁定刪除按鈕
  $("#movieList").on("click", ".btn-delete", function() {
    const movieId = $(this).data("id");
    $("#confirmDeleteBtn").data("id", movieId);
    $.ajax({
      url: `/Movies/Details/${movieId}`,
      type: "GET",
      success: function(response) {
        $("#delete-dialog-message").text(`確定要刪除這部電影《${response.data.title}》嗎？`);
        $("#deleteMovieModal").modal("show");
      },
      error: function(xhr) {
        alert(xhr.responseJSON?.message || "載入失敗");
      }
    });
  });
  
  // 綁定確認刪除按鈕
  $("#confirmDeleteBtn").on("click", function() {
    const movieId = $(this).data("id");
    ajaxWithToken({
      url: `/Movies/Delete/${movieId}`,
      type: "POST",
      data: { id: movieId },
      success: function(response) {
        $("#deleteMovieModal").modal("hide");
        // 找到對應的電影卡片並移除
        $(`[data-id="${movieId}"]`).closest(".col").remove();
      },
      error: function(xhr) {
        alert(xhr.responseJSON?.message || "刪除失敗");
      }
    });
  });
  // Tabs Content block add/remove class
  $("#searchByName").on("click", function() {
    console.log("searchByName");
    $(".tab-pane").removeClass("show active");
    $(".nav-link").removeClass("active");
    $("#searchByName").addClass("active");
    $("#searchByNameContent").addClass("show active");
  });
  $("#searchByType").on("click", function() {
    console.log("searchByType");
    $(".tab-pane").removeClass("show active");
    $(".nav-link").removeClass("active");
    $("#searchByType").addClass("active");
    $("#searchByTypeContent").addClass("show active");
  });
  $("#searchByDate").on("click", function() {
    console.log("searchByDate");
    $(".tab-pane").removeClass("show active");
    $(".nav-link").removeClass("active");
    $("#searchByDate").addClass("active");
    $("#searchByDateContent").addClass("show active");
  });
  // reset Page
  function resetPage() {
    pageNumber = 1;
    $(".page-item").removeClass("active");
    $(".page-item[data-id='1']").addClass("active");
  }
  // search by name
  $("#btn-searchByName").on("click", function() {
    const query = $("#searchByNameContent input").val();
    searchType = "name";
    resetPage();
    $("#searchTitle").text(`電影列表 - 名稱搜尋: ${query}`);
    $.ajax({
      url: "/Movies/Search",
      type: "GET",
      data: { title: query, pageNumber: pageNumber },
      success: function (response) {
        console.log(response);
        // 更新電影列表
        $("#movieList").html(response);
      },
      error: function (xhr) {
        alert(xhr.responseJSON?.message || "搜尋失敗");
      }
    });
  });
  // search by type
  $("#btn-searchByType").on("click", function() {
    const type = $("#searchByTypeContent select").val();
    $("#searchTitle").text(`電影列表 - 類型搜尋: ${type}`);
    searchType = "type";
    resetPage();
    $.ajax({
      url: "/Movies/SearchByType",
      type: "GET",
      data: { type: type, pageNumber: pageNumber },
      success: function (response) {
        console.log(response);
        // 更新電影列表
        $("#movieList").html(response);
      },
      error: function (xhr) {
        alert(xhr.responseJSON?.message || "搜尋失敗");
      }
    });
  });
  // search by date
  $("#btn-searchByDate").on("click", function() {
    const startDate = $("#startDate").val();
    const endDate = $("#endDate").val();
    searchType = "date";
    resetPage();
    $("#searchTitle").text(`電影列表 - 日期搜尋: ${startDate} ~ ${endDate}`);
    $.ajax({
      url: "/Movies/SearchByDate",
      type: "GET",
      data: { startDate: startDate, endDate: endDate, pageNumber: pageNumber },
      success: function (response) {
        console.log(response);
        // 更新電影列表
        $("#movieList").html(response);
      },
      error: function (xhr) {
        alert(xhr.responseJSON?.message || "搜尋失敗");
      }
    });
  });
  $(document).on("click", ".page-link", function() {
    pageNumber = $(this).data("id");
    console.log(pageNumber ,searchType);
    if (!pageNumber) return;
    switch (searchType) {
      case "all":
        $.ajax({
          url: "/Movies/GetPage",
          type: "GET",
          data: { pageNumber: pageNumber, pageSize: 9 },
          success: function (response) {
            $("#movieList").html(response);
          },
          error: function (xhr) {
            alert(xhr.responseJSON?.message || "搜尋失敗");
          }
        });
        break;
      case "name":
        $.ajax({
          url: "/Movies/Search",
          type: "GET",
          data: { title: $("#searchByNameContent input").val(), pageNumber: pageNumber },
          success: function (response) {
            $("#movieList").html(response);
          },
          error: function (xhr) {
            alert(xhr.responseJSON?.message || "搜尋失敗");
          }
        });
        break;
      case "type":
        $.ajax({
          url: "/Movies/SearchByType",
          type: "GET",
          data: { type: $("#searchByTypeContent select").val(), pageNumber: pageNumber },
          success: function (response) {
            $("#movieList").html(response);
          },
          error: function (xhr) {
            alert(xhr.responseJSON?.message || "搜尋失敗");
          }
        });
        break;
      case "date":
        $.ajax({
          url: "/Movies/SearchByDate",
          type: "GET",
          data: { startDate: $("#startDate").val(), endDate: $("#endDate").val(), pageNumber: pageNumber },
          success: function (response) {
            $("#movieList").html(response);
          },
          error: function (xhr) {
            alert(xhr.responseJSON?.message || "搜尋失敗");
          }
        });
        break;
      default:
        break;
    }
  });
});


// 確保 modal 可以正常捲動
document.addEventListener('DOMContentLoaded', function() {
  // 解決可能的捲動問題
  $('.modal').on('shown.bs.modal', function () {
    $(this).find('.modal-dialog-scrollable .modal-body').css({
      'overflow-y': 'auto',
      'max-height': 'calc(100vh - 200px)'
    });
    
    // 強制重新計算布局
    setTimeout(function() {
      $('.modal-dialog-scrollable .modal-body').scrollTop(1).scrollTop(0);
    }, 10);
  });  
  // 解決iOS設備上的問題
  if (/iPhone|iPad|iPod/.test(navigator.userAgent)) {
    $('.modal-body').css('-webkit-overflow-scrolling', 'touch');
  }
});