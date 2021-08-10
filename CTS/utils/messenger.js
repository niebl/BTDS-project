const Aoi = require('./../models/aoi');

class AoiMessenger {
    constructor(rss, interval, ws){
        //this needs to be awaited in methods
        this.sensor = Aoi.find({sensorID: rss});
        
        this.sensorID = null;
        this.interval = interval;
        this.ws = ws;

        this.updateloop = null;
        this.lastTimestamp = 0;
    }

    async initialise(newInterval){
        let interval = newInterval || this.interval;

        //once this sensor is awaited, it can also be used.
        let sensorList = await this.sensor;
        this.sensor = sensorList[0];
        this.sensorID = this.sensor._id;

        let ws = this.ws
        
        this.updateLoop = setInterval(
            () => {this.sendUpdate(ws)},
            interval
        )
    }

    async sendUpdate(ws){
        let observation = await Aoi.findById(this.sensorID);
        let obs = observation.observation

        //send update only if new data came in.
        if(this.lastTimestamp < obs.timestamp){
            this.lastTimestamp = obs.timestamp
            ws.send(JSON.stringify(obs));
        }

        //ws.send(JSON.stringify(obs, null, 2));
    }

    async stopUpdate(){
        clearInterval(this.interval);
        this.interval = null;
    }
}

module.exports = AoiMessenger;