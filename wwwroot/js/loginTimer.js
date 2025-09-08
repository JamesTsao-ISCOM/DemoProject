let remainingSeconds = 60*30; // 預設剩餘時間為30分鐘
// 每5秒更新一次倒數計時
const timer = setInterval(() =>{
    try{
        remainingSeconds=remainingSeconds-5;
        // 可選：顯示倒數在畫面上
        if (remainingSeconds <= 0) {
            clearInterval(timer);
            alert("登入已逾時，請重新登入。");
            window.location.href = "/Account/Login";
        }
    }
    catch(error){
    }
},5000); // 每秒更新一次
function resetTimer() {
    remainingSeconds = 60*30; // 重置倒數
}
["mousemove", "keydown", "click"].forEach(event => {
    document.addEventListener(event, resetTimer);
});