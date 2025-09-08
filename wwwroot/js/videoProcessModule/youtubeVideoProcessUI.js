export default class YTVideoProcessUI {
    constructor({
        onLoadVideo,
        onPlay,
        onPause,
        onStop,
        onSeekForward,
        onSeekBackward,
        onSeekTo,
        onChangePlaybackRate,
        onSetVolume
    }){
        this.youtubeURLInput = document.getElementById("youtubeUrlInput");
        this.loadYouTubeBtn = document.getElementById("loadVideoButton");
        this.volumeRange = document.getElementById("volumeRange");
        this.playBtn = document.getElementById("playButton");
        this.pauseBtn = document.getElementById("pauseButton");
        this.stopBtn = document.getElementById("stopButton");
        this.seekForwardBtn = document.getElementById("seekForwardButton");
        this.seekBackwardBtn = document.getElementById("seekBackwardButton");
        this.videoRange = document.getElementById("videoRange");
        this.playVideoSpeedSelect = document.getElementById("playbackRateSelect");
        this.seekTimeInput = document.getElementById("seekTimeInput");
        this.seekToBtn = document.getElementById("seekToButton");
        this.volumeRange = document.getElementById("volumeRange");
        this.ytVideoLoadingMessage = document.getElementById("ytVideoLoadingMessage");
        this.currentTimeDisplay = document.getElementById("currentTimeDisplay");
        this.durationDisplay = document.getElementById("durationDisplay");
        this.loadYouTubeBtn.addEventListener("click", ()=>{
            const url = this.youtubeURLInput.value;
            onLoadVideo && onLoadVideo(url);
        });
        this.playBtn.addEventListener("click", ()=>{
            onPlay && onPlay();
        });
        this.pauseBtn.addEventListener("click", ()=>{
            onPause && onPause();
        });
        this.stopBtn.addEventListener("click", ()=>{
            onStop && onStop();
        });
        this.seekForwardBtn.addEventListener("click", ()=>{
            onSeekForward && onSeekForward(10);
        });
        this.seekBackwardBtn.addEventListener("click", ()=>{
            onSeekBackward && onSeekBackward(10);
        });
        this.seekToBtn.addEventListener("click", ()=>{
            const time = parseFloat(this.seekTimeInput.value);
            onSeekTo && onSeekTo(time);
        });
        this.videoRange.addEventListener("input", ()=>{
            const time = parseFloat(this.videoRange.value);
            onSeekTo && onSeekTo(time);
        });
        this.playVideoSpeedSelect.addEventListener("change", ()=>{
            const rate = parseFloat(this.playVideoSpeedSelect.value);
            onChangePlaybackRate && onChangePlaybackRate(rate);
        });
        this.volumeRange.addEventListener("input", ()=>{
            const volume = parseFloat(this.volumeRange.value);
            onSetVolume && onSetVolume(volume);
        });
    }
    setProgress(currentTime, duration){
        currentTime = Math.floor(currentTime);
        duration = Math.floor(duration);
        const formatTime = (time) => {
            const minutes = Math.floor(time / 60).toString().padStart(2, '0');
            const seconds = (time % 60).toString().padStart(2, '0');
            return `${minutes}:${seconds}`;
        }
        this.currentTimeDisplay.textContent = formatTime(currentTime);
        this.durationDisplay.textContent = formatTime(duration);
        if(!this.videoRange.max || this.videoRange.max != duration){
            this.videoRange.max = duration;
        }
        this.videoRange.min = 0;
        this.videoRange.step = 1;
        this.videoRange.max = duration;
        this.videoRange.value = currentTime;
    }
}