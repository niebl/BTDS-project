const ENV_VARS = {
};

async function main(){
    await refreshDistance();

    //get all the aoi-canvases and initialise their aoiDisplay
    let aois = $('.aoi_canvas');
    let aoiDisplays = [];
    for (aoi of aois){
        let display = new AoiDisplay($(aoi).attr("name"), $(aoi));
        display.initialise();
        aoiDisplays.push(display);
    }

    let building = new Building();
    await building.initialise();
}

async function refreshDistance(){
    ENV_VARS.distance = await fetch("/config/distance").then(res => res.json());
    return ENV_VARS.distance;
}

//Building-object downloads all room info, room objects are subscribing to it
//(observer design pattern)
class Building {
    constructor(){
        this.url = "/room";
        this.response = {
            timestamp: 0,
            data: null
        };
        this.roomIndices = [];
        this.rooms = [];

        this.refreshLoop = null;
    }

    async initialise(newInterval){
        let roomData = await this.refresh()

        console.log(roomData)

        for(let i in roomData){
            //create room Objects as subscriptions
            let roomParams = {
                roomIndex: i,
                inferred: roomData[i].inhabitants_inferred,
                lastEvent: -1, //init on -1, so it gets updated
                roomNumber: roomData[i].roomNumber,
                htmlDisplay: $(`#inhab_${roomData[i].roomNumber}`)

            }
            this.rooms.push(new RoomDisplay(roomParams));
        }

        let interval = newInterval || 1000;
        console.log("creating refresh loop")
        this.refreshLoop = setInterval(
            async () => {
                await this.refresh();
                
                this.update();
            },
            interval
        )
    }

    async refresh(){
        let roomData = await fetch(this.url).then(res => res.json())
        this.response.data = roomData;
        this.response.timestamp = Date.now();
        this.update();
        return roomData
    }

    //update all room objects that are subscribing to the feed
    update(){
        for(let room of this.rooms){
            let roomParams = {
                lastEvent: this.response.data[room.roomIndex].lastEvent,
                inferred: this.response.data[room.roomIndex].inhabitants_inferred
            };

            room.notify(roomParams);
        }
    }

}

//each RoomDisplay represents a room. it observes the Building object
//When building detects changes, it notifies the RoomDisplay with notify()
class RoomDisplay {
    constructor(params){
        this.roomIndex = params.roomIndex;
        this.inferred = params.inferred || 0;
        this.lastEvent = params.lastEvent || 0;
        this.roomNumber = params.roomNumber;
        this.htmlDisplay = params.htmlDisplay;
    }

    notify(params){

        if(this.lastEvent < params.lastEvent){
            this.inferred = params.inferred;
            this.lastEvent = params.lastEvent;

            this.htmlDisplay.html(`??nhabitants: ${this.inferred}`)
        }
    }
}

class AoiDisplay {
    constructor(sensorID, canvas){
        this.sensor = sensorID;
        //this.canvas = $(`#c${sensorID}`);
        this.canvas = canvas;
        this.observation = null;

        this.socket = null;
    }

    async initialise(){
        let host = window.location.host
        this.socket = await new WebSocket(`ws://${window.location.host}/sensor/rss/${this.sensor}`);
        
        this.initCanvas();

        this.socket.onmessage = (msg)=>{
            this.observation = JSON.parse(msg.data);
    
            this.updateCanvas(this.observation);
        }

        this.socket.onopen = ()=>{
            console.log("socket open")
            this.socket.send("init")
        }
    }

    updateCanvas(observation){
        //proof that the physical distances are okay.
        let distanceKept = true;
        let BG_col = "#ede177"
        let minDist = Number.MAX_VALUE;

        for(let inhab of this.observation.inhabitants){
            try{
                if(inhab.nearestNeighbor != null){

                    minDist = Math.min(inhab.nearestNeighbor, minDist);

                    if((inhab.nearestNeighbor <= ENV_VARS.distance)){
                        //alert the user, in this case just change BG-col of view
                        distanceKept = false;
                    }
                }
            } catch(e) {
                console.log("minimum distance not yet defined");
            }
        }

        minDist = Math.trunc(minDist * 100)/100

        if(!distanceKept){
            BG_col = "#ff8282";
        }


        this.ctx.rect(0, 0, 1, 1);
        this.ctx.fillStyle = BG_col;
        this.ctx.fill();

        //draw all inhabitants
        let radius = 0.025;
        this.ctx.fillStyle = "red";

        //the witdh and depth of the observed area in m
        let roomscale = 4
        for(let inhab of this.observation.inhabitants){
            this.ctx.beginPath();

            let bodyY = inhab.y
            let bodyX = inhab.x

            bodyY = (bodyY+(roomscale/2))/roomscale
            bodyX = 1-(bodyX)/roomscale

            this.ctx.arc(bodyY, bodyX, radius, 0, 2 * Math.PI, false);
            this.ctx.fill();
        }

        //write timestamp
        this.ctx.font = "14px Sans Serif"
        this.ctx.scale(1/this.canvas[0].width,1/this.canvas[0].height)
        this.ctx.fillStyle = "black";

        let time = new Date(parseInt(this.observation.timestamp)*1000);
        time = time.toLocaleTimeString()

        this.ctx.fillText(time,10,20);

        let minDistString = `min. dist.: ${minDist} m`
        this.ctx.fillText(minDistString, 10 ,this.canvas[0].height-20)


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