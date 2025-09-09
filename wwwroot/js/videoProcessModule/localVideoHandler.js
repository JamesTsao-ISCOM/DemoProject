export default class LocalVideoProcessHandler{
    constructor(videoElement){
        this.videoElement = videoElement;
        this.effectWrapper = document.getElementById('effectWrapper');
        this.loadingModal = $('#videoProcessModal');
    }
    play(){
        this.videoElement.play();
    }
    pause(){
        this.videoElement.pause();
    }
    preview({startTime,endTime,effect}){
        this.videoElement.currentTime = startTime;
        console.log("Applying effect:", startTime,endTime,effect);
        let style_effect = "";
        if(effect === "none") style_effect = "";
        else if(effect === "blur") style_effect = "blur(5px)";
        else if(effect === "grayscale") style_effect = "grayscale(100%)";
        else if(effect === "brightness") style_effect = "brightness(1.5)";
        this.videoElement.style.filter = style_effect;
        this.videoElement.play();
        if(endTime>0){
            this.videoElement.addEventListener('timeupdate',()=>{
                if(this.videoElement.currentTime >= endTime){
                    this.videoElement.pause();
                }
            },{once:false});
        }
    }
    clearEffects(){
        // 清除所有視覺效果
        if(this.effectWrapper) {
            this.effectWrapper.style.filter = '';
        }
        this.videoElement.style.filter = '';
    }
    export(videoFile, settings){
        console.log("Exporting video file:", videoFile);
        console.log("Exporting with settings:",settings);
        this.loadingModal.modal("show");
        // 驗證參數
        if (!videoFile) {
            alert("請先選擇影片檔案");
            return;
        }
        
        if(settings.effect === ""){
            settings.effect = "none";
        }
        
        // 詳細記錄要發送的資料
        console.log("File name:", videoFile.name);
        console.log("File size:", videoFile.size);
        console.log("File type:", videoFile.type);
        console.log("Settings to send:", {
            startTime: settings.startTime,
            endTime: settings.endTime,
            effect: settings.effect,
            format: settings.format,
            resolution: settings.resolution
        });
        
        const formData = new FormData();
        formData.append("file", videoFile);
        formData.append("startTime", settings.startTime.toString());
        formData.append("endTime", settings.endTime.toString());
        formData.append("effect", settings.effect);
        formData.append("format", settings.format);
        
        // 記錄 FormData 內容
        console.log("FormData contents:");
        for (let [key, value] of formData.entries()) {
            console.log(`${key}:`, value);
        }
        
        // 發送到後端進行處理
        $.ajax({
            url:"/Video/ProcessVideo",
            method:"POST",
            data: formData,
            processData: false,
            contentType: false,
            xhrFields: {
                responseType: "blob"
            },
            success: (blob) => {
                console.log("Video processed successfully");
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = url;
                a.download = `processed_video.${settings.format}`;
                document.body.appendChild(a);
                a.click();
                setTimeout(()=>{
                    window.URL.revokeObjectURL(url);
                    a.remove();
                    this.loadingModal.modal("hide");
                },100);
            },
            error: (error) => {
                console.error("Error processing video:", error);
            }
        })
    }
}