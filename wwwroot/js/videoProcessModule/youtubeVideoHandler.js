export default class YouTubeVideoHandler{
    constructor(playerElementId){
      this.playerElementId = playerElementId;
      this.player = null;
    }
    init(videoId,onReady){
        console.log("Initializing YouTube Player with video ID:", videoId);
        console.log("Player Element ID:", this.playerElementId);
        
        // 檢查 YouTube API 是否已經載入
        if (typeof YT !== 'undefined' && YT.Player) {
            this.createPlayer(videoId, onReady);
        } else {
            // 如果 API 還沒載入，設定回調
            window.onYouTubeIframeAPIReady = () => {
                this.createPlayer(videoId, onReady);
            };
        }
    }
    
    createPlayer(videoId, onReady) {
        try {
            this.player = new YT.Player(this.playerElementId, {
                height: '360',
                width: '640',
                videoId: videoId,
                playerVars: {
                    'controls':0
                },
                events:{
                    onReady:(event)=>{
                        console.log("YouTube Player is ready.");
                        if (onReady) onReady(this.player);
                    },
                    onStateChange:(event)=>{
                        console.log("Player state changed:", event.data);
                    },
                    onError:(event)=>{
                        console.error("YouTube Player error:", event.data);
                    }
                }
            });
        } catch (error) {
            console.error("Error creating YouTube player:", error);
        }
    }
    load(url){
        try {
            const videoId = this.extractVideoID(url);
            console.log("Extracted Video ID:", videoId);
            if (!videoId) {
                console.error("Invalid YouTube URL or video ID not found");
                return;
            }
            
            if(this.player){
                this.player.loadVideoById(videoId);
            }else{
                this.init(videoId);
            }
        } catch (error) {
            console.error("Error loading video:", error);
        }
    }
    play(){
        try {
            console.log("Playing video");
            if (this.player && typeof this.player.playVideo === 'function') {
                this.player.playVideo();
            } else {
                console.warn("Player not ready or playVideo method not available");
            }
        } catch (error) {
            console.error("Error playing video:", error);
        }
    }
    
    pause(){
        try {
            console.log("Pausing video");
            if (this.player && typeof this.player.pauseVideo === 'function') {
                this.player.pauseVideo();
            } else {
                console.warn("Player not ready or pauseVideo method not available");
            }
        } catch (error) {
            console.error("Error pausing video:", error);
        }
    }
    
    stop(){
        try {
            console.log("Stopping video");
            if (this.player && typeof this.player.stopVideo === 'function') {
                this.player.stopVideo();
            } else {
                console.warn("Player not ready or stopVideo method not available");
            }
        } catch (error) {
            console.error("Error stopping video:", error);
        }
    }
    seekForward(seconds){
        try {
            console.log(`Seeking forward ${seconds} seconds`);
            if(this.player && typeof this.player.getCurrentTime === 'function' && typeof this.player.seekTo === 'function'){
                const currentTime = this.player.getCurrentTime();
                this.player.seekTo(currentTime + seconds, true);
            } else {
                console.warn("Player not ready or seek methods not available");
            }
        } catch (error) {
            console.error("Error seeking forward:", error);
        }
    }
    
    seekBackward(seconds){
        try {
            console.log(`Seeking backward ${seconds} seconds`);
            if(this.player && typeof this.player.getCurrentTime === 'function' && typeof this.player.seekTo === 'function'){
                const currentTime = this.player.getCurrentTime();
                this.player.seekTo(Math.max(0, currentTime - seconds), true);
            } else {
                console.warn("Player not ready or seek methods not available");
            }
        } catch (error) {
            console.error("Error seeking backward:", error);
        }
    }
    
    seekTo(seconds){
        try {
            console.log(`Seeking to ${seconds} seconds`);
            if (this.player && typeof this.player.seekTo === 'function') {
                this.player.seekTo(seconds, true);
            } else {
                console.warn("Player not ready or seekTo method not available");
            }
        } catch (error) {
            console.error("Error seeking to position:", error);
        }
    }
    setPlayBackRate(rate){
        try {
            console.log(`Setting playback rate to ${rate}`);
            if (this.player && typeof this.player.setPlaybackRate === 'function') {
                this.player.setPlaybackRate(rate);
            } else {
                console.warn("Player not ready or setPlaybackRate method not available");
            }
        } catch (error) {
            console.error("Error setting playback rate:", error);
        }
    }
    
    setVolume(volume){
        try {
            console.log(`Setting volume to ${volume}`);
            if (this.player && typeof this.player.setVolume === 'function') {
                this.player.setVolume(volume * 100);
            } else {
                console.warn("Player not ready or setVolume method not available");
            }
        } catch (error) {
            console.error("Error setting volume:", error);
        }
    }
    getVideoInfo(){
        if(!this.player)return null;
        const duration = this.player.getDuration();
        const currentTime = this.player.getCurrentTime();
        const videoData = this.player.getVideoData();
        console.log("Video Info:", {duration, currentTime, videoData});
        return {
            duration,
            currentTime,
            title: videoData.title,
            videoId: videoData.video_id,
        }
    }
    
    extractVideoID(url) {
        try {
            console.log("Extracting video ID from URL:", url);
            if (!url || typeof url !== 'string') {
                console.error("Invalid URL provided");
                return "";
            }
            
            const urlObj = new URL(url);
            let videoId = "";
            
            // YouTube 正常 URL
            if (urlObj.hostname.includes("youtube.com")) {
                videoId = urlObj.searchParams.get("v");
            }
            // YouTube 短網址 youtu.be
            else if (urlObj.hostname.includes("youtu.be")) {
                videoId = urlObj.pathname.substring(1);
            }
            
            return videoId || "";
        } catch (error) {
            console.error("Error extracting video ID:", error);
            return "";
        }
    }
}