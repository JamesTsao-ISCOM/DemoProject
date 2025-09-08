document.addEventListener("DOMContentLoaded", function() {
    // 按鈕和區塊元素
    const editBlock = document.getElementById("edit-block");
    const editButton = document.getElementById("btn-profile-edit");
    const cancelButton = document.getElementById("btn-profile-cancel");
    const saveButton = document.getElementById("btn-profile-save");
    // 編輯區塊輸入框
    const inputName = document.getElementById("input-name");
    const inputEmail = document.getElementById("input-email");
    const inputPhoneNumber = document.getElementById("input-phone-number");
    // 成功和錯誤訊息
    const alertSuccessMessage = document.getElementById("success-message");
    const alertErrorMessage = document.getElementById("error-message");
    // 關閉密碼更改對話框按鈕
    const btnCloseChangePwdDialog = document.getElementById("btn-close-change-pwd-dialog");
    let originalData ={};
    // 初始化時停用編輯區塊
    inputName.disabled = true;
    inputEmail.disabled = true;
    inputPhoneNumber.disabled = true;

    editButton.addEventListener("click", function() {
        console.log("Edit button clicked");
        editBlock.style.display = "flex";
        editBlock.style.gap = "8px";
        // 存下原始資料
        originalData = {
            name: inputName.value,
            email: inputEmail.value,
            phoneNumber: inputPhoneNumber.value
        };
        // 啟用輸入框
        inputName.disabled = false;
        inputEmail.disabled = false;
        inputPhoneNumber.disabled = false;
        editButton.style.display = "none";
    });
    cancelButton.addEventListener("click", function() {
        console.log("Cancel button clicked");
        editBlock.style.display = "none";
        editButton.style.display = "block"; // 顯示編輯按鈕
        inputName.disabled = true;
        inputEmail.disabled = true;
        inputPhoneNumber.disabled = true;
        // 恢復原始資料
        inputName.value = originalData.name;
        inputEmail.value = originalData.email;
        inputPhoneNumber.value = originalData.phoneNumber;
    });
    saveButton.addEventListener("click", function() {
        console.log("Save button clicked");
        editBlock.style.display = "none";
        editBlock.style.display = "block"; // 隱藏編輯區塊
        inputName.disabled = true; // 禁用輸入框
        inputEmail.disabled = true;
        inputPhoneNumber.disabled = true;
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        // use fetch api part
        fetch("/Account/UpdateProfile", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": token
            },
            body: JSON.stringify({
                name: inputName.value,
                email: inputEmail.value,
                phoneNumber: inputPhoneNumber.value
            })
        })
        .then(response => {
            if (response.ok) {
               editButton.style.display = "block"; // 顯示編輯按鈕
               editBlock.style.display = "none"; // 隱藏編輯區塊
                // 顯示成功訊息
               alertSuccessMessage.style.display = "block";
                setTimeout(() => {
                  alertSuccessMessage.style.display = "none";    
                }, 3000);
               return response.json();
            } 
            else{
                alertErrorMessage.style.display = "block";
                setTimeout(() => {
                    alertErrorMessage.style.display = "none";
                }, 3000);
                throw new Error("Network response was not ok");
            }
        })
    });
    // 密碼更改Response功能
    function showPasswordChangeSuccess() {
        var modal = new bootstrap.Modal(document.getElementById('change-success-dialog'));
        modal.show();
    }
    // 關閉密碼更改對話框
    function closeChangePasswordDialog() {
        $("#change-password-dialog").modal('hide');
        // 清空輸入框
        $("#input-current-password").val('');
        $("#input-change-password").val('');
        $("#input-confirm-password").val('');
    }
    // 關閉密碼更改對話框按鈕事件
    btnCloseChangePwdDialog.addEventListener("click", closeChangePasswordDialog);
    // 實現 jquery ajax部分
    $("#btn-change-password").click(function() {
       console.log("Change password button clicked");
       var token = $('input[name="__RequestVerificationToken"]').val(); // 獲取 CSRF Token
       if($("#input-change-password").val() !== $("#input-confirm-password").val()) {
           $("#change-pwd-error-message").show();
           $("#error-msg").text("新密碼與確認密碼不符，請重新輸入。");
           return;
       }
       const changePasswordData ={
          OldPassword: $("#input-current-password").val(),
          NewPassword: $("#input-change-password").val(),
          ConfirmNewPassword: $("#input-confirm-password").val()
       }
       $.ajax({
        url: "/Account/ChangePassword",
        type: "POST",
        contentType: "application/json",
        headers: {
            "RequestVerificationToken": token
        },
        data:JSON.stringify(changePasswordData),
        success: function() {
            closeChangePasswordDialog();
            showPasswordChangeSuccess();
            $("#change-pwd-error-message").hide();
        }
        , error: function(xhr, status, error) {
            console.error("Error changing password:", error);
            $("#change-pwd-error-message").show();
            $("#error-msg").text(xhr.responseJSON.message || "密碼更改失敗，請稍後再試。");
        }
       }) 
    })

});