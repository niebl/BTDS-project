class AoiDisplay {
    constructor(sensorID, canvas){
        this.sensor = sensorID;
        //this.canvas = $(`#c${sensorID}`);
        this.canvas = canvas;
        this.observation = null;

        this.socket = null;
    }

    async initialise(){
        this.socket = await new WebSocket(`ws://localhost:3000/sensor/rss/${this.sensor}`);
        
        this.initCanvas();

        this.socket.onmessage = (msg)=>{
            this.observation = JSON.parse(msg.data);
    
            this.updateCanvas(this.observation)
        }

        console.log(this.socket)

        this.socket.onopen = ()=>{
            console.log("socket open")
            this.socket.send("init")
        }
    }

    updateCanvas(observation){
        this.ctx.rect(0, 0, 1, 1);
        this.ctx.fillStyle = "#ede177";
        this.ctx.fill();

        //draw all inhabitants
        let radius = 0.05;
        this.ctx.fillStyle = "red";
        for(let inhab of this.observation.inhabitants){
            this.ctx.beginPath();
            this.ctx.arc(inhab.x, inhab.y, radius, 0, 2 * Math.PI, false);
            this.ctx.fill();
        }

        //write timestamp
        this.ctx.font = "14px Sans Serif"
        this.ctx.scale(1/this.canvas[0].width,1/this.canvas[0].height)
        this.ctx.fillStyle = "black";

        let time = new Date(parseInt(this.observation.timestamp)*1000);
        time = time.toLocaleTimeString()

        this.ctx.fillText(time,10,20);
        this.ctx.scale(this.canvas[0].width,this.canvas[0].height)
    }

    initCanvas(){
        this.ctx = this.canvas[0].getContext("2d");
        
        this.ctx.beginPath();

        //set canvas scale, so every object drawn is between 0 and 1
        this.ctx.scale(this.canvas[0].width,this.canvas[0].height)

        this.ctx.rect(0, 0, 1, 1);
        this.ctx.fillStyle = "#AAAAAA";
        this.ctx.fill();
    }

    regexEscape(string){
        let regex = /\./;
        return string.replace(regex, "\\\\.")
    }
}

function main(){
    //get all the aoi-canvases and initialise their aoiDisplay
    let aois = $('.aoi_canvas')
    let aoiDisplays = [];

    for (aoi of aois){
        let display = new AoiDisplay($(aoi).attr("name"), $(aoi));
        display.initialise();
        aoiDisplays.push(display)
    }
}