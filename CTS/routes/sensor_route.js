const express = require('express');
const router = express.Router();


const Room = require('./../models/room');
const Passage = require('./../models/passage');
const Aoi = require('./../models/aoi');

const AoiMessenger = require('./../utils/messenger');

//Endpoint to search for all types of sensors. allows SensorID and _id
router.get('/', async (req,res) => {
    let sensorID = req.query.sensorid || null;
    let _id = req.query.id || null;

    let query = {
    }

    if(sensorID != null){
        query.sensorID = sensorID;
    }
    if(_id != null){
        query._id = _id;
    }

    const rss = await Aoi.find(query);
    const pss = await Passage.find(query);
    
    const output = rss.concat(pss);
    
    res.send(output);
})

//Websocket endpoint for rss sensors
router.ws('/rss/:sensorID', async function(ws, req) {
    let messenger = null;

    //on message, start periodic updates
    ws.on('message', async (msg)=>{
        console.log(msg)

        if(msg == "init"){
            console.log(`initialising socket ${req.params.sensorID}`);

            messenger = new AoiMessenger(
                req.params.sensorID,
                1000,
                ws
            );

            await messenger.initialise();
        }

        if(msg == "close"){
            await messenger.stopUpdate();
        }
    });

    ws.on('close', async ()=>{
        console.log(`closing socket ${req.params.sensorID}`);
        try{
            await messenger.stopUpdate();
            messenger = null;
            console.log(`socket ${req.params.sensorID} closed`);
        } catch(e){
            console.log(e);
        }

    });

    console.log('socket', req.testing);
}); 

//Endpoint for arrival of RSS post data
//TODO: evtl observation validaten
router.post('/rss/:sensorID', async (req,res) => {
    try{
        console.log(req.body)

        let rss = await Aoi.find({sensorID: req.params.sensorID});
        rss = rss[0];

        let params = req.body;


        if((rss.observation.timestamp < params.timestamp) 
        || (rss.observation == undefined)){
            rss.observation = {
                timestamp: params.timestamp,
                inhabitants: JSON.parse(params.inhabitants)
            }
        }

        await rss.save();

        res.status(200);
        res.send(rss);
    } catch(e){
        res.status(e.status || 500)
        res.send(e.msg || e);
    }
})

//Endpoint for arrival of PSS post data
//This part here will do a lot of the inferring work
router.post('/pss/:sensorID', async (req,res) => {
    try{
        let pss = await Passage.find({sensorID: req.params.sensorID});
        pss = pss[0];

        let params = req.body;

        params.toRoom = pss.toRoom
        params.fromRoom = pss.fromRoom

        let out = await processPssEvent(params, pss);

        res.status(200);
        res.send(out);
    } catch(e){
        res.status(e.status || 500)
        res.send(e.msg || e);
    }
})

/**
 * function that infers new room inhabitant numbers upon event
 * in: means someone passes from fromRoom to toRoom
 * out: means the inverse of "in"
 * @param {Object} params Object, containing event params
 */
 async function processPssEvent(params, pss){
    //TODO: specify what the request will have to look like
    if(params.event == "in"){
        let out = await updateRoomInhabitants(
            params, params.toRoom, params.fromRoom, pss, 1
            );
        return out;

    } else if(params.event == "out"){
        let out = await updateRoomInhabitants(
            params, params.fromRoom, params.toRoom, pss, 1
            );
        return out;

    } else{
        throw({status: 400, msg:`unexpected event ${params.event}`})
    }
}

//TODO: wäre evtl. gut das hier zu refaktorisieren
async function updateRoomInhabitants(params, to, from, pss, number){
    //dummy object to assign to rooms that don't exist (outside)
    let toRoom = {
        description: "outside",
        inhabitants_naive: 0, 
        inhabitants_inferred: 0,
        inhabitants_observed: 0,
        lastEvent: 0,
        save: ()=>{
            return true;
        }
    };
    let fromRoom = toRoom;

    //get existing rooms from DB
    if(to != null){
        toRoom = await Room.findById(to);
    }
    if(from != null){
        fromRoom = await Room.findById(from);
    }
    
    //naively add inhabitants to toRoom and subtract from fromRoom
    toRoom.inhabitants_naive += number;
    fromRoom.inhabitants_naive -= number;

    //NUMBER CORRECTIONS:
    //choose largest between inferred, naive and observed to get the inferred number
    toRoom.inhabitants_inferred = Math.max(
        toRoom.inhabitants_naive, 
        toRoom.inhabitants_inferred,
        toRoom.inhabitants_observed
    );

    fromRoom.inhabitants_inferred = Math.min(
        fromRoom.inhabitants_naive, 
        fromRoom.inhabitants_inferred
    );
    fromRoom.inhabitants_inferred = Math.max(
        fromRoom.inhabitants_inferred,
        fromRoom.inhabitants_observed
    )

    //correct inferred numbers if below 0
    if (fromRoom.inhabitants_inferred < 0){
        fromRoom.inhabitants_inferred = 0;
    }
    //use naive number, if incoming event happened before lastEvent.
    //prevents inhabitant cloning when requests happen out of order.
    //also update lastEvent timestamp if it is the newest.
    if(params.timestamp < toRoom.lastEvent){
        toROom.inhabitants_inferred = toRoom.inhabitants_naive;
    } else {
        toRoom.lastEvent = params.timestamp;
    }
    if(params.timestamp < fromRoom.lastEvent){
        fromRoom.inhabitants_inferred = fromRoom.inhabitants_naive;
    } else {
        fromRoom.lastEvent = params.timestamp;
    }

    const returnObject = {toRoom: toRoom, fromRoom: fromRoom}

    await toRoom.save();
    await fromRoom.save();
    return returnObject;
}

module.exports = router;