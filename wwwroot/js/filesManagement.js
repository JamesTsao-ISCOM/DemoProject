$(document).ready(function () {
    //設定定時器
    let videoStatusInterval;
    // Bootstrap 5 modal helpers (replace jQuery .modal calls)
    function showModal(id) {
        const el = document.getElementById(id);
        if (!el || !window.bootstrap) return;
        window.bootstrap.Modal.getOrCreateInstance(el).show();
    }
    function hideModal(id) {
        const el = document.getElementById(id);
        if (!el || !window.bootstrap) return;
        window.bootstrap.Modal.getOrCreateInstance(el).hide();
    }
    let tempId = "";
    let selectedFiles = [];
    let sendFiles=[];
    let videoTempId = "";
    // 預覽選取的檔案
    $("#uploadFileForm input[type='file']").on("change", function (e) {
        const file = e.target.files[0];
        console.log(file);
        
        // 檢查檔案類型，拒絕影片檔案
        if (file) {
            const fileName = file.name.toLowerCase();
            const blockedExtensions = ['.mp4', '.avi', '.mov', '.wmv', '.flv', '.mkv', '.webm', '.m4v', '.3gp'];
            const hasBlockedExtension = blockedExtensions.some(ext => fileName.endsWith(ext));
            
            if (hasBlockedExtension) {
                $("#errorModalMessage").text("不支援上傳影片檔案，請選擇其他類型的檔案。");
                showModal("errorModal");
                // 清空檔案選擇
                $(this).val('');
                $("#input_FileName").val('');
                $("#file-preview-body").html(`
                    <tr>
                        <td colspan="3" class="text-center">尚未選擇檔案</td>
                    </tr>
                `);
                return;
            }
        }
        
        $("#input_FileName").val(file.name);
        const formData = new FormData();
        formData.append("file", file);
        $.ajax({
            url:"/Files/UploadTemp",
            type: "POST",
            data: formData,
            contentType: false,
            processData: false,
            success: function (response) {
                console.log("File uploaded successfully:", response);
                tempId = response.tempId; // Store the temporary ID
                let previewHtml = "";
                for (const contentfile of response.filesList) {
                    previewHtml += `
                    <tr>
                        <td>${contentfile.fileName}</td>
                        <td>${contentfile.fileType}</td>
                        <td>${dayjs(contentfile.lastModifiedDate).format('YYYY-MM-DD HH:mm:ss')}</td>
                    </tr>
                    `;
                }
                $("#file-preview-body").html(previewHtml);
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.error("File upload failed:", textStatus, errorThrown);
                
        $("#errorModalMessage").text("檔案上傳失敗，請稍後再試。");
        showModal("errorModal");
            }
        });
    });
    // 確認上傳
    $("#uploadFileForm").on("submit", function (e) {
        e.preventDefault();
        const formData = new FormData(this);
        formData.append("tempId", tempId);
        console.log("Submitting form with data:", formData);
        hideModal("uploadFileModal");
        showModal("loadingModal");
        $("#loadingModalMessage").text("檔案上傳中，請稍候...");
        $.ajax({
            url: "/Files/ConfirmUpload",
            type: "POST",
            data: formData,
            contentType: false,
            processData: false,
            success: function (html) {
                console.log("File uploaded successfully:", html);
                $("#uploadFileForm")[0].reset();
                hideModal("uploadFileModal");
                $("#files-list").append(html);
                hideModal("loadingModal");
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.error("File upload failed:", textStatus, errorThrown);
                $("#errorModalMessage").text("檔案上傳失敗，請稍後再試。");
                showModal("errorModal");
                hideModal("loadingModal");
            }
        });
    });
    // 取消上傳
    $("#btn-cancel-upload").on("click", function () {
        $("#uploadFileForm")[0].reset();
        $("#file-preview-body").html(`
            <tr>
                <td colspan="3" class="text-center">尚未選擇檔案</td>
            </tr>
        `);
        $.ajax({
            url: "/Files/CancelUpload",
            type: "POST",
            data: { tempId: tempId },
            success: function (response) {
                console.log("Upload canceled successfully:", response);
                hideModal("uploadFileModal");
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.error("Cancel upload failed:", textStatus, errorThrown);
            }
        });
    });
    // 關閉上傳檔案 Modal
    $("#btn-closeUploadFileModal").on("click", function () {
        $("#uploadFileForm")[0].reset();
        $("#file-preview-body").html(`
            <tr>
                <td colspan="3" class="text-center">尚未選擇檔案</td>
            </tr>
        `);
        $.ajax({
            url: "/Files/CancelUpload",
            type: "POST",
            data: { tempId: tempId },
            success: function (response) {
                console.log("Upload canceled successfully:", response);
                hideModal("uploadFileModal");
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.error("Cancel upload failed:", textStatus, errorThrown);
                $("#errorModalMessage").text("檔案上傳取消失敗，請稍後再試。");
                showModal("errorModal");
            }
        });
    });
    // 開啟上傳影片檔案 Modal
    $("#btn-upload-video-file").on("click", function () {
        $("#uploadVideoFileModal").modal("show");
        $("#videoUploadProgressContainer").hide();
    });
    function getFileExtension(filename) {
        // 從字串末尾尋找最後一個點號的位置
        const lastDotIndex = filename.lastIndexOf('.');
        
        // 如果找不到點號或者點號是第一個字元（例如 '.bashrc'），則沒有副檔名
        if (lastDotIndex <= 0) {
            return '';
        }
        
        // 截取點號之後的所有字元，並轉為小寫
        return filename.substring(lastDotIndex + 1).toLowerCase();
    }
    // 上傳影片檔案 Modal
    $("#inputVideoFile").on("change", async function (e) {
        e.preventDefault();
        let fileId = Date.now().toString();
        const videoFile = this.files[0];
        $("#input_VideoName").val(videoFile ? videoFile.name : '');
        $("#input_VideoType").val(videoFile ? getFileExtension(videoFile.name): '');
        $("#input_VideoSize").val(videoFile ? videoFile.size : '');
        if(!videoFile){
            $("#errorModalMessage").text("請選擇影片檔案。");
            showModal("errorModal");
            return;
        }
        const chunkSize = 2 * 1024 * 1024; // 2MB
        const totalChunks = Math.ceil(videoFile.size / chunkSize);
        console.log("Total chunks:", totalChunks);
        
        // 創建 Promise 陣列來處理所有分塊上傳
        const uploadPromises = [];
        
        for(let chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++){
            const start = chunkIndex * chunkSize;
            const end = Math.min(start + chunkSize, videoFile.size); // 尋找檔案結尾 若為最後一格 應該為檔案大小為Min
            const chunk = videoFile.slice(start, end);
            const formData = new FormData();
            formData.append("fileId", fileId);
            formData.append("chunkIndex", chunkIndex);
            formData.append("chunk", chunk);
            formData.append("totalChunks", totalChunks);
            formData.append("fileName", videoFile.name);
            $("#videoUploadProgressContainer").show();
            
            // 將每個 AJAX 請求包裝成 Promise 並加入陣列
            const uploadPromise = $.ajax({
                url:"/Files/UploadVideoChunk",
                type:"POST",
                data:formData,
                processData: false,
                contentType: false,
                success:function(response){
                    const progress = (chunkIndex+1)/totalChunks * 100;
                    console.log(`Video chunk ${chunkIndex+1}/${totalChunks} uploaded successfully:`, response);
                    $("#videoUploadProgressBar").css("width", progress + "%")
                    .attr("aria-valuenow", progress)
                    .text(Math.round(progress) + "%");
                },
                error:function(jqXHR, textStatus, errorThrown){
                    console.error(`Error uploading video chunk ${chunkIndex+1}:`, textStatus, errorThrown);
                    throw new Error(`Chunk ${chunkIndex+1} upload failed`);
                }
            });
            uploadPromises.push(uploadPromise);
        }
        
        try {
            // 等待所有分塊上傳完成
            await Promise.all(uploadPromises);
            console.log("All chunks uploaded successfully, starting completion...");
            
            // 所有分塊上傳完成後，執行完成上傳
            await $.ajax({
               url: `/Files/CompleteVideoUpload?fileId=${fileId}&fileName=${encodeURIComponent(videoFile.name)}`,
               type:"POST",
               success:function(response){
                   console.log("Video upload completed successfully:", response);
                   videoTempId = response.tempId; // Store the temporary ID
                   $("#videoUploadProgressMessage").text("影片上傳完成");
                   $("#videoUploadProgressBar").css("width", "100%").attr("aria-valuenow", 100).text("100%");
                   setTimeout(function(){
                       $("#videoUploadProgressContainer").hide();
                       $("#videoPreviewContainer").show();
                       $("#videoPreviewSource").attr("src", response.videoUrl);
                       $("#videoPreview")[0].load();
                   }, 2000);
               },
               error:function(jqXHR, textStatus, errorThrown){
                   console.error("Error completing video upload:", textStatus, errorThrown);
                   throw new Error("Complete upload failed");
               }
            });
        } catch (error) {
            console.error("Video upload process failed:", error);
            $("#errorModalMessage").text("影片上傳失敗，請稍後再試。");
            showModal("errorModal");
            $("#videoUploadProgressContainer").hide();
        }
    });
    // 上傳影片檔案表單提交
    $("#uploadVideoFileForm").on("submit", function(e){
        e.preventDefault();
        console.log("submit to /Files/ConfirmVideoUpload");
        const formData = new FormData(this);
        formData.append("tempId", videoTempId);
        for (const pair of formData.entries()) {
            console.log(pair[0] + ': ' + pair[1]);
        }
        $.ajax({
            url:"/Files/ConfirmVideoUpload",
            type:"POST",
            data:formData,
            processData: false,
            contentType: false,
            success:function(html){
                 console.log("File uploaded successfully:", html);
                $("#uploadVideoFileForm")[0].reset();
                hideModal("uploadVideoFileModal");
                $("#files-list").append(html);
                hideModal("loadingModal");
            },
            error:function(jqXHR, textStatus, errorThrown){
                console.error("Error confirming upload:", textStatus, errorThrown);
                $("#errorModalMessage").text("影片上傳失敗，請稍後再試。");
                showModal("errorModal");
            }
        })
    });
    // Word 匯出按鈕 - 使用事件委派
    $(document).on("click", ".btn-download-file-detail-word", function(){
        console.log("click btn-download-file-detail-word");
        const fileId = $(this).data("id");
        console.log("fileId:", fileId);
        $("#loadingModal").modal("show");
        $("#loadingModalMessage").text("匯出檔案中，請稍候...");
        location.href = "/Files/ExportFileDetailToWord/" + fileId;
        setTimeout(function(){
            $("#loadingModal").modal("hide");
        }, 2000);
    });
    // 檔案詳情 - 使用事件委派
    $(document).on("click", ".btn-file-detail", function () {
        const fileId = $(this).data("file-id");
        $.ajax({
            url:"/Files/GetFileById/" + fileId,
            type:"GET",
            success:function(html){
               $("#fileDetailModal").html(html);
               resetFileDetailModal();
            },
            error:function(jqXHR, textStatus, errorThrown){
                console.log("Error fetching file details:", textStatus, errorThrown);
                $("#errorModalMessage").text("無法取得檔案詳情，請稍後再試。");
                $("#errorModal").modal("show");
            }
        });
    });
    $(document).on("hidden.bs.modal", "#fileDetailModal", function () {
        $("#fileDetailModal").html("");
        clearInterval(videoStatusInterval);
    }); 
    // 檔案刪除 - 使用事件委派
    $(document).on("click", ".btn-file-delete", function () {
        showModal("confirmDeleteModal");
        const fileId = $(this).data("file-id");
        const fileName = $(this).data("file-name");
        $("#delete-filename").text(`${fileName}`);
        $("#btn-confirm-delete").data("file-id", fileId);
    });
    // 確定刪除
    $("#btn-confirm-delete").on("click", function () {
        const fileId = $(this).data("file-id");
        $.ajax({
            url: "/Files/DeleteFile/" + fileId,
            type:"DELETE",
            success:function(response){
                console.log("File deleted successfully:", response);
                $(`#file-row-${fileId}`).remove();
                hideModal("confirmDeleteModal");
            },
            error:function(jqXHR, textStatus, errorThrown){
                console.error("Error deleting file:", textStatus, errorThrown);
                $("#errorModalMessage").text("檔案刪除失敗，請稍後再試。");
                showModal("errorModal");
            }
        });
    });
    // 檔案選取 - 使用事件委派
    $(document).on("change", ".form-check-input", function () {
        const fileId = $(this).data("file-id");
        if ($(this).is(":checked")) {
            // 檔案已選取
            console.log("File selected:", fileId);
            selectedFiles.push(fileId);
        } 
        else {
            // 檔案已取消選取
            console.log("File deselected:", fileId);
            selectedFiles = selectedFiles.filter(id => id !== fileId);
        }
    });
    // 下載檔案
    $("#btn-download-files").click(function () {
        if (selectedFiles.length === 0) {
            $("#errorModalMessage").text("請選擇要下載的檔案。");
            $("#errorModal").modal("show");
            return;
        }
        else if(selectedFiles.length == 1){
            const fileId = selectedFiles[0];
            // 下載單個檔案
            window.location.href = "/Files/DownloadFile/" + fileId;
        }
        else{
            selectedFiles = selectedFiles.map(id => parseInt(id, 10));
            console.log("進入 DownloadMultipleFiles 方法:", selectedFiles);
            showModal("loadingModal");
            $("#loadingModalMessage").text("檔案下載中，請稍候...");
            $.ajax({
                url:"/Files/DownloadMultipleFiles",
                type:"POST",
                data:JSON.stringify(selectedFiles),
                contentType:"application/json",
                xhrFields: {
                    responseType: "blob"
                },
                success:function(blob){
                    hideModal("loadingModal");
                    $("loadingModal").modal("hide");
                    const url = window.URL.createObjectURL(blob);
                    const a = document.createElement("a");
                    a.href = url;
                    a.download = "multiple_files.zip";
                    document.body.appendChild(a);
                    a.click();
                    setTimeout(function(){
                        window.URL.revokeObjectURL(url);
                        selectedFiles=[];
                        $(".form-check-input").prop("checked", false);
                        a.remove();
                    },100);
                },
                error:function(jqXHR, textStatus, errorThrown){
                    console.error("Error downloading multiple files:", textStatus, errorThrown);
                    hideModal("loadingModal");
                    $("loadingModal").modal("hide");
                    $("#errorModalMessage").text("檔案下載失敗，請稍後再試。");
                    showModal("errorModal");
                    $("#errorModal").modal("show");
                }
            })
        }
    });
    // word to pdf 按鈕
    $("#btn-upload-word-to-pdf").click(function () {
        $("#uploadWordModal").modal("show");
    });
    // excel to pdf 按鈕
    $("#btn-upload-excel-to-pdf").click(function () {
        $("#uploadExcelModal").modal("show");
    });
    // 上傳Word檔案表單提交
    $("#uploadWordFileForm").submit(function (e) {
        e.preventDefault();
        showModal("loadingModal");
        $("#loadingModalMessage").text("檔案上傳中，請稍候...");
        const formData = new FormData(this);
        formData.append("file", $(this).find("input[type=file]")[0].files[0]);
        $.ajax({
            url: "/Files/ConvertWordToPdf",
            method: "POST",
            data: formData,
            processData: false,
            contentType: false,
            xhrFields: {
                responseType: "blob"
            },
            success: function (blob) {
                console.log("File converted successfully:", blob);
                const url = window.URL.createObjectURL(blob);
                    const a = document.createElement("a");
                    a.href = url;
                    a.download = "converted_word_file.pdf";
                    document.body.appendChild(a);
                    a.click();
                    setTimeout(function(){
                        $("#loadingModal").modal("hide");
                        window.URL.revokeObjectURL(url);
                        a.remove();
                    },100);
                $("#uploadWordModal").modal("hide");
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.error("Error converting file:", textStatus, errorThrown);
                $("#errorModalMessage").text("檔案轉換失敗，請稍後再試。");
                $("#uploadWordModal").modal("hide");
                showModal("errorModal");
            }
        });
    });
    // 上傳Excel檔案表單提交
    $("#uploadExcelFileForm").submit(function (e) {
        e.preventDefault();
        showModal("loadingModal");
        $("#loadingModalMessage").text("檔案上傳中，請稍候...");
        const formData = new FormData(this);
        formData.append("file", $(this).find("input[type=file]")[0].files[0]);
        $.ajax({
            url: "/Files/ConvertExcelToPdf",
            method: "POST",
            data: formData,
            processData: false,
            contentType: false,
            xhrFields: {
                responseType: "blob"
            },
            success: function (blob) {
                console.log("File converted successfully:", blob);
                const url = window.URL.createObjectURL(blob);
                    const a = document.createElement("a");
                    a.href = url;
                    a.download = "converted_excel_file.pdf";
                    document.body.appendChild(a);
                    a.click();
                    setTimeout(function(){
                        $("#loadingModal").modal("hide");
                        $("#uploadExcelModal").modal("hide");
                        window.URL.revokeObjectURL(url);
                        a.remove();
                    },100);
                $("#uploadExcelModal").modal("hide");
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.error("Error converting file:", textStatus, errorThrown);
                $("#errorModalMessage").text("檔案轉換失敗，請稍後再試。");
                $("#uploadExcelModal").modal("hide");
                showModal("errorModal");
            }
        });
    });
    // 開啟寄送檔案 Email 表單
    $("#btn-send-files-email").click(function () {
        if(selectedFiles.length == 0){
            $("#errorModalMessage").text("請選擇要發送的檔案。");
            $("#emailModal").modal("hide");
            showModal("errorModal");
            return;
        }
        $.ajax({
            url:"/Files/GetFileNames",
            method: "POST",
            data: JSON.stringify(selectedFiles),
            contentType: "application/json",
            success: function (fileNames) {
                let html = fileNames.map(name => `<li>${name}</li>`).join("");
                $("#emailFileList").html(html);
                $("#emailModal").modal("show");
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.error("Error getting file names:", textStatus, errorThrown);
                $("#errorModalMessage").text("無法獲取檔案名稱，請稍後再試。");
                showModal("errorModal");
            }
        });
    });
    // 寄送檔案 Email
    $("#emailFileForm").on("submit",function(e){
        e.preventDefault();
        $("#emailModal").modal("hide");
        $("#loadingModal").modal("show");
        $("#loadingModalMessage").text("信件處理中，請稍候...");
        const formData = new FormData(this);
        selectedFiles.forEach(id => {
            formData.append("attachmentIds", id); // 每個 id 單獨 append
        });
        for (let pair of formData.entries()) {
            console.log(pair[0], pair[1]);
        }
        $.ajax({
            url: "/Files/SendEmailWithAttachments",
            type: "POST",
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                console.log("Email sent successfully:", response);
                selectedFiles = [];
                $(".form-check-input").prop("checked", false);
                $("#loadingModal").modal("hide");
                $("#emailFileForm")[0].reset();
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.error("Error sending email:", textStatus, errorThrown);
                $("#errorModalMessage").text("無法發送電子郵件，請稍後再試。");
                showModal("errorModal");
            }
        });
    });
    // 分頁連結 - 使用事件委派
    $(document).on("click", ".page-link", function (e) {
        e.preventDefault(); // 防止預設的 a 標籤跳轉
        const page = $(this).data("id");
        console.log("Page clicked:", page);
        $.ajax({
            url: `/Files/GetPagedFiles?pageNumber=${page}&pageSize=10`,
            type:"GET",
            success: function (data) {
                $("#files-list-container").html(data);
                // 重置選取的檔案陣列，因為頁面內容已更新
                selectedFiles = [];
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.error("Error loading files:", textStatus, errorThrown);
                $("#errorModalMessage").text("無法載入檔案，請稍後再試。");
                showModal("errorModal");
            }
        });
    });
    function resetFileDetailModal(){
        let videoId;
        $("#detail-video").off("play").on("play", function () {
            videoId = $(this).data("id");
            console.log("video play event, videoId:", videoId);
            $.ajax({
                url:`/Video/GetWatchHistory/${videoId}`,
                type:"GET",
                success:function(response){
                    console.log("Setting video currentTime to:", response.lastPosition);
                    $("#detail-video")[0].currentTime = response.lastPosition || 0;
                },
                error:function(jqXHR,textStatus,errorThrown){
                    console.log("Error fetching watch history:", textStatus, errorThrown);
                }
            });
            startCheckVideoStatus(videoId);
        });
        // 監聽影片暫停事件
        $("#detail-video").off("pause").on("pause", function () {
            videoId = $(this).data("id");
            const currentTime = this.currentTime;
            console.log("video pause event, videoId:", videoId, "currentTime:", currentTime);
            sendProgress(videoId, currentTime, false);
            clearInterval(videoStatusInterval);
        });
    }
    // 監聽影片播放事件
    function startCheckVideoStatus(videoId){
        //影片狀態監聽
        videoStatusInterval = setInterval(() => {
                const currentTime = $(document).find("#detail-video")[0].currentTime;
                console.log("video status check event, videoId:", videoId, "currentTime:", currentTime);
                sendProgress(videoId, currentTime, false);
        }, 5000);
    }
    function sendProgress(videoId,lastPosition,isCompleted){
      console.log(
        videoId,
        lastPosition,
        isCompleted
      );
      
      $.ajax({
        url:"/Video/Watch",
        type:"POST",
        data:{
          fileId:videoId,
          lastPosition:Math.floor(lastPosition),
          isCompleted:isCompleted
        },
        success:function(response){
          console.log("影片觀看進度更新成功:",response);
        },
        error:function(jqXHR,textStatus,errorThrown){
            console.log("影片觀看進度更新失敗:",jqXHR);
            console.error("更新影片觀看進度時發生錯誤:",textStatus,errorThrown);
        }
      });
    }
})