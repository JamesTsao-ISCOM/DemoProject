import TabController from "./videoProcessModule/TabController.js";
import YTVideoProcessUI from "./videoProcessModule/youtubeVideoProcessUI.js";
import YouTubeVideoHandler from "./videoProcessModule/youtubeVideoHandler.js";
import LocalVideoProcessUI from "./videoProcessModule/localVideoProcessUI.js";
import LocalVideoProcessHandler from "./videoProcessModule/localVideoHandler.js";
$(document).ready(function () {
    try {
        const tabController = new TabController({
            onTabChange: (tab) => {
                console.log("Active tab:", tab);
            }
        });
        const youTubeVideoHandler = new YouTubeVideoHandler("youtubePlayer");
        let updateYTURLInterval = null;
        const ytVideoProcessUI = new YTVideoProcessUI({
            onLoadVideo: (url) => {
                try {
                    youTubeVideoHandler.load(url);
                    const currentTime = youTubeVideoHandler.getVideoInfo().currentTime;
                    const duration = youTubeVideoHandler.getVideoInfo().duration;
                    ytVideoProcessUI.setProgress(currentTime,duration);
                } catch (error) {
                    console.error("Error loading video:", error);
                }
            },
            onPlay: () => {
                try {
                    youTubeVideoHandler.play();
                    updateYTURLInterval = setInterval(()=>{
                        try{
                            const currentTime = youTubeVideoHandler.getVideoInfo().currentTime;
                            const duration = youTubeVideoHandler.getVideoInfo().duration;
                            ytVideoProcessUI.setProgress(currentTime,duration);
                        } catch(error){
                            console.error("Error updating progress:", error);
                        }
                    },500);
                } catch (error) {
                    console.error("Error playing video:", error);
                }
            },
            onPause: () => {
                try {
                    clearInterval(updateYTURLInterval);
                    youTubeVideoHandler.pause();
                } catch (error) {
                    console.error("Error pausing video:", error);
                }
            },
            onStop: () => {
                try {
                    clearInterval(updateYTURLInterval);
                    youTubeVideoHandler.stop();
                } catch (error) {
                    console.error("Error stopping video:", error);
                }
            },
            onSeekForward: (seconds) => {
                try {
                    youTubeVideoHandler.seekForward(seconds);
                } catch (error) {
                    console.error("Error seeking forward:", error);
                }
            },
            onSeekBackward: (seconds) => {
                try {
                    youTubeVideoHandler.seekBackward(seconds);
                } catch (error) {
                    console.error("Error seeking backward:", error);
                }
            },
            onSeekTo: (seconds) => {
                try {
                    youTubeVideoHandler.seekTo(seconds);
                } catch (error) {
                    console.error("Error seeking to position:", error);
                }
            },
            onChangePlaybackRate: (rate) => {
                try {
                    youTubeVideoHandler.setPlayBackRate(rate);
                } catch (error) {
                    console.error("Error changing playback rate:", error);
                }
            },
            onSetVolume: (volume) => {
                try {
                    youTubeVideoHandler.setVolume(volume);
                } catch (error) {
                    console.error("Error setting volume:", error);
                }
            }
        });
        const localVideoProcessHandler = new LocalVideoProcessHandler(document.getElementById("previewVideo"));
        const localVideoProcessUI = new LocalVideoProcessUI({
            onPlay: () => {
                localVideoProcessHandler.play();
            },
            onPause: () => {
                localVideoProcessHandler.pause();
            },
            onPreview: (settings) => {
                localVideoProcessHandler.preview(settings);
            },
            onExport: (videoFile, settings) => {
                localVideoProcessHandler.export(videoFile, settings);
            },
            onClearEffects: () => {
                localVideoProcessHandler.clearEffects();
            }
        });
    } catch (error) {
        console.error("Error initializing video process modules:", error);
    }
});