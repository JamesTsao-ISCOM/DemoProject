export default class TabController{
    constructor({onTabChange}){
        this.youtubeTab = document.getElementById("youtubeTab");
        this.localTab = document.getElementById("localTab");
        this.youtubeVideoContainer = document.getElementById("youtubeVideoContainer");
        this.localVideoContainer = document.getElementById("localVideoContainer");
        this.youtubeTab.addEventListener("click", ()=>{
            this.showYouTubeTab();
            onTabChange && onTabChange("youtube");
        });
        this.localTab.addEventListener("click", ()=>{
            this.showLocalTab();
            onTabChange && onTabChange("local");
        });
    }
    showYouTubeTab(){
        this.youtubeVideoContainer.style.display = "block";
        this.localVideoContainer.style.display = "none";
        this.setTabActive(this.youtubeTab);

        if(this.onTabChange) this.onTabChange("youtube");
    }
    showLocalTab(){
        this.youtubeVideoContainer.style.display = "none";
        this.localVideoContainer.style.display = "block";
        this.setTabActive(this.localTab);
        if(this.onTabChange) this.onTabChange("local");
    }
    setTabActive(tabElement) {
        document.querySelectorAll(".nav-link").forEach(el => el.classList.remove("active"));
        tabElement.classList.add("active");
    }
    
}