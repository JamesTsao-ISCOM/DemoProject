export default class LocalVideoProcessUI{
    constructor({onPlay,onPause,onPreview,onExport,onClearEffects}){
        this.videoInput = $("#videoFileInput");
        this.videoPreview = $("#previewVideo");
        this.playButton = $("#playVideoBtn");
        this.pauseButton = $("#pauseVideoBtn");
        this.startTimeInput = $("#startTimeInput");
        this.endTimeInput = $("#endTimeInput");
        this.trimButton = $("#trimBtn");
        this.videoDurationText = $("#videoDurationText");
        this.downloadTrimmedButton = $("#downloadTrimmedBtn");
        this.outputFormatSelect = $("#outputFormatSelect");
        this.effectSelect = $("#effectSelect");
        this.previewEffectButton = $("#previewEffectBtn");
        this.clearEffectButton = $("#clearEffectBtn");
        this.progressBar = $("#processProgressBar");
        this.progressText = $("#progressText");
        this.currentSetting = {
            startTime: 0,
            endTime: 0,
            effect : "none",
            format : "mp4",
            resolution : "1080p"
        }
        this.videoFile = null;
        this.videoInput.on("change",(e)=>{
            const file = e.target.files[0];
            if(!file) return ;
            const fileURL = URL.createObjectURL(file);
            this.videoFile = file;
            this.videoPreview.attr("src",fileURL);
            this.videoPreview.show();
            this.videoPreview.on("loadedmetadata",()=>{
                this.videoPreview[0].currentTime = 0;
                this.endTimeInput.attr("max",parseInt(`${this.videoPreview[0].duration}`) || 0);
                this.startTimeInput.attr("max",parseInt(`${this.videoPreview[0].duration}`) || 0);
                this.currentSetting.endTime = parseInt(`${this.videoPreview[0].duration}`) || 0;
                this.endTimeInput.val(parseInt(`${this.currentSetting.endTime}`) || 0);
                this.videoDurationText.text(`${Math.floor(this.videoPreview[0].duration)} 秒`);
            });
            this.currentSetting = {
                startTime: 0,
                endTime: 0,
                effect : "none",
                format : "mp4",
            };
            this.effectSelect.val("none");
            this.outputFormatSelect.val("mp4");
        });
        this.endTimeInput.on("change",()=>{
            const endTime = parseFloat(this.endTimeInput.val()) || 0;
            this.startTimeInput.attr("max",endTime);
        });
        this.playButton.on("click",()=>{
            if(onPlay) onPlay();
        });
        this.pauseButton.on("click",()=>{
            if(onPause) onPause();
        });
        this.previewEffectButton.on("click",()=>{
            this.currentSetting.effect = this.effectSelect.val();
            this.currentSetting.startTime = parseFloat(this.startTimeInput.val()) || 0;
            this.currentSetting.endTime = parseFloat(this.endTimeInput.val()) || 0;
        });
        this.trimButton.on("click",()=>{
            this.currentSetting.startTime = parseFloat(this.startTimeInput.val()) || 0;
            this.currentSetting.endTime = parseFloat(this.endTimeInput.val()) || 0;
            if(onPreview) onPreview(this.currentSetting);
        });
        this.previewEffectButton.on("click",()=>{
            this.currentSetting.effect = this.effectSelect.val();
            if(onPreview) onPreview(this.currentSetting);
        });
        this.clearEffectButton.on("click",()=>{
            // 重設效果選擇器到無特效
            this.effectSelect.val("none");
            this.currentSetting.effect = "";
            if(onClearEffects) onClearEffects();
        });
        this.downloadTrimmedButton.on("click",()=>{
            this.currentSetting.format = this.outputFormatSelect.val();
            if(onExport) onExport(this.videoFile,this.currentSetting);
        });
    }
}