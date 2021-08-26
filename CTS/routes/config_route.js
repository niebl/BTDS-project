require('dotenv').config()
const express = require('express');
const router = express.Router();

const Room = require('../models/room');
const Passage = require('../models/passage');
const Aoi = require('../models/aoi');

//router to get the configuration page
router.get('/', async (req,res) => {
    const rooms = await Room.find();
    const aois = await Aoi.find();
    const passages = await Passage.find();
    const distance = process.env.MINIMUM_DISTANCE;

    res.render('config', {rooms: rooms, aois: aois, passages: passages, minDistance: distance});
})

router.post('/room', async (req,res)=>{
    //delete room if exists. not exactly REST compliant but it's easier.
    await Room.deleteOne({roomNumber: req.body.roomID});

    //fix for issue #3
    if(typeof req.body.pss == "string"){
        req.body.pss = [req.body.pss]
    }
    req.body.rss = req.body.rss || null;

    //create document for room and save
    try{
        let room = new Room({
            roomNumber: req.body.roomID,
            description: req.body.description,
            aoi: req.body.rss,
        });

        for(passage of req.body.pss){
            room.passages.push(passage);
        }


        //assign room to sensors, so posting to sensor endpoints is more efficient
        console.log(room.aoi)
        if(room.aoi != null){
            await assignToRss(room._id, room.aoi);
        }
        for(passage of room.passages){
            await assignToPSS(room._id, passage);
        }   

        await room.save();
        res.redirect("/config/");

    } catch(e){
        console.log(e);
        res.redirect("/config/");
    }

});

router.post('/passage', async (req,res)=>{
    //delete passage if exists.
    await Passage.deleteOne({sensorID: req.body.sensorID});

    let passage = new Passage({
        sensorID: req.body.sensorID,
    });

    try{
        await passage.save();
        res.redirect("/config/");
    } catch(e) {
        res.send(e);
    }
});

router.post('/aoi', async (req,res)=>{
    //delete aoi if exists.
    await Aoi.deleteOne({sensorID: req.body.sensorID});

    let aoi = new Aoi({
        sensorID: req.body.sensorID,
        description: req.body.description,
        observation: {
            time: Date.now()/1000,
            inhabitants: null,
        }
    });

    try{
        await aoi.save();
        res.redirect("/config/");
    } catch(e) {
        res.send(e);
    }
});

router.post('/distance', async (req,res)=>{
    process.env.MINIMUM_DISTANCE = req.body.distance || 1.5;
    res.redirect("/config/");
});

router.get('/distance', async (req,res)=>{
    res.send(process.env.MINIMUM_DISTANCE);
});

router.post('/reset', async (req,res) => {
    console.log("resetting all database entries");
    
    let rooms = await Room.find();
    let aois = await Aoi.find();

    for(let room of rooms){
        room.inhabitants_inferred = 0;
        room.inhabitants_naive = 0;
        room.inhabitants_observed = 0;

        room.lastEvent = 0;

        room.save();
    }

    for(let aoi of aois){
        aoi.observation = {
            "timestamp" : "0",
            "inhabitants": []  
        }

        aoi.save();
    }

    res.status(200);
    res.send(rooms.concat(aois))
})

/**
 * Function that takes mongodb _id of room and rss and assigns room to rss
 * @param {*} roomID the mongo _id of room
 * @param {*} sensorID the mongo _id of sensor
 * @returns true if successful
 */
async function assignToRss(roomID, sensorID){

    let rss = await Aoi.findById(sensorID);
    console.log(rss)

    if (rss.inRoom == null){
        rss.inRoom = roomID;
        await rss.save();
        return true;
    } else {
        throw(`rss ${sensorID} already taken`);
        return false;
    }
}

/**
 * Function that takes mongodb _id of room and pss and assigns room to rss
 * @param {*} roomID the mongo _id of room
 * @param {*} sensorID the mongo _id of sensor
 * @returns true if successful
 */
async function assignToPSS(roomID, passage){
    
    let pss = await Passage.findById(passage);
    if(pss.toRoom == null){
        pss.toRoom = roomID;
        await pss.save();
        return true;
    }else if(pss.fromRoom == null){
        pss.fromRoom = roomID;
        await pss.save();
        return true;
    }else{
        throw(`pss ${passage} already taken`);
        return false;
    }
} 

module.exports = router;