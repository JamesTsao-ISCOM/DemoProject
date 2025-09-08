$(document).ready(function () {
    // 全局設定 AJAX 防偽造令牌
    // 設置所有 AJAX 請求的防偽造令牌
    $("#btn-addAdminModal").click(function () {
        console.log("Add Admin button clicked");
        $("#addAdminModal").modal("show");
    });
    $("#btn-uploadFileModal").click(function () {
        console.log("Upload File button clicked");
        $("#uploadFileModal").modal("show");
    });
    $("#btn-exportFileModal").click(function () {
        console.log("Export File button clicked");
        $("#downloadFileModal").modal("show");
    });
    function bindEditAdminForm(){
       $("#editAdminForm").off("submit").on("submit", function (e) {
        e.preventDefault();
        var id = $(this).data("admin-id");
        var form = new FormData(this);
        // 🔑 每次請求前重新獲取防偽造令牌
        var token = $('input[name="__RequestVerificationToken"]').val();
        $.ajax({
            type: "PUT",
            url: "/Admin/Edit/" + id,
            data: form,
            processData: false,
            contentType: false,
            headers: {
                'RequestVerificationToken': token
            },
            success: function (response) {
                $("#editAdminModal").modal("hide");
                console.log("response", response);
                $("tr[data-id='" + id + "']").replaceWith(response);
            },
            error: function (xhr, status, error) {
                // 嘗試從響應中獲取錯誤消息
                try {
                    // 如果伺服器返回的是 JSON 格式
                    var errorData = JSON.parse(xhr.responseText);
                    if (errorData && errorData.message) {
                        $("#errorModalMessage").text(errorData.message);
                    } else {
                        // 如果伺服器直接返回字符串錯誤消息
                        $("#errorModalMessage").text(xhr.responseText);
                    }
                } catch (e) {
                    // 如果無法解析 JSON，直接使用原始響應文本
                    $("#errorModalMessage").text(xhr.responseText || "編輯管理員失敗，請稍後再試。");
                }
                
                $("#errorModal").modal("show");
            }
        });
       });
    }
    $("#staffList").on("click", ".btn-edit-staff", function () {
        var button = $(this);
        var id = button.data("id");
        var modal = $("#editAdminModal");
        $.ajax({
            type: "GET",
            url: "/Admin/Edit/" + id,
            success: function (response) {
                modal.find(".modal-content").html(response);
                modal.modal("show");
                bindEditAdminForm();
            },
            error: function (xhr, status, error) {
                console.log("Status:", xhr.status);
                console.log("Response:", xhr.responseText);
                $("#errorModalMessage").text("編輯管理員失敗，請稍後再試。");
                $("#errorModal").modal("show");
            }
        });
    });
    $("#staffList").on("click", ".btn-delete-staff", function () {
        var button = $(this);
        var username = button.data("username");
        var modal = $("#deleteAdminModal");
        $("#btn-delete-admin").data("admin-id", button.data("id"));
        modal.find("#deleteAdminWarningMessage").text("您確定要刪除 " + username + " 嗎？");
        modal.modal("show");
    });
    $("#staffList").on("click", ".btn-reset-password", function () {
        var button = $(this);
        var username = button.data("username");
        var modal = $("#resetPasswordModal");
        $("#btn-resetPasswordConfirm").data("admin-id", button.data("id"));
        modal.find("#resetPasswordWarningMessage").text("您確定要重設 " + username + " 的密碼嗎？");
        modal.modal("show");
    });
    $("#createAdminForm").on("submit", function (e) {
        e.preventDefault();
        var form = new FormData(this);
        // 🔑 每次請求前重新獲取防偽造令牌
        var token = $('input[name="__RequestVerificationToken"]').val();
        $.ajax({
            type: "POST",
            url: "/Admin/Add",
            data: form,
            processData: false,
            contentType: false,
            headers: {
                'RequestVerificationToken': token
            },
            success: function (response) {
                console.log("Admin created successfully");
                $("#addAdminModal").modal("hide");
                $("#adminTable").append(response);
            },
            error: function (xhr, status, error) {
                try {
                    // 如果伺服器返回的是 JSON 格式
                    var errorData = JSON.parse(xhr.responseText);
                    if (errorData && errorData.message) {
                        $("#errorModalMessage").text(errorData.message);
                    } else {
                        // 如果伺服器直接返回字符串錯誤消息
                        $("#errorModalMessage").text(xhr.responseText);
                    }
                } catch (e) {
                    // 如果無法解析 JSON，直接使用原始響應文本
                    $("#errorModalMessage").text(xhr.responseText || "新增管理員失敗，請稍後再試。");
                }
                $("#errorModal").modal("show");
            }
        });
    });
    $("#btn-delete-admin").click(function () {
        var button = $(this);
        var id = button.data("admin-id");
        // 🔑 每次請求前重新獲取防偽造令牌
        var token = $('input[name="__RequestVerificationToken"]').val();
        $.ajax({
            type: "DELETE",
            url: "/Admin/Delete/" + id,
            headers: {
                'RequestVerificationToken': token
            },
            success: function (response) {
                $("#deleteAdminModal").modal("hide");
                $("tr[data-id='" + id + "']").remove();
            },
            error: function (xhr, status, error) {
                console.log("Status:", xhr.status);
                console.log("Response:", xhr.responseText);
                $("#errorModalMessage").text("刪除管理員失敗，請稍後再試。");
                $("#errorModal").modal("show");
            }
        });
    });
    $("#btn-resetPasswordConfirm").click(function () {
        $("#resetPasswordModal").modal("hide");
        var button = $(this);
        var id = button.data("admin-id");
        $.ajax({
            type: "POST",
            url: "/Admin/ResetPassword/" + id,
            success: function (response) {
                $("#resetPasswordModal").modal("hide");
                alert("密碼已重設為預設值: '1234'");
            },
            error: function (xhr, status, error) {
                try {
                    // 如果伺服器返回的是 JSON 格式
                    var errorData = JSON.parse(xhr.responseText);
                    if (errorData && errorData.message) {
                        $("#errorModalMessage").text(errorData.message);
                    } else {
                        // 如果伺服器直接返回字符串錯誤消息
                        $("#errorModalMessage").text(xhr.responseText);
                    }
                } catch (e) {
                    // 如果無法解析 JSON，直接使用原始響應文本
                    $("#errorModalMessage").text(xhr.responseText || "重設密碼失敗，請稍後再試。");
                }
                $("#errorModal").modal("show");
            }
        });
    });
    $("#btn-downloadFileTemplate").click(function () {
        window.location.href = "/Admin/DownloadEmptyExcel";
    });
    $("#btn-download-file").click(function () {
        const selectedFormat = $("#fileFormatSelect").val();
        $("#downloadFileModal").modal("hide");
        $("#downloadingFileModal").modal("show");
        setTimeout(function () {
            $("#downloadingFileModal").modal("hide");
        }, 3000);
        if (!selectedFormat) {
            alert("請選擇一個檔案格式");
            return;
        }
        if (selectedFormat === "xlsx") {
            window.location.href = "/Admin/ExportXlsx";
        } 
        else if (selectedFormat === "word") {
            window.location.href = "/Admin/ExportWord";
        } 
        else if (selectedFormat === "pdf") {
            window.location.href = "/Admin/ExportPdf";
        }
    });
    $("#btn-close-importFileErrorModal").click(function () {
        $("#importFileErrorModal").modal("hide");
        location.reload(); // 重新載入頁面以顯示更新後的資料
    });
    $("#uploadStaffForm").on("submit",function(e){
        e.preventDefault();
        console.log("Upload form submitted");
        
        // 使用 jQuery 選擇器找到文件輸入框
        const fileInput = $(this).find("input[type=file]")[0];
        if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
            $("#errorModalMessage").text("請選擇一個有效的檔案");
            $("#errorModal").modal("show");
            return;
        }
      
        const file = fileInput.files[0];
        if (file.size === 0) {
            $("#errorModalMessage").text("檔案內容不能為空");
            $("#errorModal").modal("show");
            return;
        }
        console.log("Selected file:", file);
        
        // 顯示載入中
        $("#btn-upload-file").prop("disabled", true)
        .html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> 處理中...');
        
        // 直接使用表單提交，這是最可靠的檔案上傳方式
        var formData = new FormData(this);
        
        // 檢查 FormData 內容
        console.log("FormData 內容:");
        for (var pair of formData.entries()) {
            console.log(pair[0] + ': ' + (pair[1] instanceof File ? pair[1].name : pair[1]));
        }
        $.ajax({
            url: "/Admin/ImportExcelByBatch",
            type: "POST",
            data: formData,
            processData: false,  // 不處理數據
            contentType: false,  // 不設置內容類型，讓瀏覽器自動設置正確的 boundary
            xhr: function() {
                var xhr = $.ajaxSettings.xhr();
                console.log("XHR created");
                return xhr;
            },
            beforeSend: function(xhr) {
                console.log("Before send");
            },
            success: function (response) {
                console.log("File uploaded successfully");
                console.log("Response:", response);
                $("#uploadFileModal").modal("hide");
                $("#btn-upload-file").prop("disabled", false).text("上傳");
                if(response.errors.length==0 && response.successCount > 0){
                    $("#importFileErrorModalLabel").html("匯入成功");
                    $("#importFileErrorModalMessage")
                    .html(`<strong>成功匯入 ${response.successCount} 筆資料，沒有失敗的資料。</strong>`);
                    $("#importFileErrorModal").modal("show");
                }
                else if (response && response.successCount > 0) {
                    $("#importFileErrorModalMessage").html(`
                        <strong>成功匯入 ${response.successCount} 筆資料，失敗 ${response.errors.length} 筆。</strong>
                        <p>錯誤資料如下:</p>
                        <ul>
                            ${response.errors.map(item => `
                                <li>${item}</li>
                            `).join("\n")}
                        </ul>
                    `);
                    $("#importFileErrorModal").modal("show");
                } 
                else {
                   $("#importFileErrorModalMessage").html(`
                        <strong>沒有資料匯入，失敗 ${response.errors.length} 筆。</strong><br />
                        <p>錯誤資料如下:</p>
                        <ul>
                            ${response.errors.map(item => `
                                <li>${item}</li>
                            `).join("\n")}
                        </ul>
                    `);
                    $("#importFileErrorModal").modal("show");
                }
            },
            error: function (xhr, status, error) {
                $("#btn-upload-file").prop("disabled", false).text("上傳");
                console.log("XHR Status:", xhr.status);
                console.log("Error:", error);
                console.log("Response Text:", xhr.responseText);
                
                let errorMsg = "檔案上傳失敗，請稍後再試。";
                try {
                    const errorData = JSON.parse(xhr.responseText);
                    if (errorData && errorData.message) {
                        errorMsg = errorData.message;
                    }
                } catch (e) {
                    // 如果不是 JSON 格式，直接顯示回應文本
                    if (xhr.responseText) {
                        errorMsg = xhr.responseText;
                    }
                }
                
                $("#errorModalMessage").text(errorMsg);
                $("#errorModal").modal("show");
            }
        });
    });
});